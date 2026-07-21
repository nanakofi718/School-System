using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolFeesSystem.Data;
using SchoolFeesSystem.Models;
using BCrypt.Net;

namespace SchoolFeesSystem.Controllers
{
    public class SignUpController : Controller
    {
        private readonly SchoolDbContext _context;

        public SignUpController(SchoolDbContext context)
        {
            _context = context;
        }

        // GET: SignUp
        public async Task<IActionResult> Index()
        {
            // Use ViewBag.Roles to match the logical name
            ViewBag.Roles = new SelectList(await _context.Roles.ToListAsync(), "RoleId", "RoleName");
            ViewBag.Classes = new SelectList(await _context.Classes.ToListAsync(), "ClassId", "ClassName");

            return View();
        }

        // POST: SignUp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(string fullName, string username, string password, int roleId, int? classId)
        {
            // 1. Basic Validation
            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || roleId == 0)
            {
                ModelState.AddModelError("", "All fields are required");
                await ReloadLists(roleId, classId);
                return View();
            }

            // 2. Check if username exists
            var exists = await _context.Users.AnyAsync(u => u.Username == username);
            if (exists)
            {
                ModelState.AddModelError("", "Username already taken");
                await ReloadLists(roleId, classId);
                return View();
            }

            // 3. Hash the password
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            // 4. Create User Object
            var user = new User
            {
                FullName = fullName, 
                Username = username,
                PasswordHash = hashedPassword,
                RoleId = roleId,
                ClassId = classId // This will be null for Admins/Guardians
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Account created successfully! Please login.";
            return RedirectToAction("Login", "Account");
        }

        // Helper method to reload dropdowns if there is an error
        private async Task ReloadLists(int selectedRole, int? selectedClass)
        {
            ViewBag.Roles = new SelectList(await _context.Roles.ToListAsync(), "RoleId", "RoleName", selectedRole);
            ViewBag.Classes = new SelectList(await _context.Classes.ToListAsync(), "ClassId", "ClassName", selectedClass);
        }
    }
}