using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using CSR.Models;
using CSR.Services;

namespace CSR.Controllers
{
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
                    menu.Level = parent.Level + 1;
                    if (menu.Level > 3) menu.Level = 3; // 최대 3단계
                }
            }
            else
            {
                menu.Level = 1;
            }

            await PopulateParentMenus(menu);
            return View(menu);
        }

        // POST: Menu/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Url,Controller,Action,Icon,DisplayOrder,UseYn,ParentId,Level")] Menu menu)
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

            await PopulateParentMenus(menu);
            return View(menu);
        }

        // GET: Menu/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            var menu = await _menuService.GetMenuByIdAsync(id);
            if (menu == null)
            {
                return NotFound();
            }

            await PopulateParentMenus(menu, excludeId: id);
            return View(menu);
        }

        // POST: Menu/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,Name,Url,Controller,Action,Icon,DisplayOrder,UseYn,ParentId,Level,CreatedAt,UpdatedAt")] Menu menu)
        {
            if (id != menu.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _menuService.UpdateMenuAsync(menu);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "메뉴 수정 중 오류 발생");
                    ModelState.AddModelError("", $"메뉴 수정에 실패했습니다: {ex.Message}");
                }
            }

            await PopulateParentMenus(menu, excludeId: id);
            return View(menu);
        }

        // GET: Menu/Delete/5
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

        // 부모 메뉴 목록을 ViewBag에 설정
        private async Task PopulateParentMenus(Menu menu, string? excludeId = null)
        {
            var parentMenus = await _menuService.GetParentMenusAsync(excludeId);
            
            ViewBag.ParentMenus = new SelectList(
                parentMenus,
                "Id",
                "Name",
                menu.ParentId
            );
        }
    }
}

