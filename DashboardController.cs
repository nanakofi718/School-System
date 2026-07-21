using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolFeesSystem.Data;
using SchoolFeesSystem.Models.ViewModels;
using System;
using ClosedXML.Excel;
using System.IO;

namespace SchoolFeesSystem.Controllers
{
    [RoleAuthorize("Accountant", "Admin")]
    public class DashboardController : Controller
    {
        private readonly SchoolDbContext _context;
        private readonly IConverter _converter;

        public DashboardController(SchoolDbContext context, IConverter converter)
        {
            _context = context;
            _converter = converter;
        }
        public async Task<IActionResult> Index(string term, string academicYear)
        {
            var payments = _context.Payments
                .Include(p => p.Student)
                .Include(p => p.Fee)
                .ThenInclude(f => f.Class)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(term))
                payments = payments.Where(p => p.Fee.Term == term);

            if (!string.IsNullOrEmpty(academicYear))
                payments = payments.Where(p => p.Fee.AcademicYear == academicYear);

            var totalCollected = await payments.SumAsync(p => p.AmountPaid);
            var totalStudents = await _context.Students.CountAsync();

            var fullyPaidStudentsList = await _context.Students
                .Include(s => s.Class)
                .Select(s => new
                {
                    s.StudentId,
                    TotalFee = _context.Fees
                        .Where(f => f.ClassId == s.ClassId &&
                                    (string.IsNullOrEmpty(term) || f.Term == term) &&
                                    (string.IsNullOrEmpty(academicYear) || f.AcademicYear == academicYear))
                        .Sum(f => (decimal?)f.Amount) ?? 0,
                    TotalPaid = _context.Payments
                        .Where(p => p.StudentId == s.StudentId &&
                                    (string.IsNullOrEmpty(term) || p.Fee.Term == term) &&
                                    (string.IsNullOrEmpty(academicYear) || p.Fee.AcademicYear == academicYear))
                        .Sum(p => (decimal?)p.AmountPaid) ?? 0
                })
                .ToListAsync();

            var fullyPaidStudents = fullyPaidStudentsList.Count(x => x.TotalPaid >= x.TotalFee);

            var recentPayments = await payments
                .OrderByDescending(p => p.PaymentDate)
                .Take(5)
                .Select(p => new RecentPaymentVM
                {
                    StudentName = p.Student.FullName,
                    ClassName = p.Fee.Class.ClassName,
                    AmountPaid = p.AmountPaid,
                    PaymentDate = p.PaymentDate
                })
                .ToListAsync();

            // 1. Filter payments for the current year to keep the chart relevant [cite: 132-135]
            var currentYear = DateTime.Now.Year;

            // 2. Group the existing 'payments' variable by month [cite: 143-144]
            var monthlyGroups = payments
                .Where(p => p.PaymentDate.Year == currentYear)
                .GroupBy(p => p.PaymentDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Total = g.Sum(p => p.AmountPaid)
                })
                .ToList();

            // 3. Generate the list of month names (Jan, Feb, Mar...) [cite: 145]
            var months = Enumerable.Range(1, 12)
                .Select(i => new DateTime(currentYear, i, 1).ToString("MMM"))
                .ToList();

            // 4. Map the groups to the 12 months, filling missing months with 0 [cite: 146-147]
            var monthlyCollections = Enumerable.Range(1, 12)
                .Select(m => (decimal)(monthlyGroups.FirstOrDefault(x => x.Month == m)?.Total ?? 0))
                .ToList();

            // 1. TOTAL REVENUE (The sum of all fees that SHOULD be paid by all students)
            var totalRevenue = await _context.Students
                .SelectMany(s => _context.Fees
                    .Where(f => f.ClassId == s.ClassId &&
                                (string.IsNullOrEmpty(term) || f.Term == term) &&
                                (string.IsNullOrEmpty(academicYear) || f.AcademicYear == academicYear))
                    .Select(f => f.Amount))
                .SumAsync();

            // 2. ACTUAL CASH COLLECTED (Money already paid)
            var actualCashCollected = await _context.Payments
                .Where(p => (string.IsNullOrEmpty(term) || p.Fee.Term == term) &&
                            (string.IsNullOrEmpty(academicYear) || p.Fee.AcademicYear == academicYear))
                .SumAsync(p => p.AmountPaid);

            // 3. OUTSTANDING FEES (Money still left to collect)
            var totalOutstanding = totalRevenue - actualCashCollected;

            // 4. DAILY TOTAL (Money collected today)
            var dailyTotal = await _context.Payments
                .Where(p => p.PaymentDate.Date == DateTime.Today)
                .SumAsync(p => p.AmountPaid);



            // 5. Assign these to your ViewModel
            var vm = new DashboardVM
            {
                // Total Revenue is all the money actually paid by parents
                TotalCollected = totalRevenue,

                // Total Outstanding is Total Debt minus Total Collected
                TotalOutstanding = totalOutstanding,

                TotalStudents = totalStudents,
                FullyPaidStudents = fullyPaidStudents,
                RecentPayments = recentPayments,
                Months = months,
                MonthlyCollections = monthlyCollections,
                SelectedTerm = term,
                SelectedAcademicYear = academicYear,
                DailyTotal = dailyTotal
            };

            return View(vm);

        }

        // Add this method to your DashboardController.cs
        [HttpGet]
        public async Task<IActionResult> ExportToExcel(string term, string academicYear)
        {
            var paymentsQuery = _context.Payments
                .Include(p => p.Student)
                .Include(p => p.Fee)
                .ThenInclude(f => f.Class)
                .AsQueryable();

            if (!string.IsNullOrEmpty(term))
                paymentsQuery = paymentsQuery.Where(p => p.Fee.Term == term);

            if (!string.IsNullOrEmpty(academicYear))
                paymentsQuery = paymentsQuery.Where(p => p.Fee.AcademicYear == academicYear);

            var payments = await paymentsQuery.ToListAsync();

            // 2. Create the Excel Workbook
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Finance Report");
                var currentRow = 1;

                // Header Row
                worksheet.Cell(currentRow, 1).Value = "Student Name";
                worksheet.Cell(currentRow, 2).Value = "Class";
                worksheet.Cell(currentRow, 3).Value = "Term";
                worksheet.Cell(currentRow, 4).Value = "Year";
                worksheet.Cell(currentRow, 5).Value = "Amount (GHS)";
                worksheet.Cell(currentRow, 6).Value = "Date";

                // Style the header
                var headerRange = worksheet.Range(1, 1, 1, 6);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                // 3. Fill the Data [cite: 166-168]
                foreach (var payment in payments)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = payment.Student.FullName;
                    worksheet.Cell(currentRow, 2).Value = payment.Fee.Class.ClassName;
                    worksheet.Cell(currentRow, 3).Value = payment.Fee.Term;
                    worksheet.Cell(currentRow, 4).Value = payment.Fee.AcademicYear;
                    worksheet.Cell(currentRow, 5).Value = payment.AmountPaid;
                    worksheet.Cell(currentRow, 6).Value = payment.PaymentDate.ToString("dd/MM/yyyy");
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"FinanceReport_{term}_{academicYear}.xlsx");
                }
            }
        }
    }
}
