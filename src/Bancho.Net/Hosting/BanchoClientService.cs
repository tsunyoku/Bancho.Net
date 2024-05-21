using Bancho.Net.Bancho;
using Microsoft.Extensions.Hosting;

namespace Bancho.Net.Hosting;

public class BanchoClientService(IBanchoClient banchoClient) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await banchoClient.ConnectAsync(stoppingToken);
    }
}