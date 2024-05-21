using Bancho.Net.Bancho;
using Bancho.Net.Bancho.Attributes;
using Bancho.Net.Bancho.Messages;

namespace SimpleExample.Shared;

public class ExampleCommandModule(IBanchoClient banchoClient) : CommandModule(banchoClient)
{
    // show cases multiple forms of parameter support (all parameters are validated):
    // - regular string param
    // - enum param (it does get validated - supports int value & string (name) value)
    // - nullable params (optionally provided by the user)
    // also shows support to receive the PrivateMessage type used for fetching the channel to reply to
    [Command("test")]
    public async Task HandleTestCommand(string text, Number number, string? optional, PrivateMessage privateMessage)
    {
        var message = $"Executed test command with {text} and {number}";
        if (!string.IsNullOrWhiteSpace(optional))
            message += $" and optionally {optional}";

        await BanchoClient.SendPrivateMessageAsync(privateMessage.Target, message);
    }

    public enum Number
    {
        Test = 123,
    }
}