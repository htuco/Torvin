using Torvin.Services.Configuration;
using Torvin.Services.Database;
using Torvin.Services.Notifications;
using Torvin.Worker.Consumers;
using Torvin.Worker.Email;

DotEnvLoader.LoadIfDevelopment();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDatabaseOptions();
builder.Services.AddRabbitMqOptions();
builder.Services.AddSmtpOptions();

builder.Services.AddTorvinDatabase();

builder.Services.AddSingleton<RabbitMqConnectionProvider>();
builder.Services.AddSingleton<RetryPolicy>();

builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton<EmailMessageHandler>();
builder.Services.AddHostedService<EmailConsumer>();

// Handleri koji pišu u bazu su scoped: consumer pravi novi scope za svaku poruku
builder.Services.AddScoped<NotificationWriter>();
builder.Services.AddScoped<TransactionCompletedHandler>();
builder.Services.AddScoped<CardStatusChangedHandler>();
builder.Services.AddHostedService<TransactionCompletedConsumer>();
builder.Services.AddHostedService<CardStatusChangedConsumer>();

using var host = builder.Build();

// StartAsync pokreće i provjeru Options, pa se start loguje tek kad je konfiguracija ispravna
await host.StartAsync();
host.Services.GetRequiredService<ILogger<Program>>().LogInformation("Torvin.Worker je pokrenut.");
await host.WaitForShutdownAsync();
