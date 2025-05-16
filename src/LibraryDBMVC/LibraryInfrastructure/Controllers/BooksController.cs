using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LibraryDomain.Model;
using LibraryInfrastructure;
using Microsoft.AspNetCore.Identity;
using static System.Reflection.Metadata.BlobBuilder;
using Microsoft.AspNetCore.Authorization;

namespace LibraryInfrastructure.Controllers
{
    public class BooksController : Controller
    {
        private readonly LibraryContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BooksController(LibraryContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Books
        [AllowAnonymous]
        public async Task<IActionResult> Index(string searchString, string sortOrder)
        {
            var libraryContext = _context.Books
                .Include(b => b.Worker)
                .Include(b => b.Publisher)
                .Include(b => b.AuthorsBooks)
                    .ThenInclude(ab => ab.Author)
                .Include(b => b.GenresBooks)
                    .ThenInclude(gb => gb.Genre)
                .Include(b => b.ClientsBooks)
                .Include(b => b.BookReservations)
                .AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                libraryContext = libraryContext.Where(b =>
                     b.Title.Contains(searchString)
                  || b.Worker.FullName.Contains(searchString)
                  || b.Publisher.Name.Contains(searchString)
                  || b.AuthorsBooks.Any(ab => ab.Author.FullName.Contains(searchString))
                  || b.GenresBooks.Any(gb => gb.Genre.GenreName.Contains(searchString))
                );
            }

            var currentUser = await _userManager.GetUserAsync(User);
            Client? client = null;
            if (currentUser != null)
            {
                client = await _context.Clients.FirstOrDefaultAsync(c => c.Id.ToString() == currentUser.Id);
            }

            @ViewData["TitleSortParam"] = string.IsNullOrEmpty(sortOrder) ? "title_desc" : "";
            @ViewData["AddedBySortParam"] = sortOrder == "addedby_asc" ? "addedby_desc" : "addedby_asc";
            @ViewData["PublisherSortParam"] = sortOrder == "publisher_asc" ? "publisher_desc" : "publisher_asc";
            @ViewData["AuthorSortParam"] = sortOrder == "author_asc" ? "author_desc" : "author_asc";
            @ViewData["GenreSortParam"] = sortOrder == "genre_asc" ? "genre_desc" : "genre_asc";

            libraryContext = sortOrder switch
            {
                "title_desc" => libraryContext.OrderByDescending(b => b.Title),
                "addedby_desc" => libraryContext.OrderByDescending(b => b.Worker.FullName),
                "addedby_asc" => libraryContext.OrderBy(b => b.Worker.FullName),
                "publisher_desc" => libraryContext.OrderByDescending(b => b.Publisher.Name),
                "publisher_asc" => libraryContext.OrderBy(b => b.Publisher.Name),
                "author_desc" => libraryContext.OrderByDescending(b => b.AuthorsBooks
                                        .Select(ab => ab.Author.FullName)
                                        .FirstOrDefault()),
                "author_asc" => libraryContext.OrderBy(b => b.AuthorsBooks
                                        .Select(ab => ab.Author.FullName)
                                        .FirstOrDefault()),
                "genre_desc" => libraryContext.OrderByDescending(b => b.GenresBooks
                                        .Select(gb => gb.Genre.GenreName)
                                        .FirstOrDefault()),
                "genre_asc" => libraryContext.OrderBy(b => b.GenresBooks
                                        .Select(gb => gb.Genre.GenreName)
                                        .FirstOrDefault()),
                _ => libraryContext.OrderBy(b => b.Title),
            };

            var books = await libraryContext.ToListAsync();

            var viewModel = books.Select(book => new BookViewModel
            {
                Book = book,
                IsBorrowedByUser = client != null && book.ClientsBooks.Any(cb => cb.ClientId.ToString() == client.Id && cb.ReturnDate == null)
            }).ToList();

            return View(viewModel);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Borrow(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized("Користувач не авторизований.");

            var book = await _context.Books
                .Include(b => b.ClientsBooks)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null)
                return NotFound("Книгу не знайдено.");

            var reservation = await _context.BookReservations
                .FirstOrDefaultAsync(br => br.BookId == id && br.UserId == user.Id && br.Status == "Заброньована");

            if (reservation == null)
            {
                return BadRequest("Ця книга не була заброньована або вже позичена.");
            }

            reservation.Status = "Позичена";
            _context.BookReservations.Update(reservation);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Return(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            var loan = await _context.ClientsBooks
                .FirstOrDefaultAsync(cb => cb.BookId == id && cb.ClientId.ToString() == user.Id && cb.ReturnDate == null);

            if (loan == null)
            {
                return NotFound();
            }

            loan.ReturnDate = DateOnly.FromDateTime(DateTime.Now);
            _context.ClientsBooks.Update(loan);

            var reservation = await _context.BookReservations
                .FirstOrDefaultAsync(br => br.BookId == id && br.UserId == user.Id && br.Status == "Позичена");

            if (reservation != null)
            {
                reservation.Status = "Доступна";
                _context.BookReservations.Update(reservation);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Librarian")]
        public async Task<IActionResult> ChangeStatus(int bookId, string newStatus, int? days)
        {
            var book = await _context.Books
                .Include(b => b.BookReservations)
                .FirstOrDefaultAsync(b => b.Id == bookId);

            if (book == null) return NotFound();

            var latestReservation = book.BookReservations
                .OrderByDescending(br => br.ReservationDate)
                .FirstOrDefault();

            if (latestReservation != null)
            {
                latestReservation.Status = newStatus;

                if (newStatus == "Недоступна" && days.HasValue)
                {
                    latestReservation.DueDate = DateTime.UtcNow.AddDays(days.Value);
                }
            }
            else
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("Користувач не автентифікований.");
                }

                var newReservation = new BookReservation
                {
                    BookId = bookId,
                    UserId = userId,
                    Status = newStatus,
                    ReservationDate = DateTime.UtcNow,
                    DueDate = (newStatus == "Недоступна" && days.HasValue
                        ? DateTime.UtcNow.AddDays(days.Value)
                        : DateTime.UtcNow)
                };

                _context.BookReservations.Add(newReservation);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = bookId });
        }

