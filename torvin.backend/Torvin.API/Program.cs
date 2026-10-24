using Microsoft.Extensions.Options;
using Torvin.API.Infrastructure;
using Torvin.API.Middleware;
using Torvin.Services.Configuration;

DotEnvLoader.LoadIfDevelopment();

var builder = WebApplication.CreateBuilder(args);

// Lokalno port dolazi iz .env (API_PORT). U Dockeru port zadaje okruženje kontejnera, a API_PORT je tamo host port iz compose-a
if (int.TryParse(builder.Configuration[EnvVars.ApiPort], out var apiPort)
    && builder.Configuration[EnvVars.AspNetCoreUrls] is null
    && builder.Configuration[EnvVars.AspNetCoreHttpPorts] is null)
{
    builder.WebHost.UseUrls($"http://localhost:{apiPort}");
}

builder.Services.AddApiOptions();
builder.Services.AddDatabaseOptions();
builder.Services.AddJwtOptions();
builder.Services.AddRabbitMqOptions();
builder.Services.AddCorsOptions();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
        options.InvalidModelStateResponseFactory = InvalidModelStateResponse.Create);
builder.Services.AddCors();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Prijavljuje sve neispravne varijable odjednom, a ne samo prvu na koju aplikacija naiđe
app.Services.GetRequiredService<IStartupValidator>().Validate();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

var corsOrigins = app.Services.GetRequiredService<IOptions<CorsOptions>>().Value.Origins;
app.UseCors(policy => policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod());

app.MapControllers();

app.Run();

// Javna klasa da je WebApplicationFactory u testovima može naći
public partial class Program;
