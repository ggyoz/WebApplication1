using CSR.Models;
using CSR.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Rendering;
using CSR.Authorization;
using ClosedXML.Excel;
using System.IO;
using System.Linq;

namespace CSR.Controllers
{
    [Authorize]
    public class ReqController : Controller
    {
        private readonly IReqService _reqService;
        private readonly ICommCodeService _commCodeService;
        private readonly UserService _userService;
        private readonly IAdminRelService _adminRelService;
        private readonly IAuthorizationService _authorizationService;
        private readonly ILogger<ReqController> _logger;
        private readonly CorpService _corpService;
        private readonly DeptService _deptService;

        public ReqController(IReqService reqService, ICommCodeService commCodeService, UserService userService, IAdminRelService adminRelService, CorpService corpService, ILogger<ReqController> logger, DeptService deptService, IAuthorizationService authorizationService)
        {
            _reqService = reqService;
            _commCodeService = commCodeService;
            _adminRelService = adminRelService;
            _corpService = corpService;
            _userService = userService;
            _logger = logger;
            _deptService = deptService;
            _authorizationService = authorizationService;
        }

        public async Task<IActionResult> Index(
            int pageNumber = 1, 
            int pageSize = 10, 
            string? reqTypeCd = null,
            string? procStatusCd = null,
            string? priorityCd = null,
            string? reqDate = null,
            string? dueDate = null,
            string? expectDate = null,
            string? regId = null, // Assuming this is ReqInfo.REQID for search
            string? reqUserName = null,
            string? resUserName = null,
            string? searchValue = null,
            string? corCd = null,
            string? deptCd = null,
            string? officeCd = null,
            string? teamCd = null,
            string? assignedResponsibilities = null
            ) // This is for title search
        {

            // 세션이 없으면 로그인페이지로 튕겨냄
            var sessionCorCd = HttpContext.Session.GetString("CorCd");
            var sessionTeamCd = HttpContext.Session.GetString("TeamCd");
            var sessionOfficeCd = HttpContext.Session.GetString("OfficeCd");            
            var sessionDeptCd = HttpContext.Session.GetString("DeptCd");            

            // 팀원 및 팀장 조회 제한
            if( !User.IsInRole("R3") && !User.IsInRole("R4") ){

                if ( string.IsNullOrEmpty(sessionOfficeCd) )
                {
                    return RedirectToAction("Login", "Account");
                }

            }

            var respList = string.IsNullOrEmpty(assignedResponsibilities) 
                ? new List<string>() 
                : assignedResponsibilities.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            ViewData["ReqTypeCd"] = reqTypeCd;
            ViewData["ProcStatusCd"] = procStatusCd;
            ViewData["PriorityCd"] = priorityCd;
            ViewData["ReqDate"] = reqDate;
            ViewData["DueDate"] = dueDate;
            ViewData["ExpectDate"] = expectDate;
            ViewData["RegId"] = regId;
            ViewData["ReqUserName"] = reqUserName;
            ViewData["ResUserName"] = resUserName;
            ViewData["SearchValue"] = searchValue;
            ViewData["CorCd"] = corCd;
            ViewData["DeptCd"] = deptCd;
            ViewData["OfficeCd"] = officeCd;
            ViewData["TeamCd"] = teamCd;
            ViewData["AssignedResponsibilities"] = respList;

            ViewBag.SystemCodes = await _commCodeService.GetSelectListByPCodeAsync(19);
            ViewBag.ReqTypes = await _commCodeService.GetSelectListByPCodeAsync(13);
            ViewBag.PriorityCodes = await _commCodeService.GetSelectListByPCodeAsync(1);
            ViewBag.ProcStatusCodes = await _commCodeService.GetSelectListByPCodeAsync(61); 
            ViewBag.AllResponsibilities = await _commCodeService.GetResponsibilitiesAsync();

            // 법인
            ViewBag.CorCdList = await _corpService.GetSelectListByCorpAsync();
            ViewBag.DeptCdList = new List<SelectListItem>();
            ViewBag.OfficeCdList = new List<SelectListItem>();
            ViewBag.TeamCdList = new List<SelectListItem>();

            if (!string.IsNullOrEmpty(corCd))
            {
                ViewBag.DeptCdList = await _deptService.GetSelectListByDeptAsync(corCd, "");
            }

            if (!string.IsNullOrEmpty(deptCd))
            {
                ViewBag.OfficeCdList = await _deptService.GetSelectListByDeptAsync(corCd, deptCd);
            }
            
            if (!string.IsNullOrEmpty(officeCd))
            {
                 ViewBag.TeamCdList = await _deptService.GetSelectListByDeptAsync(corCd, officeCd);
            }

            if( User.IsInRole("R1")) {
                corCd = sessionCorCd;
                teamCd = sessionTeamCd;
            }else if( User.IsInRole("R2")){ 
                corCd = sessionCorCd;
                deptCd = sessionDeptCd;
            }
            
            var pagedResult = await _reqService.GetReqInfosAsync(
                pageNumber, 
                pageSize, 
                reqTypeCd,
                procStatusCd,
                priorityCd,
                reqDate,
                dueDate,
                expectDate,
                regId,
                reqUserName,
                resUserName,
                searchValue,
                corCd, 
                deptCd, 
                officeCd, 
                teamCd,
                respList);
            return View(pagedResult);
        }

