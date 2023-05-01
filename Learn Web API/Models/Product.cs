using System;
using System.Collections.Generic;

namespace Learn_Web_API.Models;

public partial class Product
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
}
