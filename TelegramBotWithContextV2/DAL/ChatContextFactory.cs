using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBotWithContextV2.DAL
{
    internal class ChatContextFactory : IDbContextFactory<ChatContext>
    {
        public ChatContext CreateDbContext()
        {
            try
            {
                var optionsBilder = new DbContextOptionsBuilder<ChatContext>();
                IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
                optionsBilder.UseSqlite(configuration.GetConnectionString("SQLiteConnectionString"));
                return new ChatContext(optionsBilder.Options);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
    }
}
