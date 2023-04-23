using Yandex.Checkout.V3;

namespace TelegramBotWithContextV2.Entity
{
    public class Chat
    {
        public long Id { get; set; }
        public ICollection<Subscribe> Subscribes { get; set; } = new List<Subscribe>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
        public SubType SubscriptionType { get; set; }
        public DateTime SubscriptionDate { get; set; }
        public int FreeRequestsCount { get; set; } = 10; // количество бесплатных запросов
        public int FreeImagesCount { get; set; } = 5; // количество бесплатных картинок
        public string? TempPaymentId { get; set; }
        public SubType TempSub { get; set; }
        public string? TempCurrent { get; set; }
        public PaymentStatus TempPaymentStatus { get; set; }
        public int Invitations { get; set;} = 0;
    }
}
