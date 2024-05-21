using System.Reflection;
using Bancho.Net.Bancho.Attributes;
using Bancho.Net.Bancho.Messages;

namespace Bancho.Net.Bancho;

public class CommandModule
{
    protected readonly IBanchoClient BanchoClient;

    private readonly Dictionary<string, CommandHandler> _handlers = [];

    protected CommandModule(IBanchoClient banchoClient)
    {
        BanchoClient = banchoClient;
        RegisterHandler();
    }

    private async Task OnMessageReceivedAsync(PrivateMessage privateMessage)
    {
        if (string.IsNullOrWhiteSpace(privateMessage.Content))
            return;

        var parts = privateMessage.Content.Split(' ');
        if (parts[0][0] is not '!')
            return;

        var command = parts[0].TrimStart('!');

        if (!_handlers.TryGetValue(command, out var commandHandler))
            return;

        if (!commandHandler.AllowPrivate && !privateMessage.IsChannel)
            return;

        if (!commandHandler.AllowMultiplayer && privateMessage.Recipient.StartsWith("#mp_"))
            return;

        var remainingParameters = parts[1..].ToList();

        if (remainingParameters.Count < commandHandler.RequiredParameters.Count)
        {
            await BanchoClient.SendPrivateMessageAsync(privateMessage.Target,
                $"Expected at least {commandHandler.RequiredParameters.Count} parameters, got {remainingParameters.Count}");

            return;
        }

        if (remainingParameters.Count > commandHandler.RealParameters.Count)
        {
            await BanchoClient.SendPrivateMessageAsync(privateMessage.Target,
                $"Expected no more than {commandHandler.RealParameters.Count} parameters, got {remainingParameters.Count}");

            return;
        }

        var arguments = new List<object?>();
        for (var i = 0; i < commandHandler.RealParameters.Count; i++)
        {
            // since we have optional arguments, we should check if they are given
            // we know at this point we've satisfied the amount of required arguments
            var stringValue = remainingParameters.ElementAtOrDefault(i);
            if (stringValue is null)
            {
                arguments.Add(null);
                continue;
            }

            var parameter = commandHandler.RealParameters[i];

            try
            {
                var value = ConvertCommandParameter(stringValue, parameter.UnderlyingType ?? parameter.Type);
                arguments.Add(value);
            } 
            catch (Exception ex) when (ex is FormatException or OverflowException)
            {
                await BanchoClient.SendPrivateMessageAsync(privateMessage.Target,
                    $"Invalid {stringValue} specified for {parameter.Name}");

                return;
            }
        }

        if (commandHandler.PrivateMessageParameterIndex is not null)
            arguments.Insert(commandHandler.PrivateMessageParameterIndex.Value, privateMessage);
        
        var executionResult = commandHandler.MethodInfo.Invoke(this, arguments.ToArray());
        if (executionResult is Task task)
        {
            await task;
        }
    }

    private static object ConvertCommandParameter(string value, Type type)
    {
        if (type == typeof(string))
            return value;

        if (type == typeof(char))
            return char.Parse(value);

        if (type == typeof(byte))
            return byte.Parse(value);

        if (type == typeof(int))
            return int.Parse(value);

        if (type == typeof(long))
            return long.Parse(value);

        if (type == typeof(double))
            return double.Parse(value);

        if (type == typeof(float))
            return double.Parse(value);

        if (type == typeof(decimal))
            return decimal.Parse(value);

        if (type.IsEnum)
        {
            // if name isn't defined, then we'll just try to cast directly
            var containsStringValue = Enum.GetNames(type).Any(x => x.Equals(value, StringComparison.InvariantCultureIgnoreCase));
            if (containsStringValue)
                return Enum.Parse(type, value, true);

            if (int.TryParse(value, out var intValue) && Enum.IsDefined(type, intValue))
                return Enum.ToObject(type, intValue);

            throw new FormatException($"Invalid enum value {value} for {type.Name}");
        }

        if (type == typeof(bool))
            return value.ToLowerInvariant() is "true" or "false" or "y" or "n" or "yes" or "no" or "0" or "1";
        
        // TODO: support array, list etc.
        throw new NotSupportedException($"Commands do not support {type.Name} yet");
    }

    private void RegisterHandler()
    {
        foreach (var method in GetType().GetMethods())
        {
            var attribute = (CommandAttribute?)method.GetCustomAttribute(typeof(CommandAttribute));
            if (attribute is null)
                continue;

            var commandParameters = method.GetParameters().Select(x =>
            {
                var nullable = x.ParameterType.IsGenericType &&
                               x.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>);

                return new CommandParameter
                {
                    Name = x.Name!,
                    Type = x.ParameterType,
                    IsNullable = nullable,
                    UnderlyingType = nullable ? Nullable.GetUnderlyingType(x.ParameterType) : null,
                };
            }).ToList();
            
            var privateMessageParameterIndex = commandParameters.FindIndex(x => x.Type == typeof(PrivateMessage));

            var commandHandler = new CommandHandler
            {
                Name = attribute.Name,
                MethodInfo = method,
                Parameters = commandParameters,
                RealParameters = commandParameters.Where(x => x.Type != typeof(PrivateMessage)).ToList(),
                RequiredParameters = commandParameters.Where(x => !x.IsNullable).ToList(),
                AllowPrivate = attribute.AllowPrivate,
                AllowMultiplayer = attribute.AllowMultiplayer,
                PrivateMessageParameterIndex = privateMessageParameterIndex != -1 ? privateMessageParameterIndex : null,
            };
            _handlers[attribute.Name] = commandHandler;
        }

        BanchoClient.OnPrivateMessageReceived += async message => await OnMessageReceivedAsync(message);
    }

    private class CommandHandler
    {
        public required string Name { get; init; }
        public required MethodInfo MethodInfo { get; init; }
        public required List<CommandParameter> Parameters { get; init; }
        public required List<CommandParameter> RealParameters { get; init; }
        public required List<CommandParameter> RequiredParameters { get; init; }
        public required bool AllowPrivate { get; init; }
        public required bool AllowMultiplayer { get; init; }
        
        public required int? PrivateMessageParameterIndex { get; init; }
    }

    private class CommandParameter
    {
        public required string Name { get; init; }
        public required Type Type { get; init; }
        public required bool IsNullable { get; init; }
        public required Type? UnderlyingType { get; init; }
    }
}