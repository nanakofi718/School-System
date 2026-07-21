using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolFeesSystem.Data;
using SchoolFeesSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace SchoolFeesSystem.Controllers
{
    [Authorize(Roles = "Admin,Accountant")]
    public class ClassesController : Controller
    {
        private readonly SchoolDbContext _context;

        public ClassesController(SchoolDbContext context)
        {
            _context = context;
        }

        // List all classes
        public async Task<IActionResult> Index()
        {
            // Adding .Include ensures the Students list is loaded so your Count() works
            var classes = await _context.Classes
                .Include(c => c.Students)
                .ToListAsync();

            return View(classes);
        }

        // GET: Create Class
        public IActionResult Create() => View();

        // POST: Create Class
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Class model)
        {
            if (ModelState.IsValid)
            {
                _context.Add(model);
                await _context.SaveChangesAsync();

                //  notify the user
                TempData["Success"] = $"Class '{model.ClassName}' has been created successfully!";

                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }
    }
}
    

