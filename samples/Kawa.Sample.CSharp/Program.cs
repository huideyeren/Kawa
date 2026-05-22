using Kawa.Abstractions;
using Kawa.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKawa();
builder.Services.AddSingleton<IUseCase<CreateUserRequest, CreateUserResponse>, CreateUserUseCase>();

var app = builder.Build();

app.MapKawaPost<CreateUserRequest, CreateUserResponse>("/users");

app.Run();
