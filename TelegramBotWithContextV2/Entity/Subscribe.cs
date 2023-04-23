using System.ComponentModel.DataAnnotations;
using Yandex.Checkout.V3;

namespace TelegramBotWithContextV2.Entity
{
    public class Subscribe
    {
        [Key]
        public string PaymentId { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public DateTime DataCreated { get; set; }
        public decimal Amount { get; set; }
        public long ChatId { get; set; }
        public SubType SubscriptionType { get; set; }
        public string Current { get; set; }
        public Chat Chat { get; set; }

    }
}
