using Kawa.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddKawa()
    .AddKawaUseCasesFromAssemblies(typeof(CreateUser).Assembly);

var app = builder.Build();

app.MapKawaPost<CreateUser>("/users");

app.Run();
