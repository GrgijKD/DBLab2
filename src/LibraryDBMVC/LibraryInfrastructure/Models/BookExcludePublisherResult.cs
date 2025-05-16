using Microsoft.EntityFrameworkCore;

namespace LibraryInfrastructure.Models
{
    [Keyless]
    public class BookExcludePublisherResult
    {
        public string Title { get; set; }
        public string PublisherName { get; set; }
    }
}
