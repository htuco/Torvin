using RabbitMQ.Client;
using Torvin.Services.Configuration;

namespace Torvin.Services.Messaging;

public static class RabbitMqConnectionFactory
{
    // Klijent sam obnavlja konekciju i kanale kad veza pukne poslije prvog spajanja
    public static ConnectionFactory Create(RabbitMqOptions options, string clientName) => new()
    {
        HostName = options.Host,
        Port = options.Port,
        UserName = options.User,
        Password = options.Password,
        AutomaticRecoveryEnabled = true,
        ClientProvidedName = clientName,
    };
}
