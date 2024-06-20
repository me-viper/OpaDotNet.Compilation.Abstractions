using JetBrains.Annotations;

namespace OpaDotNet.Compilation.Abstractions;

/// <summary>
/// Exposes an OPA policy compiler.
/// </summary>
[PublicAPI]
public sealed class RegoCompilerConfigurator(IRegoCompiler compiler)
{
    private CompilationParameters _options = new();

    private BundleManifest? _manifest;

    private readonly List<IBundleEntry> _entries = [];

    private int _sourceIndex;

    /// <summary>
    /// Output bundle revision.
    /// </summary>
    public RegoCompilerConfigurator WithRevision(string revision)
    {
        _options = _options with { Revision = revision };
        return this;
    }

    /// <summary>
    /// Specifies bundle manifest.
    /// </summary>
    /// <param name="manifest">Bundle manifest.</param>
    public RegoCompilerConfigurator WithManifest(BundleManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        _manifest = manifest;
        return this;
    }

    /// <summary>
    /// Specifies if compilation source if file or bundle.
    /// </summary>
    public RegoCompilerConfigurator WithAsBundle(bool isBundle = true)
    {
        _options = _options with { IsBundle = isBundle };
        return this;
    }

    /// <summary>
    /// Which documents (entrypoints) will be queried when asking for policy decisions.
    /// </summary>
    public RegoCompilerConfigurator WithEntrypoints(IEnumerable<string> entrypoints)
    {
        ArgumentNullException.ThrowIfNull(entrypoints);

        _options = _options with { Entrypoints = entrypoints.ToHashSet() };
        return this;
    }

    /// <summary>
    /// Which documents (entrypoints) will be queried when asking for policy decisions.
    /// </summary>
    public RegoCompilerConfigurator WithEntrypoints(string[] entrypoints)
    {
        ArgumentNullException.ThrowIfNull(entrypoints);

        _options = _options with { Entrypoints = entrypoints.ToHashSet() };
        return this;
    }

    /// <summary>
    /// Capabilities file that defines the built-in functions and other language features that policies may depend on.
    /// </summary>
    public RegoCompilerConfigurator WithCapabilities(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        _options = _options with { CapabilitiesFilePath = path };
        return this;
    }

    /// <summary>
    /// Capabilities json that defines the built-in functions and other language features that policies may depend on.
    /// </summary>
    public RegoCompilerConfigurator WithCapabilities(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        _options = _options with { CapabilitiesStream = stream };
        return this;
    }

    /// <summary>
    /// Sets compilation source from path.
    /// </summary>
    public RegoCompilerConfigurator WithSourcePath(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        _entries.Add(new FileBundleEntry(path));
        return this;
    }

    /// <summary>
    /// Sets compilation source from stream.
    /// </summary>
    public RegoCompilerConfigurator WithSourceStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        _entries.Add(new StreamBundleSource(stream));
        return this;
    }

    /// <summary>
    /// Sets compilation source from source code.
    /// </summary>
    public RegoCompilerConfigurator WithSourceCode(string source)
    {
        ArgumentException.ThrowIfNullOrEmpty(source);

        _entries.Add(new SourceBundleEntry(source, $"src{_sourceIndex++}.rego"));
        return this;
    }

    /// <summary>
    /// Compiles OPA bundle.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compiled OPA bundle stream.</returns>
    /// <exception cref="RegoCompilationException">No compilation source specified.</exception>
    public async Task<Stream> CompileAsync(CancellationToken cancellationToken = default)
    {
        if (_entries.Count == 0)
            throw new RegoCompilationException("No compilation sources specified");

        if (_manifest == null && _entries.Count == 1)
        {
            if (_entries[0] is FileBundleEntry fbe)
                return await compiler.Compile(fbe.Path, _options, cancellationToken).ConfigureAwait(false);

            if (_entries[0] is StreamBundleSource sbs)
                return await compiler.Compile(sbs.Stream, _options, cancellationToken).ConfigureAwait(false);
        }

        using var bs = new MemoryStream();
        var bundle = new BundleWriter(bs, _manifest);

        await using (bundle.ConfigureAwait(false))
        {
            foreach (var entry in _entries)
                entry.WriteTo(bundle);
        }

        bs.Seek(0, SeekOrigin.Begin);
        WithAsBundle();

        return await compiler.Compile(bs, _options, cancellationToken).ConfigureAwait(false);
    }
}