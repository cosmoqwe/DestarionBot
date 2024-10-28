using MySqlConnector;
using Microsoft.Extensions.Configuration;
using static DestarionBot.Configuration;
using static DestarionBot.BotClient;
using System.Reflection;

namespace DestarionBot
{
    internal class Database
    {
        public static Dictionary<string, string> _connectionStrings = new Dictionary<string, string>();
        static Database()
        {
            IConfigurationSection connectionStringsSection = Config.GetSection("ConnectionStrings");
            foreach (var connectionString in connectionStringsSection.GetChildren())
            {
                _connectionStrings.Add(connectionString.Key, connectionString.Value);
            }
        }
        public static async Task<ICollection<T>> Get<T>(string query, object[] args, string selectedServer, ICollection<T> collection, Func<MySqlDataReader, T> mapFunc)
        {
            string connectionString = _connectionStrings[selectedServer];
            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        await ReplaceParameters(query, args, command);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                collection.Add(mapFunc(reader));
                            }
                        }
                    }
                    return collection;
                }
                catch (Exception ex)
                {
                    await Logger.LogAsync("Error on getting result from database!" +
                        " Query: " + query + " connection string: " + connectionString + " Exception: " + ex.Message, Logger.LogLevel.Error);
                }
            }
            return default(T) as ICollection<T>;
        }
        public static async Task<object> Get(string query, object[] args, string selectedServer)
        {
            string connectionString = _connectionStrings[selectedServer];
            using (var connection = new MySqlConnection(connectionString))
            {
                object? result;
                try
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        await ReplaceParameters(query, args, command);
                        result = await command.ExecuteScalarAsync();
                    }
                    return result ?? default(object);
                }
                catch (Exception ex)
                {
                    await Logger.LogAsync("Error on getting result from database!" +
                        " Query: " + query + " connection string: " + connectionString + " Exception: " + ex.Message, Logger.LogLevel.Error);
                }
            }
            return default(object);
        }
        public static async Task<bool> Execute(string query, object[] args, string selectedServer)
        {
            string connectionString = _connectionStrings[selectedServer];
            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        await ReplaceParameters(query, args, command);
                        await command.ExecuteNonQueryAsync();
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    await Logger.LogAsync("Error on executing command in database! Query: " + query, Logger.LogLevel.Error);
                    return false;
                }
            }
            throw new Exception("Something went wrong with method " + MethodBase.GetCurrentMethod().Name + " Query: " + query);
        }
        public static async Task ReplaceParameters(string query, object[] args, MySqlCommand command)
        {
            if (args is not null && args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string paramName = "@param" + i;
                    query = query.Replace("{" + i + "}", paramName);
                    command.Parameters.AddWithValue(paramName, args[i]);
                }
                command.CommandText = query;
            }
        }
        public static async Task DeleteFromQueueAndSend(string connectionString)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                var messages = new List<(long chatId, string message)>();
                var sentMessages = new HashSet<string>();
                try
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM telegram_messages";
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                messages.Add((Convert.ToInt64(reader["chat_id"]), reader["message"].ToString()));
                            }
                        }
                    }
                    if (messages.Count > 0)
                    {
                        var chatIds = messages.Select(m => m.chatId).ToList();

                        using (var deleteCommand = connection.CreateCommand())
                        {
                            deleteCommand.CommandText = "DELETE FROM telegram_messages WHERE chat_id IN (" +
                                                        string.Join(",", chatIds.Select((id, index) => $"@id{index}")) + ")";
                            for (int i = 0; i < chatIds.Count; i++)
                            {
                                deleteCommand.Parameters.AddWithValue($"@id{i}", chatIds[i]);
                            }
                            await deleteCommand.ExecuteNonQueryAsync();
                        }
                    }
                    foreach (var message in messages)
                    {
                        string[] builder = message.message.Split(',');
                        string awaiter = await MessageHandler.BuildServerMessage(message.chatId, builder);
                        if (!sentMessages.Contains(message.message))
                        {
                            await Bot.SendTextMessageAsync(message.chatId, awaiter);
                            sentMessages.Add(message.message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    await Logger.LogAsync("Error on send message: " + ex.Message + "Stack trace: " + ex.StackTrace, Logger.LogLevel.Error);
                }
            }
        }

    }
}
