using System;
using System.Collections.Generic;

namespace LibraryDomain.Model;

public partial class Genre: Entity
{
    public string GenreName { get; set; } = null!;

    public virtual ICollection<GenresBook> GenresBooks { get; set; } = new List<GenresBook>();
}
