using Kawa.Abstractions;
using Kawa.Grpc;
using Kawa.Grpc.ProtoAdapter;
using Kawa.Sample.Grpc.Contracts;
using Kawa.Sample.Grpc.Contracts.Generated;
using Kawa.Sample.Grpc.Server;
using Kawa.Sample.Grpc.Server.Legacy;
using Kawa.Web;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);
if (string.IsNullOrWhiteSpace(builder.Configuration["urls"]))
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenLocalhost(5021, endpoint => endpoint.Protocols = HttpProtocols.Http2);
        options.ListenLocalhost(5020, endpoint => endpoint.Protocols = HttpProtocols.Http1);
    });
}
else builder.WebHost.ConfigureKestrel(options => options.ConfigureEndpointDefaults(endpoint => endpoint.Protocols = HttpProtocols.Http2));
builder.Services.AddKawa().AddKawaWeb();
builder.Services.AddKawaGrpc();
builder.Services.AddSingleton<SendUseCase>();
builder.Services.AddSingleton<IUseCase<SendRequest, SendResponse>>(services => services.GetRequiredService<SendUseCase>());
builder.Services.AddSingleton<IServerStreamingUseCase<WatchRequest, WatchResponse>, WatchUseCase>();
builder.Services.AddSingleton<IClientStreamingUseCase<SumRequest, SumResponse>, SumUseCase>();
builder.Services.AddSingleton<IDuplexStreamingUseCase<EchoRequest, EchoResponse>, EchoStreamUseCase>();
builder.Services.AddGrpcProtoAdapter<LegacyRequestAdapter, LegacyRequest, SendRequest>();
builder.Services.AddGrpcProtoAdapter<LegacyResponseAdapter, LegacyResponse, SendResponse>();
var app = builder.Build();
app.MapGrpcService<ChatGrpcAdapter>();
app.MapGrpcService<LegacyChatService>();
app.MapKawaPost<SendUseCase>("/messages");
app.MapKawaApiCatalog();
app.MapKawaOpenApi();
app.Run();
