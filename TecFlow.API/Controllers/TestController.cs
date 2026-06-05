using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace TecFlow.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Test()
        {
            return Ok(new { message = "Swagger est� funcionando!" });
        }
    }
}