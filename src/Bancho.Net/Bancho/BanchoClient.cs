using Bancho.Net.Bancho.Messages;
using Bancho.Net.Bancho.Models;
using Bancho.Net.Irc;
using Bancho.Net.Irc.Messages;
using Microsoft.Extensions.Options;

namespace Bancho.Net.Bancho;

public class BanchoClient : IBanchoClient
{
    private readonly string _username;
    private readonly IIrcClient _ircClient;
    
    public event Action? OnConnected;
    public event Action? OnAuthenticated;
    public event Action<string>? OnIrcCommandExecute;
    public event Action<IrcMessage>? OnIrcMessageReceived;

    public event Action<PrivateMessage>? OnPrivateMessageReceived;
    public event Action<Channel>? OnChannelJoined;

    private readonly Dictionary<string, Channel> _channels = [];
    
    public BanchoClient(IOptions<BanchoClientConfiguration> options) : this(options.Value.Username, options.Value.IrcPassword) {}

    public BanchoClient(string username, string password)
    {
        _username = username;

        _ircClient = new IrcClient(
            "irc.ppy.sh",
            6667,
            username,
            username,
            password);

        _ircClient.OnConnected += () => OnConnected?.Invoke();
        _ircClient.OnAuthenticated += () => OnAuthenticated?.Invoke();
        _ircClient.OnExecute += message => OnIrcCommandExecute?.Invoke(message);
        _ircClient.OnMessageReceived += async message =>
        {
            await OnMessageReceived(message);
            OnIrcMessageReceived?.Invoke(message);
        };
    }
    
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _ircClient.ConnectAsync(cancellationToken);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _ircClient.DisconnectAsync(cancellationToken);
    }

    public async Task JoinChannelAsync(string channel, CancellationToken cancellationToken = default)
    {
        if (_channels.ContainsKey(channel))
            return;

        await _ircClient.JoinChannelAsync(channel, cancellationToken);
    }

    public async Task SendPrivateMessageAsync(string target, string content, CancellationToken cancellationToken = default)
    {
        await _ircClient.SendPrivateMessageAsync(target, content, cancellationToken);
        
        var channel = GetChannel(target);
        var privateMessage = new PrivateMessage(_username, content, target);
        channel?.AddMessage(privateMessage);
    }

    private Channel? GetChannel(string name)
    {
        return _channels.GetValueOrDefault(name);
    }

    private Channel CreateChannel(string name)
    {
        if (_channels.TryGetValue(name, out var channel))
        {
            return channel;
        }

        channel = new Channel(name);
        _channels[channel.Name] = channel;
        
        // TODO: is this a bad assumption?
        OnChannelJoined?.Invoke(channel);

        return channel;
    }

    private Channel GetOrCreateChannelFromMessage(PrivateMessage privateMessage)
    {
        var channel = GetChannel(privateMessage.Target);
        if (channel is not null)
            return channel;

        channel = CreateChannel(privateMessage.Target);
        return channel;
    }

    private async Task OnMessageReceived(IrcMessage ircMessage, CancellationToken cancellationToken = default)
    {
        // private message
        if (ircMessage.Command is "PRIVMSG")
        {
            var privateMessage = new PrivateMessage(ircMessage);
            OnPrivateMessageReceived?.Invoke(privateMessage);

            var channel = GetOrCreateChannelFromMessage(privateMessage);
            channel.AddMessage(privateMessage);
        }
        
        // topic (channel joined)
        if (ircMessage.Command is "332")
        {
            var channelName = ircMessage.Parameters[1];
            CreateChannel(channelName);
        }
    }

    public void Dispose()
    {
        DisposeAsync(true).GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async Task DisposeAsync(bool disposing)
    {
        if (!disposing)
            return;

        await DisconnectAsync();
        await _ircClient.DisposeAsync();
    }
}