        public async Task<IActionResult> ExportToExcel(
            string? reqTypeCd = null,
            string? procStatusCd = null,
            string? priorityCd = null,
            string? reqDate = null,
            string? dueDate = null,
            string? expectDate = null,
            string? regId = null,
            string? reqUserName = null,
            string? resUserName = null,
            string? searchValue = null,
            string? corCd = null,
            string? deptCd = null,
            string? officeCd = null,
            string? teamCd = null,
            string? assignedResponsibilities = null
            )
        {
            // 세션이 없으면 로그인페이지로 튕겨냄
            var sessionCorCd = HttpContext.Session.GetString("CorCd");
            var sessionTeamCd = HttpContext.Session.GetString("TeamCd");
            var sessionOfficeCd = HttpContext.Session.GetString("OfficeCd");            
            var sessionDeptCd = HttpContext.Session.GetString("DeptCd");  

            if (!User.IsInRole("R3") && !User.IsInRole("R4"))
            {
                if (string.IsNullOrEmpty(sessionOfficeCd))
                {
                    return RedirectToAction("Login", "Account");
                }
            }

            corCd = sessionCorCd;

            if (User.IsInRole("R1"))
            {
                teamCd = sessionTeamCd;
            }else if (User.IsInRole("R2"))
            {
                deptCd = sessionDeptCd;
            }

            var respList = string.IsNullOrEmpty(assignedResponsibilities) 
                ? new List<string>() 
                : assignedResponsibilities.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            var requests = await _reqService.GetAllReqInfosAsync(
                reqTypeCd,
                procStatusCd,
                priorityCd,
                reqDate,
                dueDate,
                expectDate,
                regId,
                reqUserName,
                resUserName,
                searchValue,
                corCd,
                deptCd,
                officeCd,
                teamCd,
                respList);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Requests");
                var currentRow = 1;

                // Header
                worksheet.Cell(currentRow, 1).Value = "요청번호";
                worksheet.Cell(currentRow, 2).Value = "제목";
                worksheet.Cell(currentRow, 3).Value = "소속(법인)";
                worksheet.Cell(currentRow, 4).Value = "소속(본부)";
                worksheet.Cell(currentRow, 5).Value = "소속(사업장)";
                worksheet.Cell(currentRow, 6).Value = "소속(팀)";
                worksheet.Cell(currentRow, 7).Value = "요청구분";
                worksheet.Cell(currentRow, 8).Value = "대상시스템";
                worksheet.Cell(currentRow, 9).Value = "요청메뉴";
                worksheet.Cell(currentRow, 10).Value = "요청자";
                worksheet.Cell(currentRow, 11).Value = "요청일";
                worksheet.Cell(currentRow, 12).Value = "조치자";
                worksheet.Cell(currentRow, 13).Value = "완료요구일";
                worksheet.Cell(currentRow, 14).Value = "완료예정일";
                worksheet.Cell(currentRow, 15).Value = "진행상태";
                worksheet.Cell(currentRow, 16).Value = "긴급도";

                // Header Styling
                var headerRange = worksheet.Range(currentRow, 1, currentRow, 16);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Data
                foreach (var req in requests)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = req.REQID;
                    worksheet.Cell(currentRow, 2).Value = req.TITLE;
                    worksheet.Cell(currentRow, 3).Value = req.CorpName;
                    worksheet.Cell(currentRow, 4).Value = req.DeptName;
                    worksheet.Cell(currentRow, 5).Value = req.OfficeName;
                    worksheet.Cell(currentRow, 6).Value = req.TeamName;
                    worksheet.Cell(currentRow, 7).Value = req.ReqTypeName;
                    worksheet.Cell(currentRow, 8).Value = req.SystemName;
                    worksheet.Cell(currentRow, 9).Value = req.ReqMenuName;
                    worksheet.Cell(currentRow, 10).Value = req.ReqUserName;
                    worksheet.Cell(currentRow, 11).Value = req.REQDATE;
                    worksheet.Cell(currentRow, 12).Value = req.ResUserName;
                    worksheet.Cell(currentRow, 13).Value = req.DUEDATE;
                    worksheet.Cell(currentRow, 14).Value = req.EXPECTDATE;
                    worksheet.Cell(currentRow, 15).Value = req.ProcStatusName;
                    worksheet.Cell(currentRow, 16).Value = req.PriorityName;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"RequestList_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
                }
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var reqInfo = await _reqService.GetReqInfoByIdAsync(id);

