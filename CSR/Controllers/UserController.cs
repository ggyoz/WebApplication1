using Microsoft.AspNetCore.Mvc;
using CSR.Models;
using CSR.Services;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CSR.Controllers
{
    public class UserController : Controller
    {
        private readonly UserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(UserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // GET: User
        public async Task<IActionResult> Index([FromQuery] UserSearchViewModel search, int pageNumber = 1, int pageSize = 15)
        {

            try
            {
                var pagedUsers = await _userService.GetUsersAsync(search, pageNumber, pageSize);
                
                var viewModel = new UserIndexViewModel
                {
                    Users = pagedUsers,
                    Search = search
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 목록을 가져오는 중 오류 발생: {Message}", ex.Message);
                ViewBag.Error = $"사용자 목록을 불러오는데 실패했습니다: {ex.Message}";
                
                var viewModel = new UserIndexViewModel
                {
                    Users = new PagedResult<User>(new List<User>(), 0, pageNumber, pageSize),
                    Search = search
                };
                return View(viewModel);
            }
        }

        // GET: User/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: User/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("UserId,UserPwd,UserName,EmpNo,CorCd,DeptCd,OfficeCd,TeamCd,SysCd,MobPhoneNo,EmailAddr")]
            User user)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (string.IsNullOrEmpty(user.RegUserId)) user.RegUserId = "ADMIN";        
                    if (string.IsNullOrEmpty(user.UseYn)) user.UseYn = "Y";
                    if (string.IsNullOrEmpty(user.CustCd)) user.CustCd = "";
                    if (string.IsNullOrEmpty(user.VendCd)) user.VendCd = "";                    
                    if (string.IsNullOrEmpty(user.UserDiv)) user.UserDiv = "";                    

                    await _userService.CreateUserAsync(user);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "사용자 생성 중 오류 발생");
                    ModelState.AddModelError("", $"사용자 생성에 실패했습니다: {ex.Message}");
                }
            }
            return View(user);
        }

        // GET: User/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id,
            [Bind("UserId,UserPwd,UserName,EmpNo,CorCd,DeptCd,OfficeCd,TeamCd,SysCd,BizCd,TelNo,MobPhoneNo,EmailAddr,UserStat,RetireDate,AdminFlag,CustCd,VendCd,AuthFlag,UserDiv,PwMissCount,RegDate,RegUserId,UpdateUserId,UseYn")]
            User user)
        {
            if (id != user.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // UpdateUserId should be set, let DB handle UpdateDate
                    if (string.IsNullOrEmpty(user.UpdateUserId)) user.UpdateUserId = "ADMIN"; // Replace with actual user later

                    await _userService.UpdateUserAsync(user);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "사용자 수정 중 오류 발생");
                    ModelState.AddModelError("", $"사용자 수정에 실패했습니다: {ex.Message}");
                }
            }
            return View(user);
        }

        // GET: User/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: User/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                await _userService.DeleteUserAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 삭제 중 오류 발생");
                TempData["Error"] = $"사용자 삭제에 실패했습니다: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: User/ResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "사용자 ID가 제공되지 않았습니다.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                string newPassword = await _userService.ResetPasswordAsync(id);
                TempData["SuccessMessage"] = $"사용자 ID '{id}'의 비밀번호가 성공적으로 사원번호({newPassword})로 초기화되었습니다.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "비밀번호 초기화 중 오류 발생: {Message}", ex.Message);
                TempData["ErrorMessage"] = "비밀번호 초기화 중 오류가 발생했습니다.";
            }

            return RedirectToAction(nameof(Details), new { id = id });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}