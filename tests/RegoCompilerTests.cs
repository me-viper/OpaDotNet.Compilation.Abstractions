using Microsoft.Extensions.Logging;

using Moq;

using Xunit.Abstractions;

namespace OpaDotNet.Compilation.Abstractions.Tests;

public class RegoCompilerTests
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
            && a.CapabilitiesStream?.Length == b.CapabilitiesStream?.Length;
    }

    [Fact]
    public async Task CompilationParameters()
    {
        var cp = new CompilationParameters
        {
            IsBundle = true,
            CapabilitiesStream = Stream.Null,
            Entrypoints = new HashSet<string>(["ep1", "ep2"]),
            CapabilitiesFilePath = "/d/caps.json",
            Revision = "r1",
        };

        var compiler = new Mock<IRegoCompiler>();
        compiler.Setup(
                p => p.Compile(
                    It.IsAny<Stream>(),
                    It.Is<CompilationParameters>(pp => CompareCompilationParameters(cp, pp)),
                    CancellationToken.None
                    )
                )
            .ReturnsAsync(Stream.Null);

        var policy = await compiler.Object.Configure()
            .WithRevision("r1")
            .WithEntrypoints(["ep1", "ep2"])
            .WithCapabilities("/d/caps.json")
            .WithCapabilities(Stream.Null)
            .WithSourceCode("package p1")
            .WithAsBundle()
            .CompileAsync(CancellationToken.None);

        Assert.NotNull(policy);
    }

    [Fact]
    public async Task MultipleSources()
    {
        var compiler = new TestCompiler();

        static string Src(int i) => $"""
            package test.src{i}
            default allow := true
            """;

        var ms = new MemoryStream();
        var bw = new BundleWriter(ms);
        bw.WriteEntry(Src(1), "p1.rego");
        bw.WriteEntry(Src(2), "p2.rego");
        await bw.DisposeAsync();

        ms.Seek(0, SeekOrigin.Begin);

        var policy = await compiler.Configure()
            .WithSourcePath("TestData/p1.rego")
            .WithSourcePath("TestData/p2.rego")
            .WithSourceCode(Src(1))
            .WithSourceCode(Src(2))
            .WithSourceStream(ms)
            .CompileAsync(CancellationToken.None);

        AssertBundle.DumpBundle(policy, _outputHelper);

        AssertBundle.Content(
            policy,
            p => p.Name.Equals("/TestData/p1.rego") && p.DataStream?.Length > 0,
            p => p.Name.Equals("/TestData/p2.rego") && p.DataStream?.Length > 0,
            p => p.Name.Equals("/src0.rego") && p.DataStream?.Length > 0,
            p => p.Name.Equals("/src1.rego") && p.DataStream?.Length > 0,
            p => p.Name.Equals("/p1.rego") && p.DataStream?.Length > 0,
            p => p.Name.Equals("/p2.rego") && p.DataStream?.Length > 0
            );
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