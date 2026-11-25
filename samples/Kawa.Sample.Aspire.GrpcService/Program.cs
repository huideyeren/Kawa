using Kawa.Abstractions;
using Kawa.Aspire;
using Kawa.Grpc;
using Kawa.Sample.Aspire.ServiceDefaults;
using Kawa.Sample.Grpc.Contracts;
using Kawa.Sample.Grpc.Contracts.Generated;

var builder = WebApplication.CreateBuilder(args);
builder.AddSampleDefaults();
builder.Services.AddKawaGrpc();
builder.Services.AddSingleton<IUseCase<SendRequest, SendResponse>, SendUseCase>();
builder.Services.AddSingleton<IServerStreamingUseCase<WatchRequest, WatchResponse>, WatchUseCase>();
builder.Services.AddSingleton<IClientStreamingUseCase<SumRequest, SumResponse>, SumUseCase>();
builder.Services.AddSingleton<IDuplexStreamingUseCase<EchoRequest, EchoResponse>, EchoStreamUseCase>();
var app = builder.Build();
app.MapGrpcService<ChatGrpcAdapter>();
app.MapKawaAspireHealthChecks();
app.Run();
