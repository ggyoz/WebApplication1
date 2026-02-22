using Microsoft.AspNetCore.Mvc;
using CSR.Models;
using CSR.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CSR.Controllers
{
    [Authorize] // Authorize all actions in this controller for any logged-in user
    public class MyPageController : Controller
    {
        private readonly MyPageService _myPageService;
        private readonly ILogger<MyPageController> _logger;
        private readonly CorpService _corpService;
        private readonly DeptService _deptService;
        
        public MyPageController(MyPageService myPageService, CorpService corpService, DeptService deptService, ILogger<MyPageController> logger)
        {
            _myPageService = myPageService;
            _corpService = corpService;
            _deptService = deptService;
            _logger = logger;
        }

        // GET: MyPage/Index
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _myPageService.GetMyInfoAsync(userId);
            if (user == null)
            {
                return NotFound();
            }


            // 조직도
            ViewBag.CorCdList = await _corpService.GetSelectListByCorpAsync();            
            ViewBag.DeptCdList = new List<SelectListItem>();
            ViewBag.OfficeCdList = new List<SelectListItem>();
            ViewBag.TeamCdList = new List<SelectListItem>();

            if (!string.IsNullOrEmpty(user.CorCd))
            {
                ViewBag.DeptCdList = await _deptService.GetSelectListByDeptAsync(user.CorCd, "");
            }

            if (!string.IsNullOrEmpty(user.DeptCd))
            {
                ViewBag.OfficeCdList = await _deptService.GetSelectListByDeptAsync(user.CorCd, user.DeptCd);
            }
            
            if (!string.IsNullOrEmpty(user.OfficeCd))
            {
                 ViewBag.TeamCdList = await _deptService.GetSelectListByDeptAsync(user.CorCd, user.OfficeCd);
            }

            return View(user);
        }

        // POST: MyPage/Index
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([Bind("UserName,TelNo,MobPhoneNo,EmailAddr,UserPwd,CorCd,DeptCd,OfficeCd,TeamCd")] User user){

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            user.UserId = userId;

            ModelState.Clear(); 
            
            if (TryValidateModel(user)) // Re-validate after clearing
            {

                try
                {
                    await _myPageService.UpdateMyInfoAsync(user, userId);
                    
                    var updatedUser = await _myPageService.GetMyInfoAsync(userId);

                    // Re-populate lists for the view
                    ViewBag.CorCdList = await _corpService.GetSelectListByCorpAsync();
                    ViewBag.DeptCdList = await _deptService.GetSelectListByDeptAsync(updatedUser.CorCd ?? "", "");
                    ViewBag.OfficeCdList = await _deptService.GetSelectListByDeptAsync(updatedUser.CorCd ?? "", updatedUser.DeptCd ?? "");
                    ViewBag.TeamCdList = await _deptService.GetSelectListByDeptAsync(updatedUser.CorCd ?? "", updatedUser.OfficeCd ?? "");

                    ViewBag.ShowSuccessAlert = true; // Set flag for alert
                    // Return the updated user info to the view for immediate display
                    return View(updatedUser);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "마이페이지 사용자 정보 수정 중 오류 발생: {Message}", ex.Message);
                    ModelState.AddModelError("", $"회원 정보 업데이트에 실패했습니다: {ex.Message}");
                }
            }
            
            // If model state is not valid or an exception occurred, return to the view with errors
            var fullUser = await _myPageService.GetMyInfoAsync(userId);  

            if (fullUser != null)
            {
                // Repopulate the model with the submitted values to show the user what they entered
                fullUser.UserName = user.UserName;
                fullUser.TelNo = user.TelNo;
                fullUser.MobPhoneNo = user.MobPhoneNo;
                fullUser.EmailAddr = user.EmailAddr;
                
                // Re-populate lists for the view
                ViewBag.CorCdList = await _corpService.GetSelectListByCorpAsync();
                ViewBag.DeptCdList = await _deptService.GetSelectListByDeptAsync(fullUser.CorCd ?? "", "");
                ViewBag.OfficeCdList = await _deptService.GetSelectListByDeptAsync(fullUser.CorCd ?? "", fullUser.DeptCd ?? "");
                ViewBag.TeamCdList = await _deptService.GetSelectListByDeptAsync(fullUser.CorCd ?? "", fullUser.OfficeCd ?? "");

                return View(fullUser);
            }
            
            return View(user);
        }
    }
}
