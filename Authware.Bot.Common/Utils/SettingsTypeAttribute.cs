namespace Authware.Bot.Common.Utils;

public class SettingsTypeAttribute : Attribute
{
    public SettingsDataType DateType { get; }
    public SettingsTypeAttribute(SettingsDataType type)
    {
        DateType = type;
    }
}