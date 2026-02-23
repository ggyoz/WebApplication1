using Microsoft.AspNetCore.Mvc;

namespace CSR.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppApiController : ControllerBase
    {
        // API 로직을 여기에 작성하세요.
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { message = "API Controller is working" });
        }
    }
}
