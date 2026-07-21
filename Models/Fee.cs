namespace SchoolFeesSystem.Models
{
    public class Fee
    {
        public int FeeId { get; set; }

        public int ClassId { get; set; }
        public virtual Class Class { get; set; }

        public decimal Amount { get; set; }
        public string? Term { get; set; }
        public string? AcademicYear { get; set; }

        public ICollection<Payment>? Payments { get; set; }

        //public virtual Class Class { get; set; }

    }
}
