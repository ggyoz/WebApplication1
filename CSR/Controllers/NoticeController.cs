using CSR.Models;
using CSR.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CSR.Controllers
{
    [Authorize]
    public class NoticeController : Controller
    {
        private readonly INoticeService _noticeService;
        private readonly CommCodeService _commCodeService;
        private readonly ILogger<NoticeController> _logger;

        public NoticeController(INoticeService noticeService, CommCodeService commCodeService, ILogger<NoticeController> logger)
        {
            _noticeService = noticeService;
            _commCodeService = commCodeService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string searchField = "TITLE", string searchValue = "")
        {
            ViewData["SearchField"] = searchField;
            ViewData["SearchValue"] = searchValue;
            var pagedResult = await _noticeService.GetNoticesAsync(page, pageSize, searchField, searchValue);
            return View(pagedResult);
        }

        public async Task<IActionResult> Details(int id)
        {
            var notice = await _noticeService.GetNoticeByIdAsync(id);
            if (notice == null)
            {
                return NotFound();
            }
            return View(notice);
        }

        public async Task<IActionResult> Create()
        {
            var allCodes = await _commCodeService.GetAllCommCodesAsync();
            var noticeTypes = allCodes.Where(c => c.PARENTID == 54).ToList();
            ViewBag.NoticeTypes = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(noticeTypes, "CODE", "CODENM");

            var model = new Notice
            {
                CORCD = "1200"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Notice notice, List<IFormFile> files)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // For demo, hardcoding some values. In a real app, get these from user session or context.
                    notice.REG_USERID = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
                    notice.CORCD = "1200"; // Example
                    
                    var newNoticeId = await _noticeService.CreateNoticeAsync(notice);
                    
                    if (files != null && files.Count > 0)
                    {
                        await _noticeService.AddNoticeFilesAsync(newNoticeId, files, notice.REG_USERID);
                    }
                    
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating notice.");
                    ModelState.AddModelError("", "An error occurred while creating the notice.");
                }
            }
            return View(notice);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var notice = await _noticeService.GetNoticeByIdAsync(id);
            if (notice == null)
            {
                return NotFound();
            }
            return View(notice);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Notice notice, List<IFormFile> newFiles, [FromForm]List<int> deletedFiles)
        {
            if (id != notice.ID)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    notice.UPDATE_USERID = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
                    
                    await _noticeService.UpdateNoticeAsync(notice);

                    if (newFiles != null && newFiles.Count > 0)
                    {
                        await _noticeService.AddNoticeFilesAsync(id, newFiles, notice.UPDATE_USERID);
                    }

                    if (deletedFiles != null && deletedFiles.Count > 0)
                    {
                        foreach (var fileId in deletedFiles)
                        {
                            await _noticeService.DeleteNoticeFileAsync(fileId);
                        }
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating notice.");
                    ModelState.AddModelError("", "An error occurred while updating the notice.");
                }
            }
            // If model state is invalid, we need to reload the files for the view
            var originalNotice = await _noticeService.GetNoticeByIdAsync(id);
            notice.AttachFiles = originalNotice?.AttachFiles ?? new List<NoticeFile>();
            return View(notice);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _noticeService.DeleteNoticeAsync(id);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DownloadFile(int id)
        {
            var file = await _noticeService.GetNoticeFileByIdAsync(id);
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
