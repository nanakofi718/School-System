using System;
using System.Collections.Generic;

namespace SchoolFeesSystem.Models.ViewModels
{
    public class MonthlyReportVM
    {
        public string Term { get; set; }
        public string AcademicYear { get; set; }
        public string ClassName { get; set; }
        public decimal TotalCollected { get; set; }
        public List<MonthlyPaymentRowVM> Payments { get; set; }
    }

    public class MonthlyPaymentRowVM
    {
        public string StudentName { get; set; }
        public decimal AmountPaid { get; set; }
        public string PaymentMethod { get; set; }
        public string Reference { get; set; }

        // Rename Date to PaymentDate to avoid ambiguity
        public string PaymentDate { get; set; }
        public DateTime Date { get; internal set; }
    }
}
