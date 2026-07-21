using BCrypt.Net; // Make sure BCrypt.Net-Next package is installed
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolFeesSystem.Data;
using SchoolFeesSystem.Models;
using SchoolFeesSystem.Models.ViewModels;
using System.Security.Claims;

public class AccountController : Controller
{
    private readonly SchoolDbContext _context;

    public AccountController(SchoolDbContext context)
    {
        _context = context;
    }

    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == username);

        // 1. Validate Credentials (If user not found or password doesn't match)
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            ViewBag.Error = "Invalid username or password";
            return View();
        }

        // --- NEW: OFFICIAL SIGN-IN LOGIC ---
        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, user.Role.RoleName),
        new Claim("UserId", user.UserId.ToString())
    };

        var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
        };

        await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

        // 2. Store Session Data
        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetString("Role", user.Role.RoleName);

        // --- FIX: Check for First Login AFTER successful verification ---
        if (user.IsFirstLogin)
        {
            TempData["Info"] = "For your security, please change your temporary password.";
            return RedirectToAction("ChangePassword", "Account");
        }

        // 3. Multi-Portal Redirect Logic
        switch (user.Role.RoleName)
        {
            case "Guardian":
                var guardian = await _context.Guardians.FirstOrDefaultAsync(g => g.UserId == user.UserId);
                if (guardian != null)
                {
                    HttpContext.Session.SetInt32("GuardianId", guardian.GuardianId);
                }
                return RedirectToAction("Dashboard", "Guardian");

            case "Staff":
                return RedirectToAction("MyClass", "Staff");

            case "Admin":
            case "Accountant":
                return RedirectToAction("Index", "Dashboard");

            default:
                return RedirectToAction("Index", "Home");
        }
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        // Ensure the user is logged in via session 
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
            return RedirectToAction("Login");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordVM model)
    {
        if (!ModelState.IsValid) return View(model);

        var username = HttpContext.Session.GetString("Username");
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

        if (user == null) return RedirectToAction("Login");

        // Verify the current password hash 
        if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
        {
            ModelState.AddModelError("CurrentPassword", "Incorrect current password.");
            return View(model);
        }

        // 1. Hash the new password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

        // 2. FIX: Mark that they have now completed their first login setup
        user.IsFirstLogin = false;

        _context.Update(user);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Password changed successfully!";

        // 3. Redirect based on Role (Guardians go to Guardian Dashboard, Admin to Admin)
        var role = HttpContext.Session.GetString("Role");
        if (role == "Guardian")
        {
            return RedirectToAction("Dashboard", "Guardian");
        }

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost] // It's safer to use Post for Logout to prevent accidental logouts
    public async Task<IActionResult> Logout()
    {
        // 1. Delete the Authentication Cookie
        // Use the same name "CookieAuth" you used in your Login method
        await HttpContext.SignOutAsync("CookieAuth");

        // 2. Clear all Session data (Username, Role, etc.)
        HttpContext.Session.Clear();

        // 3. Redirect to Login page
        return RedirectToAction("Login", "Account");
    }

    public IActionResult AccessDenied()
    {
        return View();
    }
}
