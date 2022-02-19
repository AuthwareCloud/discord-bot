namespace Authware.Bot.Common.Models;

public class TrackContext
{
    public ulong RequesterId { get; set; }
    public string OriginalQuery { get; set; }
}