using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using static DestarionBot.Configuration;
using static DestarionBot.BotClient;
namespace DestarionBot
{
    public static class BotService
    {
        public static ConcurrentDictionary<long, User> activeUsers = new ConcurrentDictionary<long, User>();
        public static readonly List<string> servers;
        private static readonly int requestLimit;
        private static readonly int intervalSeconds;
        public static int RequestLimit
        {
            get => requestLimit;
        }
        public static int IntervalSeconds
        {
            get => intervalSeconds;
        }
        static BotService()
        {
            servers = Config.GetSection("Servers").Get<List<string>>().ToList();
            requestLimit = Config.GetValue<int>("BotConfiguration:RequestLimit");
            intervalSeconds = Config.GetValue<int>("BotConfiguration:IntervalSeconds");
            Task.Run(CheckActiveUsers);
        }
        public static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
        {
            var ErrorMessage = error switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => error.ToString()
            };
            Logger.LogAsync(ErrorMessage, Logger.LogLevel.Error);
            return Task.CompletedTask;
        }
        public static async Task HandleUpdateAsync(ITelegramBotClient _botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                var userId = update.Message?.Chat?.Id ?? update.CallbackQuery?.From?.Id;
                if (!userId.HasValue) return;
                if (!activeUsers.TryGetValue(userId.Value, out User user))
                {
                    user = await User.TryGetUser(userId.Value);
                    if (user is not null)
                    {
                        activeUsers.TryAdd(userId.Value, user);
                        user.Username = update.Message?.Chat?.Username ?? update.CallbackQuery?.From?.Username;
                    }
                }
                if (!user.IsRequestAllowed)
                {
                    await Bot.SendTextMessageAsync(userId.Value, await Language.Get(user.Language ?? "English", Language.MessageType.OutOfLimit));
                    return;
                }
                if(update.Type is UpdateType.CallbackQuery)
                    await CallbackQueryHandler.Handle(update.CallbackQuery, user);
                if (String.IsNullOrEmpty(user.Language))
                {
                    await MessageHandler.Handle(user, "/choose_language");
                    return;
                }
                if(String.IsNullOrEmpty(user.State) && String.IsNullOrEmpty(user.Server))
                {
                    await MessageHandler.Handle(user, "/start");
                }
                switch (update.Type)
                {
                    case UpdateType.Message:
                        await MessageHandler.Handle(user, update.Message.Text);
                        break;
                }
            }
            catch (Exception ex)
            {
                await Logger.LogAsync("Error on handling update: " + ex.Message + "Exception Type: " + ex.GetType() + "Stack trace: " + ex.StackTrace, Logger.LogLevel.Error);
            }
        }
        public static async Task CheckActiveUsers()
        {
            while (true)
            {
                await Task.Delay(600000);
                foreach (var user in activeUsers.Values)
                {
                    if ((DateTime.Now - user.LastRequestTime).TotalSeconds > 600)
                    {
                        await user.SaveData();
                        activeUsers.TryRemove(user.ChatId, out _);
                    }
                }
            }
        }
    }
}