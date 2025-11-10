using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace TFinanceBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        [Authorize(Roles = "Premium")]
        [HttpGet("app")]
        public IActionResult DownloadAppZip([FromServices] IWebHostEnvironment env)
        {
            var path = Path.Combine(env.ContentRootPath, "Files", "T-Finance.zip");
            if (!System.IO.File.Exists(path))
                return NotFound(new { message = "Файл не найден" });

            var stream = System.IO.File.OpenRead(path);
            return File(stream, "application/zip", "T-Finance.zip");
        }
    }
}
