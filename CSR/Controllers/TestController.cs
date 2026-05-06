using CSR.Models;
using CSR.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CSR.Controllers
{
    [Authorize(Policy = "RequireSuperAdmin")] // 관리자만 접근 가능하도록 설정
    public class TestController : Controller
    {
        private readonly IEmailService _emailService;
        private readonly IRazorViewToStringRenderer _renderer;

        public TestController(IEmailService emailService, IRazorViewToStringRenderer renderer)
        {
            _emailService = emailService;
            _renderer = renderer;
        }

        public IActionResult EmailTest()
        {
            return View();
        }

        public IActionResult HtmlEditorTest()
        {
            return View();
        }

        public IActionResult FileUploadTest()
        {
            return View();
        }

        // useForm 모드: 여러 파일을 폼으로 한 번에 받아 정보 반환 (실제 저장 없음)
        [HttpPost]
        public IActionResult FileUploadTest(List<IFormFile> files)
        {
            var result = (files ?? new List<IFormFile>())
                .Where(f => f.Length > 0)
                .Select(f => new
                {
                    realFileName = f.FileName,
                    contentType = f.ContentType,
                    size = f.Length
                });

            return Json(new { files = result });
        }

        // instantly / useButtons 모드: 파일 1개씩 AJAX로 받아 정보 반환 (실제 저장 없음)
        [HttpPost]
        public IActionResult FileUploadSingle(IFormFile files)
        {
            if (files == null || files.Length == 0)
                return BadRequest(new { error = "No file uploaded." });

            return Json(new
            {
                realFileName = files.FileName,
                contentType = files.ContentType,
                size = files.Length
            });
        }

        [HttpPost]
        public async Task<IActionResult> SendTestEmail(string toEmail, string subject, string body)
        {
            try
            {
                await _emailService.SendEmailAsync(toEmail, subject, body);
                ViewBag.Message = "이메일이 성공적으로 전송되었습니다.";
                ViewBag.IsSuccess = true;
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"이메일 전송 실패: {ex.Message}";
                ViewBag.IsSuccess = false;
            }

            return View("EmailTest");
        }

        [HttpPost]
        public async Task<IActionResult> SendTemplateEmail(string toEmail)
        {
            try
            {
                // 테스트용 가상 모델 데이터 생성
                var model = new ReqInfo
                {
                    REQID = 12345,
                    TITLE = "ERP 시스템 권한 추가 요청",
                    RegUserName = "홍길동",
                    ReqTypeName = "시스템개선",
                    ProcStatusName = "검토중"
                };

                // Razor 뷰를 HTML 문자열로 렌더링
                string htmlBody = await _renderer.RenderViewToStringAsync("Test/SampleEmail", model);

                // 메일 전송
                await _emailService.SendEmailAsync(toEmail, "[테스트] CSR 시스템 템플릿 메일 발송", htmlBody);

                ViewBag.Message = "템플릿 메일이 성공적으로 전송되었습니다.";
                ViewBag.IsSuccess = true;
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"템플릿 메일 전송 실패: {ex.Message}";
                ViewBag.IsSuccess = false;
            }

            return View("EmailTest");
        }

        [HttpPost]
        public async Task<IActionResult> SendRequestToProcessorEmail(string toEmail)
        {
            try
            {
                // 테스트용 가상 모델 데이터 생성 (조치자용)
                var model = new ReqInfo
                {
                    REQID = 99999,
                    TITLE = "[테스트] 시스템 접근 권한 승인 요청",
                    ReqUserName = "김요청",
                    CorpName = "모베이스",
                    DeptName = "IT기획팀",
                    ResUserName = "이조치", // 조치자 이름
                    ReqTypeName = "시스템권한",
                    SystemName = "ERP 시스템",
                    PRIORITYCD = "3", // 높음
                    PriorityName = "높음",
                    REQDATE = DateTime.Now,
                    DUEDATE = DateTime.Now.AddDays(3),
                    CONTENTS_HTML = "<p>신규 입사자에 대한 ERP 시스템 접근 권한을 요청합니다.</p><ul><li>대상자: 박신입</li><li>요청권한: 영업조회</li></ul>"
                };

                // Razor 뷰를 HTML 문자열로 렌더링 (방금 만든 템플릿 사용)
                string htmlBody = await _renderer.RenderViewToStringAsync("EmailTemplates/RequestToProcessor", model);

                // 메일 전송
                await _emailService.SendEmailAsync(toEmail, "[배정알림] 새로운 요청이 등록되었습니다. (TEST)", htmlBody);

                ViewBag.Message = "조치자용 템플릿 메일이 성공적으로 전송되었습니다.";
                ViewBag.IsSuccess = true;
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"템플릿 메일 전송 실패: {ex.Message}";
                ViewBag.IsSuccess = false;
            }

            return View("EmailTest");
        }

        [HttpPost]
        public async Task<IActionResult> SendResponseToRequesterEmail(string toEmail)
        {
            try
            {
                // 테스트용 가상 모델 데이터 생성 (요청자용 답변 알림)
                var model = new ReqInfo
                {
                    REQID = 88888,
                    TITLE = "[테스트] ERP 시스템 권한 추가 요청 건에 대하여",
                    ReqUserName = "김요청", // 요청자
                    ResUserName = "이조치", // 조치자
                    ProcStatusName = "조치완료",
                    UPDATE_DATE = DateTime.Now,
                    ANSWER_HTML = "<p>요청하신 ERP 시스템 권한 추가가 완료되었습니다.</p><p>로그아웃 후 다시 로그인하여 확인 부탁드립니다.</p><br/><ul><li>적용권한: 영업조회, 매출관리</li><li>조치일시: 2024-05-20 14:00</li></ul>",
                    NOTE_HTML = "시스템 설정 완료"
                };

                // Razor 뷰를 HTML 문자열로 렌더링 (방금 만든 템플릿 사용)
                string htmlBody = await _renderer.RenderViewToStringAsync("EmailTemplates/ResponseToRequester", model);

                // 메일 전송
                await _emailService.SendEmailAsync(toEmail, "[조치완료] 요청하신 사항에 대한 답변이 등록되었습니다. (TEST)", htmlBody);

                ViewBag.Message = "요청자용 답변 알림 메일이 성공적으로 전송되었습니다.";
                ViewBag.IsSuccess = true;
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"템플릿 메일 전송 실패: {ex.Message}";
                ViewBag.IsSuccess = false;
            }

            return View("EmailTest");
        }
    }
}
