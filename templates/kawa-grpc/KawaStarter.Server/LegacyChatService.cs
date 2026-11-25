using Grpc.Core;
using Kawa.Core;
using Kawa.Grpc;
using Kawa.Grpc.ProtoAdapter;
using KawaStarter.Contracts;
using KawaStarter.Server.Legacy;

namespace KawaStarter.Server;

public sealed class LegacyRequestAdapter : IGrpcProtoAdapter<LegacyRequest, SendRequest>
{
    public SendRequest ToContract(LegacyRequest message) => new() { Message = message.Text };
    public LegacyRequest ToProto(SendRequest contract) => new() { Text = contract.Message };
}

public sealed class LegacyResponseAdapter : IGrpcProtoAdapter<LegacyResponse, SendResponse>
{
    public SendResponse ToContract(LegacyResponse message) => new() { Message = message.Text };
    public LegacyResponse ToProto(SendResponse contract) => new() { Text = contract.Message };
}

public sealed class LegacyChatService(
    SendUseCase useCase, UseCaseExecutor executor,
    IGrpcProtoAdapter<LegacyRequest, SendRequest> requests,
    IGrpcProtoAdapter<LegacyResponse, SendResponse> responses) : LegacyChat.LegacyChatBase
{
    public override async Task<LegacyResponse> Send(LegacyRequest request, ServerCallContext context)
    {
        var result = await executor.ExecuteAsync(useCase, requests.ToContract(request), context.CancellationToken);
        if (result.IsFailure) throw KawaGrpcErrorMapper.ToRpcException(result.Error!);
        return responses.ToProto(result.Value!);
    }
}
