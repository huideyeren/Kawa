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
}
