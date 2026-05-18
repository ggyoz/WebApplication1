using CSR.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CSR.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReqApiController : ControllerBase
    {
        private readonly IReqService _reqService;
        private readonly ILogger<ReqApiController> _logger;

        public ReqApiController(IReqService reqService, ILogger<ReqApiController> logger)
        {
            _reqService = reqService;
            _logger = logger;
        }

        /// <summary>
        /// 로그인한 사용자가 등록한 요청사항들의 진행상태별 갯수를 가져옵니다.
        /// </summary>
        /// <returns>대기(61), 접수(62), 종결(68), 진행중(나머지) 카운트</returns>
        [HttpGet("my-counts")]
        public async Task<IActionResult> GetMyRequestCounts()
        {
            try
            {
                // 로그인한 사용자의 ID 가져오기 (ClaimTypes.Name 또는 Identity.Name)
                var corCd = HttpContext.Session.GetString("CorCd");
                var userId = User.Identity?.Name;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var counts = await _reqService.GetMyRequestCountsByStatusAsync(corCd, userId);
                return Ok(counts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "로그인 사용자 요청 카운트 조회 중 오류 발생. UserId: {UserId}", User.Identity?.Name);
                return StatusCode(500, new { message = "내부 서버 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 로그인한 사용자의 요청 처리율 및 지연율 통계를 가져옵니다.
        /// </summary>
        [HttpGet("my-performance")]
        public async Task<IActionResult> GetMyRequestPerformance()
        {
            try
            {
                var corCd = HttpContext.Session.GetString("CorCd");
                var userId = User.Identity?.Name;
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var performance = await _reqService.GetMyRequestPerformanceAsync(corCd, userId);
                return Ok(performance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "로그인 사용자 퍼포먼스 데이터 조회 중 오류 발생. UserId: {UserId}", User.Identity?.Name);
                return StatusCode(500, new { message = "내부 서버 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 로그인한 사용자가 속한 법인의 요청사항 상태별 갯수를 가져옵니다.
        /// </summary>
        [HttpGet("corp-counts")]
        public async Task<IActionResult> GetCorpRequestCounts()
        {
            try
            {
                // 세션에서 법인 코드 가져오기
                var corCd = HttpContext.Session.GetString("CorCd");

                if (string.IsNullOrEmpty(corCd))
                {
                    // 세션에 없으면 에러보다는 빈 데이터 또는 0으로 응답하거나,
                    // 필요시 DB에서 다시 조회하는 로직을 넣을 수 있습니다.
                    return Ok(new { WAITCOUNT = 0, RECEIVEDCOUNT = 0, CLOSEDCOUNT = 0, INPROGRESSCOUNT = 0 });
                }

                var counts = await _reqService.GetCorpRequestCountsByStatusAsync(corCd);
                return Ok(counts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "법인별 요청 카운트 조회 중 오류 발생. CorCd: {CorCd}", HttpContext.Session.GetString("CorCd"));
                return StatusCode(500, new { message = "내부 서버 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 로그인한 사용자가 속한 법인의 요청 처리율 및 지연율 통계를 가져옵니다.
        /// </summary>
        [HttpGet("corp-performance")]
        public async Task<IActionResult> GetCorpRequestPerformance()
        {
            try
            {
                var corCd = HttpContext.Session.GetString("CorCd");
                if (string.IsNullOrEmpty(corCd))
                {
                    return Ok(new { TotalCount = 0, CompletedCount = 0, DelayedCount = 0, CompletionRate = 0, DelayRate = 0 });
                }

                var performance = await _reqService.GetCorpRequestPerformanceAsync(corCd);
                return Ok(performance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "법인 퍼포먼스 데이터 조회 중 오류 발생. CorCd: {CorCd}", HttpContext.Session.GetString("CorCd"));
                return StatusCode(500, new { message = "내부 서버 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 로그인한 사용자가 속한 부서의 요청사항 상태별 갯수를 가져옵니다.
        /// </summary>
        [HttpGet("dept-counts")]
        public async Task<IActionResult> GetDeptRequestCounts()
        {
            try
            {
                var deptCd = HttpContext.Session.GetString("DeptCd");
                if (string.IsNullOrEmpty(deptCd)) return Ok(new { WAITCOUNT = 0, RECEIVEDCOUNT = 0, CLOSEDCOUNT = 0, ANSWEREDCOUNT = 0, INPROGRESSCOUNT = 0 });
                var counts = await _reqService.GetDeptRequestCountsByStatusAsync(deptCd);
                return Ok(counts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "부서별 요청 카운트 조회 중 오류 발생. DeptCd: {DeptCd}", HttpContext.Session.GetString("DeptCd"));
                return StatusCode(500, new { message = "내부 서버 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 로그인한 사용자가 속한 부서의 요청 처리율 및 지연율 통계를 가져옵니다.
        /// </summary>
        [HttpGet("dept-performance")]
        public async Task<IActionResult> GetDeptRequestPerformance()
        {
            try
            {
                var deptCd = HttpContext.Session.GetString("DeptCd");
                if (string.IsNullOrEmpty(deptCd)) return Ok(new { TotalCount = 0, CompletedCount = 0, DelayedCount = 0, CompletionRate = 0, DelayRate = 0 });
                var performance = await _reqService.GetDeptRequestPerformanceAsync(deptCd);
                return Ok(performance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "부서 퍼포먼스 데이터 조회 중 오류 발생. DeptCd: {DeptCd}", HttpContext.Session.GetString("DeptCd"));
                return StatusCode(500, new { message = "내부 서버 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 로그인한 사용자가 속한 사무실의 요청사항 상태별 갯수를 가져옵니다.
        /// </summary>
        [HttpGet("office-counts")]
        public async Task<IActionResult> GetOfficeRequestCounts()
        {
            try
            {
                var officeCd = HttpContext.Session.GetString("OfficeCd");
                if (string.IsNullOrEmpty(officeCd)) return Ok(new { WAITCOUNT = 0, RECEIVEDCOUNT = 0, CLOSEDCOUNT = 0, ANSWEREDCOUNT = 0, INPROGRESSCOUNT = 0 });
                var counts = await _reqService.GetOfficeRequestCountsByStatusAsync(officeCd);
                return Ok(counts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사무실별 요청 카운트 조회 중 오류 발생. OfficeCd: {OfficeCd}", HttpContext.Session.GetString("OfficeCd"));
                return StatusCode(500, new { message = "내부 서버 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 로그인한 사용자가 속한 사무실의 요청 처리율 및 지연율 통계를 가져옵니다.
        /// </summary>
        [HttpGet("office-performance")]
        public async Task<IActionResult> GetOfficeRequestPerformance()
        {
            try
            {
                var officeCd = HttpContext.Session.GetString("OfficeCd");
                if (string.IsNullOrEmpty(officeCd)) return Ok(new { TotalCount = 0, CompletedCount = 0, DelayedCount = 0, CompletionRate = 0, DelayRate = 0 });
                var performance = await _reqService.GetOfficeRequestPerformanceAsync(officeCd);
                return Ok(performance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사무실 퍼포먼스 데이터 조회 중 오류 발생. OfficeCd: {OfficeCd}", HttpContext.Session.GetString("OfficeCd"));
                return StatusCode(500, new { message = "내부 서버 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 로그인한 사용자가 속한 팀의 요청사항 상태별 갯수를 가져옵니다.
        /// </summary>
        [HttpGet("team-counts")]
        public async Task<IActionResult> GetTeamRequestCounts()
        {
            try
            {
                var corCd = HttpContext.Session.GetString("CorCd");
                var teamCd = HttpContext.Session.GetString("TeamCd");
                if (string.IsNullOrEmpty(teamCd)) return Ok(new { WAITCOUNT = 0, RECEIVEDCOUNT = 0, CLOSEDCOUNT = 0, ANSWEREDCOUNT = 0, INPROGRESSCOUNT = 0 });
                var counts = await _reqService.GetTeamRequestCountsByStatusAsync(corCd, teamCd);
                return Ok(counts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "팀별 요청 카운트 조회 중 오류 발생. TeamCd: {TeamCd}", HttpContext.Session.GetString("TeamCd"));
                return StatusCode(500, new { message = "내부 서버 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 로그인한 사용자가 속한 팀의 요청 처리율 및 지연율 통계를 가져옵니다.
        /// </summary>
        [HttpGet("team-performance")]
        public async Task<IActionResult> GetTeamRequestPerformance()
        {
            try
            {
                var corCd = HttpContext.Session.GetString("CorCd");
                var teamCd = HttpContext.Session.GetString("TeamCd");
                if (string.IsNullOrEmpty(teamCd)) return Ok(new { TotalCount = 0, CompletedCount = 0, DelayedCount = 0, CompletionRate = 0, DelayRate = 0 });
                var performance = await _reqService.GetTeamRequestPerformanceAsync(corCd, teamCd);
                return Ok(performance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "팀 퍼포먼스 데이터 조회 중 오류 발생. TeamCd: {TeamCd}", HttpContext.Session.GetString("TeamCd"));
                return StatusCode(500, new { message = "내부 서버 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 전체 시스템의 요청사항 상태별 갯수를 가져옵니다. (R3, R4 전용)
        /// </summary>
        [HttpGet("all-counts")]
        [Authorize(Roles = "R3,R4")]
        public async Task<IActionResult> GetAllRequestCounts()
        {
            try
            {
                var counts = await _reqService.GetAllRequestCountsByStatusAsync();
                return Ok(counts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "전체 요청 카운트 조회 중 오류 발생");
                return StatusCode(500, new { message = "내부 서버 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 전체 시스템의 요청 처리율 및 지연율 통계를 가져옵니다. (R3, R4 전용)
        /// </summary>
        [HttpGet("all-performance")]
        [Authorize(Roles = "R3,R4")]
        public async Task<IActionResult> GetAllRequestPerformance()
        {
            try
            {
                var performance = await _reqService.GetAllRequestPerformanceAsync();
                return Ok(performance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "전체 퍼포먼스 데이터 조회 중 오류 발생");
                return StatusCode(500, new { message = "내부 서버 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// DevExtreme DataGrid용 요청 목록을 가져옵니다.
        /// </summary>
        [HttpGet("list")]
        public async Task<IActionResult> GetRequests(
            [FromQuery] int skip = 0,
            [FromQuery] int take = 10,
            [FromQuery] string? reqTypeCd = null,
            [FromQuery] string? procStatusCd = null,
            [FromQuery] string? priorityCd = null,
            [FromQuery] string? reqDate = null,
            [FromQuery] string? dueDate = null,
            [FromQuery] string? expectDate = null,
            [FromQuery] string? regId = null,
            [FromQuery] string? reqUserName = null,
            [FromQuery] string? resUserName = null,
            [FromQuery] string? searchValue = null,
            [FromQuery] string? corCd = null,
            [FromQuery] string? deptCd = null,
            [FromQuery] string? officeCd = null,
            [FromQuery] string? teamCd = null,
            [FromQuery] string? tcode = null,
            [FromQuery] string? assignedResponsibilities = null,
            [FromQuery] string? terminateYn = null,
            [FromQuery] string? importanceCd = null,
            [FromQuery] string? filter = null,
            [FromQuery] string? sortColumn = "REQID",
            [FromQuery] string? sortOrder = "DESC"
            )
        {
            try
            {
                var sessionCorCd = HttpContext.Session.GetString("CorCd");
                var sessionDeptCd = HttpContext.Session.GetString("DeptCd");
                var sessionOfficeCd = HttpContext.Session.GetString("OfficeCd");
                var sessionTeamCd = HttpContext.Session.GetString("TeamCd");

                // 팀원 및 팀장 조회 제한 로직 (ReqController.Index와 동일)
                if (!User.IsInRole("R3") && !User.IsInRole("R4"))
                {
                    if (string.IsNullOrEmpty(sessionDeptCd))
                    {
                        return Unauthorized(new { message = "세션 정보가 없습니다." });
                    }
                }

                var respList = string.IsNullOrEmpty(assignedResponsibilities)
                    ? new List<string>()
                    : assignedResponsibilities.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

                string defaultDeptStr = sessionDeptCd + ",ST00, CM00";
                string defaultOfficeStr = sessionOfficeCd + ",ST01, CM01";
                string defaultTeamStr = sessionTeamCd + ",ST0101, CM0101";

                if (User.IsInRole("R1"))
                {
                    if (string.IsNullOrEmpty(corCd) && string.IsNullOrEmpty(deptCd) && string.IsNullOrEmpty(officeCd) && string.IsNullOrEmpty(teamCd))
                    {
                        corCd = sessionCorCd;
                        teamCd = defaultTeamStr;
                    }
                    else
                    {
                        corCd = sessionCorCd;
                        teamCd = sessionTeamCd;
                    }
                }
                else if (User.IsInRole("R2"))
                {
                    corCd = sessionCorCd;
                    deptCd = sessionDeptCd;
                    if (!string.IsNullOrEmpty(sessionOfficeCd)) officeCd = sessionOfficeCd;
                    if (!string.IsNullOrEmpty(sessionTeamCd)) teamCd = sessionTeamCd;
                }

                int pageNumber = (skip / take) + 1;
                int pageSize = take;

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
                    tcode,
                    respList,
                    terminateYn,
                    importanceCd,
                    filter,
                    sortColumn,
                    sortOrder);

                return Ok(new { data = pagedResult.Items, totalCount = pagedResult.TotalCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "요청 목록 조회 중 오류 발생");
                return StatusCode(500, new { message = "내부 서버 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 로그인한 사용자가 조치자로 지정된 요청사항 중 답변완료 건수를 가져옵니다.
        /// </summary>
        [HttpGet("my-assigned-answered-count")]
        public async Task<IActionResult> GetMyAssignedAnsweredCount()
        {
            try
            {
                var userId = User.Identity?.Name;
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var count = await _reqService.GetMyAssignedAnsweredCountAsync(userId);
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "조치자 답변완료 건수 조회 중 오류 발생. UserId: {UserId}", User.Identity?.Name);
                return StatusCode(500, new { message = "내부 서버 오류가 발생했습니다." });
            }
        }

    }
}
