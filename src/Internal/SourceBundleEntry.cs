namespace OpaDotNet.Compilation.Abstractions;

internal class SourceBundleEntry(string source, string fileName) : IBundleEntry
{
    public string Source => source;

    public void WriteTo(BundleWriter bundle) => bundle.WriteEntry(source, fileName);
}