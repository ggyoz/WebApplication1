using CSR.Models;
using CSR.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System;

namespace CSR.Controllers
{
    [Authorize]
    public class ReqController : Controller
    {
        private readonly IReqService _reqService;
        private readonly ICommCodeService _commCodeService;
        private readonly UserService _userService;
        private readonly ILogger<ReqController> _logger;

        public ReqController(IReqService reqService, ICommCodeService commCodeService, UserService userService, ILogger<ReqController> logger)
        {
            _reqService = reqService;
            _commCodeService = commCodeService;
            _userService = userService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string searchField = "TITLE", string searchValue = "")
        {
            ViewData["SearchField"] = searchField;
            ViewData["SearchValue"] = searchValue;
            var pagedResult = await _reqService.GetReqInfosAsync(page, pageSize, searchField, searchValue);
            return View(pagedResult);
        }

        public async Task<IActionResult> Details(int id)
        {
            var reqInfo = await _reqService.GetReqInfoByIdAsync(id);
            if (reqInfo == null)
            {
                return NotFound();
            }
            return View(reqInfo);
        }
        
        [Authorize] // Authorization can be more specific if needed
        public async Task<IActionResult> Create()
        {
            ViewBag.SystemCodes = await _commCodeService.GetSelectListByPCodeAsync(19);
            ViewBag.ReqTypes = await _commCodeService.GetSelectListByPCodeAsync(13);
            ViewBag.PriorityCodes = await _commCodeService.GetSelectListByPCodeAsync(1);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userService.GetUserWithDetailsByIdAsync(userId);

            var reqInfo = new ReqInfo
            {
                REQDATE = DateTime.Now,
                EXPECTDATE = DateTime.Now.AddDays(7),
                ReqUserName = user?.UserName,
                ReqUserEmail = user?.EmailAddr,
                ReqUserTel = user?.TelNo,
                CorpName = user?.CorpName,
                DeptName = user?.DeptName,
                OfficeName = user?.OfficeName,
                TeamName = user?.TeamName,
                REQUSERID = userId
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
            
            // If we got this far, something failed, redisplay form
            ViewBag.SystemCodes = await _commCodeService.GetSelectListByPCodeAsync(19);
            ViewBag.ReqTypes = await _commCodeService.GetSelectListByPCodeAsync(13);
            ViewBag.PriorityCodes = await _commCodeService.GetSelectListByPCodeAsync(1);
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

            ViewBag.SystemCodes = await _commCodeService.GetSelectListByPCodeAsync(19);
            ViewBag.ReqTypes = await _commCodeService.GetSelectListByPCodeAsync(13);
            ViewBag.PriorityCodes = await _commCodeService.GetSelectListByPCodeAsync(1);
            ViewBag.ProcStatusCodes = await _commCodeService.GetSelectListByPCodeAsync(0); // 상태 수정해야됨

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
                    reqInfo.UPDATE_USERID = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
                    
                    await _reqService.UpdateReqInfoAsync(reqInfo, newFiles, deletedFiles);

                    return RedirectToAction(nameof(Details), new { id = reqInfo.REQID });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating requirement.");
                    ModelState.AddModelError("", "An error occurred while updating the requirement.");
                }
            }
            
            // If model state is invalid, reload data for the view
            ViewBag.SystemCodes = await _commCodeService.GetSelectListByPCodeAsync(19);
            ViewBag.ReqTypes = await _commCodeService.GetSelectListByPCodeAsync(13);
            ViewBag.PriorityCodes = await _commCodeService.GetSelectListByPCodeAsync(1);
            ViewBag.ProcStatusCodes = await _commCodeService.GetSelectListByPCodeAsync(0); // 상태 수정해야됨
            var originalReq = await _reqService.GetReqInfoByIdAsync(id);
            reqInfo.AttachFiles = originalReq?.AttachFiles ?? new List<ReqFile>();
            
            return View(reqInfo);
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
    }
}
