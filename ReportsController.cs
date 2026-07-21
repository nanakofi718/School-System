using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolFeesSystem.Data;
using SchoolFeesSystem.Models.ViewModels;

namespace SchoolFeesSystem.Controllers
{
    [RoleAuthorize("Admin","Accountant")]
    public class ReportsController : Controller
    {
        private readonly SchoolDbContext _context;

        public ReportsController(SchoolDbContext context)
        {
            _context = context;
        }

        // GET: Reports/Monthly
        public async Task<IActionResult> Monthly(string term, string academicYear)
        {
            // Default: show all terms if not specified
            var paymentsQuery = _context.Payments
                .Include(p => p.Student)
                .ThenInclude(s => s.Class)
                .Include(p => p.Fee)
                .AsQueryable();

            if (!string.IsNullOrEmpty(term))
            {
                paymentsQuery = paymentsQuery.Where(p => p.Fee.Term == term);
            }

            if (!string.IsNullOrEmpty(academicYear))
            {
                paymentsQuery = paymentsQuery.Where(p => p.Fee.AcademicYear == academicYear);
            }

            var payments = await paymentsQuery
                .OrderBy(p => p.Fee.Class.ClassName)
                .ThenBy(p => p.PaymentDate)
                .ToListAsync();

            // Group by Class + Term + AcademicYear
            var reportData = payments
                .GroupBy(p => new { p.Fee.Term, p.Fee.AcademicYear, p.Fee.Class.ClassName })
                .Select(g => new MonthlyReportVM
                {
                    Term = g.Key.Term,
                    AcademicYear = g.Key.AcademicYear,
                    ClassName = g.Key.ClassName,
                    TotalCollected = g.Sum(x => x.AmountPaid),
                    Payments = g.Select(p => new MonthlyPaymentRowVM
                    {
                        StudentName = p.Student.FullName,
                        AmountPaid = p.AmountPaid,
                        PaymentMethod = p.PaymentMethod.ToString(),
                        Reference = p.Reference,
                        Date = p.PaymentDate
                    }).ToList()
                }).ToList();

            ViewData["SelectedTerm"] = term;
            ViewData["SelectedYear"] = academicYear;

            return View(reportData);
        }

        public async Task<IActionResult> Notifications()
        {
            // Fetch notifications and include student and guardian info [cite: 20-22, 272]
            var notifications = await _context.Notifications
                .Include(n => n.Student)
                .ThenInclude(s => s.Guardians)
                .OrderByDescending(n => n.SentDate)
                .ToListAsync();

            return View(notifications);
        }
    }
}
