namespace Kawa.Grpc;

/// <summary>
/// Assigns a stable protobuf field number to a Kawa contract member.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class KawaProtoFieldAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KawaProtoFieldAttribute"/> class.
    /// </summary>
    /// <param name="number">The positive, stable field number used in generated schemas.</param>
    public KawaProtoFieldAttribute(int number)
    {
        if (number <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(number), number, "Protobuf field numbers must be positive.");
        }

        Number = number;
    }

    /// <summary>
    /// Gets the stable protobuf field number.
    /// </summary>
    public int Number { get; }
}
