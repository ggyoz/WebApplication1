using Microsoft.AspNetCore.Mvc;
using CSR.Models;
using CSR.Services;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Collections.Generic;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Authorization;

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
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var user = await _userService.AuthenticateAsync(model.UserId!, model.Password!);

                if (user != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.UserId),
                        new Claim(ClaimTypes.NameIdentifier, user.UserId),
                        new Claim("UserName", user.UserName),
                    };

                    // Add UserDiv as a role claim if it exists
                    if (!string.IsNullOrEmpty(user.UserDiv))
                    {
                        claims.Add(new Claim(ClaimTypes.Role, user.UserDiv));
                    }
                    
                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true, // 무조건 영구 쿠키로 생성하여 브라우저 재시작 시에도 유지
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1) // 1일 유지
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme, 
                        new ClaimsPrincipal(claimsIdentity), 
                        authProperties);

                    // User정보 세선 등록
                    HttpContext.Session.SetString("UserId", user.UserId);
                    HttpContext.Session.SetString("UserName", user.UserName);
                    HttpContext.Session.SetString("CorCd", user.CorCd);
                    HttpContext.Session.SetString("DeptCd", user.DeptCd);
                    HttpContext.Session.SetString("OfficeCd", user.OfficeCd);
                    HttpContext.Session.SetString("TeamCd", user.TeamCd);
                    HttpContext.Session.SetString("SysCd", user.SysCd);                    
                    HttpContext.Session.SetString("TelNo", user.TelNo);
                    HttpContext.Session.SetString("MobPhoneNo", user.MobPhoneNo);
                    HttpContext.Session.SetString("EmailAddr", user.EmailAddr);

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
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        [ActionName("Logout")]
        public async Task<IActionResult> LogoutGet()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }
    }
}
