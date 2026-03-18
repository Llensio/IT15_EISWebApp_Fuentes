using Executive_Fuentes.Data;
using Executive_Fuentes.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

public class AuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuditService(
        ApplicationDbContext context,
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }

    public async Task LogAsync(string action, string module, string description)
    {
        var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
        var roles = await _userManager.GetRolesAsync(user);

        var audit = new AuditLog
        {
            UserId = user.Id,
            UserName = user.UserName,
            Role = roles.FirstOrDefault(),
            Action = action,
            Module = module,
            Description = description,
            DateTime = DateTime.Now,
            IPAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        _context.AuditLogs.Add(audit);
        await _context.SaveChangesAsync();
    }
}