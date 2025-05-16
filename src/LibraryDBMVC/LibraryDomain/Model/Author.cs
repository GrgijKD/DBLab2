using System;
using System.Collections.Generic;

namespace LibraryDomain.Model;

public partial class Author: Entity
{
    public string FullName { get; set; } = null!;

    public string Country { get; set; } = null!;

    public virtual ICollection<AuthorsBook> AuthorsBooks { get; set; } = new List<AuthorsBook>();
}
