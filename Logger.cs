using Microsoft.Extensions.Configuration;
namespace DestarionBot
{
    public class Logger
    {
        private static readonly string logFilePath;
        static Logger()
        {
            var builder = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            IConfigurationRoot configuration = builder.Build();
            logFilePath = configuration.GetValue<string>("BotConfiguration:LogFilePath");
        }
        public enum LogLevel
        {
            Info,
            Warning,
            Error
        }

        public static async Task LogAsync(string message, LogLevel level = LogLevel.Info)
        {
            try
            {
                using (var stream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream))
                {
                    await writer.WriteLineAsync($"{DateTime.Now} [{level.ToString()}]: {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while logging: {ex.Message}");
            }
        }
        public static void Log(string message, LogLevel level = LogLevel.Info)
        {
            try
            {
                using (var stream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream))
                {
                    writer.WriteLine($"{DateTime.Now} [{level.ToString()}]: {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while logging: {ex.Message}");
            }
        }
    }
}

