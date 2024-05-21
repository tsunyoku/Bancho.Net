using Bancho.Net.Bancho;
using Microsoft.Extensions.Hosting;

namespace SimpleExample.Host;

public class BanchoClientHandler(IBanchoClient banchoClient) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        banchoClient.OnConnected += () => Console.WriteLine("Connected to IRC");
        banchoClient.OnAuthenticated += () => Console.WriteLine("Authenticated");
        banchoClient.OnChannelJoined += channel => Console.WriteLine($"Joined {channel.Name}");
        banchoClient.OnPrivateMessageReceived += message =>
            Console.WriteLine($"Received {message.Content} from {message.Sender} in {message.Recipient}");

        return Task.CompletedTask;
    }
}