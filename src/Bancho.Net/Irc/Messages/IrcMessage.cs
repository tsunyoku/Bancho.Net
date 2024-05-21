namespace Bancho.Net.Irc.Messages;

// https://datatracker.ietf.org/doc/html/rfc2812
public class IrcMessage
{
    public string Message { get; init; }
    public string Command { get; private set; } = null!;
    public string Prefix { get; private set; } = null!;
    public IReadOnlyDictionary<string, string> Tags { get; private set; } = null!;

    public IReadOnlyList<string> Parameters => _parameters;

    public IrcMessage(string message)
    {
        Message = message;
        Parse();
    }

    private int _position;

    private readonly List<string> _parameters = [];

    public override string ToString() => Message;

    private void Parse()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Message);

        var nextSpace = 0;

        // tags
        if (Message[0] is '@')
        {
            nextSpace = Message.IndexOf(' ');
            
            // no space
            if (nextSpace == -1)
            {
                throw new FormatException("Invalid message tag format");
            }

            Tags = Message.Substring(1, nextSpace - 1)
                .Split(';')
                .Select(tag => tag.Split('='))
                .ToDictionary(tag => tag[0], tag => tag.ElementAtOrDefault(1) ?? "true");

            _position = nextSpace + 1;
        }

        AdjustPositionForSpaces();

        // prefix
        if (Message[_position] is ':')
        {
            nextSpace = Message.IndexOf(' ', _position);
            
            // no space
            if (nextSpace == -1)
            {
                throw new FormatException("Invalid message prefix format");
            }

            Prefix = Message.Substring(_position + 1, nextSpace - _position - 1);

            _position = nextSpace + 1;
            AdjustPositionForSpaces();
        }

        nextSpace = Message.IndexOf(' ', _position);
        if (nextSpace == -1)
        {
            if (Message.Length > _position)
            {
                Command = Message[_position..];
            }

            return;
        }

        Command = Message.Substring(_position, nextSpace - _position);

        _position = nextSpace + 1;
        AdjustPositionForSpaces();

        while (_position < Message.Length)
        {
            nextSpace = Message.IndexOf(' ', _position);
            
            // trailing param
            if (Message[_position] is ':')
            {
                _parameters.Add(Message[(_position + 1)..]);
                break;
            }

            // only param remaining
            if (nextSpace is -1)
            {
                _parameters.Add(Message[_position..]);
                break;
            }

            _parameters.Add(Message.Substring(_position, nextSpace - _position));

            _position = nextSpace + 1;
            AdjustPositionForSpaces();
        }
    }
    
    private void AdjustPositionForSpaces()
    {
        while (_position < Message.Length && Message[_position] is ' ')
            _position++;
    }
}