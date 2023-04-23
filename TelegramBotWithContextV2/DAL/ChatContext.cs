using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TelegramBotWithContextV2.Entity;

namespace TelegramBotWithContextV2.DAL
{
    /// <summary>
    /// Класс ChatContext наследуется от DbContext и содержит две сущности: Chat и Message. 
    /// Chat представляет собой чат с пользователями, а Message - сообщение в этом чате. Связь между ними устанавливается с помощью внешнего ключа ChatId. 
    /// В классе Chat определена коллекция Messages для хранения сообщений, связанных с этим чатом.
    /// </summary>
    public class ChatContext : DbContext
    {
        public ChatContext(DbContextOptions<ChatContext> options) : base(options) { Database.EnsureCreated(); }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Subscribe> Subscribes { get; set; }
        public DbSet<MessageError> MessageErrors { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            optionsBuilder.UseSqlite(configuration.GetConnectionString("SQLiteConnectionString"));
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Chat>()
                .HasMany(c => c.Messages)
                .WithOne(m => m.Chat)
                .HasForeignKey(m => m.ChatId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Message>()
                .HasKey(m => m.Id);

            modelBuilder.Entity<Subscribe>()
                .HasOne(s => s.Chat)
                .WithMany(c => c.Subscribes)
                .HasForeignKey(s => s.ChatId);

            modelBuilder.Entity<MessageError>()
                .HasKey(f => f.Id);
        }
    }
}

