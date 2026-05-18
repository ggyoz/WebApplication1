using CSR.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CSR.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StatisticsApiController : ControllerBase
    {
        private readonly IReqService _reqService;
        private readonly ILogger<StatisticsApiController> _logger;

        public StatisticsApiController(IReqService reqService, ILogger<StatisticsApiController> logger)
        {
            _reqService = reqService;
            _logger = logger;
        }

        [HttpGet("total-count")]
        public async Task<IActionResult> GetTotalCount()
        {
            try
            {
                var count = await _reqService.GetAllRequestsCountAsync();
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "전체 요청 건수 조회 중 오류 발생");
                return StatusCode(500, new { message = "내부 서버 오류가 발생했습니다." });
            }
        }

        [HttpGet("this-week-count")]
        public async Task<IActionResult> GetThisWeekCount()
        {
            try
            {
                var count = await _reqService.GetRequestsCountAddedThisWeekAsync();
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "이번주 추가된 요청 건수 조회 중 오류 발생");
                return StatusCode(500, new { message = "내부 서버 오류가 발생했습니다." });
            }
        }

        [HttpGet("due-this-week-count")]
        public async Task<IActionResult> GetDueThisWeekCount()
        {
            try
            {
                var count = await _reqService.GetRequestsDueThisWeekCountAsync();
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "이번주 완료예정 요청 건수 조회 중 오류 발생");
                return StatusCode(500, new { message = "내부 서버 오류가 발생했습니다." });
            }
        }

        [HttpGet("by-division")]
        public async Task<IActionResult> GetByDivision()
        {
            try
            {
                var stats = await _reqService.GetRequestsCountByDivisionAndPriorityAsync();
                return Ok(new { success = true, stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사업부별 요청 통계 조회 중 오류 발생");
                return StatusCode(500, new { success = false, message = "내부 서버 오류가 발생했습니다." });
            }
        }

        [HttpGet("by-office")]
        public async Task<IActionResult> GetByOffice()
        {
            try
            {
                var stats = await _reqService.GetRequestsCountByOfficeAndPriorityAsync();
                return Ok(new { success = true, stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "실별 요청 통계 조회 중 오류 발생");
                return StatusCode(500, new { success = false, message = "내부 서버 오류가 발생했습니다." });
            }
        }

        [HttpGet("by-admin")]
        public async Task<IActionResult> GetByAdmin()
        {
            try
            {
                var stats = await _reqService.GetRequestsCountByAdminAndPriorityAsync();
                return Ok(new { success = true, stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "조치자별 요청 통계 조회 중 오류 발생");
                return StatusCode(500, new { success = false, message = "내부 서버 오류가 발생했습니다." });
            }
        }
    }
}
