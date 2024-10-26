using Microsoft.Extensions.Configuration;
using Telegram.Bot.Types;
using static DestarionBot.Configuration;
namespace DestarionBot
{
    internal static class CallbackQueryHandler
    {
        private static readonly int intervalSeconds;
        static CallbackQueryHandler()
        {
            intervalSeconds = Config.GetValue<int>("BotConfiguration:IntervalSeconds");
        }
        public static async Task Handle(CallbackQuery query, User user)
        {
            if(Language.Languages.Contains(query.Data))
                await HandleChooseLanguage(query, user);
            else if(BotService.servers.Contains(query.Data))
                await HandleChooseServer(query, user);
        }
        private static async Task HandleChooseLanguage(CallbackQuery query, User user)
        {
            user.Language = query.Data;
            await BotClient.Bot.SendTextMessageAsync(user.ChatId, await MessageHandler.Build(user, Language.MessageType.LanguageSelected, new string[] { user.Language}));
            await BotClient.Bot.SendTextMessageAsync(user.ChatId, await MessageHandler.Build(user, Language.MessageType.Help));
        }
        private static async Task HandleChooseServer(CallbackQuery query, User user)
        {
            user.Server = query.Data;
            await BotClient.Bot.SendTextMessageAsync(user.ChatId, await MessageHandler.Build(user, Language.MessageType.ServerSelected, new string[] { user.Server }));
            await BotClient.Bot.SendTextMessageAsync(user.ChatId, await MessageHandler.Build(user, Language.MessageType.Help));
        }
    }
}