        // GET: Books/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Worker)
                .Include(b => b.Publisher)
                .Include(b => b.AuthorsBooks).ThenInclude(ab => ab.Author)
                .Include(b => b.GenresBooks).ThenInclude(gb => gb.Genre)
                .Include(b => b.BookReservations)
                .ThenInclude(br => br.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // GET: Books/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            var publishers = await _context.Publishers.ToListAsync() ?? [];
            var authors = await _context.Authors.ToListAsync() ?? [];
            var genres = await _context.Genres.ToListAsync() ?? [];

            ViewBag.Publishers = publishers != null && publishers.Any()
                ? new SelectList(publishers, "Id", "Name")
                : new SelectList(new List<Publisher>(), "Id", "Name");

            ViewBag.Authors = authors != null && authors.Any()
                ? new MultiSelectList(authors, "Id", "FullName")
                : new MultiSelectList(new List<Author>(), "Id", "FullName");

            ViewBag.Genres = genres?.Count > 0
                ? new MultiSelectList(genres, "Id", "GenreName")
                : new MultiSelectList(new List<Genre>(), "Id", "GenreName");

            var user = await _userManager.GetUserAsync(User);
            var worker = await _context.Workers.FirstOrDefaultAsync(w => w.ApplicationUserId == user.Id);

            string displayValue = worker != null && !string.IsNullOrEmpty(worker.FullName)
                ? worker.FullName
                : user?.Email ?? "Невідомий користувач";

            int workerId = worker?.Id ?? 0;

            ViewBag.CurrentWorkerId = workerId;
            ViewBag.CurrentUserDisplay = displayValue;

            return View(new Book { AddedBy = workerId });
        }

        // POST: Books/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Book book, int[] selectedAuthors, int[] selectedGenres)
        {
            ModelState.Remove("Publisher");
            ModelState.Remove("Worker");

            var user = await _userManager.GetUserAsync(User);
            Worker worker = null;

            if (user != null)
            {
                worker = await _context.Workers.FirstOrDefaultAsync(w => w.ApplicationUserId == user.Id);

                if (worker == null)
                {
                    worker = new Worker
                    {
                        ApplicationUserId = user.Id,
                        FullName = user.Email ?? "Невідомий користувач"
                    };

                    _context.Workers.Add(worker);
                    await _context.SaveChangesAsync();
                }

                book.AddedBy = worker.Id;
            }
            else
            {
                return Unauthorized();
            }


            if (selectedAuthors == null || !selectedAuthors.Any())
            {
                ModelState.AddModelError("AuthorsBooks", "Оберіть хоча б одного автора");
            }

            if (selectedGenres == null || !selectedGenres.Any())
            {
                ModelState.AddModelError("GenresBooks", "Оберіть хоча б один жанр");
            }

            if (ModelState.IsValid)
            {
                book.AuthorsBooks = selectedAuthors.Select(authorId => new AuthorsBook
                {
                    AuthorId = authorId,
                    Book = book
                }).ToList();

                book.GenresBooks = selectedGenres.Select(genreId => new GenresBook
                {
                    GenreId = genreId,
                    Book = book
                }).ToList();

                _context.Add(book);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine(error.ErrorMessage);
            }

            ViewData["Publishers"] = new SelectList(_context.Publishers, "Id", "Name", book.PublisherId);
            ViewData["Authors"] = new MultiSelectList(_context.Authors, "Id", "FullName", selectedAuthors);
            ViewData["Genres"] = new MultiSelectList(_context.Genres, "Id", "GenreName", selectedGenres);

            ViewBag.CurrentUserDisplay = user?.Email ?? "Невідомий користувач";
            ViewBag.CurrentWorkerId = worker?.Id ?? 0;

            return View(book);
        }

