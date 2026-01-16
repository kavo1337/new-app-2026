using System;
using System.Collections.Generic;

namespace app.API.Data.Models;

public partial class ProductMatrix
{
    public int ProductMatrixId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<VendingMachine> VendingMachine { get; set; } = new List<VendingMachine>();
}
