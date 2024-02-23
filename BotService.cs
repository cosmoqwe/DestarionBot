using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Configuration;
using DestarionBot;
using System.Configuration;

public class BotService
{
    public static Dictionary<long, (long lastRequestTime, int requestCount)> Requests = new();
    public static Dictionary<long, string> userStates = new Dictionary<long, string>();
    public static Dictionary<long, string> userServers = new Dictionary<long, string>();
    public static IConfigurationRoot configuration;
    private static readonly List<string> servers;
   
    private static readonly int requestLimit;
    private static readonly int intervalSeconds;
    static BotService()
    {
        var builder = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        configuration = builder.Build();
        servers = configuration.GetSection("Servers").Get<List<string>>().ToList();
        
        requestLimit = configuration.GetValue<int>("BotConfiguration:RequestLimit");
        intervalSeconds = configuration.GetValue<int>("BotConfiguration:IntervalSeconds");
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

            if (!IsRequestAllowed(userId.Value))
            {
                await _botClient.SendTextMessageAsync(userId.Value, "You have exceeded your request limit. Try later.");
                return;
            }
            switch (update.Type)
            {
                case UpdateType.Message:
                    Message message = update.Message;
                    HandleMessage(_botClient, message);
                    break;
                case UpdateType.CallbackQuery:
                    {

                        CallbackQuery query = update.CallbackQuery;
                        HandleCallbackQuery(_botClient, query);
                    }
                    break;
            }

        }
        catch (Exception ex)
        {
            await Logger.LogAsync("Error on update: " + ex.Message, Logger.LogLevel.Error);
        }
    }
    public static async Task HandleMessage(ITelegramBotClient _botClient, Message message)
    {
        if (message != null && message.Text != null)
        {
            long chatId = message.Chat.Id;
            string username = message.Chat.Username;
            string messageText = message.Text;
            switch (message.Type)
            {
                case MessageType.Text:
                    {
                        await Logger.LogAsync(username + "  " + messageText, Logger.LogLevel.Info);
                        if (message.Text.Length == 9 && int.TryParse(message.Text, out int id) && servers.Contains(userServers[chatId]))
                        {
                            switch (userStates[chatId])
                            {
                                case "/delete_character":
                                    if (await Database.CheckUserHaveIdAsync(id, userServers[chatId]) && await Database.DeleteCharacter(id, userServers[chatId]))
                                    {
                                        await _botClient.SendTextMessageAsync(chatId, "The character is unlinked from Telegram.");
                                    }
                                    else
                                    {
                                        await _botClient.SendTextMessageAsync(chatId, "This user was not found.");
                                    }
                                    break;
                                case "/add_character":
                                    if (await Database.CheckUserAlreadyVerified(message.Chat.Id, userServers[chatId]))
                                    {
                                        await _botClient.SendTextMessageAsync(chatId, "Character limit exceeded.");
                                    }
                                    else if (await Database.CheckUserHaveIdAsync(id, userServers[chatId]))
                                    {
                                        if (await Database.RegisterUserAsync(chatId, username, messageText, userServers[chatId]))
                                            await _botClient.SendTextMessageAsync(chatId, "Your character was connected with Telegram. Server: " + userServers[chatId]);
                                        else
                                            await _botClient.SendTextMessageAsync(chatId,"The user has already been verified.");
                                    }
                                    else
                                    {
                                        await _botClient.SendTextMessageAsync(chatId, "This user was not found.");
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            switch (messageText)
                            {
                                case "/add_character":
                                    if (userServers.ContainsKey(chatId)) { 
                                        lock (userStates)
                                        {
                                            if (!userStates.ContainsKey(chatId))
                                            {
                                                userStates.Add(chatId, messageText);
                                            }
                                            else
                                            {
                                                userStates[chatId] = messageText;
                                            }
                                        }
                                        await _botClient.SendTextMessageAsync(chatId, 
                                           "Send me your ID. You can get it in the game using the <code>.telegram</code> command"
                                            + "\n \n" + "<b>Attention! Do not share this code with anyone.</b>", parseMode: ParseMode.Html);
                                    }
                                    else
                                        await _botClient.SendTextMessageAsync(chatId, "First select a server.");
                                    break;
                                case "/delete_character":
                                    if (userServers.ContainsKey(chatId))
                                    {
                                        lock (userStates)
                                        {
                                            if (!userStates.ContainsKey(chatId))
                                            {
                                                userStates.Add(chatId, messageText);
                                            }
                                            else
                                            {
                                                userStates[chatId] = messageText;
                                            }
                                        }
                                        await _botClient.SendTextMessageAsync(chatId, "Send me your ID. You can get it in the game using the .telegram command");
                                    }
                                    else
                                        await _botClient.SendTextMessageAsync(chatId, "First select a server.");
                                    break;
                                case "/choose_server":
                                    var buttons = servers.Select(server =>
                                            InlineKeyboardButton.WithCallbackData(server)).ToArray();

                                    var inlineKeyboard = new InlineKeyboardMarkup(buttons);

                                    await _botClient.SendTextMessageAsync(chatId, "Choose server", replyMarkup: inlineKeyboard);
                                    break;
                                case "/start":
                                    inlineKeyboard = new InlineKeyboardMarkup(new[]
                                           {
                                                        new []
                                                        {
                                                            InlineKeyboardButton.WithCallbackData("Add character (max 3)", "/add_character"),
                                                        },
                                                     });
                                    await _botClient.SendTextMessageAsync(chatId,
                                             "Hello. I am a Bot of the L2 High Five game server complex of the Destarion project."
                                              + " With my help, you can receive notifications about the death of your character during automatic farming, "
                                              + "and also receive promotional codes with special gifts!", replyMarkup: inlineKeyboard);
                                    lock (userStates)
                                    {
                                        if (!userStates.ContainsKey(chatId))
                                        {
                                            userStates.Add(chatId, messageText);
                                        }
                                        else
                                        {
                                            userStates[chatId] = messageText;
                                        }
                                    }
                                    break;
                                case "/current_server":
                                    if (!userServers.ContainsKey(chatId))
                                    {
                                        await _botClient.SendTextMessageAsync(chatId, "Server not selected.");
                                    }
                                    else
                                        await _botClient.SendTextMessageAsync(chatId, "Server: " + userServers[chatId]);
                                    break;
                                default:
                                    await _botClient.SendTextMessageAsync(chatId, "I don't understand this command, or did you send the character's nickname. To control, use <b>Menu</b>", parseMode: ParseMode.Html);
                                    break;
                            }

                        }
                    }
                    break;
            }
        }
    }
        public static async Task HandleCallbackQuery(ITelegramBotClient _botClient, CallbackQuery query)
        {
            var callbackData = query.Data;
            var messageId = query.Message.MessageId;
            var chatId = query.Message.Chat.Id;
            if (servers.Contains(callbackData))
            {
                await ProcessServerSelection(_botClient, chatId, callbackData);
                return;
            }
            switch (callbackData)
            {
                case "/add_character":
                    {
                        lock (userStates)
                        {
                            if (!userStates.ContainsKey(chatId))
                            {
                                userStates.Add(chatId, query.Data);
                            }
                            else
                            {
                                userStates[chatId] = query.Data;
                            }
                        }
                        var buttons = servers.Select(server =>
                            InlineKeyboardButton.WithCallbackData(server)).ToArray();

                        var inlineKeyboard = new InlineKeyboardMarkup(buttons);

                        await _botClient.SendTextMessageAsync(chatId, "Select a server.", replyMarkup: inlineKeyboard);
                    }

                    break;

            }
        }
    
    public static bool IsRequestAllowed(long userId)
    {
        var currentUnixTimestamp = GetCurrentUnixTimestampSeconds();
        lock (Requests)
        {
            if (Requests.TryGetValue(userId, out var userInfo))
            {
                if (currentUnixTimestamp - userInfo.lastRequestTime <= intervalSeconds)
                {
                    if (userInfo.requestCount >= requestLimit)
                    {
                        return false;
                    }
                    else
                    {
                        Requests[userId] = (currentUnixTimestamp, userInfo.requestCount + 1);
                    }
                }
                else
                {
                    Requests[userId] = (currentUnixTimestamp, 1);
                }
            }
            else
            {
                Requests.Add(userId, (currentUnixTimestamp, 1));
            }
        }

        return true;
    }
    private static async Task ProcessServerSelection(ITelegramBotClient botClient, long chatId, string selectedServer)
    {
        lock(userServers)
        {
            if (!userServers.ContainsKey(chatId))
            {
                userServers.Add(chatId, selectedServer);
            }
            else
            {
                userServers[chatId] = selectedServer;
            }
        }
        await botClient.SendTextMessageAsync(chatId, $"You have selected a server: {selectedServer}.");
        await botClient.SendTextMessageAsync(chatId, "<b>To add or remove a character, click Menu.</b>", parseMode: ParseMode.Html);
    }
    public static long GetCurrentUnixTimestampSeconds()
    {
        var currentTime = DateTime.UtcNow;
        var unixStartTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        return (long)(currentTime - unixStartTime).TotalSeconds;
    }
    public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        var unixStartTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        return unixStartTime.AddSeconds(unixTimeStamp);
    }
}
