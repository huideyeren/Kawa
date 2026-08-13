namespace Kawa.Grpc;

/// <summary>
/// Identifies a Kawa use case group for Kawa-originated protobuf schema generation.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class KawaProtoServiceAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KawaProtoServiceAttribute"/> class.
    /// </summary>
    /// <param name="name">The stable protobuf service name.</param>
    public KawaProtoServiceAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <summary>
    /// Gets the stable protobuf service name.
    /// </summary>
    public string Name { get; }
}
