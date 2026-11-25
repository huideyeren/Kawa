using Kawa.Grpc.BuildFixture;
using Xunit;

namespace Kawa.Grpc.Tests;

/// <summary>
/// Verifies that the Kawa schema task runs early enough for Grpc.Tools to compile its output.
/// </summary>
public sealed class KawaProtoBuildIntegrationTests
{
    /// <summary>
    /// Ensures the standard gRPC server base generated from the Kawa-originated schema is usable.
    /// </summary>
    [Fact]
    public void BuildFixture_CompilesGeneratedGrpcServiceBase()
    {
        Assert.True(typeof(Kawa.Grpc.BuildFixture.Proto.Chat.ChatBase).IsAbstract);
    }

    /// <summary>
    /// Ensures Kawa emits one concrete server adapter that owns every annotated operation in its service.
    /// </summary>
    [Fact]
    public void BuildFixture_CompilesGeneratedServiceAdapter()
    {
        var adapter = typeof(Kawa.Grpc.BuildFixture.Generated.ChatGrpcAdapter);

        Assert.True(adapter.IsSealed);
        Assert.NotNull(adapter.GetMethod("Send"));
        Assert.NotNull(adapter.GetMethod("Watch"));
        Assert.NotNull(adapter.GetMethod("Sum"));
        Assert.NotNull(adapter.GetMethod("EchoStream"));
    }
}
