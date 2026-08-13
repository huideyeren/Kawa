using Kawa.Grpc.ProtoAdapter;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kawa.Grpc.Tests;

/// <summary>
/// Verifies that existing protobuf contracts use an explicit, application-owned conversion boundary.
/// </summary>
public sealed class GrpcProtoAdapterTests
{
    /// <summary>
    /// Ensures registration retains the explicit request and response conversion chosen by the application.
    /// </summary>
    [Fact]
    public void AddGrpcProtoAdapter_RegistersExplicitConversion()
    {
        var services = new ServiceCollection();
        services.AddGrpcProtoAdapter<LegacyMessageAdapter, LegacyMessage, ChatMessage>();

        using var provider = services.BuildServiceProvider();
        var adapter = provider.GetRequiredService<IGrpcProtoAdapter<LegacyMessage, ChatMessage>>();

        Assert.Equal("hello", adapter.ToContract(new LegacyMessage("hello")).Text);
        Assert.Equal("reply", adapter.ToProto(new ChatMessage("reply")).Body);
    }

    private sealed class LegacyMessageAdapter : IGrpcProtoAdapter<LegacyMessage, ChatMessage>
    {
        public ChatMessage ToContract(LegacyMessage message) => new(message.Body);

        public LegacyMessage ToProto(ChatMessage contract) => new(contract.Text);
    }

    private sealed record LegacyMessage(string Body);

    private sealed record ChatMessage(string Text);
}
