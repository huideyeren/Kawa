using Kawa.Grpc;
using Xunit;

namespace Kawa.Grpc.Tests;

/// <summary>
/// Verifies the stable schema annotations used by Kawa-originated protobuf contracts.
/// </summary>
public sealed class KawaProtoAttributeTests
{
    /// <summary>
    /// Ensures an explicitly assigned protobuf field number is retained by reflection.
    /// </summary>
    [Fact]
    public void ProtoFieldAttribute_PreservesStableFieldNumber()
    {
        var property = typeof(ChatRequest).GetProperty(nameof(ChatRequest.Message));
        var attribute = property!.GetCustomAttributes(typeof(KawaProtoFieldAttribute), inherit: false)
            .Cast<KawaProtoFieldAttribute>()
            .Single();

        Assert.Equal(1, attribute.Number);
    }

    private sealed class ChatRequest
    {
        [KawaProtoField(1)]
        public string Message { get; init; } = string.Empty;
    }
}
