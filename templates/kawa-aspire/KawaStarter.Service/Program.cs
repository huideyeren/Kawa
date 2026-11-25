using Kawa.Aspire;
using KawaStarter.ServiceDefaults;
using Kawa.Abstractions;
using Kawa.MagicOnion;
using KawaStarter.ChatContracts;
using Kawa.Web;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);
if (string.IsNullOrWhiteSpace(builder.Configuration["urls"]))
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenLocalhost(5011, endpoint => endpoint.Protocols = HttpProtocols.Http2);
        options.ListenLocalhost(5010, endpoint => endpoint.Protocols = HttpProtocols.Http1);
    });
}
else builder.WebHost.ConfigureKestrel(options => options.ConfigureEndpointDefaults(endpoint => endpoint.Protocols = HttpProtocols.Http2));

builder.AddSampleDefaults();
builder.Services.AddKawa().AddKawaWeb();
builder.Services.AddKawaMagicOnion();
builder.Services.AddSingleton<IUseCase<string, string>, SendUseCase>();
builder.Services.AddSingleton<IServerStreamingUseCase<string, string>, WatchUseCase>();
builder.Services.AddSingleton<IClientStreamingUseCase<string, int>, UploadUseCase>();
builder.Services.AddSingleton<IDuplexStreamingUseCase<string, string>, ExchangeUseCase>();
builder.Services.AddSingleton<IStreamingHubUseCase<string, string>, ChatHubUseCase>();
var app = builder.Build();
app.MapKawaAspireHealthChecks();
app.MapKawaMagicOnionServices();
app.MapKawaPost<SendUseCase>("/messages");
app.MapKawaApiCatalog();
app.MapKawaOpenApi();
app.Run();
