using Microsoft.EntityFrameworkCore;

namespace LibraryInfrastructure.Models
{
    [Keyless]
    public class WorkerSearchResult
    {
        public string FullName { get; set; } = string.Empty;
        public int BooksAdded { get; set; }
    }
}
