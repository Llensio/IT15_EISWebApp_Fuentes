using System.ComponentModel.DataAnnotations;

public class RegisterViewModel
{
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string Role { get; set; }   // Maps to the Role dropdown
    public string Status { get; set; } // Maps to the Status dropdown
}
