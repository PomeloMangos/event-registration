using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pomelo.Wow.EventRegistration.Web.Models;
using Pomelo.Wow.EventRegistration.Web.Models.ViewModels;

namespace Pomelo.Wow.EventRegistration.Web.Controllers
{
    #region Common
    [Route("api/[controller]")]
    [ApiController]
    public class GuildController : ControllerBase
    {
        ILogger<UserController> _logger;
        static Regex guildDomainRegex = new Regex("^[0-9a-zA-Z-_]{4,16}$");

        public GuildController(ILogger<UserController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async ValueTask<PagedApiResult<Guild>> Get(
            [FromServices] WowContext db,
            [FromQuery] string name = null,
            [FromQuery] int pageSize = 10,
            [FromQuery] int page = 1,
            CancellationToken cancellationToken = default)
        {
            if (pageSize > 100)
            {
                pageSize = 100;
            }

            IQueryable<Guild> query = db.Guilds;

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(x => x.Name.Contains(name));
            }

            return await PagedApiResultAsync(
                query.OrderByDescending(x => x.Id),
                page - 1,
                pageSize,
                cancellationToken);
        }

        [HttpGet("{id}")]
        public async ValueTask<ApiResult<Guild>> Get(
            [FromServices] WowContext db,
            [FromRoute] string id,
            CancellationToken cancellationToken = default)
        {
            var guild = await db.Guilds.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (guild == null)
            {
                return ApiResult<Guild>(404, "没有找到指定的公会");
            }

            return ApiResult(guild);
        }

        [HttpPost]
        public async ValueTask<ApiResult<Guild>> Post(
            [FromServices] WowContext db,
            [FromBody] Guild guild,
            CancellationToken cancellationToken = default)
        { 
            if (!User.Identity.IsAuthenticated)
            {
                return ApiResult<Guild>(401, "请登录");
            }

            if (await db.Guilds.AnyAsync(x => x.Id == guild.Id, cancellationToken))
            {
                return ApiResult<Guild>(400, $"公会ID {guild.Id} 已经存在，请更换后再试");
            }

            if (!guildDomainRegex.IsMatch(guild.Id))
            {
                return ApiResult<Guild>(400, "公会ID不合法");
            }

            guild.UserId = Convert.ToInt32(User.Identity.Name);
            guild.Managers.Add(new GuildManager 
            {
                UserId = guild.UserId
            });
            db.Guilds.Add(guild);
            await db.SaveChangesAsync(cancellationToken);

            return ApiResult(guild);
        }

        [HttpPut("{guildId}/manager/{username}")]
        public async ValueTask<ApiResult> Put(
            [FromServices] WowContext db,
            [FromRoute] string username,
            CancellationToken cancellationToken = default)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return ApiResult(401, "请登录");
            }

            if (GuildId == null)
            {
                return ApiResult(400, "请在公会中进行该操作");
            }

            if (!await ValidateUserPermissionToCurrentGuildAsync(db, true, cancellationToken))
            {
                return ApiResult(403, "您没有权限这样做");
            }

            var user = await db.Users.SingleOrDefaultAsync(x => x.Username == username, cancellationToken);
            if (user == null)
            {
                return ApiResult(400, "指定的用户不存在");
            }

            if (!await db.GuildManagers.AnyAsync(x => x.GuildId == GuildId && x.UserId == user.Id))
            {
                db.GuildManagers.Add(new GuildManager
                {
                    GuildId = GuildId,
                    UserId = user.Id
                });
                await db.SaveChangesAsync(cancellationToken);
            }

