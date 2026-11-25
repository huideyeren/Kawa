using System;

namespace Kawa.MagicOnion;

/// <summary>
/// Marks a Kawa streaming hub use case for MagicOnion hub generation.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class KawaMagicOnionHubAttribute : Attribute
{
    /// <summary>
    /// Initializes the attribute with the stable StreamingHub name.
    /// </summary>
    /// <param name="name">The StreamingHub name shared by generated hub types.</param>
    public KawaMagicOnionHubAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <summary>
    /// Gets the stable generated StreamingHub name.
    /// </summary>
    public string Name { get; }
}
