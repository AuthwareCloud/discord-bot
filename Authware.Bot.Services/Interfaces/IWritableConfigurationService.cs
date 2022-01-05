namespace Authware.Bot.Services.Interfaces;

public interface IWritableConfigurationService<TConfiguration>
{
    void Save(TConfiguration configuration);
    TConfiguration Load();
}