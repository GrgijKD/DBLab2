using System;
using System.Collections.Generic;

namespace LibraryDomain.Model;

public partial class Worker: Entity
{
    public string FullName { get; set; } = null!;

    public string ApplicationUserId { get; set; }

    public ApplicationUser ApplicationUser { get; set; }

    public virtual ICollection<Book> Books { get; set; } = new List<Book>();
}
