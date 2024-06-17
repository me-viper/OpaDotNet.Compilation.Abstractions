using System.Globalization;

using Microsoft.Extensions.Logging;

namespace OpaDotNet.Compilation.Abstractions.Tests;

using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

using Xunit.Abstractions;

internal static class AssertBundle
{
    public static void IsValid(Stream bundle, bool hasData = false)
    {
        var policy = TarGzHelper.ReadBundle(bundle);

        Assert.True(policy.Policy.Length > 0);

        if (hasData)
            Assert.True(policy.Data.Length > 0);
    }

    public static bool HasEntry(TarEntry entry, string fileName)
    {
        Assert.NotNull(entry);

        if (!string.Equals(entry.Name, fileName, StringComparison.Ordinal))
            return false;

        Assert.Equal(TarEntryType.RegularFile, entry.EntryType);
        Assert.NotNull(entry.DataStream);
        Assert.True(entry.DataStream.Length > 0);

        return true;
    }

    public static bool HasNonEmptyData(TarEntry entry)
    {
        Assert.NotNull(entry);

        if (!string.Equals(entry.Name, "/data.json", StringComparison.Ordinal))
            return false;

        Assert.Equal(TarEntryType.RegularFile, entry.EntryType);
        Assert.NotNull(entry.DataStream);
        Assert.True(entry.DataStream.Length > 0);

        var buf = new byte[entry.DataStream.Length];
        _ = entry.DataStream.Read(buf);

        if (Encoding.UTF8.GetString(buf).StartsWith("{}"))
            Assert.Fail("Expected non empty data.json");

        return true;
    }

    public static bool AssertData(TarEntry entry, Predicate<JsonDocument> inspector)
    {
        Assert.NotNull(entry);

        if (!string.Equals(entry.Name, "/data.json", StringComparison.Ordinal))
            return false;

        Assert.Equal(TarEntryType.RegularFile, entry.EntryType);
        Assert.NotNull(entry.DataStream);
        Assert.True(entry.DataStream.Length > 0);

        var buf = new byte[entry.DataStream.Length];
        _ = entry.DataStream.Read(buf);

        if (Encoding.UTF8.GetString(buf).StartsWith("{}"))
            Assert.Fail("Expected non empty data.json");

        var json = JsonDocument.Parse(buf);

        return inspector(json);
    }

    public static void DumpBundle(Stream bundle, ITestOutputHelper output)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        using var gzip = new GZipStream(bundle, CompressionMode.Decompress, true);
        using var ms = new MemoryStream();

        gzip.CopyTo(ms);
        ms.Seek(0, SeekOrigin.Begin);

        using var tr = new TarReader(ms);

        while (tr.GetNextEntry() is { } entry)
            output.WriteLine($"{entry.Name} [{entry.EntryType}]");

        bundle.Seek(0, SeekOrigin.Begin);
    }

    public static void Content(Stream bundle, params Predicate<TarEntry>[] inspectors)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        using var gzip = new GZipStream(bundle, CompressionMode.Decompress);
        using var ms = new MemoryStream();

        gzip.CopyTo(ms);
        ms.Seek(0, SeekOrigin.Begin);

        using var tr = new TarReader(ms);
        var entries = new List<TarEntry>();

        while (tr.GetNextEntry() is { } entry)
            entries.Add(entry);

        var i = 0;

        foreach (var inspector in inspectors)
        {
            var hasMatch = entries.Any(p => inspector(p));

            if (!hasMatch)
            {
                var content = string.Join(Environment.NewLine, entries.Select(p => p.Name));
                Assert.Fail($"Inspector at index {i} didn't match any entry in the bundle.\n{content}");
            }

            i++;
        }
    }
}

internal record OpaPolicy(ReadOnlyMemory<byte> Policy, ReadOnlyMemory<byte> Data);

