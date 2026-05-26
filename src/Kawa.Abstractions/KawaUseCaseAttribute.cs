namespace Kawa.Abstractions;

/// <summary>
/// Describes catalog metadata for a Kawa use case.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class KawaUseCaseAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KawaUseCaseAttribute" /> class.
    /// </summary>
    /// <param name="name">The stable use case name.</param>
    public KawaUseCaseAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <summary>
    /// Gets the stable use case name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the short use case summary.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Gets or sets the longer use case description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the use case contract version.
    /// </summary>
    public string Version { get; set; } = "v1";

    /// <summary>
    /// Gets or sets the catalog tags associated with the use case.
    /// </summary>
    public string[] Tags { get; set; } = [];
}
