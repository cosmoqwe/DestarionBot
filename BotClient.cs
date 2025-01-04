using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static DestarionBot.Configuration;

// 

namespace DestarionBot
{
    internal class BotClient : TelegramBotClient
    {
        private readonly int intervalSeconds; // Delay between message sends in seconds. If delay is set to 0, the bot risks being blocked by Telegram due to high request frequency, as API rate limits may be exceeded.
        private static BotClient _instance;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private static readonly object _lock = new object();
        public static BotClient Bot // singleton
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new BotClient(Config.GetValue<string>("BotConfiguration:BotToken"));
                    }
                    return _instance;
                }
            }
        }
        private BotClient(string token, HttpClient? httpClient = null) : base(token, httpClient)
        {
            ReceiverOptions _receiverOptions;
            using var cts = new CancellationTokenSource();
            _receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[]
                {
                    UpdateType.Message,
                    UpdateType.CallbackQuery,
                },
                ThrowPendingUpdates = true, //handling received messages while bot was turned off, true - disabled, false - enabled
            };
            intervalSeconds = Config.GetValue<int>("BotConfiguration:IntervalSendMessageMillis");
            this.StartReceiving(BotService.HandleUpdateAsync, BotService.ErrorHandler, _receiverOptions, cts.Token);
        }
        public async Task SendTextMessageAsync(long chat_id, string message, InlineKeyboardMarkup keyboard = null)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (keyboard is null)
                    await this.SendTextMessageAsync(chat_id, message, parseMode: ParseMode.Html);
                else
                    await this.SendTextMessageAsync(chat_id, message, replyMarkup: keyboard);
                await Task.Delay(intervalSeconds);
            }
            catch (Exception ex)
            {
                await Logger.LogAsync("Exception on sending message! Exception: " + ex.Message + " Stack Trace: " + ex.StackTrace, Logger.LogLevel.Error);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
