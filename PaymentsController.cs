using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolFeesSystem.Data;
using SchoolFeesSystem.Models;
using SchoolFeesSystem.Models.ViewModels;
using SchoolFeesSystem.Services;

namespace SchoolFeesSystem.Controllers
{
    [RoleAuthorize("Accountant","Admin","Staff", "Guardian")]
    public class PaymentsController : Controller
    {
        private readonly SchoolDbContext _context;
        private readonly SmsService _smsService;

        public PaymentsController(SchoolDbContext context, SmsService smsService)
        {
            _context = context;
            _smsService = smsService;

        }

        // GET: Payments
        public async Task<IActionResult> Index()
        {
            var payments = await _context.Payments
                .Include(p => p.Student)
                .Include(p => p.Fee)
                .ThenInclude(f => f.Class)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return View(payments);
        }

        // GET: Payments/Create
        public IActionResult Create()
        {
            ViewData["StudentId"] = new SelectList(
                _context.Students.Include(s => s.Class),
                "StudentId",
                "FullName"
            );
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int StudentId, string Term, string AcademicYear, decimal AmountPaid, PaymentMethod PaymentMethod, string Reference)
        {
            // 1. Fetch Student first
            var student = await _context.Students
                .Include(s => s.Guardians)
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.StudentId == StudentId);

            // SAFETY CHECK: Check if student exists BEFORE using student.ClassId
            if (student == null)
            {
                return NotFound("Student not found.");
            }

            // 2. Fetch Fee information now that we know student is not null
            var fee = await _context.Fees
                .Where(f => f.ClassId == student.ClassId)
                .FirstOrDefaultAsync();

            if (fee == null)
            {
                TempData["Error"] = "Fee structure not found for this student's class.";
                return RedirectToAction(nameof(Index));
            }

            // 3. DYNAMIC BALANCE CALCULATION
            decimal totalFees = await _context.Fees
                .Where(f => f.ClassId == student.ClassId)
                .SumAsync(f => f.Amount);

            decimal totalPaidSoFar = await _context.Payments
                .Where(p => p.StudentId == StudentId)
                .SumAsync(p => p.AmountPaid);

            decimal currentBalance = totalFees - totalPaidSoFar;
            decimal newBalance = currentBalance - AmountPaid;

            // 4. Create and Save the Payment
            var payment = new Payment
            {
                StudentId = StudentId,
                FeeId = fee.FeeId,
                AmountPaid = AmountPaid,
                PaymentMethod = PaymentMethod,
                Reference = Reference,
                ReferenceNumber = Reference,
                PaymentDate = DateTime.Now,
                DatePaid = DateTime.Now,
                BalanceAfter = newBalance
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // 5. Send SMS via Arkesel
            var guardian = student.Guardians.FirstOrDefault();
            if (guardian != null && !string.IsNullOrEmpty(guardian.Phone))
            {
                try
                {
                    string message = $"Payment Received!\n" +
                                     $"Student: {student.FullName}\n" +
                                     $"Amount: GHS {AmountPaid:N2}\n" +
                                     $"New Balance: GHS {newBalance:N2}\n" +
                                     $"Ref: {Reference}\n" +
                                     $"Thank you.";

                    await _smsService.SendSmsAsync(guardian.Phone, message);
                }
                catch (Exception)
                {
                    TempData["Warning"] = "Payment saved, but SMS notification failed.";
                }
            }

            TempData["Success"] = "Payment recorded successfully!";
            return RedirectToAction(nameof(Index));
        }

        private async Task<decimal> GetOutstandingBalance(int studentId, int feeId)
        {
            var fee = await _context.Fees.FindAsync(feeId);

            var totalPaid = await _context.Payments
                .Where(p => p.StudentId == studentId && p.FeeId == feeId)
                .SumAsync(p => p.AmountPaid);

            return fee.Amount - totalPaid;
        }
        public async Task<IActionResult> Receipt(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Student)
                .Include(p => p.Fee)
                .ThenInclude(f => f.Class)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            var school = await _context.SchoolInfos.FirstOrDefaultAsync();

            if (payment == null) return NotFound();

            var receipt = new ReceiptVM
            {
                StudentName = payment.Student.FullName,
                ClassName = payment.Fee.Class.ClassName,
                Term = payment.Fee.Term,
                AcademicYear = payment.Fee.AcademicYear,
                AmountPaid = payment.AmountPaid,
                BalanceAfter = payment.BalanceAfter,
                PaymentDate = payment.PaymentDate,
                PaymentMethod = payment.PaymentMethod,
                Reference = payment.Reference,
                SchoolName = school.SchoolName,
                SchoolAddress = school.Address,
                SchoolPhone = school.Phone,
                LogoPath = school.LogoPath
            };

            return View(receipt);
        }

        public async Task<IActionResult> Statement(int studentId, string term, string academicYear)
        {
            var student = await _context.Students
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null) return NotFound();

            var fee = await _context.Fees.FirstOrDefaultAsync(f =>
                f.ClassId == student.ClassId &&
                f.Term == term &&
                f.AcademicYear == academicYear);

            if (fee == null) return NotFound("Fee not set");

            var payments = await _context.Payments
                .Where(p => p.StudentId == studentId && p.FeeId == fee.FeeId)
                .OrderBy(p => p.PaymentDate)
                .ToListAsync();

            var totalPaid = payments.Sum(p => p.AmountPaid);
            var balance = fee.Amount - totalPaid;

            var vm = new StudentStatementVM
            {
                StudentName = student.FullName,
                ClassName = student.Class.ClassName,
                Term = term,
                AcademicYear = academicYear,
                TotalFee = fee.Amount,
                TotalPaid = totalPaid,
                Balance = balance,
                Payments = payments.Select(p => new StudentPaymentRowVM
                {
                    Date = p.PaymentDate,
                    AmountPaid = p.AmountPaid,
                    BalanceAfter = p.BalanceAfter,
                    Method = p.PaymentMethod.ToString(),
                    Reference = p.Reference
                }).ToList()
            };

            return View(vm);
        }
        [HttpPost]
        public async Task<IActionResult> ProcessNotifications()
        {
            var pending = await _context.Notifications
                .Where(n => n.Status == "Pending")
                .ToListAsync();

            foreach (var note in pending)
            {
                // Here is where you would call an API like Twilio or Hubtel [cite: 17-19]
               // Example: await _smsService.SendAsync(note.Student.Guardians.First().Phone, note.MessageText);

                note.Status = "Sent";
                note.SentDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Notifications", "Reports");
        }

    }

}

    
