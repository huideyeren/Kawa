using Aspire.Hosting;
using Kawa.Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);
string Project(string name) => Path.GetFullPath($"../{name}/{name}.csproj", builder.AppHostDirectory);
var chat = builder.AddKawaMagicOnionProject("chat", Project("Kawa.Sample.Aspire.Service"));
var proto = builder.AddProject("proto", Project("Kawa.Sample.Aspire.GrpcService"), options => options.ExcludeLaunchProfile = true)
    .WithHttpEndpoint(name: "grpc")
    .WithEndpoint("grpc", endpoint => endpoint.Transport = "http2")
    .WithEnvironment("ASPNETCORE_Kestrel__EndpointDefaults__Protocols", "Http2");
builder.AddKawaCliProject("magic-cli", Project("Kawa.Sample.Aspire.Client"), ["--transport", "magic", "--message", "hello"])
    .WithKawaMagicOnionReference(chat);
builder.AddKawaCliProject("grpc-cli", Project("Kawa.Sample.Aspire.Client"), ["--transport", "grpc", "--message", "hello"])
    .WithReference(proto.GetEndpoint("grpc")).WaitFor(proto);
builder.Build().Run();
