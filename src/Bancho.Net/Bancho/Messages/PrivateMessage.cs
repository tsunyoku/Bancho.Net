using Bancho.Net.Irc.Messages;

namespace Bancho.Net.Bancho.Messages;

public class PrivateMessage
{
    public string Sender { get; }
    public string Content { get; }
    public string Recipient { get; }

    public bool IsChannel => Recipient[0] is '#' || Sender[0] is '#';
    public bool IsBanchoBotMessage => Sender is "BanchoBot";
    
    public string Target => IsChannel ? Recipient : Sender;

    public PrivateMessage(string message) : this(new IrcMessage(message)) { }

    public PrivateMessage(IrcMessage ircMessage)
    {
        Sender = ircMessage.Prefix.Split("!cho@ppy.sh").First().TrimStart(':');
        Content = ircMessage.Parameters[1];
        Recipient = ircMessage.Parameters[0];
    }

    public PrivateMessage(string sender, string content, string recipient)
    {
        Sender = sender;
        Content = content;
        Recipient = recipient;
    }
}