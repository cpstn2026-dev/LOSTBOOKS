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
                Id = b.BookID,
                Name = b.Title,
                Category = "Books",
                Price = b.SellingPrice,
                Stock = b.Quantity
            }).ToList());

            // PRODUCTS
            items.AddRange(_context.Products.Select(p => new POSViewModels
            {
                Id = p.ProductID,
                Name = p.ProductName,
                Category = "Products",
                Price = p.SellingPrice,
                Stock = 9999
            }).ToList());

            // MERCHANDISE
            items.AddRange(_context.Merchandises.Select(m => new POSViewModels
            {
                Id = m.MerchandiseID,
                Name = m.MerchandiseName,
                Category = "Merchandise",
                Price = m.SellingPrice,
                Stock = m.Quantity

            }).ToList());

            // SERVICES
            items.AddRange(_context.Services
            .Where(s => s.Status == "Ready for Payment")
            .Select(s => new POSViewModels
            {
                Id = s.ServiceID,
                Name = s.CustomerName + " - " + s.ServiceType,
                Category = "Services",
                Price = s.AssessedPrice ?? 0,
                Stock = 0
            }).ToList());


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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
