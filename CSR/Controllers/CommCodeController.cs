using Microsoft.AspNetCore.Mvc;
using CSR.Models;
using CSR.Services;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CSR.Controllers
{
    public class CommCodeController : Controller
    {
        private readonly CommCodeService _commCodeService;
        private readonly ILogger<CommCodeController> _logger;

        public CommCodeController(CommCodeService commCodeService, ILogger<CommCodeController> logger)
        {
            _commCodeService = commCodeService;
            _logger = logger;
        }

        // GET: CommCode
        public async Task<IActionResult> Index()
        {
            try
            {
                var codes = await _commCodeService.GetCommCodeTreeAsync();
                return View(codes);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "공통코드 목록을 가져오는 중 오류가 발생했습니다.");
                // TODO: Implement a proper error view
                return View(new List<CommCode>());
            }
        }

        // GET: CommCode/Create
        public async Task<IActionResult> Create(int parentId = 0)
        {
            var model = new CommCode
            {
                PARENTID = parentId,
                USEYN = "Y"
            };
            ViewBag.AllCodes = await _commCodeService.GetAllCommCodesAsync();
            return View(model);
        }

        // POST: CommCode/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CommCode commCode)
        {

            commCode.REG_USERID = "Admin";

            Console.WriteLine("ModelState.IsValid ; " + ModelState.IsValid);

            if (ModelState.IsValid)
            {
                try
                {
                    
                    await _commCodeService.CreateCommCodeAsync(commCode);
                    return RedirectToAction(nameof(Index));
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "공통코드 생성 중 오류가 발생했습니다.");
                    ModelState.AddModelError(string.Empty, "저장 중 오류가 발생했습니다.");
                }
            }
            return View(commCode);
        }

        // GET: CommCode/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var commCode = await _commCodeService.GetCommCodeByIdAsync(id);
            if (commCode == null)
            {
                return NotFound();
            }
            return PartialView("_CommCodeEditForm", commCode);
        }

        // POST: CommCode/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CommCode commCode)
        {
            if (id != commCode.CODEID)
            {
                return Json(new { success = false, errors = new[] { "ID mismatch." } });
            }

            if (ModelState.IsValid)
            {
                try
                {
                    commCode.UPDATE_USERID = "system"; // TODO: Get user from auth
                    await _commCodeService.UpdateCommCodeAsync(commCode);
                    return Json(new { success = true });
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "공통코드 수정 중 오류가 발생했습니다.");
                    return Json(new { success = false, errors = new[] { $"수정 중 오류가 발생했습니다: {ex.Message}" } });
                }
            }
            
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, errors });
        }
    }
}
