namespace OpaDotNet.Compilation.Abstractions;

internal class FileBundleEntry(string path) : IBundleEntry
{
    public string Path => path;

    public void WriteTo(BundleWriter bundle) => bundle.WriteFile(path);
}