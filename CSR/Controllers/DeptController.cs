using Microsoft.AspNetCore.Mvc;
using CSR.Services;
using CSR.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;

namespace CSR.Controllers
{
    public class DeptController : Controller
    {
        private readonly DeptService _deptService;
        private readonly ILogger<DeptController> _logger;

        public DeptController(DeptService deptService, ILogger<DeptController> logger)
        {
            _deptService = deptService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var depts = await _deptService.GetDeptTreeAsync();
                return View(depts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting department list: {Message}", ex.Message);
                ViewBag.Error = $"Failed to load department list: {ex.Message}";
                return View(new List<Dept>());
            }
        }

        public async Task<IActionResult> Create(long? parentId = null)
        {
            var dept = new Dept { UseYn = "Y", SortOrder = 0 };

            if (parentId.HasValue)
            {
                var parent = await _deptService.GetDeptByIdAsync(parentId.Value);
                if (parent != null)
                {
                    dept.ParentId = parent.DeptId;
                    dept.DeptLevel = parent.DeptLevel + 1;
                }
            }
            else
            {
                dept.DeptLevel = 1;
            }
            ViewBag.AllDepts = await _deptService.GetAllDeptsForDropdownAsync();
            return View(dept);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DeptCd,ParentId,DeptName,CorCd,SortOrder,Note,UseYn")] Dept dept)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _deptService.CreateDeptAsync(dept);
                    TempData["SuccessMessage"] = "Department created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating department.");
                    ModelState.AddModelError("", $"Failed to create department: {ex.Message}");
                }
            }
            ViewBag.AllDepts = await _deptService.GetAllDeptsForDropdownAsync();
            return View(dept);
        }

        public async Task<IActionResult> Edit(long id)
        {
            var dept = await _deptService.GetDeptByIdAsync(id);
            if (dept == null)
            {
                return NotFound();
            }
            ViewBag.AllDepts = await _deptService.GetAllDeptsForDropdownAsync();
            return View(dept);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("DeptId,DeptCd,ParentId,DeptName,CorCd,SortOrder,Note,UseYn")] Dept dept)
        {
            if (id != dept.DeptId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _deptService.UpdateDeptAsync(dept);
                    TempData["SuccessMessage"] = "Department updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating department.");
                    ModelState.AddModelError("", $"Failed to update department: {ex.Message}");
                }
            }
            ViewBag.AllDepts = await _deptService.GetAllDeptsForDropdownAsync();
            return View(dept);
        }

        public async Task<IActionResult> Delete(long id)
        {
            var dept = await _deptService.GetDeptByIdAsync(id);
            if (dept == null)
            {
                return NotFound();
            }
            return View(dept);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            try
            {
                await _deptService.DeleteDeptAsync(id);
                TempData["SuccessMessage"] = "Department deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting department.");
                TempData["ErrorMessage"] = $"Failed to delete department: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> MoveUp(long id)
        {
            var result = await _deptService.MoveUpAsync(id);
            if (!result)
            {
                TempData["ErrorMessage"] = "Cannot move the item further.";
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> MoveDown(long id)
        {
            var result = await _deptService.MoveDownAsync(id);
            if (!result)
            {
                TempData["ErrorMessage"] = "Cannot move the item further.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetAutoCompleteDept(string searchString, string corCd)
        {            
            // MenuService에 특정 레벨의 메뉴를 가져오는 메서드를 호출
            var searchDeptCd = await _deptService.GetAutoCompleteDeptAsync(searchString, corCd);

            // JavaScript에서 사용하기 쉽도록 필요한 데이터만 가공 (Value, Text)
            var result = searchDeptCd.Select(m => new { value = m.DeptCd, text = m.DeptName });
            
            //Console.WriteLine("Parameters: " + JsonConvert.SerializeObject(result, Formatting.Indented));

            return Json(result);
        }
    }
}
