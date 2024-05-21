using Bancho.Net.Bancho.Messages;

namespace Bancho.Net.Bancho.Models;

public class Channel
{
    public string Name { get; init; }

    public IReadOnlyList<PrivateMessage> Messages => _messages;

    private readonly List<PrivateMessage> _messages = [];
    private readonly bool _saveMessages;

    public Channel(string name, bool saveMessages = true)
    {
        Name = name;

        _saveMessages = saveMessages;
    }

    public void AddMessage(PrivateMessage privateMessage)
    {
        if (!_saveMessages)
            return;

        _messages.Add(privateMessage);
    }
}