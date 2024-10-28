using System;
using System.Collections.Generic;

namespace EFAPIMavel.Models;

public partial class TblAvenger
{
    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public virtual ICollection<TblContact> TblContacts { get; set; } = new List<TblContact>();
}
