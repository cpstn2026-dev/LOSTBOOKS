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
        public async Task<IActionResult> Index(string searchString)
        {
            var consignors = _context.Consignors.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                consignors = consignors.Where(c =>
                    c.ConsignorID.ToString().Contains(searchString) ||
                    c.ConsignorName.Contains(searchString) ||
                    c.ContactNumber.ToString().Contains(searchString) ||
                    c.EmailAddress.Contains(searchString) ||
                    c.HomeAddress.Contains(searchString) ||
                    c.GcashNumber.Contains(searchString) ||
                    (c.BankName != null && c.BankName.Contains(searchString)) ||
                    (c.BankAccountNumber != null && c.BankAccountNumber.Contains(searchString)) ||
                    (c.AccountName != null && c.AccountName.Contains(searchString))
                );
            }

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

            return View(consignor); // FIX: show details instead of redirect
        }

        // GET: Consignors/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Consignors/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ConsignorID,ConsignorName,ContactNumber,EmailAddress,HomeAddress,GcashNumber,BankName,BankAccountNumber,AccountName")] Consignor consignor)
        {
            if (ModelState.IsValid)
            {
                _context.Add(consignor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // IMPORTANT: return view so errors + input stays
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

            return View(consignor); // FIX: must return View, not redirect
        }

        // POST: Consignors/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ConsignorID,ConsignorName,ContactNumber,EmailAddress,HomeAddress,GcashNumber,BankName,BankAccountNumber,AccountName")] Consignor consignor)
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

        // GET: Consignors/Delete/5
        public async Task<IActionResult> Delete(int? id)
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

            return View(consignor); // FIX: must show confirmation page
        }

        // POST: Consignors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var consignor = await _context.Consignors.FindAsync(id);

            if (consignor != null)
            {
                _context.Consignors.Remove(consignor);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ConsignorExists(int id)
        {
            return _context.Consignors.Any(e => e.ConsignorID == id);
        }
    }
}