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
    public class MerchandisesController : Controller
    {
        private readonly LOSTBOOKSContext _context;

        public MerchandisesController(LOSTBOOKSContext context)
        {
            _context = context;
        }

        // GET: Merchandises
        [HttpGet]
        public async Task<IActionResult> Index(string searchString)
        {
            ViewBag.Consignors = new SelectList(
                _context.Consignors,
                "ConsignorID",
                "ConsignorName"
            );

            var merchandises = _context.Merchandises
                .Include(m => m.Consignor)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                merchandises = merchandises.Where(m =>
                    m.MerchandiseID.ToString().Contains(searchString) ||
                    m.MerchandiseName.Contains(searchString) ||
                    m.Category.Contains(searchString) ||
                    m.Quantity.ToString().Contains(searchString) ||
                    m.SellingPrice.ToString().Contains(searchString) ||
                    m.StoreSharePercentage.ToString().Contains(searchString) ||
                    m.ConsignorID.ToString().Contains(searchString)
                );
            }

            return View(await merchandises.ToListAsync());
        }

        // POST: Merchandises
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(Merchandise merchandise)
        {
            if (ModelState.IsValid)
            {
                _context.Add(merchandise);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Consignors = new SelectList(
                _context.Consignors,
                "ConsignorID",
                "ConsignorName"
            );

            var merchandises = await _context.Merchandises
                .Include(m => m.Consignor)
                .ToListAsync();

            return View(merchandises);
        }

        // GET: Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var merchandise = await _context.Merchandises.FindAsync(id);

            if (merchandise == null)
                return NotFound();

            ViewBag.Consignors = new SelectList(
                _context.Consignors,
                "ConsignorID",
                "ConsignorName",
                merchandise.ConsignorID
            );

            return View(merchandise);
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Merchandise merchandise)
        {
            if (id != merchandise.MerchandiseID)
                return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(merchandise);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.ConsignorID = new SelectList(
                _context.Consignors,
                "ConsignorID",
                "ConsignorName",
                merchandise.ConsignorID
            );

            return View(merchandise);
        }

        // GET: Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var merchandise = await _context.Merchandises
                .FirstOrDefaultAsync(m => m.MerchandiseID == id);

            if (merchandise == null)
                return NotFound();

            return View(merchandise);
        }

        // POST: Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var merchandise = await _context.Merchandises.FindAsync(id);

            if (merchandise != null)
            {
                _context.Merchandises.Remove(merchandise);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}