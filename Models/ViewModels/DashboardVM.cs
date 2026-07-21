namespace SchoolFeesSystem.Models.ViewModels
{
    public class DashboardVM
    {
        public decimal TotalCollected { get; set; }
        public decimal TotalOutstanding { get; set; }
        public int TotalStudents { get; set; }
        public int FullyPaidStudents { get; set; }

        public List<RecentPaymentVM> RecentPayments { get; set; }

        // For chart
        public List<string> Months { get; set; }  // Jan, Feb, Mar...
        public List<decimal> MonthlyCollections { get; set; }  // GHS collected per month

        // NEW: Selected filters
        public string SelectedTerm { get; set; }
        public string SelectedAcademicYear { get; set; }
        public decimal DailyTotal { get; internal set; }
    }

    public class RecentPaymentVM
    {
        public string StudentName { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime PaymentDate { get; set; }
        public string ClassName { get; set; }
    }


}
