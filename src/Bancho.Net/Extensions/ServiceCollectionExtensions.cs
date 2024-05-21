using System.Reflection;
using Bancho.Net.Bancho;
using Bancho.Net.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Bancho.Net.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddBanchoClient(this IServiceCollection collection,
        Action<BanchoClientConfiguration, IServiceProvider> config)
    {
        collection.AddOptions<BanchoClientConfiguration>()
            .Configure(config)
            .Validate(x => !string.IsNullOrWhiteSpace(x.Username))
            .Validate(x => !string.IsNullOrWhiteSpace(x.IrcPassword))
            .ValidateOnStart();

        collection.AddSingleton<IBanchoClient, BanchoClient>();
        collection.AddHostedService<BanchoClientService>();
    }

    public static void AddCommandModules(this IServiceCollection collection, Assembly assembly)
    {
        var modules = assembly.GetTypes()
            .Where(t => t != typeof(CommandModule) && typeof(CommandModule).IsAssignableFrom(t));

        foreach (var module in modules)
        {
            collection.AddSingleton(module);
        }
    }
}