            if (reqInfo == null)
            {
                return NotFound();
            }

            var authorizationResult = await _authorizationService.AuthorizeAsync(User, reqInfo, new SameTeamRequirement());
            if (!authorizationResult.Succeeded)
            {
                return Forbid();
            }
            
            ViewBag.ImportantCodes = await _commCodeService.GetSelectListByPCodeAsync(78); // CODEID = 78  중요도
            ViewBag.difficultCodes = await _commCodeService.GetSelectListByPCodeAsync(7); // CODEID = 7  난이도

            var users = await _userService.GetManagerOrHigherUsersForDropDownAsync();
            ViewBag.UserList = users.Select(u => new SelectListItem
            {
                Value = u.UserId,
                Text = u.UserName,
                Selected = (u.UserId == reqInfo.RESUSERID)
            }).ToList();
            
            return View(reqInfo);
        }
        
        [Authorize] 
        public async Task<IActionResult> Create()
        {
            // 시스템
            ViewBag.SystemCodes = await _commCodeService.GetSelectListByPCodeAsync(19);
            // 요청유형
            ViewBag.ReqTypes = await _commCodeService.GetSelectListByPCodeAsync(13);
            // 긴급도 
            ViewBag.PriorityCodes = await _commCodeService.GetSelectListByPCodeAsync(1);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userService.GetUserWithDetailsByIdAsync(userId);

            var reqInfo = new ReqInfo
            {
                REQDATE = DateTime.Now,
                EXPECTDATE = DateTime.Now.AddDays(7),
                ReqUserName = user?.UserName,                
                ReqUserEmail = user?.EmailAddr,
                ReqUserTel = user.MobPhoneNo != null ? user.MobPhoneNo : user.TelNo,                
                CorpName = user?.CorpName,
                DeptName = user?.DeptName,
                OfficeName = user?.OfficeName,
                TeamName = user?.TeamName,
                CORCD = user?.CorCd,
                DEPTCD = user?.DeptCd,
                OFFICECD = user?.OfficeCd,
                TEAMCD = user?.TeamCd,
                REQUSERID = userId,
                REG_USERID = userId,

            };

            return View(reqInfo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(ReqInfo reqInfo, List<IFormFile> files)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    reqInfo.REG_USERID = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";                    
                    await _reqService.CreateReqInfoAsync(reqInfo, files);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating requirement.");
                    ModelState.AddModelError("", "An error occurred while creating the requirement.");
                }
            }
            else
            {
                // Log ModelState errors
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                _logger.LogWarning("ModelState is invalid. Errors:");
                foreach (var error in errors)
                {
                    _logger.LogWarning(error.ErrorMessage);
                }
            }
            
