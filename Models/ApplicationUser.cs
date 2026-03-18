using Microsoft.AspNetCore.Identity;
using System;


public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; }
    public string Status { get; set; }  // Active / Inactive
    public DateTime DateCreated { get; set; }
}
