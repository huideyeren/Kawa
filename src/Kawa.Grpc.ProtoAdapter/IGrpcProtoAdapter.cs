namespace Kawa.Grpc.ProtoAdapter;

/// <summary>
/// Explicitly converts between an application-owned protobuf type and a Kawa contract.
/// </summary>
/// <typeparam name="TProto">The generated or hand-authored protobuf type.</typeparam>
/// <typeparam name="TContract">The transport-independent Kawa contract type.</typeparam>
/// <remarks>
/// Kawa intentionally does not infer member mappings. Applications own compatibility decisions
/// for existing protobuf schemas by implementing both conversion directions explicitly.
/// </remarks>
public interface IGrpcProtoAdapter<TProto, TContract>
{
    /// <summary>
    /// Converts an incoming protobuf value to its Kawa contract value.
    /// </summary>
    /// <param name="message">The protobuf value received by the transport.</param>
    /// <returns>The corresponding Kawa contract value.</returns>
    TContract ToContract(TProto message);

    /// <summary>
    /// Converts an outgoing Kawa contract value to its protobuf value.
    /// </summary>
    /// <param name="contract">The Kawa contract value produced by the use case.</param>
    /// <returns>The corresponding protobuf value.</returns>
    TProto ToProto(TContract contract);
}
