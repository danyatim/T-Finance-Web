using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Threading.Tasks;
using TFinanceBackend.Data;

namespace TFinanceBackend.Controllers
{
    [Authorize(Roles = "User")]
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {

        [Authorize(Roles = "User")]
        [HttpPost("premium")]
        [Obsolete("Используйте /api/payment/create для создания платежа")]
        public IActionResult GetPremium()
        {
            // Этот endpoint устарел, используйте /api/payment/create
            return BadRequest(new { 
                message = "Этот endpoint устарел. Используйте /api/payment/create для создания платежа через YooKassa" 
            });
        }
    }
}
