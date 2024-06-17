using JetBrains.Annotations;

namespace OpaDotNet.Compilation.Abstractions;

/// <summary>
/// Compilation extensions.
/// </summary>
[PublicAPI]
public static class RegoCompilerExtensions
{
    /// <summary>
    /// Compiles OPA bundle from bundle directory.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="bundlePath">Bundle directory or bundle archive path.</param>
    /// <param name="entrypoints">Which documents (entrypoints) will be queried when asking for policy decisions.</param>
    /// <param name="capabilitiesFilePath">
    /// Capabilities file that defines the built-in functions and other language features that policies may depend on.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compiled OPA bundle stream.</returns>
    public static async Task<Stream> CompileBundle(
        this IRegoCompiler compiler,
        string bundlePath,
        IEnumerable<string>? entrypoints = null,
        string? capabilitiesFilePath = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(bundlePath);

        var c = new RegoCompilerWrapper(compiler)
            .WithAsBundle()
            .WithSourcePath(bundlePath);

        if (entrypoints != null)
            c.WithEntrypoints(entrypoints);

        if (capabilitiesFilePath != null)
            c.WithCapabilities(capabilitiesFilePath);

        return await c.CompileAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Compiles OPA bundle from rego policy source file.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="sourceFilePath">Source file path.</param>
    /// <param name="entrypoints">Which documents (entrypoints) will be queried when asking for policy decisions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compiled OPA bundle stream.</returns>
    public static async Task<Stream> CompileFile(
        this IRegoCompiler compiler,
        string sourceFilePath,
        IEnumerable<string>? entrypoints = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceFilePath);

        var c = new RegoCompilerWrapper(compiler).WithSourcePath(sourceFilePath);

        if (entrypoints != null)
            c.WithEntrypoints(entrypoints);

        return await c.CompileAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Compiles OPA bundle from rego bundle stream.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="bundle">Rego bundle stream.</param>
    /// <param name="entrypoints">Which documents (entrypoints) will be queried when asking for policy decisions.</param>
    /// <param name="capabilitiesJson">
    /// Capabilities json that defines the built-in functions and other language features that policies may depend on.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compiled OPA bundle stream.</returns>
    public static async Task<Stream> CompileStream(
        this IRegoCompiler compiler,
        Stream bundle,
        IEnumerable<string>? entrypoints = null,
        Stream? capabilitiesJson = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        var c = new RegoCompilerWrapper(compiler)
            .WithAsBundle()
            .WithSourceStream(bundle);

        if (entrypoints != null)
            c.WithEntrypoints(entrypoints);

        if (capabilitiesJson != null)
            c.WithCapabilities(capabilitiesJson);

        return await c.CompileAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Compiles OPA bundle from rego policy source code.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="source">Source file path.</param>
    /// <param name="entrypoints">Which documents (entrypoints) will be queried when asking for policy decisions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compiled OPA bundle stream.</returns>
    public static async Task<Stream> CompileSource(
        this IRegoCompiler compiler,
        string source,
        IEnumerable<string>? entrypoints = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(source);

        using var bundle = new MemoryStream();
        var bw = new BundleWriter(bundle);

        await using (bw.ConfigureAwait(false))
        {
            bw.WriteEntry(source, "policy.rego");
        }

        bundle.Seek(0, SeekOrigin.Begin);

        var c = new RegoCompilerWrapper(compiler)
            .WithAsBundle()
            .WithSourceStream(bundle);

        if (entrypoints != null)
            c.WithEntrypoints(entrypoints);

        return await c.CompileAsync(cancellationToken).ConfigureAwait(false);
    }
}