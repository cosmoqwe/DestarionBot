using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Configuration;

namespace DestarionBot
{
    internal class BotMain
    {

        private static ITelegramBotClient _botClient;
        private static ReceiverOptions _receiverOptions;
        
        static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            IConfigurationRoot configuration = builder.Build();
            string token = configuration.GetSection("BotConfiguration")["BotToken"];
            
            _botClient = new TelegramBotClient(token);
            using var cts = new CancellationTokenSource();


            _receiverOptions = new ReceiverOptions 
            {
                AllowedUpdates = new[]
                {
                    UpdateType.Message,
                    UpdateType.CallbackQuery,
                },
                ThrowPendingUpdates = true, //обработка полученных сообщений пока бот был отключен, true - выключено, false - включено
            };
            _botClient.StartReceiving(BotService.HandleUpdateAsync, BotService.ErrorHandler, _receiverOptions, cts.Token);
            Logger.LogAsync("Bot is up and running " + _botClient.GetMeAsync().Result.FirstName + " version 1.0.3", Logger.LogLevel.Info);
            while(true)
            {
                
                foreach(var key in Database._connectionStrings) 
                {
                    await Database.DeleteFromQueueAndSend(key.Value);
                }
                
                await Task.Delay(5000);
            }

        }
        public static async Task SendMessageToUsersAsync(long chat_id, string message, int delay)
        {
           await _botClient.SendTextMessageAsync(chat_id, message);
           await Task.Delay(delay);
        }

    }
}