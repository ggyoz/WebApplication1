using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using CSR.Models;
using CSR.Services;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;

namespace CSR.Controllers
{
    [Authorize(Policy = "RequireManagerOrHigher")]
    public class MenuController : Controller
    {
        private readonly MenuService _menuService;
        private readonly ILogger<MenuController> _logger;

        public MenuController(MenuService menuService, ILogger<MenuController> logger)
        {
            _menuService = menuService;
            _logger = logger;
        }

        // GET: Menu
        public async Task<IActionResult> Index()
        {
            try
            {
                var menus = await _menuService.GetMenuTreeAsync(activeOnly: false);
                return View(menus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "메뉴 목록을 가져오는 중 오류 발생: {Message}", ex.Message);
                ViewBag.Error = $"메뉴 목록을 불러오는데 실패했습니다: {ex.Message}";
                return View(new List<Menu>());
            }
        }

        // GET: Menu/Create
        public async Task<IActionResult> Create(string? parentId = null)
        {
            var menu = new Menu
            {
                UseYn = "Y",
                SortOrder = 0
            };

            if (!string.IsNullOrEmpty(parentId))
            {
                var parent = await _menuService.GetMenuByIdAsync(parentId);
                if (parent != null)
                {
                    menu.ParentId = parent.Id;
                    menu.MenuLevel = parent.MenuLevel + 1;
                    if (menu.MenuLevel > 3) menu.MenuLevel = 3; // 최대 3단계
                }
            }
            else
            {
                menu.MenuLevel = 1;
            }

            return View(menu);
        }

        // POST: Menu/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MenuName,Url,Controller,Action,Icon,SortOrder,UseYn,ParentId,MenuLevel")] Menu menu)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _menuService.CreateMenuAsync(menu);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "메뉴 생성 중 오류 발생");
                    ModelState.AddModelError("", $"메뉴 생성에 실패했습니다: {ex.Message}");
                }
            }

            return View(menu);
        }

        // POST: Menu/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MenuId,MenuName,Url,Controller,Action,Icon,SortOrder,UseYn,ParentId,MenuLevel,CreateDate")] Menu menu)
        {

            Console.WriteLine("Parameters: " + JsonConvert.SerializeObject(menu, Formatting.Indented));


            if (id != menu.Id)
            {
                return Json(new { success = false, errors = new[] { "ID mismatch." } });
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _menuService.UpdateMenuAsync(menu);
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "메뉴 수정 중 오류 발생");
                    return Json(new { success = false, errors = new[] { $"메뉴 수정에 실패했습니다: {ex.Message}" } });
                }
            }
            
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, errors });
        }

        // GET: Menu/GetMenuDetails/5
        [HttpGet]
        public async Task<IActionResult> GetMenuDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("ID cannot be null or empty.");
            }

            var menu = await _menuService.GetMenuByIdAsync(id);
            if (menu == null)
            {
                return NotFound();
            }
            
            return PartialView("_MenuEditForm", menu);
        }

        // 삭제 페이지에서 내용 조회용
        public async Task<IActionResult> Delete(string id)
        {
            var menu = await _menuService.GetMenuByIdAsync(id);
            if (menu == null)
            {
                return NotFound();
            }

            return View(menu);
        }

        // POST: Menu/Delete/5
        // 실제 삭제 기능
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                await _menuService.DeleteMenuAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "메뉴 삭제 중 오류 발생");
                TempData["Error"] = $"메뉴 삭제에 실패했습니다: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // 순서 변경: 위로
        public async Task<IActionResult> MoveUp(string id)
        {
            var result = await _menuService.MoveUpAsync(id);
            if (!result)
            {
                TempData["Error"] = "순서를 변경할 수 없습니다.";
            }
            return RedirectToAction(nameof(Index));
        }

        // 순서 변경: 아래로
        public async Task<IActionResult> MoveDown(string id)
        {
            var result = await _menuService.MoveDownAsync(id);
            if (!result)
            {
                TempData["Error"] = "순서를 변경할 수 없습니다.";
            }
            return RedirectToAction(nameof(Index));
        }


        // 부모메뉴 목록 출력 
        [HttpGet]
        public async Task<IActionResult> GetParentMenusForLevel(int level)
        {
            // 부모 메뉴는 선택된 레벨보다 1 작아야 합니다.
            int parentLevel = level - 1;
            if (parentLevel < 1)
            {
                // 1레벨 메뉴는 부모가 없으므로 빈 목록 반환
                return Json(new List<Menu>());
            }

            // MenuService에 특정 레벨의 메뉴를 가져오는 메서드를 호출
            var parentMenus = await _menuService.GetMenusByLevelAsync(parentLevel);

            // JavaScript에서 사용하기 쉽도록 필요한 데이터만 가공 (Value, Text)
            var result = parentMenus.Select(m => new { value = m.MenuId, text = m.MenuName });

            return Json(result);
        }

        
    }
}

