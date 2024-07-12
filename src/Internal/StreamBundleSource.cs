namespace OpaDotNet.Compilation.Abstractions;

internal class StreamBundleEntry(Stream stream) : IBundleEntry
{
    public Stream Stream => stream;

    public void WriteTo(BundleWriter bundle) => bundle.WriteBundle(stream);
}