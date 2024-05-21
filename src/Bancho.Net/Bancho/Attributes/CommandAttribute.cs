namespace Bancho.Net.Bancho.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute(string name, bool allowPrivate = true, bool allowMultiplayer = false)
    : Attribute
{
    public string Name { get; } = name;
    public bool AllowPrivate { get; } = allowPrivate;
    public bool AllowMultiplayer { get; } = allowMultiplayer;
}