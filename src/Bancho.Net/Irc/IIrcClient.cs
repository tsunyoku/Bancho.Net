using Bancho.Net.Irc.Messages;

namespace Bancho.Net.Irc;

public interface IIrcClient : IDisposable, IAsyncDisposable
{
    public bool Connected { get; }
    
    public bool Authenticated { get; }

    public event Action? OnConnected;
    public event Action? OnAuthenticated;
    public event Action<string>? OnExecute;
    public event Action<IrcMessage>? OnMessageReceived;

    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task JoinChannelAsync(string channel, CancellationToken cancellationToken = default);
    Task SendPrivateMessageAsync(string target, string content, CancellationToken cancellationToken = default);
}