namespace Authware.Bot.Common;

public static class ExtensionMethods
{
    public static IEnumerable<T> TruncateList<T>(this IEnumerable<T> list, uint amount)
    {
        var index = 0;
        var newList = new List<T>();
        foreach (var item in list.TakeWhile(item => index != amount))
        {
            newList.Add(item);
            index++;
        }

        return newList;
    }
}