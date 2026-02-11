using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CSR.Models;
using CSR.Services;
using Microsoft.AspNetCore.Localization; // 추가
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace CSR.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserService _userService;

        public HomeController(ILogger<HomeController> logger, UserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        public IActionResult Index()
        {

            // 세션이 없으면 로그인페이지로 튕겨냄
            var sessionUserId = HttpContext.Session.GetString("UserId");
            
            if (string.IsNullOrEmpty(sessionUserId) )
            {
                return RedirectToAction("Login", "Account");
            }



            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> KeepAlive()
        {
            // Check if user is authenticated and if session is empty
            if (User.Identity?.IsAuthenticated == true && string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await _userService.GetUserByIdAsync(userId);
                    if (user != null)
                    {
                        // Re-populate session
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
                    }
                }
            }
            return Ok();
        }

        [HttpPost] // POST 요청으로만 동작하도록 설정
        [AllowAnonymous]  //모든 사용자
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { 
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true // 필수 쿠키로 설정 (GDPR 등 고려)
                }
            );

            return LocalRedirect(returnUrl);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
