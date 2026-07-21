namespace SchoolFeesSystem.Models.ViewModels
{
    public class ReceiptVM
    {
        public string StudentName { get; set; }
        public string ClassName { get; set; }
        public string Term { get; set; }
        public string AcademicYear { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal BalanceAfter { get; set; }
        public DateTime PaymentDate { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string Reference { get; set; }
        public string SchoolName { get; internal set; }
        public string SchoolAddress { get; internal set; }
        public string SchoolPhone { get; internal set; }
        public string LogoPath { get; internal set; }
    }
}
