using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
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
        public static Regex EmailRegex = new Regex("^\\s*([A-Za-z0-9_-]+(\\.\\w+)*@(\\w+\\.)+\\w{2,5})\\s*$");
        internal static Random Random = new Random();
        internal static SHA256 Sha256 = SHA256.Create();

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
            [FromBody] CreateUserRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.Username.Length < 3)
            {
                return ApiResult<User>(400, "用户名长度必须大于3");
            }

            if (await db.Users.AnyAsync(x => x.Username == request.Username, cancellationToken))
            {
                return ApiResult<User>(400, $"用户名{request.Username}已经存在");
            }

            if (!EmailRegex.IsMatch(request.Email))
            {
                return ApiResult<User>(400, "电子邮箱地址不合法");
            }

            if (string.IsNullOrEmpty(request.DisplayName))
            {
                return ApiResult<User>(400, "昵称不能为空");
            }

            var user = new User 
            {
                Username = request.Username,
                Email = request.Email,
                DisplayName = request.DisplayName,
                Salt = new byte[32]
            };

            var buffer = new List<byte>(Encoding.UTF8.GetBytes(request.Password));
            Random.NextBytes(user.Salt);
            buffer.AddRange(user.Salt);
            user.PasswordHash = Sha256.ComputeHash(buffer.ToArray());

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
                return ApiResult<LoginResponse>(400, "用户名或密码错误");
            }

            var buffer = new List<byte>();
            buffer.AddRange(Encoding.UTF8.GetBytes(request.Password));
            buffer.AddRange(user.Salt);
            var hash = Sha256.ComputeHash(buffer.ToArray());
            if (!hash.SequenceEqual(user.PasswordHash))
            {
                return ApiResult<LoginResponse>(400, "用户名或密码错误");
            }

            var us = new UserSession 
            {
                CreatedAt = DateTime.UtcNow,
                ExpiredAt = DateTime.UtcNow.AddDays(7),
                UserId = user.Id,
                Id = GenerateTokenId()
            };

            db.UserSessions.Add(us);
            await db.SaveChangesAsync(cancellationToken);

            return ApiResult(new LoginResponse { Token = us.Id, Role = user.Role.ToString() });
        }

        private string GenerateTokenId()
        {
            return $"{DateTime.UtcNow.Ticks}-{GenerateRandomString(64)}";
        }

        static string randomStringDic = "1234567890qwertyuiopasdfghjklzxcvbnm";

        private string GenerateRandomString(int length)
        {
            var sb = new StringBuilder();
            while(sb.Length < length)
            {
                sb.Append(randomStringDic[Random.Next(0, randomStringDic.Length)]);
            }
            return sb.ToString();
        }

        [HttpGet("{username}/session/{session}")]
        public async ValueTask<ApiResult> Get(
            [FromServices] WowContext db,
            [FromRoute] string username,
            [FromRoute] string session,
            CancellationToken cancellationToken = default)
        {
            if (User.Identity.IsAuthenticated)
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
                return ApiResult(403, "请登录");
            }

            var user = await db.Users.SingleOrDefaultAsync(x => x.Username == username, cancellationToken);
            if (user == null)
            {
                return ApiResult(404, "未找到用户");
            }

            var buffer = new List<byte>();
            buffer.AddRange(Encoding.UTF8.GetBytes(request.Old));
            buffer.AddRange(user.Salt);
            var hash = Sha256.ComputeHash(buffer.ToArray());
            if (!hash.SequenceEqual(user.PasswordHash))
            {
                return ApiResult(400, "旧密码不正确");
            }

            user.Salt = new byte[32];
            buffer = new List<byte>(Encoding.UTF8.GetBytes(request.New));
            Random.NextBytes(user.Salt);
            buffer.AddRange(user.Salt);
            user.PasswordHash = Sha256.ComputeHash(buffer.ToArray());
            await db.SaveChangesAsync();
            return ApiResult(200, "密码修改成功");
        }
    }
}
