using Torvin.Services.Configuration;
using Torvin.Worker.Consumers;
using Torvin.Worker.Email;

DotEnvLoader.LoadIfDevelopment();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDatabaseOptions();
builder.Services.AddRabbitMqOptions();
builder.Services.AddSmtpOptions();

builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton<EmailMessageHandler>();
builder.Services.AddHostedService<EmailConsumer>();

using var host = builder.Build();

// StartAsync pokreće i provjeru Options, pa se start loguje tek kad je konfiguracija ispravna
await host.StartAsync();
host.Services.GetRequiredService<ILogger<Program>>().LogInformation("Torvin.Worker je pokrenut.");
await host.WaitForShutdownAsync();
