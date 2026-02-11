using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; // ILogger 사용을 위해 추가

namespace CSR.Controllers
{
    [Authorize] // 인증된 사용자만 접근 가능하도록 설정
    public class StatisticsController : Controller
    {
        private readonly ILogger<StatisticsController> _logger; // 로깅을 위한 필드 (필요하다면 추가)

        // 생성자 (필요하다면 의존성 주입)
        public StatisticsController(ILogger<StatisticsController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // 통계 페이지의 초기 뷰를 반환
            return View();
        }

        // 추가적인 통계 페이지 액션들을 여기에 구현
        // 예: 월별 통계, 유형별 통계 등
        // public IActionResult MonthlyReport() { return View(); }
    }
}
