namespace SchoolFeesSystem.Models.ViewModels
{
    public class StudentStatementVM
    {
        public string StudentName { get; set; }
        public string ClassName { get; set; }
        public string Term { get; set; }
        public string AcademicYear { get; set; }

        public decimal TotalFee { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Balance { get; set; }

        public List<StudentPaymentRowVM> Payments { get; set; }
    }

    public class StudentPaymentRowVM
    {
        public DateTime Date { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal BalanceAfter { get; set; }
        public string Method { get; set; }
        public string Reference { get; set; }
        public string StudentName { get; internal set; }
        public string PaymentMethod { get; internal set; }
    }
}
