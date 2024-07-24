using JetBrains.Annotations;

namespace OpaDotNet.Compilation.Abstractions;

/// <summary>
/// Exposes an OPA policy compiler.
/// </summary>
[PublicAPI]
public interface IRegoConfigurableCompiler
{
    /// <summary>
    /// Output bundle revision.
    /// </summary>
    IRegoConfigurableCompiler WithRevision(string revision);

    /// <summary>
    /// Specifies bundle manifest.
    /// </summary>
    /// <param name="manifest">Bundle manifest.</param>
    IRegoConfigurableCompiler WithManifest(BundleManifest manifest);

    /// <summary>
    /// Specifies if compilation source if file or bundle.
    /// </summary>
    IRegoConfigurableCompiler WithAsBundle(bool isBundle = true);

    /// <summary>
    /// Which documents (entrypoints) will be queried when asking for policy decisions.
    /// </summary>
    IRegoConfigurableCompiler WithEntrypoints(IEnumerable<string> entrypoints);

    /// <summary>
    /// Which documents (entrypoints) will be queried when asking for policy decisions.
    /// </summary>
    IRegoConfigurableCompiler WithEntrypoints(string[] entrypoints);

    /// <summary>
    /// Capabilities file that defines the built-in functions and other language features that policies may depend on.
    /// </summary>
    IRegoConfigurableCompiler WithCapabilities(string path);

    /// <summary>
    /// Capabilities json that defines the built-in functions and other language features that policies may depend on.
    /// </summary>
    IRegoConfigurableCompiler WithCapabilities(Stream stream);

    /// <summary>
    /// Sets compilation source from path.
    /// </summary>
    IRegoConfigurableCompiler WithSourcePath(string path);

    /// <summary>
    /// Sets compilation source from stream.
    /// </summary>
    IRegoConfigurableCompiler WithSourceStream(Stream stream);

    /// <summary>
    /// Sets compilation source from source code.
    /// </summary>
    IRegoConfigurableCompiler WithSourceCode(string source);

    /// <summary>
    /// Compiles OPA bundle.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compiled OPA bundle stream.</returns>
    /// <exception cref="RegoCompilationException">No compilation source specified.</exception>
    Task<Stream> CompileAsync(CancellationToken cancellationToken = default);
}