using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LibraryDomain.Model;

public partial class Client
{
    [Key]
    public string Id { get; set; }

    public string FullName { get; set; } = null!;

    public virtual ICollection<ClientsBook> ClientsBooks { get; set; } = new List<ClientsBook>();
}
