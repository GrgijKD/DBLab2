using Microsoft.EntityFrameworkCore;

namespace LibraryInfrastructure.Models
{
    [Keyless]
    public class BookStatusSearchResult
    {
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
