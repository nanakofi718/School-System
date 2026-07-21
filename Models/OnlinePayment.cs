namespace SchoolFeesSystem.Models
{
    public class OnlinePayment
    {
        public int Id { get; set; }
        public string Reference { get; set; } // Paystack Ref
        public decimal Amount { get; set; }
        public int StudentId { get; set; }
        public string Status { get; set; } // Pending, Success, Failed
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
