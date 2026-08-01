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

        public HomeController(
         ILogger<HomeController> logger,
         LOSTBOOKSContext context)
        {
            _logger = logger;
            _context = context;
        }
        public IActionResult Index(string search, string category)
        {
            var items = new List<POSViewModels>();

            // BOOKS
            items.AddRange(_context.Books.Select(b => new POSViewModels
            {
                Id = "BK-" + b.BookID.ToString("D4"),
                Name = b.Title,
                Category = "Books",
                Price = b.SellingPrice,
                Stock = b.Quantity
            }).ToList());

            // PRODUCTS
            items.AddRange(_context.Products.Select(p => new POSViewModels
            {
                Id = "PROD-" + p.ProductID.ToString("D4"),
                Name = p.ProductName,
                Category = "Products",
                Price = p.SellingPrice,
                Stock = 9999
            }).ToList());

            // MERCHANDISE
            items.AddRange(_context.Merchandises.Select(m => new POSViewModels
            {
                Id = "MER-" + m.MerchandiseID.ToString("D4"),
                Name = m.MerchandiseName,
                Category = "Merchandise",
                Price = m.SellingPrice,
                Stock = m.Quantity

            }).ToList());

            items.AddRange(_context.Services.Select(s => new POSViewModels
            {
                Id = "SER-" + s.ServiceID.ToString("0000"),
                Name = s.ServiceType + " - " + s.Size,
                Category = "Services",
                Price = s.AssessedPrice ?? 0
            }));


            // SEARCH
            if (search != null)
            {
                items = items.Where(x => x.Name.Contains(search)).ToList();
            }

            // CATEGORY
            if (category != null && category != "All")
            {
                items = items.Where(x => x.Category == category).ToList();
            }
            return View(items);
        }

        public IActionResult Privacy()
        {

            return View();
        }


        [HttpPost]
        public IActionResult Checkout([FromBody] List<SalesRecording> sales)
        {
            if (sales == null || sales.Count == 0)
            {
                return BadRequest();
            }

            foreach (var item in sales)
            {
                item.TransactionDate = DateTime.Now;

                _context.SalesRecordings.Add(item);

                // BOOKS
                if (item.Category == "Books")
                {
                    int id = int.Parse(item.ItemID.Replace("BK-", ""));

                    var book = _context.Books.FirstOrDefault(x => x.BookID == id);

                    if (book != null)
                    {
                        book.Quantity -= item.QuantitySold;
                    }
                }

                // MERCHANDISE
                if (item.Category == "Merchandise")
                {
                    int id = int.Parse(item.ItemID.Replace("MER-", ""));

                    var merchandise = _context.Merchandises.FirstOrDefault(x => x.MerchandiseID == id);

                    if (merchandise != null)
                    {
                        merchandise.Quantity -= item.QuantitySold;
                    }
                }
            }

            _context.SaveChanges();

            return Ok();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}

