using Bancho.Net.Bancho;
using SimpleExample.Shared;

var client = new BanchoClient(
    Environment.GetEnvironmentVariable("OSU_IRC_USERNAME")!,
    Environment.GetEnvironmentVariable("OSU_IRC_PASSWORD")!);

client.OnConnected += () => Console.WriteLine("Connected to IRC");
client.OnAuthenticated += () => Console.WriteLine("Authenticated");
client.OnChannelJoined += channel => Console.WriteLine($"Joined {channel.Name}");
client.OnPrivateMessageReceived += message =>
    Console.WriteLine($"Received {message.Content} from {message.Sender} in {message.Recipient}");

// use DI in a real scenario
_ = new ExampleCommandModule(client);

await client.ConnectAsync();