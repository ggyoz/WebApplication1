using CSR.Models;
using CSR.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;


namespace CSR.Controllers
{
    public class CorpController : Controller
    {
        private readonly CorpService _corpService;

        public CorpController(CorpService corpService)
        {
            _corpService = corpService;
        }

        // GET: Corp
        public async Task<IActionResult> Index()
        {
            var corps = await _corpService.GetAllCorpsAsync();
            return View(corps);
        }

        // GET: Corp/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var corp = await _corpService.GetCorpByIdAsync(id);
            if (corp == null)
            {
                return NotFound();
            }

            return View(corp);
        }

        // GET: Corp/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Corp/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CorCd,CorNm,NationCd,CoinCd,Language,AccTitle")] Corp corp)
        {
            if (ModelState.IsValid)
            {
                // Check if the primary key already exists
                var existing = await _corpService.GetCorpByIdAsync(corp.CorCd);
                if (existing != null)
                {
                    ModelState.AddModelError("CorCd", "이미 존재하는 법인코드입니다.");
                    return View(corp);
                }
                
                await _corpService.CreateCorpAsync(corp);
                return RedirectToAction(nameof(Index));
            }
            return View(corp);
        }

        // GET: Corp/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var corp = await _corpService.GetCorpByIdAsync(id);
            if (corp == null)
            {
                return NotFound();
            }
            return View(corp);
        }

        // POST: Corp/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("CorCd,CorNm,NationCd,CoinCd,Language,AccTitle")] Corp corp)
        {
            if (id != corp.CorCd)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _corpService.UpdateCorpAsync(corp);
                }
                catch (System.Exception)
                {
                    if (await _corpService.GetCorpByIdAsync(id) == null)
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(corp);
        }

        // GET: Corp/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var corp = await _corpService.GetCorpByIdAsync(id);
            if (corp == null)
            {
                return NotFound();
            }

            return View(corp);
        }

        // POST: Corp/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            await _corpService.DeleteCorpAsync(id);
            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> GetAutoCompleteCorp(string searchString)
        {            
            // MenuService에 특정 레벨의 메뉴를 가져오는 메서드를 호출
            var searchCorCd = await _corpService.GetAutoCompleteCorpAsync(searchString);

            // JavaScript에서 사용하기 쉽도록 필요한 데이터만 가공 (Value, Text)
            var result = searchCorCd.Select(m => new { value = m.CorCd, text = m.CorNm });
            
            Console.WriteLine("Parameters: " + JsonConvert.SerializeObject(result, Formatting.Indented));

            return Json(result);
        }

    }
}