        // GET: Books/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.AuthorsBooks).ThenInclude(ab => ab.Author)
                .Include(b => b.GenresBooks).ThenInclude(gb => gb.Genre)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (book == null)
            {
                return NotFound();
            }

            var worker = await _context.Workers.FindAsync(book.AddedBy);
            string displayValue = worker != null && !string.IsNullOrEmpty(worker.FullName)
                ? worker.FullName
                : "Невідомий користувач";

            ViewBag.CurrentUserDisplay = displayValue;
            ViewBag.CurrentWorkerId = book.AddedBy;

            ViewBag.Publishers = new SelectList(_context.Publishers, "Id", "Name", book.PublisherId);
            ViewBag.Authors = new MultiSelectList(_context.Authors, "Id", "FullName", book.AuthorsBooks.Select(ab => ab.AuthorId));
            ViewBag.Genres = new MultiSelectList(_context.Genres, "Id", "GenreName", book.GenresBooks.Select(gb => gb.GenreId));

            return View(book);
        }

        // POST: Books/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Title,PublisherId,AddedBy,Id")] Book book, int[] selectedAuthors, int[] selectedGenres)
        {
            if (id != book.Id)
            {
                return NotFound();
            }

            ModelState.Remove("Publisher");
            ModelState.Remove("Worker");

            if (selectedAuthors == null || !selectedAuthors.Any())
            {
                ModelState.AddModelError("AuthorsBooks", "Оберіть хоча б одного автора");
            }

            if (selectedGenres == null || !selectedGenres.Any())
            {
                ModelState.AddModelError("GenresBooks", "Оберіть хоча б один жанр");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingBook = await _context.Books
                        .Include(b => b.AuthorsBooks)
                        .Include(b => b.GenresBooks)
                        .FirstOrDefaultAsync(b => b.Id == id);

                    if (existingBook != null)
                    {
                        existingBook.Title = book.Title;
                        existingBook.PublisherId = book.PublisherId;

                        var authorsToRemove = existingBook.AuthorsBooks.ToList();
                        foreach (var ab in authorsToRemove)
                        {
                            _context.AuthorsBooks.Remove(ab);
                        }
                        foreach (var authorId in selectedAuthors)
                        {
                            existingBook.AuthorsBooks.Add(new AuthorsBook { AuthorId = authorId, BookId = existingBook.Id });
                        }

                        var genresToRemove = existingBook.GenresBooks.ToList();
                        foreach (var gb in genresToRemove)
                        {
                            _context.GenresBooks.Remove(gb);
                        }
                        foreach (var genreId in selectedGenres)
                        {
                            existingBook.GenresBooks.Add(new GenresBook { GenreId = genreId, BookId = existingBook.Id });
                        }

                        _context.Update(existingBook);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(book.Id))
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

            ViewBag.Publishers = new SelectList(_context.Publishers, "Id", "Name", book.PublisherId);
            ViewBag.Authors = new MultiSelectList(_context.Authors, "Id", "FullName", selectedAuthors);
            ViewBag.Genres = new MultiSelectList(_context.Genres, "Id", "GenreName", selectedGenres);

            var worker = await _context.Workers.FindAsync(book.AddedBy);
            string displayValue = worker != null && !string.IsNullOrEmpty(worker.FullName)
                ? worker.FullName
                : "Невідомий користувач";
            ViewBag.CurrentUserDisplay = displayValue;
            ViewBag.CurrentWorkerId = book.AddedBy;

            return View(book);
        }

        // GET: Books/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Worker)
                .Include(b => b.Publisher)
                .Include(b => b.AuthorsBooks).ThenInclude(ab => ab.Author)
                .Include(b => b.GenresBooks).ThenInclude(gb => gb.Genre)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (book == null)
            {
                return NotFound();
            }

            string displayValue = "Невідомий користувач";
            if (book.Worker != null)
            {
                var user = await _userManager.GetUserAsync(User);
                displayValue = !string.IsNullOrEmpty(book.Worker.FullName)
                    ? book.Worker.FullName
                    : user.Email;
            }
            ViewBag.CurrentUserDisplay = displayValue;

            return View(book);
        }


        // POST: Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books
                .Include(b => b.AuthorsBooks)
                .Include(b => b.GenresBooks)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book != null)
            {
                var clientsBooks = await _context.ClientsBooks
                    .Where(cb => cb.BookId == id)
                    .ToListAsync();
                _context.ClientsBooks.RemoveRange(clientsBooks);

                _context.AuthorsBooks.RemoveRange(book.AuthorsBooks);
                _context.GenresBooks.RemoveRange(book.GenresBooks);

                _context.Books.Remove(book);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.Id == id);
        }
    }
}
