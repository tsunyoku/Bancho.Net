using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using Bancho.Net.Irc.Messages;

namespace Bancho.Net.Irc;

internal class IrcClient : IIrcClient
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _nickname;
    private readonly string _username;
    private readonly string _password;

    private TcpClient? _tcpClient;
    private StreamWriter? _writer;
    private StreamReader? _reader;

    [MemberNotNullWhen(returnValue: true, nameof(_tcpClient), nameof(_writer), nameof(_reader))]
    public bool Connected { get; private set; }
    
    public bool Authenticated { get; private set; }

    public event Action? OnConnected;
    public event Action? OnAuthenticated;
    public event Action<string>? OnExecute;
    public event Action<IrcMessage>? OnMessageReceived;

    public IrcClient(
        string host,
        int port,
        string nickname,
        string username,
        string password)
    {
        _host = host;
        _port = port;
        _nickname = nickname;
        _username = username;
        _password = password;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (Connected)
        {
            return;
        }

        _tcpClient = new TcpClient(_host, _port);
        Connected = true;
        OnConnected?.Invoke();

        var stream = _tcpClient.GetStream();

        _reader = new StreamReader(stream);
        _writer = new StreamWriter(stream)
        {
            NewLine = "\r\n",
            AutoFlush = true,
        };

        await AuthenticateAsync(cancellationToken);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (!Connected)
        {
            return;
        }

        await Execute("QUIT", cancellationToken);

        Connected = false;

        _tcpClient.Dispose();
        _tcpClient = null;

        await _writer.DisposeAsync();
        _writer = null;

        _reader.Dispose();
        _reader = null;
    }

    public async Task JoinChannelAsync(string channel, CancellationToken cancellationToken = default)
    {
        // public channel
        if (channel[0] is '#')
        {
            await Execute($"JOIN {channel}", cancellationToken);
        }
        else
        {
            await Execute($"QUERY {channel}", cancellationToken);
        }
    }

    public async Task SendPrivateMessageAsync(string target, string content, CancellationToken cancellationToken = default)
    {
        await Execute($"PRIVMSG {target} {content}", cancellationToken);
    }

    private async Task AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        if (!Connected)
        {
            return;
        }

        // NOTE: we don't assume we're authenticated here
        //       as we need to receive some confirmation first (MOTD)
        await Execute($"PASS {_password}", cancellationToken);
        await Execute($"NICK {_nickname}", cancellationToken);
        await Execute($"USER {_username}", cancellationToken);

        // at this point, we are ready to consume messages
        await ReadLoopAsync(cancellationToken);
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        while (Connected && !cancellationToken.IsCancellationRequested)
        {
            var rawMessage = await _reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(rawMessage))
            {
                continue;
            }
            
            // TODO: strongly typed commands
            var message = new IrcMessage(rawMessage);

            // auth failed
            if (message.Command is "464")
            {
                await DisconnectAsync(cancellationToken);
                throw new Exception("IRC client failed to authenticate");
            }
            
            // MOTD (we are now authenticated)
            if (message.Command is "001")
            {
                Authenticated = true;
                OnAuthenticated?.Invoke();
            }

            if (message.Command is "PING")
            {
                await Execute("PONG", cancellationToken);
            }

            OnMessageReceived?.Invoke(message);
        }
    }

    // TODO: strongly typed commands
    private async Task Execute(string command, CancellationToken cancellationToken = default)
    {
        if (!Connected)
        {
            return;
        }

        // TODO: ensure provided command can be done without authentication or the user is authenticated
        await _writer.WriteLineAsync(command.AsMemory(), cancellationToken);
        
        // TODO: rate-limiting
        
        OnExecute?.Invoke(command);
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
    }
}