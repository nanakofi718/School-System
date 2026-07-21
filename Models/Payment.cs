namespace SchoolFeesSystem.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }

        public int StudentId { get; set; }
        public Student Student { get; set; }

        public int FeeId { get; set; }
        public Fee Fee { get; set; }

        public decimal AmountPaid { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public PaymentMethod PaymentMethod { get; set; }
        public string Reference { get; set; }
        public decimal BalanceAfter { get; set; }
        public DateTime DatePaid { get; internal set; }
        public string ReferenceNumber { get; internal set; }
    }
}
