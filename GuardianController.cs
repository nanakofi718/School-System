using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolFeesSystem.Data; // Ensure this matches your namespace
using SchoolFeesSystem.Models;
using SchoolFeesSystem.Services;
using System.Net.Http;

namespace SchoolFeesSystem.Controllers
{
    [Authorize(Roles = "Guardian")]
    public class GuardianController : Controller
    {
        private readonly SchoolDbContext _context;
        private readonly SmsService _smsService;

        public GuardianController(SchoolDbContext context, SmsService smsService)
        {
            _context = context;
            _smsService = smsService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var username = HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // Fetch children linked to this guardian account
            // We include Fees so the view can calculate the balance
            var myChildren = await _context.Students
                .Include(s => s.Class)
                    .ThenInclude(c => c.Fees)
                .Include(s => s.Payments)
                .Include(s => s.Guardians)
                .Where(s => s.Guardians.Any(g => g.Email == username || (g.User != null && g.User.Username == username)))
                .ToListAsync();

            return View(myChildren);
        }

        [HttpPost]
        public async Task<IActionResult> InitializePayment(int studentId, decimal amount)
        {
            var email = HttpContext.Session.GetString("Username") ?? "guardian@school.com";
            string secretKey = "sk_live_cc6126fba4b168810b09d81529bf000b33b35322";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {secretKey}");

                var payload = new
                {
                    email = email,
                    amount = (int)(amount * 100),
                    callback_url = Url.Action("VerifyPayment", "Guardian", new { studentId = studentId }, Request.Scheme),
                    metadata = new { student_id = studentId }
                };

                var response = await client.PostAsJsonAsync("https://api.paystack.co/transaction/initialize", payload);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Instead of Redirect(authUrl), return the JSON to the browser
                    return Content(jsonResponse, "application/json");
                }
            }

            return BadRequest(new { message = "Could not initialize payment" });
        }

        [HttpGet]
        public async Task<IActionResult> VerifyPayment(string reference, int studentId)
        {
            using (var client = new HttpClient())
            {
                string secretKey = "sk_live_cc6126fba4b168810b09d81529bf000b33b35322";
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {secretKey}");

                var response = await client.GetAsync($"https://api.paystack.co/transaction/verify/{reference}");
                var jsonResponse = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(jsonResponse);
                    var root = doc.RootElement;

                    bool isStatusTrue = root.GetProperty("status").GetBoolean();
                    string transactionStatus = root.GetProperty("data").GetProperty("status").GetString();

                    if (isStatusTrue && transactionStatus == "success")
                    {
                        long amountInPesewas = root.GetProperty("data").GetProperty("amount").GetInt64();
                        decimal amountPaid = (decimal)amountInPesewas / 100;

                        bool alreadyExists = await _context.Payments.AnyAsync(p => p.ReferenceNumber == reference);
                        if (alreadyExists) return RedirectToAction("Dashboard");

                        // --- DYNAMIC BALANCE CALCULATION ---

                        // 1. Fetch Student with Class and current Payments
                        var studentInfo = await _context.Students
                            .Include(s => s.Class)
                            .Include(s => s.Payments)
                            .FirstOrDefaultAsync(s => s.StudentId == studentId);

                        // 2. Calculate Total Fees for this student's class
                        decimal totalFees = await _context.Fees
                            .Where(f => f.ClassId == studentInfo.ClassId)
                            .SumAsync(f => f.Amount);

                        // 3. Calculate Total Payments made by this student (before this current one)
                        decimal totalPaidSoFar = studentInfo.Payments.Sum(p => p.AmountPaid);

                        // 4. Calculate the new BalanceAfter
                        // Balance = Total Owed - (What was paid before + What is being paid now)
                        decimal newBalance = totalFees - (totalPaidSoFar + amountPaid);

                        var firstFee = await _context.Fees
                            .Where(f => f.ClassId == studentInfo.ClassId)
                            .FirstOrDefaultAsync();

                        if (firstFee == null)
                        {
                            TempData["Error"] = "Payment failed: No fees set for this class.";
                            return RedirectToAction("Dashboard");
                        }

                        // 5. Create and Save Payment Record
                        var payment = new Payment
                        {
                            StudentId = studentId,
                            FeeId = firstFee.FeeId,
                            AmountPaid = amountPaid,
                            DatePaid = DateTime.Now,
                            PaymentDate = DateTime.Now,
                            PaymentMethod = SchoolFeesSystem.Models.PaymentMethod.Online,
                            Reference = reference,
                            ReferenceNumber = reference,
                            BalanceAfter = newBalance // <--- Now dynamic!
                        };

                        _context.Payments.Add(payment);
                        await _context.SaveChangesAsync();

                        // --- SMS LOGIC ---
                        var primaryGuardian = studentInfo.Guardians?.FirstOrDefault();
                        if (primaryGuardian != null && !string.IsNullOrEmpty(primaryGuardian.Phone))
                        {
                            string msg = $"Payment Successful!\nStudent: {studentInfo.FullName}\nAmount: GHS {amountPaid:N2}\nRemaining Balance: GHS {newBalance:N2}\nRef: {reference}";
                            await _smsService.SendSmsAsync(primaryGuardian.Phone, msg);
                            TempData["Success"] = $"GHS {amountPaid:N2} payment confirmed. Remaining Balance: GHS {newBalance:N2}";
                        }
                    }
                }
            }
            return RedirectToAction("Dashboard");
        }
        public async Task<IActionResult> StudentDetails(int id)
        {
            var username = HttpContext.Session.GetString("Username");

            var student = await _context.Students
                .Include(s => s.Class)
                .Include(s => s.Payments)
                .Include(s => s.Guardians)
                .FirstOrDefaultAsync(s => s.StudentId == id &&
                     s.Guardians.Any(g => g.Email == username || (g.User != null && g.User.Username == username)));

            if (student == null) return Forbid();

            // Fetch class fees separately to show the parent what they are paying for
            ViewBag.Fees = await _context.Fees.Where(f => f.ClassId == student.ClassId).ToListAsync();

            return View(student);
        }
    }
}