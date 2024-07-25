using Microsoft.Extensions.Logging;

using Moq;

using Xunit.Abstractions;

namespace OpaDotNet.Compilation.Abstractions.Tests;

internal class RegoCompilerTests
{
    private readonly ILoggerFactory _loggerFactory;

    private readonly ITestOutputHelper _outputHelper;

    public RegoCompilerTests(ITestOutputHelper output)
    {
        _outputHelper = output;
        _loggerFactory = new LoggerFactory(new[] { new XunitLoggerProvider(output) });
    }

    private bool CompareCompilationParameters(CompilationParameters a, CompilationParameters b)
    {
        return a.IsBundle == b.IsBundle
            && string.Equals(a.Revision, b.Revision, StringComparison.Ordinal)
            && (a.Entrypoints?.SetEquals(b.Entrypoints ?? new HashSet<string>()) ?? true)
            && string.Equals(a.CapabilitiesFilePath, b.CapabilitiesFilePath, StringComparison.Ordinal)
            && a.CapabilitiesBytes.Span.SequenceEqual(b.CapabilitiesBytes.Span);
    }
}

internal class TestCompiler : IRegoCompiler
{
    public Task<RegoCompilerVersion> Version(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<RegoCompilerVersion>(new() { Version = "1" });
    }

    public Task<Stream> Compile(string path, CompilationParameters parameters, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<Stream> Compile(Stream stream, CompilationParameters parameters, CancellationToken cancellationToken)
    {
        var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }
}