            // If we got this far, something failed, redisplay form
            ViewBag.SystemCodes = await _commCodeService.GetSelectListByPCodeAsync(19);
            ViewBag.ReqTypes = await _commCodeService.GetSelectListByPCodeAsync(13);
            ViewBag.PriorityCodes = await _commCodeService.GetSelectListByPCodeAsync(1);
            return View(reqInfo);
        }

        [Authorize]
        public async Task<IActionResult> Answer(int id)
        {
            var reqInfo = await _reqService.GetReqInfoByIdAsync(id);
            if (reqInfo == null)
            {
                return NotFound();
            }

            var authorizationResult = await _authorizationService.AuthorizeAsync(User, reqInfo, new SameTeamRequirement());
            if (!authorizationResult.Succeeded)
            {
                return Forbid();
            }

            ViewBag.SystemCodes = await _commCodeService.GetSelectListByPCodeAsync(19);
            ViewBag.ReqTypes = await _commCodeService.GetSelectListByPCodeAsync(13);
            ViewBag.PriorityCodes = await _commCodeService.GetSelectListByPCodeAsync(1);
            ViewBag.ProcStatusCodes = await _commCodeService.GetSelectListByPCodeAsync(61);
            ViewBag.ImportantCodes = await _commCodeService.GetSelectListByPCodeAsync(78);
            ViewBag.difficultCodes = await _commCodeService.GetSelectListByPCodeAsync(7);

            return View(reqInfo);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Answer(int id, ReqInfo reqInfo, List<IFormFile> newFiles, [FromForm]List<int> deletedFiles)
        {
            if (id != reqInfo.REQID)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _reqService.UpdateReqInfoAsync(reqInfo, newFiles, deletedFiles, "NonHistory");
                    return RedirectToAction(nameof(Details), new { id = reqInfo.REQID });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving answer for requirement.");
                    ModelState.AddModelError("", "An error occurred while saving the answer.");
                }
            }

            // If ModelState is invalid, return to Answer view
            ViewBag.SystemCodes = await _commCodeService.GetSelectListByPCodeAsync(19);
            ViewBag.ReqTypes = await _commCodeService.GetSelectListByPCodeAsync(13);
            ViewBag.PriorityCodes = await _commCodeService.GetSelectListByPCodeAsync(1);
            ViewBag.ProcStatusCodes = await _commCodeService.GetSelectListByPCodeAsync(61);
            ViewBag.ImportantCodes = await _commCodeService.GetSelectListByPCodeAsync(78);
            ViewBag.difficultCodes = await _commCodeService.GetSelectListByPCodeAsync(7);

            var originalReq = await _reqService.GetReqInfoByIdAsync(id);
            reqInfo.AttachFiles = originalReq?.AttachFiles ?? new List<ReqFile>();
            
            return View(reqInfo);
        }


        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var reqInfo = await _reqService.GetReqInfoByIdAsync(id);

            if (reqInfo == null)
            {
                return NotFound();
            }

            var authorizationResult = await _authorizationService.AuthorizeAsync(User, reqInfo, new SameTeamRequirement());
            if (!authorizationResult.Succeeded)
            {
                return Forbid();
            }

            var user = await _userService.GetUserWithDetailsByIdAsync(reqInfo.REQUSERID);

            reqInfo.ReqUserName = user?.UserName;
            reqInfo.ReqUserEmail = user?.EmailAddr;
            reqInfo.ReqUserTel = user?.MobPhoneNo;
            reqInfo.CorpName = user?.CorpName;
            reqInfo.DeptName = user?.DeptName;
            reqInfo.OfficeName = user?.OfficeName;
            reqInfo.TeamName = user?.TeamName;

            ViewBag.SystemCodes = await _commCodeService.GetSelectListByPCodeAsync(19);
            ViewBag.ReqTypes = await _commCodeService.GetSelectListByPCodeAsync(13);
            ViewBag.PriorityCodes = await _commCodeService.GetSelectListByPCodeAsync(1);
            ViewBag.ProcStatusCodes = await _commCodeService.GetSelectListByPCodeAsync(61); 
            

            return View(reqInfo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, ReqInfo reqInfo, List<IFormFile> newFiles, [FromForm]List<int> deletedFiles)
        {
            if (id != reqInfo.REQID)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var historyReasonValue = "";
                    reqInfo.UPDATE_USERID = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
                    await _reqService.UpdateReqInfoAsync(reqInfo, newFiles, deletedFiles, historyReasonValue);
                    return RedirectToAction(nameof(Details), new { id = reqInfo.REQID });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating requirement.");
                    ModelState.AddModelError("", "An error occurred while updating the requirement.");
                }
            }

            // If ModelState is invalid, return to Edit view
            ViewBag.SystemCodes = await _commCodeService.GetSelectListByPCodeAsync(19);
            ViewBag.ReqTypes = await _commCodeService.GetSelectListByPCodeAsync(13);
            ViewBag.PriorityCodes = await _commCodeService.GetSelectListByPCodeAsync(1);
            ViewBag.ProcStatusCodes = await _commCodeService.GetSelectListByPCodeAsync(61); 
            var originalReq = await _reqService.GetReqInfoByIdAsync(id);
            reqInfo.AttachFiles = originalReq?.AttachFiles ?? new List<ReqFile>();
            
            return View(reqInfo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> SubmitAnswer(int id, ReqInfo reqInfo, List<IFormFile> newFiles, [FromForm]List<int> deletedFiles)
        {
            if (id != reqInfo.REQID)
            {
                return BadRequest();
            }

            // 답변등록 시 유효성 제외 목록
            ModelState.Remove(nameof(ReqInfo.TITLE));
            ModelState.Remove(nameof(ReqInfo.CONTENTS_HTML));
            ModelState.Remove(nameof(ReqInfo.DUEDATE));            
            ModelState.Remove(nameof(ReqInfo.REQDATE));
            ModelState.Remove(nameof(ReqInfo.REQTYPE));
            ModelState.Remove(nameof(ReqInfo.REQMENU));
            ModelState.Remove(nameof(ReqInfo.REQMENU_ETC));
            ModelState.Remove(nameof(ReqInfo.RESUSERID));
            ModelState.Remove(nameof(ReqInfo.SYSTEMCD));
            ModelState.Remove(nameof(ReqInfo.REQUSERID));
            ModelState.Remove(nameof(ReqInfo.PRIORITYCD));
            ModelState.Remove(nameof(ReqInfo.CORCD));
            ModelState.Remove(nameof(ReqInfo.DEPTCD));
            ModelState.Remove(nameof(ReqInfo.OFFICECD));
            ModelState.Remove(nameof(ReqInfo.TEAMCD));
            ModelState.Remove(nameof(ReqInfo.REG_USERID));

            var modelToReturn = await _reqService.GetReqInfoByIdAsync(id);

            if (modelToReturn == null) { return NotFound(); }

            modelToReturn.DFCLTCD = reqInfo.DFCLTCD;
            modelToReturn.IMPTCD = reqInfo.IMPTCD;
            modelToReturn.MAN_DAY = reqInfo.MAN_DAY;
            modelToReturn.BXTID = reqInfo.BXTID;
            modelToReturn.RESTCODE = reqInfo.RESTCODE;
            modelToReturn.ANSWER_HTML = reqInfo.ANSWER_HTML;
            modelToReturn.NOTE_HTML = reqInfo.NOTE_HTML;
            
            if (ModelState.IsValid)
            {
                try
                {
                    var historyReasonValue = "73"; // 요청사항 히스토리 ( 답변 등록 )
                    modelToReturn.UPDATE_USERID = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
                    modelToReturn.PROC_STATUS = "65"; // Set status for answering
                    await _reqService.UpdateReqInfoAsync(modelToReturn, newFiles, deletedFiles, historyReasonValue);
                    return RedirectToAction(nameof(Details), new { id = reqInfo.REQID });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error submitting answer for requirement.");
                    ModelState.AddModelError("", "An error occurred while submitting the answer.");
                }
            }

            // Repopulate ViewBag for Details view
            ViewBag.ImportantCodes = await _commCodeService.GetSelectListByPCodeAsync(78);
            ViewBag.difficultCodes = await _commCodeService.GetSelectListByPCodeAsync(7);
            
            return View("Details", modelToReturn);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> SaveDraft(int id, ReqInfo reqInfo, List<IFormFile> newFiles, [FromForm]List<int> deletedFiles)
        {
            if (id != reqInfo.REQID)
            {
                return BadRequest();
            }

            try
            {
                reqInfo.UPDATE_USERID = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
                
                await _reqService.SaveDraftAsync(reqInfo, newFiles, deletedFiles);

                TempData["ToastMessage"] = "임시저장 되었습니다.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving draft for requirement.");
                TempData["ToastMessage"] = "임시저장 중 오류가 발생했습니다.";
            }
            
            return RedirectToAction(nameof(Details), new { id = reqInfo.REQID });
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            await _reqService.DeleteReqInfoAsync(id, userId);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DownloadFile(int id)
        {
            var file = await _reqService.GetReqFileByIdAsync(id);
            if (file == null || string.IsNullOrEmpty(file.FILEPATH) || string.IsNullOrEmpty(file.REAL_FILENAME))
            {
                return NotFound();
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file.FILEPATH);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }
            
            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/octet-stream", file.REAL_FILENAME);
        }

        [HttpGet]
        public async Task<IActionResult> GetTeamRequestCount()
        {
            var teamCd = HttpContext.Session.GetString("TeamCd");
            if (string.IsNullOrEmpty(teamCd))
            {
                return Json(new { success = false, message = "TeamCd not found in session." });
            }

            try
            {
                var count = await _reqService.GetTeamTotalRequestsCountAsync(teamCd);
                return Json(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team request count for teamCd: {TeamCd}", teamCd);
                return Json(new { success = false, message = "Error retrieving count." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMyRequestCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User ID not found." });
            }

            try
            {
                var count = await _reqService.GetMyTotalRequestsCountAsync(userId);
                return Json(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting my request count for userId: {UserId}", userId);
                return Json(new { success = false, message = "Error retrieving count." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMyPendingRequestsCount()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated." });
            }
            var count = await _reqService.GetMyPendingRequestsCountAsync(userId);
            return Json(new { success = true, count = count });
        }

        [HttpGet]
        public async Task<IActionResult> GetMyInProgressRequestsCount()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated." });
            }
            var count = await _reqService.GetMyInProgressRequestsCountAsync(userId);
            return Json(new { success = true, count = count });
        }

        [HttpGet]
        public async Task<IActionResult> GetMyInProgressCount()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated." });
            }
            var count = await _reqService.GetMyInProgressCountAsync(userId);
            return Json(new { success = true, count = count });
        }

        [HttpGet]
        public async Task<IActionResult> GetMyOverdueRequestsCount()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated." });
            }
            var count = await _reqService.GetMyOverdueRequestsCountAsync(userId);
            return Json(new { success = true, count = count });
        }

        [HttpGet]
        public async Task<IActionResult> GetMyCompletedRequestsCount()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated." });
            }
            var count = await _reqService.GetMyCompletedRequestsCountAsync(userId);
            return Json(new { success = true, count = count });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRequestsCount()
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return Json(new { success = false, message = "User not authenticated." });
            }
            try
            {
                var count = await _reqService.GetAllRequestsCountAsync();
                return Json(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all requests count.");
                return Json(new { success = false, message = "Error retrieving count." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDeptTotalRequestsCount()
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return Json(new { success = false, message = "User not authenticated." });
            }

            var corCd = HttpContext.Session.GetString("CorCd");
            var deptCd = HttpContext.Session.GetString("DeptCd");

            Console.WriteLine("corCd: " + corCd);
            Console.WriteLine("deptCd: " + deptCd);

            if (string.IsNullOrEmpty(corCd) || string.IsNullOrEmpty(deptCd))
            {
                _logger.LogWarning("CorCd or DeptCd not found in session for GetDeptTotalRequestsCount.");
                return Json(new { success = false, message = "CorCd or DeptCd not found in session." });
            }

            try
            {
                var count = await _reqService.GetDeptTotalRequestsCountAsync(corCd, deptCd);
                return Json(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting department total requests count for CorCd: {CorCd}, DeptCd: {DeptCd}", corCd, deptCd);
                return Json(new { success = false, message = "Error retrieving count." });
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> GetCorpTotalRequestsCount()
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return Json(new { success = false, message = "User not authenticated." });
            }

            var corCd = HttpContext.Session.GetString("CorCd");
            Console.WriteLine("corCd: " + corCd);

            if (string.IsNullOrEmpty(corCd))
            {
                _logger.LogWarning("CorCd not found in session for GetCorpTotalRequestsCount.");
                return Json(new { success = false, message = "CorCd not found in session." });
            }

            try
            {
                var count = await _reqService.GetCorpTotalRequestsCountAsync(corCd);
                return Json(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting corp total requests count for CorCd: {CorCd}", corCd);
                return Json(new { success = false, message = "Error retrieving count." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMyRecentReqActivities()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated." });
            }
            try
            {
                var activities = await _reqService.GetMyRecentReqActivitiesAsync(userId);
                return Json(new { success = true, activities = activities });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent request activities for userId: {UserId}", userId);
                return Json(new { success = false, message = "Error retrieving recent activities." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMyAssignedRecentReqActivities()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated." });
            }
            try
            {
                var activities = await _reqService.GetMyAssignedRecentReqActivitiesAsync(userId);
                return Json(new { success = true, activities = activities });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent assigned request activities for userId: {UserId}", userId);
                return Json(new { success = false, message = "Error retrieving recent assigned activities." });
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> GetRequestsCountThisWeek()
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return Json(new { success = false, message = "User not authenticated." });
            }
            try
            {
                var count = await _reqService.GetRequestsCountAddedThisWeekAsync();
                return Json(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting requests count added this week.");
                return Json(new { success = false, message = "Error retrieving count." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRequestsCountByDivisionAndPriority()
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return Json(new { success = false, message = "User not authenticated." });
            }
            try
            {
                var stats = await _reqService.GetRequestsCountByDivisionAndPriorityAsync();
                return Json(new { success = true, stats = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting requests count by division and priority.");
                return Json(new { success = false, message = "Error retrieving stats." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRequestsCountByOfficeAndPriority()
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return Json(new { success = false, message = "User not authenticated." });
            }
            try
            {
                var stats = await _reqService.GetRequestsCountByOfficeAndPriorityAsync();
                return Json(new { success = true, stats = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting requests count by office and priority.");
                return Json(new { success = false, message = "Error retrieving stats." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRequestsCountByAdminAndPriority()
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return Json(new { success = false, message = "User not authenticated." });
            }
            try
            {
                var stats = await _reqService.GetRequestsCountByAdminAndPriorityAsync();
                return Json(new { success = true, stats = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting requests count by office and priority.");
                return Json(new { success = false, message = "Error retrieving stats." });
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> GetRequestsDueThisWeekCount()
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return Json(new { success = false, message = "User not authenticated." });
            }
            try
            {
                var count = await _reqService.GetRequestsDueThisWeekCountAsync();
                return Json(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting requests due this week count.");
                return Json(new { success = false, message = "Error retrieving count." });
            }
        }


        // 조치자 목록 
        [HttpGet]
        public async Task<IActionResult> GetAdminRel(int menuId)
        {
            var admin = await _adminRelService.GetSelectListByAssignedUserAsync(menuId);            
            return Json(admin);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetProcStatus(int id, string status)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ToastMessage"] = "오류: 사용자 ID를 찾을 수 없습니다.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var result = await _reqService.UpdateProcStatusAsync(id, status, userId);
                if (result)
                {
                    TempData["ToastMessage"] = "상태가 성공적으로 업데이트되었습니다.";
                }
                else
                {
                    TempData["ToastMessage"] = "상태 업데이트에 실패했습니다.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for request {ReqId}", id);
                TempData["ToastMessage"] = "상태 업데이트 중 오류가 발생했습니다.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireManagerOrHigher")]
        public async Task<IActionResult> UpdateExpectDate(int id, DateTime expectDate)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                // This should not happen for an authorized user, but as a safeguard:
                return Challenge(); 
            }

            var result = await _reqService.UpdateExpectDateAsync(id, expectDate, userId);

            if (result)
            {
                TempData["ToastMessage"] = "완료예정일이 성공적으로 변경되었습니다.";
            }
            else
            {
                TempData["ToastMessage"] = "완료예정일 변경에 실패했습니다. (요청을 찾을 수 없거나 업데이트 중 오류 발생)";
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireManagerOrHigher")]
        public async Task<IActionResult> UpdateResUser(int id, string resUserId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == null)
            {
                return Challenge();
            }

            var result = await _reqService.UpdateResUserAsync(id, resUserId, currentUserId);

            if (result)
            {
                TempData["ToastMessage"] = "조치자가 성공적으로 변경되었습니다.";
            }
            else
            {
                TempData["ToastMessage"] = "조치자 변경에 실패했습니다. (요청을 찾을 수 없거나 업데이트 중 오류 발생)";
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReInquiry(int id, string reInquiryContent)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge(); // Not logged in
            }

            var reqInfo = await _reqService.GetReqInfoByIdAsync(id);
            if (reqInfo == null)
            {
                return NotFound();
            }

            // Authorization: Only the original requester can submit a re-inquiry
            if (reqInfo.REQUSERID != userId)
            {
                return Forbid();
            }

            // Append re-inquiry content
            string reInquiryHeader = $"<hr><h4>재문의 ({DateTime.Now:yyyy-MM-dd})</h4>";
            reqInfo.CONTENTS_HTML += reInquiryHeader + reInquiryContent;

            // Update status to "Re-inquiry"
            reqInfo.PROC_STATUS = "83";
            reqInfo.UPDATE_USERID = userId;

            // Call the general update service
            try
            {
                await _reqService.UpdateReqInfoAsync(reqInfo, null, null, "77"); // Using "76" for Re-inquiry history
                TempData["ToastMessage"] = "재문의가 성공적으로 등록되었습니다.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting re-inquiry for requirement {ReqId}.", id);
                TempData["ToastMessage"] = "재문의 등록 중 오류가 발생했습니다.";
            }

            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}
