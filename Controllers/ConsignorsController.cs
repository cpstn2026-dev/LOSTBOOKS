using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LOSTBOOKS.Data;
using LOSTBOOKS.Models;

namespace LOSTBOOKS.Controllers
{
    public class ConsignorsController : Controller
    {
        private readonly LOSTBOOKSContext _context;

        public ConsignorsController(LOSTBOOKSContext context)
        {
            _context = context;
        }

        // GET: Consignors
        public async Task<IActionResult> Index(string searchString, bool showInactive = false)
        {
            var consignors = _context.Consignors.AsQueryable();

            // NEW: by default, hide deactivated consignors
            // (showInactive=true shows everyone, active + inactive)
            if (!showInactive)
            {
                consignors = consignors.Where(c => c.IsActive);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                consignors = consignors.Where(c =>
                    c.ConsignorID.ToString().Contains(searchString) ||
                    c.ConsignorName.Contains(searchString) ||
                    c.ContactNumber.ToString().Contains(searchString) ||
                    c.EmailAddress.Contains(searchString)
                );
            }

            ViewBag.ShowInactive = showInactive;

            return View(await consignors.ToListAsync());
        }

        // GET: Consignors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var consignor = await _context.Consignors
                .FirstOrDefaultAsync(m => m.ConsignorID == id);

            if (consignor == null)
            {
                return NotFound();
            }

            return View(consignor);
        }

        // GET: Consignors/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Consignors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ConsignorID,ConsignorName,ContactNumber,EmailAddress")] Consignor consignor)
        {
            if (ModelState.IsValid)
            {
                _context.Add(consignor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(consignor);
        }

        // GET: Consignors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var consignor = await _context.Consignors.FindAsync(id);

            if (consignor == null)
            {
                return NotFound();
            }

            return View(consignor);
        }

        // POST: Consignors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ConsignorID,ConsignorName,ContactNumber,EmailAddress,IsActive")] Consignor consignor)
        {
            if (id != consignor.ConsignorID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(consignor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ConsignorExists(consignor.ConsignorID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            return View(consignor);
        }

        // =====================================================
        // DEACTIVATE (replaces hard Delete)
        // =====================================================

        // GET: Consignors/Deactivate/5
        public async Task<IActionResult> Deactivate(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var consignor = await _context.Consignors
                .FirstOrDefaultAsync(m => m.ConsignorID == id);

            if (consignor == null)
            {
                return NotFound();
            }

            return View(consignor);
        }

        // POST: Consignors/Deactivate/5
        [HttpPost, ActionName("Deactivate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateConfirmed(int id)
        {
            var consignor = await _context.Consignors.FindAsync(id);

            if (consignor != null)
            {
                consignor.IsActive = false;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // REACTIVATE
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(int id)
        {
            var consignor = await _context.Consignors.FindAsync(id);

            if (consignor != null)
            {
                consignor.IsActive = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ConsignorExists(int id)
        {
            return _context.Consignors.Any(e => e.ConsignorID == id);
        }
    }
}