using Kawa.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddKawa()
    .AddKawaUseCasesFromAssemblies(typeof(CreateUser).Assembly)
    .AddKawaWeb();

var app = builder.Build();

app.MapKawaPost<CreateUser>("/users");
app.MapKawaOpenApi();

if (app.Environment.IsDevelopment())
{
    app.MapKawaSwagger();
    app.MapKawaReDoc();
}

app.Run();
