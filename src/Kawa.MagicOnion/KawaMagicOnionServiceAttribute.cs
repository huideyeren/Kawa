using System;

namespace Kawa.MagicOnion;

/// <summary>
/// Marks a Kawa use case for generation as a MagicOnion service operation.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class KawaMagicOnionServiceAttribute : Attribute
{
    /// <summary>
    /// Initializes the attribute with the stable MagicOnion service name.
    /// </summary>
    /// <param name="name">The service name shared by generated operations.</param>
    public KawaMagicOnionServiceAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <summary>
    /// Gets the stable MagicOnion service name.
    /// </summary>
    public string Name { get; }
}
