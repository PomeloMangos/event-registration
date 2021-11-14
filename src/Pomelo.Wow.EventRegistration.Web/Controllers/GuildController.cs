﻿using System;
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
    public class GuildController : ControllerBase
    {
        ILogger<UserController> _logger;

        public GuildController(ILogger<UserController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async ValueTask<ApiResult<List<Guild>>> Get(
            [FromServices] WowContext db,
            CancellationToken cancellationToken = default)
        {
            var guilds = await db.Guilds.ToListAsync(cancellationToken);
            return ApiResult(guilds);
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

            guild.UserId = Convert.ToInt32(User.Identity.Name);
            db.Guilds.Add(guild);
            await db.SaveChangesAsync(cancellationToken);

            return ApiResult(guild);
        }

        [HttpPut("manager/{username}")]
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

        [HttpDelete("manager/{username}")]
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

            return await db.GuildManagers.AnyAsync(x => x.GuildId == GuildId && x.UserId == userId, cancellationToken);
        }
    }
}
