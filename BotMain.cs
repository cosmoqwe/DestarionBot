using Telegram.Bot;
using static DestarionBot.BotClient;
namespace DestarionBot
{
    internal class BotMain
    {   
        static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += async (sender, eventArgs) =>
            {
                await OnApplicationExit();
            };
            Language.LoadMessages();
            Logger.LogAsync("Bot is up and running " + Bot.GetMeAsync().Result.FirstName, Logger.LogLevel.Info); 
            while (true)
            {
                foreach(var key in Database._connectionStrings) 
                {
                    await Database.DeleteFromQueueAndSend(key.Value);
                }
                await Task.Delay(5000);
            }
        }
        static async Task OnApplicationExit()
        {
            foreach(var user in BotService.activeUsers.Values)
            {
                await user.SaveData();
            }
        }
    }
}
