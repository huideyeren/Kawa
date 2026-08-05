using System;

namespace Kawa.MagicOnion;

/// <summary>
/// Overrides the operation name generated for a Kawa MagicOnion use case.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class KawaMagicOnionOperationAttribute : Attribute
{
    /// <summary>
    /// Initializes the attribute with a stable operation name.
    /// </summary>
    /// <param name="name">The operation name exposed by the generated service.</param>
    public KawaMagicOnionOperationAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <summary>
    /// Gets the stable generated operation name.
    /// </summary>
    public string Name { get; }
}
