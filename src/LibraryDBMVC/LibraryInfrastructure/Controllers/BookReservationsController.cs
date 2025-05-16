using LibraryDomain.Model;
using LibraryInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class BookReservationsController : Controller
{
    private readonly LibraryContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public BookReservationsController(LibraryContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpPost]
    public async Task<IActionResult> Reserve(int bookId, int days)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var book = await _context.Books
            .Include(b => b.BookReservations)
            .FirstOrDefaultAsync(b => b.Id == bookId);

        if (book == null)
            return NotFound();

        var latestReservation = book.BookReservations
            .OrderByDescending(br => br.ReservationDate)
            .FirstOrDefault();

        if (latestReservation != null && latestReservation.Status != "Доступна")
        {
            ModelState.AddModelError(string.Empty, "Книга недоступна для бронювання.");
            TempData["Error"] = "Книга недоступна для бронювання.";
            return RedirectToAction("Details", "Books", new { id = bookId });
        }

        var reservation = new BookReservation
        {
            UserId = user.Id,
            BookId = bookId,
            DueDate = DateTime.UtcNow.AddDays(days),
            Status = "Заброньована"
        };

        _context.BookReservations.Add(reservation);

        var loan = new ClientsBook
        {
            BookId = bookId,
            ClientId = user.Id,
            BorrowingDate = DateOnly.FromDateTime(DateTime.Now),
            ReturnDate = null
        };

        _context.ClientsBooks.Add(loan);

        await _context.SaveChangesAsync();

        return RedirectToAction("Index", "Books");
    }

    [Authorize(Roles = "Librarian")]
    [HttpPost]
    public async Task<IActionResult> CheckOverdue()
    {
        var overdueReservations = await _context.BookReservations
            .Where(r => r.Status == "Недоступна" && r.DueDate < DateTime.UtcNow)
            .ToListAsync();

        foreach (var reservation in overdueReservations)
        {
            reservation.Status = "Прострочена";
        }

        await _context.SaveChangesAsync();
        return RedirectToAction("Index", "Books");
    }

    [Authorize(Roles = "Librarian")]
    [HttpPost]
    public async Task<IActionResult> ExpireReservations()
    {
        var expiredReservations = await _context.BookReservations
            .Where(r => r.Status == "Заброньована" && r.ReservationDate.AddDays(1) < DateTime.UtcNow)
            .ToListAsync();

        foreach (var reservation in expiredReservations)
        {
            reservation.Status = "Доступна";
        }

        await _context.SaveChangesAsync();

        return RedirectToAction("Index", "Books");
    }
}
