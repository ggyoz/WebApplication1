using Microsoft.AspNetCore.Mvc;
using CSR.Models;
using CSR.Services;
using System.Diagnostics;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.AspNetCore;
using FluentValidation.Results;
using CSR.Filters;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization; // Added for [Authorize]
using System.Security.Claims; // Added for ClaimTypes.NameIdentifier
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;


namespace CSR.Controllers
{
    [Authorize(Roles = "R3,R4")]
    public class UserController : Controller
    {
        private readonly UserService _userService;
        private readonly ICommCodeService _commCodeService;
        private readonly IAdminRelService _adminRelService;
        private readonly ILogger<UserController> _logger;
        private readonly IValidator<User> _validator;
        private readonly CorpService _corpService;
        private readonly DeptService _deptService;

        public UserController(UserService userService, ICommCodeService commCodeService, IAdminRelService adminRelService, CorpService corpService, DeptService deptService, ILogger<UserController> logger, IValidator<User> validator)
        {
            _userService = userService;
            _commCodeService = commCodeService;
            _adminRelService = adminRelService;
            _corpService = corpService;
            _deptService = deptService;
            _logger = logger;
            _validator = validator;
        }

        // GET: User
        public async Task<IActionResult> Index([FromQuery] UserSearchViewModel search, int pageNumber = 1, int pageSize = 15)
        {
            try
            {
                var pagedUsers = await _userService.GetUsersAsync(search, pageNumber, pageSize);

                ViewData["CorCd"] = search.CorCd;
                ViewData["DeptCd"] = search.DeptCd;
                ViewData["OfficeCd"] = search.OfficeCd;
                ViewData["TeamCd"] = search.TeamCd;

                // 법인
                ViewBag.CorCdList = await _corpService.GetSelectListByCorpAsync();
                ViewBag.DeptCdList = new List<SelectListItem>();
                ViewBag.OfficeCdList = new List<SelectListItem>();
                ViewBag.TeamCdList = new List<SelectListItem>();

                if (!string.IsNullOrEmpty(search.CorCd))
                {
                    ViewBag.DeptCdList = await _deptService.GetSelectListByDeptAsync(search.CorCd, "");
                }

                if (!string.IsNullOrEmpty(search.DeptCd))
                {
                    ViewBag.OfficeCdList = await _deptService.GetSelectListByDeptAsync(search.CorCd, search.DeptCd);
                }
                
                if (!string.IsNullOrEmpty(search.OfficeCd))
                {
                    ViewBag.TeamCdList = await _deptService.GetSelectListByDeptAsync(search.CorCd, search.OfficeCd);
                }
                
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

            // 유저정보 조회
            var user = await _userService.GetUserWithDetailsByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Prepare assigned responsibilities in a grouped format for display
            var assignedResponsibilitiesGrouped = new Dictionary<string, List<string>>();
            var allResponsibilitiesDict = await _commCodeService.GetResponsibilitiesAsync();
            var assignedMenuIds = await _adminRelService.GetAssignedMenuIdsForUserAsync(id);


            if (assignedMenuIds != null && assignedMenuIds.Any())
            {
                foreach (var category in allResponsibilitiesDict)
                {
                    var assignedInCategory = category.Value
                        .Where(r => r.CODEID != null && assignedMenuIds.Contains(r.CODEID.ToString()))
                        .Select(r => r.CODENM)
                        .ToList();

                    if (assignedInCategory.Any())
                    {
                        assignedResponsibilitiesGrouped[category.Key] = assignedInCategory;
                    }
                }
            }
            ViewBag.AssignedResponsibilitiesGrouped = assignedResponsibilitiesGrouped;
            user.AssignedResponsibilities = new List<string>(); 

            return View(user);
        }

        // GET: User/Create
        public async Task<IActionResult> Create()
        {

            ViewBag.CorCdList = await _corpService.GetSelectListByCorpAsync();

            return View();
        }

        // POST: User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AsyncValidationFilter]
        public async Task<IActionResult> Create([Bind("UserId,UserPwd,UserName,EmpNo,CorCd,DeptCd,OfficeCd,TeamCd,SysCd,MobPhoneNo,EmailAddr,UserDiv")] User user)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (string.IsNullOrEmpty(user.RegUserId)) user.RegUserId = "ADMIN";        
                    if (string.IsNullOrEmpty(user.UseYn)) user.UseYn = "Y";
                    if (string.IsNullOrEmpty(user.CustCd)) user.CustCd = "";
                    if (string.IsNullOrEmpty(user.VendCd)) user.VendCd = "";                    
                    if (string.IsNullOrEmpty(user.UserDiv)) user.UserDiv = "R1";

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

            // Fetch responsibilities            
            ViewBag.AllResponsibilities = await _commCodeService.GetResponsibilitiesAsync();
            var assignedMenuIds = await _adminRelService.GetAssignedMenuIdsForUserAsync(id);
            user.AssignedResponsibilities = assignedMenuIds;

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

        // POST: User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AsyncValidationFilter]
        public async Task<IActionResult> Edit(string id,
            [Bind("UserId,UserName,EmpNo,CorCd,DeptCd,OfficeCd,TeamCd,SysCd,BizCd,TelNo,MobPhoneNo,EmailAddr,UserStat,RetireDate,AdminFlag,CustCd,VendCd,AuthFlag,UserDiv,PwMissCount,RegDate,RegUserId,UpdateUserId,UseYn,AssignedResponsibilities")]
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
                    if (string.IsNullOrEmpty(user.UpdateUserId)) user.UpdateUserId = "ADMIN";

                    await _userService.UpdateUserAsync(user);
                    await _adminRelService.UpdateResponsibilitiesForUserAsync(user.UserId, user.AssignedResponsibilities, "ADMIN"); // TODO: Get actual admin user

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
                TempData["SuccessMessage"] = $"사용자 '{id}'의 비밀번호가 사원번호({newPassword})로 초기화되었습니다.";
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

        public async Task<IActionResult> SearchUsersJson(string searchText)
        {
            var users = await _userService.GetUsersForSearchAsync(searchText);
            return Json(users);
        }

        // GET: User/UploadExcel
        public IActionResult UploadExcel()
        {
            return View(new List<User>());
        }

        // POST: User/UploadExcel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            var userList = new List<User>();

            if (file != null && file.Length > 0)
            {
                try
                {
                    // 법인 정보 미리 로드
                    var allCorps = await _corpService.GetAllCorpsAsync();
                    var corpDict = allCorps.ToDictionary(c => c.CorNm ?? "", c => c.CorCd ?? "", StringComparer.OrdinalIgnoreCase);

                    // 부서 정보 미리 로드 및 레벨 계산
                    var allDepts = await _deptService.GetAllDeptsAsync();
                    var deptById = allDepts.ToDictionary(d => d.DeptId);

                    // 각 부서의 레벨을 계산하는 로컬 함수 (부서=1, 사무실=2, 팀=3...)
                    int GetDeptLevel(Dept d)
                    {
                        int level = 1;
                        var current = d;
                        while (current.ParentId != null && deptById.TryGetValue(current.ParentId.Value, out var parent))
                        {
                            level++;
                            current = parent;
                        }
                        return level;
                    }

                    // 레벨별/법인별 딕셔너리 생성
                    var deptsWithLevel = allDepts.Select(d => new { d.CorCd, d.DeptName, d.DeptCd, Level = GetDeptLevel(d) }).ToList();

                    var level1Lookup = deptsWithLevel.Where(x => x.Level == 1)
                        .GroupBy(x => x.CorCd).ToDictionary(g => g.Key!, g => g.GroupBy(x => x.DeptName!).ToDictionary(dg => dg.Key, dg => dg.First().DeptCd, StringComparer.OrdinalIgnoreCase));
                    
                    var level2Lookup = deptsWithLevel.Where(x => x.Level == 2)
                        .GroupBy(x => x.CorCd).ToDictionary(g => g.Key!, g => g.GroupBy(x => x.DeptName!).ToDictionary(dg => dg.Key, dg => dg.First().DeptCd, StringComparer.OrdinalIgnoreCase));
                    
                    var level3Lookup = deptsWithLevel.Where(x => x.Level >= 3)
                        .GroupBy(x => x.CorCd).ToDictionary(g => g.Key!, g => g.GroupBy(x => x.DeptName!).ToDictionary(dg => dg.Key, dg => dg.First().DeptCd, StringComparer.OrdinalIgnoreCase));

                    using (var stream = new MemoryStream())
                    {
                        await file.CopyToAsync(stream);
                        using (var workbook = new XLWorkbook(stream))
                        {
                            var worksheet = workbook.Worksheet(1);
                            var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip header

                            foreach (var row in rows)
                            {
                                string excelCorNm = row.Cell(6).GetValue<string>().Trim();
                                string mappedCorCd = corpDict.ContainsKey(excelCorNm) ? corpDict[excelCorNm] : "매핑x";
                                string userDivNm = row.Cell(10).GetValue<string>();
                                string userDivVal = "R1";
                                if( userDivNm == "팀장" || userDivNm == "실장"){
                                    userDivVal = "R2";
                                }
                                // 레벨별 매핑 함수
                                string GetMappedCd(string corCd, string name, Dictionary<string, Dictionary<string, string>> lookup)
                                {
                                    if (string.IsNullOrEmpty(name)) return "";
                                    string trimmedName = name.Trim();
                                    if (lookup.TryGetValue(corCd, out var corpMap))
                                    {
                                        if (corpMap.TryGetValue(trimmedName, out var cd)) return cd;
                                    }
                                    return trimmedName;
                                }

                                var user = new User
                                {
                                    UserId = row.Cell(1).GetValue<string>(),
                                    UserName = row.Cell(2).GetValue<string>(),
                                    EmpNo = row.Cell(1).GetValue<string>(),
                                    TelNo = row.Cell(3).GetValue<string>(),
                                    MobPhoneNo = row.Cell(4).GetValue<string>(),
                                    EmailAddr = row.Cell(5).GetValue<string>(),
                                    CorpName = excelCorNm,
                                    CorCd = mappedCorCd,
                                    DeptCd = GetMappedCd(mappedCorCd, row.Cell(7).GetValue<string>(), level1Lookup),
                                    OfficeCd = GetMappedCd(mappedCorCd, row.Cell(8).GetValue<string>(), level2Lookup),
                                    TeamCd = GetMappedCd(mappedCorCd, row.Cell(9).GetValue<string>(), level3Lookup),
                                    UserDiv = userDivVal,
                                    UseYn = "Y"
                                };
                                
                                if (!string.IsNullOrEmpty(user.UserId))
                                {
                                    userList.Add(user);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Excel upload error");
                    ModelState.AddModelError("", "엑셀 파일을 읽는 중 오류가 발생했습니다: " + ex.Message);
                }
            }
            else
            {
                ModelState.AddModelError("", "파일을 선택해주세요.");
            }

            return View(userList);
        }
        
        // POST: User/SaveExcelData
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveExcelData([FromBody] List<User> users)
        {
            if (users == null || !users.Any())
            {
                return Json(new { success = false, message = "저장할 데이터가 없습니다." });
            }

            try
            {
                int successCount = 0;
                string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";

                foreach (var user in users)
                {
                    if (string.IsNullOrEmpty(user.UserId)) continue;

                    user.RegUserId = currentUserId;
                    user.UpdateUserId = currentUserId;
                    
                    await _userService.UpsertUserAsync(user);
                    successCount++;
                }

                return Json(new { success = true, message = $"{successCount}건의 데이터가 성공적으로 저장/업데이트되었습니다." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving excel data");
                return Json(new { success = false, message = "저장 중 오류가 발생했습니다: " + ex.Message });
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}