using JetBrains.Annotations;

namespace OpaDotNet.Compilation.Abstractions;


/// <summary>
/// Language and runtime compatibility.
/// </summary>
[PublicAPI]
public enum OpaCompatibility
{
    /// <summary>
    /// Pre OPA v1.0 release.
    /// </summary>
    Legacy = 0,

    /// <summary>
    /// OPA v1.0 release.
    /// </summary>
    V1,
}