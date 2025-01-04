using System.Runtime.CompilerServices;
using System.Text.Json;
using static DestarionBot.Configuration;

namespace DestarionBot
{
    public static class Language
    {
        public enum MessageType
        {
            #region Bot Messages

            Start, 
            OutOfLimit,
            LanguageSelected,
            SendIdPrompt,
            ServerSelectionPrompt, 
            CharacterUnlinked,
            CharacterNotFound,
            CharacterLimitExceeded, 
            CharacterLinked, 
            ServerNotSelected,
            ServerSelected,
            UnknownCommand,
            AlreadyVerified,
            CurrentServerInfo,
            Help,

            #endregion
            #region Server Messages

            CharacterDied,
            CharacterWasKilled,
            CharacterSold,
            CharacterSoldStackable,
            CharacterSoldPackage,
            CharacterBought,
            CharacterBoughtStackable,
            #endregion
        }
        
        private static Dictionary<string, Dictionary<MessageType, string>> messages = new Dictionary<string, Dictionary<MessageType, string>>();
        
        public static Dictionary<string, Dictionary<MessageType, string>> Messages
        {
            get
            {
                if (messages == null)
                {
                    LoadMessages();
                }
                return messages;
            }
        }
        public static string[] Languages
        {
            get => messages.Keys.ToArray();
        }
        public static string Get(string language, MessageType type)
        {
            if (Messages.TryGetValue(language, out var messageDictionary) && messageDictionary.TryGetValue(type, out string message))
                return message;
            throw new KeyNotFoundException($"Message for language '{language}' and type '{type}' not found. Stack trace: {Environment.StackTrace}");
        }
        public static void LoadMessages()
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, "Localization", "localization.json");
                if (!File.Exists(path))
                    throw new FileNotFoundException($"The required file was not found: {path}");
                var json = File.ReadAllText(path);
                var tempMessages = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
                foreach (var entry in tempMessages)
                {
                    var language = entry.Key;
                    var messages = new Dictionary<MessageType, string>();
                    foreach (var message in entry.Value)
                    {
                        if (Enum.TryParse(message.Key, true, out MessageType messageType))
                            messages[messageType] = message.Value;
                        else
                            throw new ArgumentException($"Invalid message type {message.Key} in language {language}");
                    }
                    Messages[language] = messages;
                }
                foreach (var entry in Messages)
                {
                    if (entry.Value.Count != Enum.GetValues(typeof(MessageType)).Length)
                    {
                        throw new InvalidOperationException($"Not all message types were defined in language {entry.Key}");
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Log($"Exception on loading messages! Exception: {ex.Message}", Logger.LogLevel.Error);
                throw;
            }
        }
    }
}
