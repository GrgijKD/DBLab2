using System;
using System.Collections.Generic;

namespace LibraryDomain.Model;

public partial class GenresBook: Entity
{
    public int GenreId { get; set; }

    public int BookId { get; set; }

    public virtual Book Book { get; set; } = null!;

    public virtual Genre Genre { get; set; } = null!;
}
