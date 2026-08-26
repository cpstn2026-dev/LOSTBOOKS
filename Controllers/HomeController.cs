using System.Diagnostics;
using LOSTBOOKS.Data;
using LOSTBOOKS.Models;
using LOSTBOOKS.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LOSTBOOKS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly LOSTBOOKSContext _context;
        private readonly LOSTBOOKS.Services.IActivityLogger _activityLogger;

        public HomeController(
            ILogger<HomeController> logger,
            LOSTBOOKSContext context,
            LOSTBOOKS.Services.IActivityLogger activityLogger)
        {
            _logger = logger;
            _context = context;
            _activityLogger = activityLogger;
        }

        // =========================
        // POS
        // =========================
        public IActionResult Index(string search, string category)
        {
            var items = new List<POSViewModels>();

            // =========================
            // BOOKS
            // =========================

            items.AddRange(_context.Books
                .Where(b => b.Quantity > 0 && b.Consignor.IsActive)
                .Select(b => new POSViewModels
                {
                    Id = "BK-" + b.BookID.ToString("D4"),
                    Name = b.Title,
                    Category = "Books",
                    Price = b.SellingPrice,
                    Stock = b.Quantity
                })
                .ToList());

            // =========================
            // PRODUCTS
            // =========================
            items.AddRange(_context.Products
                .Select(p => new POSViewModels
                {
                    Id = "PROD-" + p.ProductID.ToString("D4"),
                    Name = p.ProductName,
                    Category = "Products",
                    Price = p.SellingPrice,
                    Stock = 9999
                })
                .ToList());

            // =========================
            // MERCHANDISE
            // =========================

            items.AddRange(_context.Merchandises
                .Where(m => m.Quantity > 0 && m.Consignor.IsActive) // hide out-of-stock AND deactivated-consignor items
                .Select(m => new POSViewModels
                {
                    Id = "MER-" + m.MerchandiseID.ToString("D4"),
                    Name = m.MerchandiseName,
                    Category = "Merchandise",
                    Price = m.SellingPrice,
                    Stock = m.Quantity
                })
                .ToList());

            // =========================
            // SERVICES
            // NEW: only show services that are Ready for Payment
            // and already have an AssessedPrice. Pending Assessment
            // items must NOT appear in POS.
            // =========================
            items.AddRange(_context.Services
                .Where(s => s.Status == "Ready for Payment" && s.AssessedPrice != null)
                .Select(s => new POSViewModels
                {
                    Id = "SER-" + s.ServiceID.ToString("D4"),
                    Name = s.ServiceType + " - " + s.Size,
                    Category = "Services",
                    Price = s.AssessedPrice ?? 0
                })
                .ToList());

            // =========================
            // SEARCH
            // =========================
            if (!string.IsNullOrEmpty(search))
            {
                items = items
                    .Where(x => x.Name.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // =========================
            // CATEGORY FILTER
            // =========================
            if (!string.IsNullOrEmpty(category) &&
                category != "All")
            {
                items = items
                    .Where(x => x.Category == category)
                    .ToList();
            }

            return View(items);
        }

        // =========================
        // PRIVACY
        // =========================
        public IActionResult Privacy()
        {
            return View();
        }

        // =========================
        // CHECKOUT
        // =========================
        [HttpPost]
        public IActionResult Checkout([FromBody] List<History> sales)
        {
            if (sales == null || sales.Count == 0)
            {
                return BadRequest();
            }

            // NEW: track items nga naubos na ang stock samtang naa sa cart
            var insufficientItems = new List<string>();

            foreach (var item in sales)
            {
                item.TransactionDate = DateTime.Now;

                // BOOKS
                if (item.Category == "Books")
                {
                    int id = int.Parse(item.ItemID.Replace("BK-", ""));

                    var book = _context.Books
                        .FirstOrDefault(x => x.BookID == id);

                    // NEW: check stock before deducting
                    if (book == null || book.Quantity < item.QuantitySold)
                    {
                        insufficientItems.Add(item.ItemName);
                        continue;
                    }

                    book.Quantity -= item.QuantitySold;

                    item.ConsignorID = book.ConsignorID;
                    item.StoreSharePercentage =
                        book.StoreSharePercentage;
                }

                // MERCHANDISE
                if (item.Category == "Merchandise")
                {
                    int id = int.Parse(item.ItemID.Replace("MER-", ""));

                    var merchandise = _context.Merchandises
                        .FirstOrDefault(x => x.MerchandiseID == id);

                    // NEW: check stock before deducting
                    if (merchandise == null || merchandise.Quantity < item.QuantitySold)
                    {
                        insufficientItems.Add(item.ItemName);
                        continue;
                    }

                    merchandise.Quantity -= item.QuantitySold;

                    item.ConsignorID = merchandise.ConsignorID;
                    item.StoreSharePercentage =
                        merchandise.StoreSharePercentage;
                }

                // IMPORTANT
                // Save the selected Cash / Digital Payment (+ sub-type)
                _context.Histories.Add(item);
            }

            // NEW: kung naa'y item nga naubos na ang stock, i-reject
            // ang TIBUOK transaction (dili mag-partial checkout)
            if (insufficientItems.Count > 0)
            {
                return BadRequest(new
                {
                    message = "Naa'y item nga naubos na ang stock samtang naa sa cart:",
                    items = insufficientItems
                });
            }

            _context.SaveChanges();

            decimal saleTotal = sales.Sum(s =>
                s.SellingPrice * s.QuantitySold);

            _activityLogger.Log(
                "POS",
                "Sale Recorded",
                $"Sale Recorded — {sales.Count} item(s), Total ₱{saleTotal:N2}");

            return Ok();
        }

        // =========================
        // ERROR
        // =========================
        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            });
        }
    }
}