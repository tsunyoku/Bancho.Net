using Bancho.Net.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleExample.Host;
using SimpleExample.Shared;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddEnvironmentVariables();

// sets up what is necessary for the bancho client to be created & ran
builder.Services.AddBanchoClient((config, _) =>
{
    config.Username = builder.Configuration["IrcUsername"]!;
    config.IrcPassword = builder.Configuration["IrcPassword"]!;
});

// will add all modules which inherit from CommandModule in the provided assembly
builder.Services.AddCommandModules(typeof(ExampleCommandModule).Assembly);

// handles listening to IRC events on the client
builder.Services.AddHostedService<BanchoClientHandler>();

var host = builder.Build();
await host.RunAsync();