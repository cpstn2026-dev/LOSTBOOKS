
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LOSTBOOKS.Models;
using LOSTBOOKS.Data;

public class SalesRecordingsController : Controller
{
    private readonly LOSTBOOKSContext _context;

    public SalesRecordingsController(LOSTBOOKSContext context)
    {
        _context = context;
    }

    // GET: SALESRECORDINGS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.SalesRecordings.ToListAsync());
    }

    // GET: SALESRECORDINGS/Details/5
    public async Task<IActionResult> Details(int? salesrecordingid)
    {
        if (salesrecordingid == null)
        {
            return NotFound();
        }

        var salesrecording = await _context.SalesRecordings
            .FirstOrDefaultAsync(m => m.SalesRecordingID == salesrecordingid);
        if (salesrecording == null)
        {
            return NotFound();
        }

        return View(salesrecording);
    }

    // GET: SALESRECORDINGS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: SALESRECORDINGS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("SalesRecordingID,TransactionDate,ItemID,ItemName,Category,QuantitySold,SellingPrice")] SalesRecording salesrecording)
    {
        if (ModelState.IsValid)
        {
            _context.Add(salesrecording);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(salesrecording);
    }

    // GET: SALESRECORDINGS/Edit/5
    public async Task<IActionResult> Edit(int? salesrecordingid)
    {
        if (salesrecordingid == null)
        {
            return NotFound();
        }

        var salesrecording = await _context.SalesRecordings.FindAsync(salesrecordingid);
        if (salesrecording == null)
        {
            return NotFound();
        }
        return View(salesrecording);
    }

    // POST: SALESRECORDINGS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? salesrecordingid, [Bind("SalesRecordingID,TransactionDate,ItemID,ItemName,Category,QuantitySold,SellingPrice")] SalesRecording salesrecording)
    {
        if (salesrecordingid != salesrecording.SalesRecordingID)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(salesrecording);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SalesRecordingExists(salesrecording.SalesRecordingID))
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
        return View(salesrecording);
    }

    // GET: SALESRECORDINGS/Delete/5
    public async Task<IActionResult> Delete(int? salesrecordingid)
    {
        if (salesrecordingid == null)
        {
            return NotFound();
        }

        var salesrecording = await _context.SalesRecordings
            .FirstOrDefaultAsync(m => m.SalesRecordingID == salesrecordingid);
        if (salesrecording == null)
        {
            return NotFound();
        }

        return View(salesrecording);
    }

    // POST: SALESRECORDINGS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? salesrecordingid)
    {
        var salesrecording = await _context.SalesRecordings.FindAsync(salesrecordingid);
        if (salesrecording != null)
        {
            _context.SalesRecordings.Remove(salesrecording);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool SalesRecordingExists(int? salesrecordingid)
    {
        return _context.SalesRecordings.Any(e => e.SalesRecordingID == salesrecordingid);
    }
}
