using static DestarionBot.Queries.QueryType;
using static DestarionBot.BotClient;

namespace DestarionBot
{
    public class User
    {
        private readonly long chat_id;
        private string username;
        private string server;
        private string state;
        private string language;
        private DateTime lastRequestTime;
        private int requestCount;
        private readonly object _lock = new object();
        public string Server
        {
            get => server;
            set => server = value;
        }
        public string State
        {
            get => state;
            set => state = value;
        }
        public string Username
        {
            get => username ?? "anonymous";
            set => username = value is null ? "anonymous" : value;
        }
        public DateTime LastRequestTime
        {
            get => lastRequestTime;
            set => lastRequestTime = value;
        }
        public int RequestCount
        {
            get => requestCount;
            set => requestCount = value;
        }
        public string Language
        {
            get => language ?? "English";
            set => language = value;
        }
        public long ChatId
        {
            get => chat_id;
        }
        public bool IsRequestAllowed
        {
            get
            {
                lock(_lock)
                {
                    var currentTime = DateTime.UtcNow;
                    if ((currentTime - LastRequestTime).TotalSeconds <= BotService.IntervalSeconds)
                    {
                        if (RequestCount >= BotService.RequestLimit)
                        {
                            LastRequestTime = currentTime;
                            return false;
                        }
                        RequestCount++;
                    }
                    else
                    {
                        LastRequestTime = currentTime;
                        RequestCount = 1;
                    }
                    return true;
                }
            }
        }
        public User(long chat_id)
        {
            this.chat_id = chat_id;
        }
        public static async Task<User> TryGetUser(long chat_id)
        {
            User? user = await SQLiteQueries.Get(SQLiteQueries.QueryType.GetUser, new object[] { chat_id }, reader => new User(chat_id)
            {
                    Server = reader["Server"].ToString() ?? string.Empty,
                    State = reader["State"].ToString() ?? string.Empty,
                    LastRequestTime = DateTime.Now,
                    RequestCount = 0,
                    Username = reader["Username"].ToString(),
                    Language = reader["Language"].ToString()
            });
            return user ?? new User(chat_id);
        }
        public async Task SaveData()
        {
            await SQLiteQueries.Execute(SQLiteQueries.QueryType.SaveUserData, new object[] { ChatId, Username, Server ?? "", State ?? "", Language ?? "", LastRequestTime, RequestCount});
        }
        public async Task ProcessRegistering(int id)
        {
            if(String.IsNullOrEmpty(Server))
            {
                await Bot.SendTextMessageAsync(ChatId, MessageHandler.Build(this, DestarionBot.Language.MessageType.ServerNotSelected));
                await Bot.SendTextMessageAsync(ChatId, MessageHandler.Build(this, DestarionBot.Language.MessageType.Help));
                return;
            }
            if (Convert.ToInt32(await Queries.Get(GetVerifiesCount, new object[] { ChatId }, Server)) >= 3)
            {
                await Bot.SendTextMessageAsync(ChatId, MessageHandler.Build(this, DestarionBot.Language.MessageType.CharacterLimitExceeded));
                return;
            }
            else if ((await Queries.Get(GetCharacterObjId, new object[] { id }, Server) is int objId)
                && (Convert.ToInt32(await Queries.Get(IsCharacterLinked, new object[] { objId }, Server))) == 0)
            {
                if (await Queries.Execute(RegisterUserAndLinkCharacter, new object[] { ChatId, Username, objId, }, Server))
                {
                    await Bot.SendTextMessageAsync(ChatId, MessageHandler.Build(this, DestarionBot.Language.MessageType.CharacterLinked, new string[] { Server }));
                    await Bot.SendTextMessageAsync(ChatId, MessageHandler.Build(this, DestarionBot.Language.MessageType.Help));
                }
                else
                {
                    await Bot.SendTextMessageAsync(ChatId, MessageHandler.Build(this, DestarionBot.Language.MessageType.AlreadyVerified));
                    await Bot.SendTextMessageAsync(ChatId, MessageHandler.Build(this, DestarionBot.Language.MessageType.Help));
                }
            }
            else
            {
                await Bot.SendTextMessageAsync(ChatId, MessageHandler.Build(this, DestarionBot.Language.MessageType.CharacterNotFound));
                await Bot.SendTextMessageAsync(ChatId, MessageHandler.Build(this, DestarionBot.Language.MessageType.Help));
            }
        }
        public async Task ProcessDeleting(int id)
        {
            if(String.IsNullOrEmpty(Server))
            {
                await Bot.SendTextMessageAsync(ChatId, MessageHandler.Build(this, DestarionBot.Language.MessageType.ServerNotSelected));
                await Bot.SendTextMessageAsync(ChatId, MessageHandler.Build(this, DestarionBot.Language.MessageType.Help));
                return;
            }
            int obj_id = Convert.ToInt32(await Queries.Get(GetCharacterObjId, new object[] { id }, Server));
            if (obj_id > 0)
            {
                if (Convert.ToInt32(await Queries.Get(IsCharacterLinked, new object[] { obj_id }, Server)) > 0)
                {
                    if (await Queries.Execute(UnlinkCharacter, new object[] { obj_id }, Server))
                    {
                        await Bot.SendTextMessageAsync(ChatId, MessageHandler.Build(this, DestarionBot.Language.MessageType.CharacterUnlinked));
                        await Bot.SendTextMessageAsync(ChatId, MessageHandler.Build(this, DestarionBot.Language.MessageType.Help));
                    }
                }
                else
                {
                    await Bot.SendTextMessageAsync(ChatId, MessageHandler.Build(this, DestarionBot.Language.MessageType.CharacterNotFound));
                    await Bot.SendTextMessageAsync(ChatId, MessageHandler.Build(this, DestarionBot.Language.MessageType.Help));
                }
            }
            else
            {
                await Bot.SendTextMessageAsync(ChatId, MessageHandler.Build(this, DestarionBot.Language.MessageType.CharacterNotFound));
                await Bot.SendTextMessageAsync(ChatId, MessageHandler.Build(this, DestarionBot.Language.MessageType.Help));
            }
        }
    }
}
