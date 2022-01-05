namespace Authware.Bot.Common.Utils;

public class FriendlyNameAttribute : Attribute
{
    public string Name { get; }
    public FriendlyNameAttribute(string name)
    {
        Name = name;
    }
}