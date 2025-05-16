using Microsoft.EntityFrameworkCore;

namespace LibraryInfrastructure.Models
{
    [Keyless]
    public class BookSearchResult
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string Genre { get; set; }
    }
}