internal static class TarGzHelper
{
    public static OpaPolicy ReadBundle(Stream archive)
    {
        ArgumentNullException.ThrowIfNull(archive);

        using var gzip = new GZipStream(archive, CompressionMode.Decompress);
        using var ms = new MemoryStream();

        gzip.CopyTo(ms);
        ms.Seek(0, SeekOrigin.Begin);

        using var tr = new TarReader(ms);

        static Memory<byte> ReadEntry(TarEntry entry)
        {
            if (entry.DataStream == null)
                throw new InvalidOperationException($"Failed to read {entry.Name}");

            var result = new byte[entry.DataStream.Length];
            var bytesRead = entry.DataStream.Read(result);

            if (bytesRead < entry.DataStream.Length)
                throw new Exception($"Failed to read tar entry {entry.Name}");

            return result;
        }

        Memory<byte>? policy = null;
        Memory<byte>? data = null;

        while (tr.GetNextEntry() is { } entry)
        {
            if (string.Equals(entry.Name, "/policy.wasm", StringComparison.OrdinalIgnoreCase))
                policy = ReadEntry(entry);

            if (string.Equals(entry.Name, "/data.json", StringComparison.OrdinalIgnoreCase))
                data = ReadEntry(entry);
        }

        if (policy == null)
            throw new Exception("Bundle does not contain policy.wasm file");

        return new(policy.Value, data ?? Memory<byte>.Empty);
    }
}

public class XunitLoggerProvider : ILoggerProvider
{
    // Used to distinguish when multiple apps are running as part of the same test.
    private static int _instanceCount = 0;

    private readonly int _providerInstanceId = Interlocked.Increment(ref _instanceCount);
    private readonly ITestOutputHelper _output;
    private readonly LogLevel _minLevel;
    private readonly DateTimeOffset? _logStart;

    public XunitLoggerProvider(ITestOutputHelper output, LogLevel minLevel = LogLevel.Trace, DateTimeOffset? logStart = null)
    {
        _output = output;
        _minLevel = minLevel;
        _logStart = logStart;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new XunitLogger(_output, categoryName, _minLevel, _logStart, _providerInstanceId);
    }

    public void Dispose()
    {
    }
}

public class XunitLogger : ILogger
{
    private static readonly string[] NewLineChars = { Environment.NewLine };
    private readonly string _category;
    private readonly LogLevel _minLogLevel;
    private readonly ITestOutputHelper _output;
    private readonly DateTimeOffset? _logStart;
    private readonly int _providerInstanceId;

    public XunitLogger(ITestOutputHelper output, string category, LogLevel minLogLevel, DateTimeOffset? logStart, int providerInstanceId)
    {
        _minLogLevel = minLogLevel;
        _category = category;
        _output = output;
        _logStart = logStart;
        _providerInstanceId = providerInstanceId;
    }

    public void Log<TState>(
        LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        // Buffer the message into a single string in order to avoid shearing the message when running across multiple threads.
        var messageBuilder = new StringBuilder();

        var timestamp = _logStart.HasValue ? $"{(DateTimeOffset.UtcNow - _logStart.Value).TotalSeconds.ToString("N3", CultureInfo.InvariantCulture)}s" : DateTimeOffset.UtcNow.ToString("s", CultureInfo.InvariantCulture);

        var firstLinePrefix = $"| [{timestamp}] I:{_providerInstanceId} {_category} {logLevel}: ";
        var lines = formatter(state, exception).Split(NewLineChars, StringSplitOptions.RemoveEmptyEntries);
        messageBuilder.AppendLine(firstLinePrefix + lines.FirstOrDefault() ?? string.Empty);

        var additionalLinePrefix = "|" + new string(' ', firstLinePrefix.Length - 1);

        foreach (var line in lines.Skip(1))
        {
            messageBuilder.AppendLine(additionalLinePrefix + line);
        }

        if (exception != null)
        {
            lines = exception.ToString().Split(NewLineChars, StringSplitOptions.RemoveEmptyEntries);
            additionalLinePrefix = "| ";

            foreach (var line in lines)
            {
                messageBuilder.AppendLine(additionalLinePrefix + line);
            }
        }

        // Remove the last line-break, because ITestOutputHelper only has WriteLine.
        var message = messageBuilder.ToString();

        if (message.EndsWith(Environment.NewLine, StringComparison.Ordinal))
        {
            message = message.Substring(0, message.Length - Environment.NewLine.Length);
        }

        try
        {
            _output.WriteLine(message);
        }
        catch (Exception)
        {
            // We could fail because we're on a background thread and our captured ITestOutputHelper is
            // busted (if the test "completed" before the background thread fired).
            // So, ignore this. There isn't really anything we can do but hope the
            // caller has additional loggers registered
        }
    }

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLogLevel;

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
        => new NullScope();

    private sealed class NullScope : IDisposable
    {
        public void Dispose()
        {
        }
    }
}