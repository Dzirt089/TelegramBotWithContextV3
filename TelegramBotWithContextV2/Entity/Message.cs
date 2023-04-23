namespace TelegramBotWithContextV2.Entity
{
    public class Message
    {
        public long Id { get; set; }
        public long ChatId { get; set; }
        public string? Text { get; set; }
        public DateTime? Data { get; set; }
        public string? languageCode { get; set; }
        public Chat Chat { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }
}
