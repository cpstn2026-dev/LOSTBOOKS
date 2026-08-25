using LOSTBOOKS.Data;
using LOSTBOOKS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LOSTBOOKS.Controllers
{
    public class BooksController : Controller
    {
        private readonly LOSTBOOKSContext _context;
        private readonly LOSTBOOKS.Services.IActivityLogger _activityLogger;

        public BooksController(
            LOSTBOOKSContext context,
            LOSTBOOKS.Services.IActivityLogger activityLogger)
        {
            _context = context;
            _activityLogger = activityLogger;
        }

        // GET: Books
        public async Task<IActionResult> Index(string searchString)
        {
            ViewBag.Consignors = new SelectList(
                _context.Consignors,
                "ConsignorID",
                "ConsignorName"
            );

            var books = _context.Books
                .Include(b => b.Consignor)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                books = books.Where(b =>
                    b.BookID.ToString().Contains(searchString) ||
                    b.ISBN.Contains(searchString) ||
                    b.Title.Contains(searchString) ||
                    b.Author.Contains(searchString) ||
                    b.Condition.Contains(searchString) ||
                    b.Quantity.ToString().Contains(searchString) ||
                    b.SellingPrice.ToString().Contains(searchString) ||
                    b.StoreSharePercentage.ToString().Contains(searchString) ||
                    b.ConsignorID.ToString().Contains(searchString)
                );
            }

            return View(await books.ToListAsync());
        }

        // POST: Books/Create   (FIXED FROM Index → Create)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Book book)
        {
            if (book.Condition == "Secondhand")
            {
                book.Quantity = 1;
            }

            // IMPORTANT: show errors (for debugging)
            if (!ModelState.IsValid)
            {
                ViewBag.Consignors = new SelectList(
                    _context.Consignors,
                    "ConsignorID",
                    "ConsignorName",
                    book.ConsignorID
                );

                var books = await _context.Books
                    .Include(b => b.Consignor)
                    .ToListAsync();

                return View("Index", books);
            }

            _context.Add(book);
            await _context.SaveChangesAsync();

            _activityLogger.Log(
                "Books",
                "Book Added",
                $"Book Added — BK -{book.BookID:D4},{book.Title}"
            );

            return RedirectToAction(nameof(Index));
        }

        // GET: Books/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var book = await _context.Books
                .Include(b => b.Consignor)
                .FirstOrDefaultAsync(m => m.BookID == id);

            if (book == null)
                return NotFound();

            return View(book);
        }

        // GET: Books/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var book = await _context.Books.FindAsync(id);

            if (book == null)
                return NotFound();

            return View(book);
        }

        // POST: Books/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Book book)
        {
            if (id != book.BookID)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(book);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(book.BookID))
                        return NotFound();
                    else
                        throw;
                }

                _activityLogger.Log(
                    "Books",
                    "Book Edited",
                    $"Book Edited — BK -{book.BookID:D4},{book.Title}"
                );

                return RedirectToAction(nameof(Index));
            }

            return View(book);
        }

        // GET: Books/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var book = await _context.Books.FindAsync(id);

            if (book != null)
            {
                _context.Books.Remove(book);
            }

            await _context.SaveChangesAsync();

            if (book != null)
            {
                _activityLogger.Log(
                    "Books",
                    "Book Deleted",
                    $"Book Deleted — BK -{book.BookID:D4}{book.Title}"
                );
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books.FindAsync(id);

            if (book != null)
            {
                _context.Books.Remove(book);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.BookID == id);
        }
    }
}