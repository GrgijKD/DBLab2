using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LibraryDomain.Model;

public partial class Book: Entity
{
    public string Title { get; set; } = null!;

    public int PublisherId { get; set; }

    public virtual Publisher Publisher { get; set; }

    public int AddedBy { get; set; }

    public virtual Worker Worker { get; set; }

    public virtual ICollection<AuthorsBook> AuthorsBooks { get; set; } = new List<AuthorsBook>();

    public virtual ICollection<ClientsBook> ClientsBooks { get; set; } = new List<ClientsBook>();

    public virtual ICollection<GenresBook> GenresBooks { get; set; } = new List<GenresBook>();

    public virtual ICollection<BookReservation> BookReservations { get; set; } = new List<BookReservation>();
}
