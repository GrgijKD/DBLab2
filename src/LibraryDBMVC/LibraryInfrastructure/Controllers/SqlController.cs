using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using LibraryInfrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibraryInfrastructure.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SqlController : Controller
    {
        private readonly LibraryContext _context;

        public SqlController(LibraryContext context)
        {
            _context = context;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> Index1(string workerFullName)
        {
            ViewBag.Workers = _context.Workers
            .Select(w => new SelectListItem
            {
                Value = w.FullName,
                Text = w.FullName
            }).ToList();

            if (string.IsNullOrWhiteSpace(workerFullName))
            {
                ViewBag.Results = new List<dynamic>();
                return View();
            }

            var parameter = new SqlParameter("@FullName", workerFullName);

            var results = await _context.Set<BookSearchResult>()
                .FromSqlRaw(@"
                    SELECT b.Title, a.[Full Name] AS Author, g.[Genre Name] AS Genre
                    FROM Books b
                    JOIN AuthorsBooks ab ON b.Id = ab.[Book Id]
                    JOIN Authors a ON ab.[Author Id] = a.Id
                    JOIN GenresBooks gb ON b.Id = gb.[Book Id]
                    JOIN Genres g ON gb.[Genre Id] = g.Id
                    JOIN Workers w ON b.[Added By] = w.Id
                    WHERE w.[Full Name] = {0}
                ", workerFullName)
                .ToListAsync();

            ViewBag.InputName = workerFullName;
            return View(results);
        }

        [HttpGet]
        public async Task<IActionResult> Index2(string? genreName, string? authorName)
        {
            ViewBag.Genres = await _context.Genres
                .Select(g => g.GenreName)
                .Distinct()
                .ToListAsync();

            ViewBag.Authors = await _context.Authors
                .Select(a => a.FullName)
                .Distinct()
                .ToListAsync();

            ViewBag.GenreInput = genreName;
            ViewBag.AuthorInput = authorName;

            List<BookSearchResult> results = new();

            if (!string.IsNullOrEmpty(genreName) && !string.IsNullOrEmpty(authorName))
            {
                var genreParam = new SqlParameter("@GenreName", genreName);
                var authorParam = new SqlParameter("@AuthorName", authorName);

                results = await _context.Set<BookSearchResult>().FromSqlRaw(@"
                    SELECT b.Title, a.[Full Name] AS Author, g.[Genre Name] AS Genre
                    FROM Books b
                    JOIN AuthorsBooks ab ON b.Id = ab.[Book Id]
                    JOIN Authors a ON ab.[Author Id] = a.Id
                    JOIN GenresBooks gb ON b.Id = gb.[Book Id]
                    JOIN Genres g ON gb.[Genre Id] = g.Id
                    LEFT JOIN (
                        SELECT br1.*
                        FROM BookReservations br1
                        JOIN (
                            SELECT BookId, MAX(ReservationDate) AS MaxDate
                            FROM BookReservations
                            GROUP BY BookId
                        ) br2 ON br1.BookId = br2.BookId AND br1.ReservationDate = br2.MaxDate
                    ) AS LatestReservations ON b.Id = LatestReservations.BookId
                    WHERE ISNULL(LatestReservations.Status, 'Доступна') = 'Доступна' AND a.[Full Name] = @AuthorName AND g.[Genre Name] = @GenreName
                ", authorParam, genreParam).ToListAsync();
            }

            return View(results);
        }

        public async Task<IActionResult> Index3(int? minBooks)
        {
            ViewBag.MinBooks = minBooks;

            if (minBooks == null)
            {
                ViewBag.Results = new List<WorkerSearchResult>();
                return View();
            }

            var param = new SqlParameter("@MinBooks", minBooks);

            var results = await _context.Set<WorkerSearchResult>()
                .FromSqlRaw(@"
                    SELECT w.[Full Name] AS FullName, COUNT(b.Id) AS BooksAdded
                    FROM Workers w
                    JOIN Books b ON b.[Added By] = w.Id
                    GROUP BY w.[Full Name]
                    HAVING COUNT(b.Id) > @MinBooks
                ", param)
                .ToListAsync();

            ViewBag.Results = results;
            return View();
        }

        public async Task<IActionResult> Index4()
        {
            var books = await _context.Books.ToListAsync();
            ViewBag.Books = new SelectList(books, "Id", "Title");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SearchBooksByStatus(int bookId)
        {
            var books = await _context.Books
                .Select(b => new { b.Id, b.Title })
                .ToListAsync();

            ViewBag.Books = new SelectList(books, "Id", "Title");

            var latestStatus = await _context.BookReservations
                .Where(br => br.BookId == bookId)
                .OrderByDescending(br => br.ReservationDate)
                .Select(br => br.Status)
                .FirstOrDefaultAsync();

            var status = string.IsNullOrEmpty(latestStatus) ? "Доступна" : latestStatus;

            ViewBag.BookStatus = status;

            var parameter = new SqlParameter("@Status", status);

            var results = await _context.Set<BookStatusSearchResult>()
                .FromSqlRaw(@"
                    SELECT b.Title,
                           ISNULL(br.Status, 'Доступна') AS Status
                    FROM Books b
                    LEFT JOIN (
                        SELECT br1.*
                        FROM BookReservations br1
                        JOIN (
                            SELECT BookId, MAX(ReservationDate) AS MaxDate
                            FROM BookReservations
                            GROUP BY BookId
                        ) latest ON br1.BookId = latest.BookId AND br1.ReservationDate = latest.MaxDate
                    ) br ON b.Id = br.BookId
                    WHERE ISNULL(br.Status, 'Доступна') = @Status
                ", parameter)
                .ToListAsync();

            ViewBag.SelectedStatus = status;
            return View("Index4", results);
        }

        public async Task<IActionResult> Index5()
        {
            var publishers = await _context.Publishers
                .Select(p => new { p.Id, p.Name })
                .ToListAsync();

            ViewBag.Publishers = new SelectList(publishers, "Id", "Name");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SearchBooksExcludePublisher(int publisherId)
        {
            var parameter = new SqlParameter("@PublisherId", publisherId);

            var results = await _context.Set<BookExcludePublisherResult>()
                .FromSqlRaw(@"
                    SELECT b.Title, p.Name AS PublisherName
                    FROM Books b
                    JOIN Publishers p ON b.[Publisher Id] = p.Id
                    WHERE p.Id != @PublisherId
                ", parameter)
                .ToListAsync();

            var publishers = await _context.Publishers
                .Select(p => new { p.Id, p.Name })
                .ToListAsync();
            ViewBag.Publishers = new SelectList(publishers, "Id", "Name");
            ViewBag.SelectedPublisherId = publisherId;

            return View("Index5", results);
        }

        public IActionResult Index6()
        {
            var genres = _context.Genres
                .Select(g => new SelectListItem { Value = g.Id.ToString(), Text = g.GenreName })
                .ToList();

            ViewBag.Genres = genres;
            ViewBag.SelectedGenres = new List<int>();
            return View(new List<string>());
        }

        [HttpPost]
        public async Task<IActionResult> SearchAuthorsByGenres(List<int> genreIds)
        {
            var genres = _context.Genres
                .Select(g => new SelectListItem { Value = g.Id.ToString(), Text = g.GenreName })
                .ToList();

            ViewBag.Genres = genres;
            ViewBag.SelectedGenres = genreIds;

            if (genreIds == null || !genreIds.Any())
            {
                ViewBag.Message = "Оберіть принаймні один жанр.";
                return View("Index6", new List<string>());
            }

            var parameters = string.Join(",", genreIds.Select((id, index) => "@p" + index));
            var sql = $@"
                SELECT a.*
                FROM Authors a
                WHERE NOT EXISTS (
                    SELECT g.Id
                    FROM Genres g
                    WHERE g.Id IN ({parameters})
                    EXCEPT
                    SELECT gb.[Genre Id]
                    FROM AuthorsBooks ab
                    JOIN GenresBooks gb ON ab.[Book Id] = gb.[Book Id]
                    WHERE ab.[Author Id] = a.Id
                )";

            var sqlParams = genreIds.Select((id, index) => new SqlParameter("@p" + index, id)).ToArray();

            var results = await _context.Authors
                .FromSqlRaw(sql, sqlParams)
                .Select(a => a.FullName)
                .ToListAsync();

            return View("Index6", results);
        }

        public IActionResult Index7()
        {
            var authors = _context.Authors
                .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.FullName })
                .ToList();

            ViewBag.Authors = authors;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SearchBooksByAuthors(List<int> authorIds)
        {
            if (authorIds == null || !authorIds.Any())
            {
                ViewBag.Authors = _context.Authors.Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.FullName }).ToList();
                ViewBag.Message = "Оберіть принаймні одного автора.";
                return View("Index7", new List<string>());
            }

            string authorList = string.Join(",", authorIds);

            var results = await _context.Books
                .FromSqlRaw($@"
                    SELECT b.*
                    FROM Books b
                    JOIN AuthorsBooks ab ON b.Id = ab.[Book Id]
                    WHERE ab.[Author Id] IN ({authorList})
                    AND NOT EXISTS (
                        SELECT 1 FROM (
                            SELECT br1.BookId, br1.Status
                            FROM BookReservations br1
                            JOIN (
                                SELECT BookId, MAX(ReservationDate) AS MaxDate
                                FROM BookReservations
                                GROUP BY BookId
                            ) br2 ON br1.BookId = br2.BookId AND br1.ReservationDate = br2.MaxDate
                        ) latest
                        WHERE latest.BookId = b.Id AND latest.Status <> 'Доступна'
                    )
                ")
                .Select(b => b.Title)
                .ToListAsync();

            ViewBag.Authors = _context.Authors.Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.FullName }).ToList();
            ViewBag.SelectedAuthors = authorIds;

            return View("Index7", results);
        }

        public IActionResult Index8()
        {
            ViewBag.Statuses = new List<SelectListItem>
            {
                new SelectListItem { Value = "Доступна", Text = "Доступна" },
                new SelectListItem { Value = "Недоступна", Text = "Недоступна" },
                new SelectListItem { Value = "Заброньована", Text = "Заброньована" },
                new SelectListItem { Value = "Прострочена", Text = "Прострочена" }
            };

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SearchBooksByStatuses(List<string> statuses)
        {
            ViewBag.Statuses = new List<SelectListItem>
    {
        new SelectListItem { Value = "Доступна", Text = "Доступна" },
        new SelectListItem { Value = "Недоступна", Text = "Недоступна" },
        new SelectListItem { Value = "Заброньована", Text = "Заброньована" },
        new SelectListItem { Value = "Прострочена", Text = "Прострочена" }
    };
            ViewBag.SelectedStatuses = statuses;

            if (statuses == null || !statuses.Any())
            {
                ViewBag.Message = "Оберіть хоча б один статус.";
                return View("Index8", new List<string>());
            }

            var sqlParams = statuses.Select((s, i) => new Microsoft.Data.SqlClient.SqlParameter($"@p{i}", s)).ToArray();
            var paramNames = string.Join(",", sqlParams.Select(p => p.ParameterName));

            string sql = $@"
                SELECT b.Title
                FROM Books b
                LEFT JOIN (
                    SELECT br.BookId, br.Status
                    FROM BookReservations br
                    INNER JOIN (
                        SELECT BookId, MAX(ReservationDate) AS MaxDate
                        FROM BookReservations
                        GROUP BY BookId
                    ) lastRes ON br.BookId = lastRes.BookId AND br.ReservationDate = lastRes.MaxDate
                ) AS lastStatus ON b.Id = lastStatus.BookId
                WHERE 
                    (lastStatus.Status IN ({paramNames}))
                    OR
                    (lastStatus.Status IS NULL AND 'Доступна' IN ({paramNames}))
            ";

            var result = await _context.Books
                .FromSqlRaw(sql, sqlParams)
                .Select(b => b.Title)
                .ToListAsync();

            return View("Index8", result);
        }
    }
}
