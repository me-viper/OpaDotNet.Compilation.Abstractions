namespace OpaDotNet.Compilation.Abstractions;

internal interface IBundleEntry
{
    void WriteTo(BundleWriter bundle);
}