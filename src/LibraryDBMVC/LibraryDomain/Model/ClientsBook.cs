using System;
using System.Collections.Generic;

namespace LibraryDomain.Model;

public partial class ClientsBook: Entity
{
    public string ClientId { get; set; }

    public int BookId { get; set; }

    public DateOnly BorrowingDate { get; set; }

    public DateOnly? ReturnDate { get; set; }

    public virtual Book Book { get; set; } = null!;

    public virtual Client Client { get; set; } = null!;
}
