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
                var userId = User.Identity?.Name;
                
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var counts = await _reqService.GetMyRequestCountsByStatusAsync(userId);
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
                var userId = User.Identity?.Name;
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var performance = await _reqService.GetMyRequestPerformanceAsync(userId);
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
    }
}
