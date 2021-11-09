using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pomelo.Wow.EventRegistration.Web.Models;
using Pomelo.Wow.EventRegistration.Web.Models.ViewModels;

namespace Pomelo.Wow.EventRegistration.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        ILogger<UserController> _logger;

        public UserController(ILogger<UserController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async ValueTask<ApiResult<List<User>>> Get(
            [FromServices] WowContext db,
            CancellationToken cancellationToken = default)
        {
            var users = await db.Users.ToListAsync(cancellationToken);
            return ApiResult(users);
        }

        [HttpPost]
        public async ValueTask<ApiResult<User>> Post(
            [FromServices] WowContext db,
            [FromBody] User user,
            CancellationToken cancellationToken = default)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return ApiResult<User>(403, "You can't create user");
            }

            if (!User.IsInRole("Admin"))
            {
                user.Role = UserRole.User;
            }

            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);

            return ApiResult(user);
        }

        [HttpPost("{username}/session")]
        public async ValueTask<ApiResult<LoginResponse>> Post(
            [FromServices] WowContext db,
            [FromRoute] string username,
            [FromBody] LoginRequest request,
            CancellationToken cancellationToken = default)
        {
            var user = await db.Users.SingleOrDefaultAsync(x => x.Username == username, cancellationToken);
            if (user == null)
            {
                return ApiResult<LoginResponse>(400, "Login failed");
            }

            if (user.PasswordHash != request.Password)
            {
                return ApiResult<LoginResponse>(400, "Login failed");
            }

            var token = Authentication.TokenAuthenticateHandler.GenerateToken(user);
            return ApiResult(new LoginResponse { Token = token, Role = user.Role.ToString() });
        }

        [HttpGet("{username}/session/{session}")]
        public async ValueTask<ApiResult> Get(
            [FromServices] WowContext db,
            [FromRoute] string username,
            [FromRoute] string session,
            CancellationToken cancellationToken = default)
        {
            if (Authentication.TokenAuthenticateHandler.Check(session))
            {
                return ApiResult(200, "ok");
            }
            else
            {
                return ApiResult(400, "failed");
            }
        }

        [HttpPost("{username}/password")]
        public async ValueTask<ApiResult> Post(
            [FromServices] WowContext db,
            [FromRoute] string username,
            [FromBody] ResetPasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return ApiResult(403, "Permission denied");
            }

            var user = await db.Users.SingleOrDefaultAsync(x => x.Username == username);
            if (!User.IsInRole("Admin") && request.Old != user.PasswordHash)
            {
                return ApiResult(400, "Old password is incorrect");
            }

            user.PasswordHash = request.New;
            await db.SaveChangesAsync();
            return ApiResult(200, "ok");
        }
    }
}
