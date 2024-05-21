using Bancho.Net.Bancho.Messages;
using Bancho.Net.Bancho.Models;
using Bancho.Net.Irc.Messages;

namespace Bancho.Net.Bancho;

public interface IBanchoClient : IDisposable, IAsyncDisposable
{
    public event Action? OnConnected;
    public event Action? OnAuthenticated;
    public event Action<string>? OnIrcCommandExecute;
    public event Action<IrcMessage>? OnIrcMessageReceived;
    
    public event Action<PrivateMessage>? OnPrivateMessageReceived;
    public event Action<Channel>? OnChannelJoined;

    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task JoinChannelAsync(string channel, CancellationToken cancellationToken = default);
    Task SendPrivateMessageAsync(string target, string content, CancellationToken cancellationToken = default);
}