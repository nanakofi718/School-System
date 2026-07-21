using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolFeesSystem.Data;
using SchoolFeesSystem.Models;

namespace SchoolFeesSystem.Controllers
{
    [RoleAuthorize("Admin","Accountant","Staff")]
    public class FeesController : Controller
    {
        private readonly SchoolDbContext _context;

        public FeesController(SchoolDbContext context)
        {
            _context = context;
        }

        // GET: Fees
        public async Task<IActionResult> Index()
        {
            // Use .Include(f => f.Class) to join the tables
            var fees = await _context.Fees
                .Include(f => f.Class)
                .OrderByDescending(f => f.AcademicYear)
                .ToListAsync();

            return View(fees);
        }

        // GET: Fees/Create
        public IActionResult Create()
        {
            ViewData["ClassId"] = new SelectList(_context.Classes, "ClassId", "ClassName");
            return View();
        }

        // POST: Fees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Fee fee)
        {
            // IMPORTANT: Remove 'Class' from validation. 
            // The form sends ClassId (the number), not the whole Class object.
            ModelState.Remove("Class");

            // Check for duplicates
            bool exists = await _context.Fees.AnyAsync(f =>
                f.ClassId == fee.ClassId &&
                f.Term == fee.Term &&
                f.AcademicYear == fee.AcademicYear);

            if (exists)
            {
                ModelState.AddModelError("", "A fee record already exists for this class, term, and academic year.");
            }

            if (ModelState.IsValid)
            {
                _context.Fees.Add(fee);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Fee structure updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            // If we reach here, validation failed. Reload the dropdown.
            ViewData["ClassId"] = new SelectList(_context.Classes, "ClassId", "ClassName", fee.ClassId);
            return View(fee);
        }

        // GET: Fees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var fee = await _context.Fees.FindAsync(id);
            if (fee == null) return NotFound();

            ViewData["ClassId"] = new SelectList(_context.Classes, "ClassId", "ClassName", fee.ClassId);
            return View(fee);
        }

        // POST: Fees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Fee fee)
        {
            if (id != fee.FeeId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(fee);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FeeExists(fee.FeeId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(fee);
        }

        // POST: Fees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var fee = await _context.Fees.FindAsync(id);
            if (fee != null)
            {
               // Check if payments exist before deleting [cite: 196-197]
                bool hasPayments = await _context.Payments.AnyAsync(p => p.FeeId == id);
                if (hasPayments)
                {
                    TempData["Error"] = "Cannot delete fee. Payments have already been recorded against it.";
                    return RedirectToAction(nameof(Index));
                }
                _context.Fees.Remove(fee);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool FeeExists(int id) => _context.Fees.Any(e => e.FeeId == id);

    }
}
