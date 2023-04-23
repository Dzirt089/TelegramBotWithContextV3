using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TelegramBotWithContextV2.DAL;
using TelegramBotWithContextV2.Services;

namespace TelegramBotWithContextV2
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var section = configuration.GetSection("Appsettings");

            var optionsBuilder = new DbContextOptionsBuilder<ChatContext>();
            optionsBuilder.UseSqlite(section["sqlite"]);
            var chatContext = new ChatContext(optionsBuilder.Options);
            var chatContextFactory = new ChatContextFactory();

            var telegramBotToken = section["TelegramBotToken"];
            var telegramBotTokenTest = section["TelegramBotTokenTest"];
            var chatGptApiKey = section["ChatGptApiKey"];
            var shopId = section["shopId"];
            var secretKey = section["secretKey"];
            //var telegramBot = new TelegramBot(telegramBotTokenTest, chatGptApiKey, chatContext, shopId, secretKey);
            var telegramBot = new TelegramBot(telegramBotToken, chatGptApiKey, chatContextFactory, shopId, secretKey);
            // Запуск бота
            await telegramBot.StartAsync();

        }
    }
}