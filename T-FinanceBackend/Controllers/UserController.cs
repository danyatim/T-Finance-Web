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
    public class UserController(TFinanceDbContext context) : ControllerBase
    {
        private readonly TFinanceDbContext _context = context;

        [Authorize(Roles = "User")]
        [HttpPost("premium")]
        public async Task<IActionResult> GetPremium()
        {
            var login = User.Identity?.Name; // заполняется из ClaimTypes.Name
            if (string.IsNullOrWhiteSpace(login))
                return Unauthorized("Не удалось определить пользователя");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == login);
            if (user == null) return BadRequest("Пользователя не существует");

            user.IsPremium = true;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Успех" });
        }
    }
}
