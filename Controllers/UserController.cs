using Executive_Fuentes.Data;
using Executive_Fuentes.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;

namespace Executive_Fuentes.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // 1. LIST USERS
        public async Task<IActionResult> List()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        // CREATE - GET
        public IActionResult Index() => View();

        private async Task SendEmail(string toEmail, string username, string password)
        {
            var fromEmail = "pawpawfuentes73@gmail.com";
            var fromPassword = "eugwmqaruaxnpqlr";

            var smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(fromEmail, fromPassword),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail),
                Subject = "Your Account Credentials",
                Body = $"Hello {username},\n\nYour account has been created.\n\nUsername: {username}\nPassword: {password}\n\nYou can now Login :).",
                IsBodyHtml = false
            };

            message.To.Add(toEmail);

            await smtp.SendMailAsync(message);
        }
        // CREATE - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(UserViewModel model)
        {
            //if (!ModelState.IsValid) return View("Index", model);

            var user = new ApplicationUser
            {
                UserName = model.Username,
                Email = model.Email,
                FullName = model.FullName,
                Status = model.Status,
                DateCreated = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            //if (result.Succeeded)
            //{
            await _userManager.AddToRoleAsync(user, model.Role);
            await SendEmail(model.Email, model.Username, model.Password);
            return RedirectToAction("List");
            //}

            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View("Index", model);
        }

        // 2. ARCHIVE / TOGGLE STATUS
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.Status = (user.Status == "Active") ? "Inactive" : "Active";
            await _userManager.UpdateAsync(user);

            return RedirectToAction("List");
        }

        // 3. UPDATE - GET (Load existing data)
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var model = new UserViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                Username = user.UserName,
                Status = user.Status,
                Role = roles.FirstOrDefault() ?? "AuthorizedUser"
            };

            ViewBag.UserId = id;
            return View(model);
        }

        // UPDATE - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserViewModel model)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Username;
            user.Status = model.Status;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Update Role
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, model.Role);

                return RedirectToAction("List");
            }

            return View(model);
        }
    }
}