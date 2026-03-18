using System.Net;
using System.Net.Mail;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Executive_Fuentes.Models;
using Executive_Fuentes.Data;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    // Google test secret key (works for development)
    private const string RecaptchaSecret = "6LfwpIQsAAAAAItmChuRTyt5WgPwS6IZKZruh87y";

    public AccountController(UserManager<ApplicationUser> userManager,
                             SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // GET: /Account/Login
    [HttpGet]
    public IActionResult Login() => View();

    // POST: /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        // CAPTCHA VALIDATION
        var captchaResponse = Request.Form["g-recaptcha-response"];

        using (var client = new HttpClient())
        {
            var response = await client.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={RecaptchaSecret}&response={captchaResponse}",
                null);

            var json = await response.Content.ReadAsStringAsync();
            var captchaResult = JsonSerializer.Deserialize<RecaptchaResponse>(json);

            if (!captchaResult.success)
            {
                ModelState.AddModelError("", "Captcha verification failed.");
                return View(model);
            }
        }

        var user = await _userManager.FindByNameAsync(model.Username);

        if (user != null && user.Status == "Active")
        {
            var result = await _signInManager.PasswordSignInAsync(
                model.Username,
                model.Password,
                model.RememberMe,
                false);

            if (result.Succeeded)
            {
                if (await _userManager.IsInRoleAsync(user, "SuperAdmin"))
                    return RedirectToAction("AdminDashboard", "Dashboard");

                return RedirectToAction("Index", "Dashboard");
            }
        }

        ModelState.AddModelError("", "Invalid login attempt or account inactive.");
        return View(model);
    }

   

    // POST: /Account/CreateUser
    //[HttpPost]
    //[Authorize(Roles = "SuperAdmin")]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> CreateUser(RegisterViewModel model)
    //{
    //    if (!ModelState.IsValid) return View(model);

    //    var user = new ApplicationUser
    //    {
    //        FullName = model.FullName,
    //        Email = model.Email,
    //        UserName = model.Username,
    //        Status = model.Status,
    //        DateCreated = DateTime.Now
    //    };

    //    var result = await _userManager.CreateAsync(user, model.Password);

    //    if (result.Succeeded)
    //    {
    //        await _userManager.AddToRoleAsync(user, model.Role);

    //        await SendEmail(model.Email, model.Username, model.Password);

    //        return RedirectToAction("AdminDashboard", "Dashboard");
    //    }

    //    foreach (var error in result.Errors)
    //    {
    //        ModelState.AddModelError("", error.Description);
    //    }

    //    return View(model);
    //}

    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login");
    }
}

public class RecaptchaResponse
{
    public bool success { get; set; }
}