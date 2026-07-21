using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolFeesSystem.Data;
using SchoolFeesSystem.Models;
using SchoolFeesSystem.Models.ViewModels;
using SchoolFeesSystem.Services;
using SchoolFeesSystem.ViewModels;

namespace SchoolFeesSystem.Controllers
{
    [Authorize(Roles = "Admin,Accountant,Guardian")]
    public class StudentsController : Controller
    {
        private readonly SchoolDbContext _context;
        private readonly SmsService _smsService;

        public StudentsController(SchoolDbContext context, SmsService smsService)
        {
            _context = context;
            _smsService = smsService;
        }

        public async Task<IActionResult> Index()
        {
            var students = await _context.Students
                                .Include(s => s.Class)
                                .ToListAsync();
            return View(students);
        }

        public IActionResult Create()
        {
            ViewData["ClassId"] = new SelectList(_context.Classes, "ClassId", "ClassName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student student)
        {
            // 1. CLEAR VALIDATION: Ignore objects we handle manually
            ModelState.Remove("Class");
            ModelState.Remove("PhotoPath");
            ModelState.Remove("Payments");
            ModelState.Remove("Guardians");

            if (ModelState.IsValid)
            {
                // 2. IMAGE UPLOAD
                if (student.PhotoFile != null)
                {
                    string folder = "images/students/";
                    string fileName = Guid.NewGuid().ToString() + "_" + student.PhotoFile.FileName;
                    string serverPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folder, fileName);

                    Directory.CreateDirectory(Path.GetDirectoryName(serverPath));

                    using (var stream = new FileStream(serverPath, FileMode.Create))
                    {
                        await student.PhotoFile.CopyToAsync(stream);
                    }
                    student.PhotoPath = "/" + folder + fileName;
                }

                // 3. SAVE STUDENT & GUARDIANS
                _context.Students.Add(student);
                await _context.SaveChangesAsync();
                // 4. AUTO-CREATE GUARDIAN LOGIN ACCOUNTS & SEND SMS
                if (student.Guardians != null && student.Guardians.Any())
                {
                    foreach (var guardian in student.Guardians)
                    {
                        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == guardian.Email);
                        var guardianRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Guardian");

                        if (existingUser == null && !string.IsNullOrEmpty(guardian.Email))
                        {
                            var guardianUser = new User
                            {
                                FullName = guardian.FullName,
                                Username = guardian.Email,
                                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Parent123"),
                                RoleId = guardianRole.RoleId,
                                IsActive = true,
                                IsFirstLogin = true
                            };

                            _context.Users.Add(guardianUser);
                            await _context.SaveChangesAsync();

                            guardian.UserId = guardianUser.UserId;
                            _context.Update(guardian);

                            // --- SMS TRIGGER: Notify Parent of Login Details ---
                            if (!string.IsNullOrEmpty(guardian.Phone))
                            {
                                string message = $"Hello {guardian.FullName}, an account has been created for you at {student.Class?.ClassName}. \n" +
                                                 $"Login: {guardian.Email}\n" +
                                                 $"Pass: Parent123\n" +
                                                 $"Link: yourschoolsite.com/Account/Login";

                                // This calls your Arkesel service
                                await _smsService.SendSmsAsync(guardian.Phone, message);
                            }
                        }
                        else if (existingUser != null)
                        {
                            guardian.UserId = existingUser.UserId;
                            _context.Update(guardian);
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Student registered! Parent login: Email / Password: Parent123";
                return RedirectToAction(nameof(Index));
            }

            ViewData["ClassId"] = new SelectList(_context.Classes, "ClassId", "ClassName", student.ClassId);
            return View(student);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students
                .Include(s => s.Class)
                .Include(s => s.Guardians)
                .Include(s => s.Payments)
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null) return NotFound();

            var fees = await _context.Fees
                .Where(f => f.ClassId == student.ClassId)
                .OrderByDescending(f => f.AcademicYear)
                .ThenByDescending(f => f.Term)
                .ToListAsync();

            var vm = new StudentDetailsViewModel
            {
                Student = student,
                Fees = fees
            };

            return View(vm);
        }

        public async Task<IActionResult> ClearanceList()
        {
            var students = await _context.Students.Include(s => s.Class).ToListAsync();
            var clearanceData = new List<StudentClearanceVM>();

            foreach (var s in students)
            {
                // Calculate fees and payments accurately
                var totalFee = await _context.Fees.Where(f => f.ClassId == s.ClassId).SumAsync(f => (decimal?)f.Amount) ?? 0;
                var totalPaid = await _context.Payments.Where(p => p.StudentId == s.StudentId).SumAsync(p => (decimal?)p.AmountPaid) ?? 0;

                clearanceData.Add(new StudentClearanceVM
                {
                    StudentId = s.StudentId,
                    FullName = s.FullName,
                    ClassName = s.Class?.ClassName ?? "No Class",
                    TotalFee = totalFee,
                    TotalPaid = totalPaid
                });
            }

            return View(clearanceData);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var student = await _context.Students.Include(s => s.Class).FirstOrDefaultAsync(s => s.StudentId == id);
            if (student == null) return NotFound();

            ViewData["ClassId"] = new SelectList(_context.Classes, "ClassId", "ClassName", student.ClassId);
            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Student student)
        {
            if (id != student.StudentId) return NotFound();

            ModelState.Remove("Class");
            ModelState.Remove("Guardians");
            ModelState.Remove("Payments");

            if (ModelState.IsValid)
            {
                // Handle Photo Update during Edit
                if (student.PhotoFile != null)
                {
                    string folder = "images/students/";
                    string fileName = Guid.NewGuid().ToString() + "_" + student.PhotoFile.FileName;
                    string serverPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folder, fileName);

                    using (var stream = new FileStream(serverPath, FileMode.Create))
                    {
                        await student.PhotoFile.CopyToAsync(stream);
                    }
                    student.PhotoPath = "/" + folder + fileName;
                }

                _context.Update(student);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClassId"] = new SelectList(_context.Classes, "ClassId", "ClassName", student.ClassId);
            return View(student);
        }

        public async Task<IActionResult> PrintClearance(int id)
        {
            var student = await _context.Students
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null) return NotFound();

            var totalFee = await _context.Fees.Where(f => f.ClassId == student.ClassId).SumAsync(f => (decimal?)f.Amount) ?? 0;
            var totalPaid = await _context.Payments.Where(p => p.StudentId == id).SumAsync(p => (decimal?)p.AmountPaid) ?? 0;

            if (totalPaid < totalFee) return Content("Access Denied: Student has outstanding balance.");

            return View(student);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PromoteStudents(int fromClassId, int toClassId)
        {
            var students = await _context.Students.Where(s => s.ClassId == fromClassId).ToListAsync();

            if (!students.Any())
            {
                TempData["Error"] = "No students found in the selected class.";
                return RedirectToAction("Index");
            }

            foreach (var student in students)
            {
                student.ClassId = toClassId;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Successfully promoted {students.Count} students.";
            return RedirectToAction("Index");
        }
    }
}