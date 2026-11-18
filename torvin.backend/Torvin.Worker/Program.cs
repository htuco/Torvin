using Torvin.Services.Configuration;

DotEnvLoader.LoadIfDevelopment();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDatabaseOptions();
builder.Services.AddRabbitMqOptions();
builder.Services.AddSmtpOptions();

using var host = builder.Build();

// StartAsync pokreće i provjeru Options, pa se start loguje tek kad je konfiguracija ispravna
await host.StartAsync();
host.Services.GetRequiredService<ILogger<Program>>().LogInformation("Torvin.Worker je pokrenut.");
await host.WaitForShutdownAsync();
