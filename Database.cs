using MySqlConnector;
using Microsoft.Extensions.Configuration;

namespace DestarionBot
{
    internal class Database
    {
        public static Dictionary<string,string> _connectionStrings = new Dictionary<string,string>();
        static int delaySendMessage;
        static Database()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();
            IConfigurationSection connectionStringsSection = configuration.GetSection("ConnectionStrings");
            delaySendMessage = configuration.GetValue<int>("BotConfiguration:IntervalSendMessageMillis");
            foreach (var connectionString in connectionStringsSection.GetChildren())
            {
                _connectionStrings.Add(connectionString.Key, connectionString.Value);
            }
        }
        
        public static async Task DeleteFromQueueAndSend(string connectionString)
        {
            using(var connection = new MySqlConnection(connectionString))
            {
                var messages = new List<(long chatId, string message)>();
                var sentMessages = new HashSet<string>();
                int id = 0;
                try
                {
                    await connection.OpenAsync();
                    using (var checkCommand = connection.CreateCommand())
                    {
                        checkCommand.CommandText = "SELECT COUNT(*) FROM telegram_messages";
                        var count = (long)await checkCommand.ExecuteScalarAsync();  
                        if (count == 0)
                        {
                            return;
                        }
                    }
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM telegram_messages";
                        using(var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                messages.Add((Convert.ToInt64(reader["chat_id"]), reader["message"].ToString()));
                            }
                        }
                    }
                    foreach (var message in messages)
                    {
                        using (var deleteCommand = connection.CreateCommand())
                        {
                            deleteCommand.CommandText = "DELETE FROM telegram_messages WHERE chat_id = @id AND message = @message";
                            deleteCommand.Parameters.AddWithValue("@id", message.chatId);
                            deleteCommand.Parameters.AddWithValue("@message", message.message);
                            await deleteCommand.ExecuteNonQueryAsync();
                        }
                        if(!sentMessages.Contains(message.message))
                        {
                            await BotMain.SendMessageToUsersAsync(message.chatId, message.message, delaySendMessage);
                            sentMessages.Add(message.message);
                        }
                    }

                }
                catch (Exception ex)
                {
                    await Logger.LogAsync("Error on send message: " + ex.Message, Logger.LogLevel.Error);
                }
            }
        }
        public static async Task<bool> RegisterUserAsync(long chatId, string username, string messageText, string selectedServer)
        {
            string connectionString = _connectionStrings[selectedServer];
            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    int? object_id = null;
                    string name;
                    await connection.OpenAsync();
                    using (var takeCommand = connection.CreateCommand())
                    {
                        takeCommand.CommandText = "SELECT obj_id FROM character_variables WHERE value = @value1";
                        takeCommand.Parameters.AddWithValue("@value1", messageText);
                        var result = await takeCommand.ExecuteScalarAsync();
                        if (result != null)
                        {
                            object_id = Convert.ToInt32(result);
                        }
                    }
                    using(var checkCommand = connection.CreateCommand())
                    {
                        checkCommand.CommandText = "SELECT COUNT(*) FROM telegram_verifies WHERE object_id = @value1";
                        checkCommand.Parameters.AddWithValue("@value1", object_id);
                        var result = await checkCommand.ExecuteScalarAsync();
                        int count = Convert.ToInt32(result);
                        if (count != 0)
                        {
                            return false;
                        }
                    }
                    using(var command = connection.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO telegram_verifies (chat_id, telegram_id, object_id) VALUES (@value1, @value2, @value3)";
                        command.Parameters.AddWithValue("@value1", chatId);
                        command.Parameters.AddWithValue("@value2", username);
                        command.Parameters.AddWithValue("@value3", object_id);
                        await command.ExecuteNonQueryAsync();
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    await Logger.LogAsync("Error on registration: " + ex.Message, Logger.LogLevel.Error);
                    return false;
                }
            }
        }
        public static async Task<bool> CheckUserAlreadyVerified(long chat_id, string selectedServer)
        {
            string connectionString = _connectionStrings[selectedServer];
            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT COUNT(*) FROM telegram_verifies WHERE chat_id = @id";
                        command.Parameters.AddWithValue("@id", chat_id.ToString());
                        var result = await command.ExecuteScalarAsync();
                        int count = Convert.ToInt32(result);
                        if(count < 3)
                        {
                            return false;
                        }
                        else return true;

                    }
                }
                catch(Exception ex)
                {
                    await Logger.LogAsync("Error on check user: " + ex.Message, Logger.LogLevel.Error);
                    return false;
                }
            }
        }
        public static async Task<bool> CheckUserHaveIdAsync(int id, string selectedServer)
        {
            string connectionString = _connectionStrings[selectedServer];
            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    using(var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT COUNT(*) FROM character_variables WHERE value = @value1";
                        command.Parameters.AddWithValue("@value1", id.ToString());
                        var result = await command.ExecuteScalarAsync();
                        int count = Convert.ToInt32(result);
                        return count > 0;
                    }
                }
                catch(Exception ex)
                {
                    await Logger.LogAsync("Error on Check Id: " + ex.Message, Logger.LogLevel.Error);
                    return false;
                }
            }
        }
        public static async Task<bool> DeleteCharacter(int id, string selectedServer)
        {
            string connectionString = _connectionStrings[selectedServer];
            using (var connection = new MySqlConnection(connectionString))
            {
                int object_id;
                try
                {
                    await connection.OpenAsync();
                    using (var takeCommand = connection.CreateCommand())
                    {
                        takeCommand.CommandText = "SELECT obj_id FROM character_variables WHERE value = @value1";
                        takeCommand.Parameters.AddWithValue("@value1", Convert.ToString(id));
                        var result = await takeCommand.ExecuteScalarAsync();
                        object_id = Convert.ToInt32(result);
                    }
                    using(var checkCommand = connection.CreateCommand())
                    {
                        checkCommand.CommandText = "SELECT COUNT(*) FROM telegram_verifies WHERE object_id = @object_id";
                        checkCommand.Parameters.AddWithValue("@object_id", object_id);
                        var result = await checkCommand.ExecuteScalarAsync();
                        int count = Convert.ToInt32(result);
                        if(count == 0)
                        {
                            return false;
                        }
                    }
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "DELETE FROM telegram_verifies WHERE object_id = @value1";
                        command.Parameters.AddWithValue("@value1", object_id);
                        await command.ExecuteNonQueryAsync();
                        return true;
                    }
                }
                catch (Exception ex) 
                {
                    await Logger.LogAsync("Error on Check Id: " + ex.Message, Logger.LogLevel.Error);
                    return false;
                }
            }
        }
    }
}