            return ApiResult(200, $"已将{username}添加为公会管理员");
        }

        [HttpDelete("{guildId}/manager/{username}")]
        public async ValueTask<ApiResult> Delete(
            [FromServices] WowContext db,
            [FromRoute] string username,
            CancellationToken cancellationToken = default)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return ApiResult(401, "请登录");
            }

            if (GuildId == null)
            {
                return ApiResult(400, "请在公会中进行该操作");
            }

            if (!await ValidateUserPermissionToCurrentGuildAsync(db, true, cancellationToken))
            {
                return ApiResult(403, "您没有权限这样做");
            }

            var manager = await db.GuildManagers.SingleOrDefaultAsync(x => x.GuildId == GuildId && x.User.Username == username, cancellationToken);
            if (manager == null)
            {
                return ApiResult(404, "指定的管理员不存在");
            }

            db.GuildManagers.Remove(manager);
            await db.SaveChangesAsync(cancellationToken);
            return ApiResult(200, $"已移除管理员{username}");
        }

        private async ValueTask<bool> ValidateUserPermissionToCurrentGuildAsync(
            WowContext db, 
            bool isOwner = false,
            CancellationToken cancellationToken = default)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return false;
            }

            if (GuildId == null)
            {
                return false;
            }

            var userId = Convert.ToInt32(User.Identity.Name);
            if (isOwner)
            {
                return Guild.UserId == userId;
            }

            return Guild.UserId == userId || await db.GuildManagers.AnyAsync(x => x.GuildId == GuildId && x.UserId == userId, cancellationToken);
        }
        #endregion

        #region Price
        [HttpGet("{guildId}/price")]
        public async ValueTask<ApiResult<List<PriceResponse>>> GetPrices(
            [FromServices] WowContext db,
            [FromRoute] string guildId,
            CancellationToken cancellationToken = default)
        {
            var prices = await db.Prices
                .Where(x => x.GuildId == guildId)
                .Select(x => new PriceResponse 
                {
                    CreatedAt = x.CreatedAt,
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync(cancellationToken);

            return ApiResult(prices);
        }

        [HttpGet("{guildId}/price/{id:Guid}")]
        public async ValueTask<ApiResult<Price>> GetPrice(
            [FromServices] WowContext db,
            [FromRoute] Guid id,
            [FromRoute] string guildId,
            CancellationToken cancellationToken = default)
        {
            var price = await db.Prices
                .Where(x => x.GuildId == guildId && x.Id == id)
                .SingleOrDefaultAsync(cancellationToken);

            if (price == null)
            {
                return ApiResult<Price>(404, "没有找到这个价目表");
            }

            return ApiResult(price);
        }

        [HttpPost("{guildId}/price")]
        public async ValueTask<ApiResult<Price>> PostPrice(
            [FromServices] WowContext db,
            [FromRoute] string guildId,
            [FromBody] Price price,
            CancellationToken cancellationToken = default)
        {
            if (!await ValidateUserPermissionToCurrentGuildAsync(db, false, cancellationToken))
            {
                return ApiResult<Price>(403, "您没有权限这样做");
            }

            price.GuildId = GuildId;
            db.Prices.Add(price);
            await db.SaveChangesAsync(cancellationToken);
            return ApiResult(price);
        }

        [HttpPatch("{guildId}/price/{id:Guid}")]
        public async ValueTask<ApiResult<Price>> PatchPrice(
            [FromServices] WowContext db,
            [FromRoute] Guid id,
            [FromRoute] string guildId,
            [FromBody] Price model,
            CancellationToken cancellationToken = default)
        {
            if (!await ValidateUserPermissionToCurrentGuildAsync(db, false, cancellationToken))
            {
                return ApiResult<Price>(403, "您没有权限这样做");
            }

            var price = await db.Prices
                .Where(x => x.GuildId == guildId && x.Id == id)
                .SingleOrDefaultAsync(cancellationToken);

            if (price == null)
            {
                return ApiResult<Price>(404, "没有找到这个价目表");
            }

            price.Name = model.Name;
            price.Data = model.Data;
            await db.SaveChangesAsync(cancellationToken);

            return ApiResult(price);
        }

        [HttpDelete("{guildId}/price/{id:Guid}")]
        public async ValueTask<ApiResult> DeletePrice(
            [FromServices] WowContext db,
            [FromRoute] Guid id,
            [FromRoute] string guildId,
            [FromBody] Price model,
            CancellationToken cancellationToken = default)
        {
            if (!await ValidateUserPermissionToCurrentGuildAsync(db, false, cancellationToken))
            {
                return ApiResult(403, "您没有权限这样做");
            }

            var price = await db.Prices
                .Where(x => x.GuildId == guildId && x.Id == id)
                .SingleOrDefaultAsync(cancellationToken);

            if (price == null)
            {
                return ApiResult(404, "没有找到这个价目表");
            }

            db.Prices.Remove(price);
            await db.SaveChangesAsync(cancellationToken);

            return ApiResult(200, "已删除价目表");
        }
        #endregion
    }
}
