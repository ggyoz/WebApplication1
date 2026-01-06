using Microsoft.AspNetCore.Mvc;
using CSR.Models;
using CSR.Services;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Collections.Generic;
using Microsoft.Extensions.Localization;

namespace CSR.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserService _userService;
        private readonly IStringLocalizer<SharedStrings> _sharedLocalizer;

        public AccountController(UserService userService, IStringLocalizer<SharedStrings> sharedLocalizer)
        {
            _userService = userService;
            _sharedLocalizer = sharedLocalizer;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var user = await _userService.AuthenticateAsync(model.UserId, model.Password);

                if (user != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.UserId),
                        new Claim(ClaimTypes.NameIdentifier, user.UserId),
                        new Claim("UserName", user.UserName),
                    };

                    if (user.AdminFlag)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                    }

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme, 
                        new ClaimsPrincipal(claimsIdentity), 
                        authProperties);

                    return LocalRedirect(returnUrl ?? "/Home/Index");
                }
                
                ModelState.AddModelError(string.Empty, _sharedLocalizer["InvalidLoginAttempt"]);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}
