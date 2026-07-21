using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolFeesSystem.Data;
using SchoolFeesSystem.Models.ViewModels; // Ensure your VM is here

namespace SchoolFeesSystem.Controllers
{
    public class StaffController : Controller
    {
        private readonly SchoolDbContext _context;

        public StaffController(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> MyClass()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Account");

            // 1. Find the staff and their assigned class
            var staffMember = await _context.Users
                .Include(u => u.Class)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (staffMember?.ClassId == null) return View("NoClassAssigned");

            // 2. Fetch students with a calculated balance
            var students = await _context.Students
                .Where(s => s.ClassId == staffMember.ClassId)
                .Select(s => new StudentStatusVM
                {
                    FullName = s.FullName,
                    TotalPaid = s.Payments.Sum(p => (decimal?)p.AmountPaid) ?? 0,
                    // Pull the total fees for this specific class
                    TotalBilled = _context.Fees
                        .Where(f => f.ClassId == s.ClassId)
                        .Sum(f => (decimal?)f.Amount) ?? 0
                })
                .ToListAsync();

            ViewBag.ClassName = staffMember.Class.ClassName;
            return View(students);
        }

        public async Task<IActionResult> ViewStudent(int id)
        {
            var username = HttpContext.Session.GetString("Username");

            // 1. Find the staff member's assigned class
            var staff = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            // 2. Fetch the student ONLY if they are in the staff's class
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == id && s.ClassId == staff.ClassId);

            if (student == null)
            {
                return Unauthorized("You are not authorized to view students outside your assigned class.");
            }

            return View(student);
        }
    }
}