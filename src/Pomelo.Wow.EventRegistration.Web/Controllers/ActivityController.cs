using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pomelo.Wow.EventRegistration.WCL;
using Pomelo.Wow.EventRegistration.Web.Models;
using Pomelo.Wow.EventRegistration.Web.Models.ViewModels;

namespace Pomelo.Wow.EventRegistration.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActivityController : ControllerBase
    {
        ILogger<ActivityController> _logger;

        public ActivityController(ILogger<ActivityController> logger)
        {
            _logger = logger;
        }

        #region Basics
        [HttpGet]
        public async ValueTask<PagedApiResult<ActivityViewModel>> Get(
            [FromServices] WowContext db,
            [FromQuery] int pageSize = 10,
            [FromQuery] int page = 1,
            [FromQuery] ActivityStatus status = ActivityStatus.All,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            CancellationToken cancellationToken = default)
        { 
            if (pageSize > 100)
            {
                pageSize = 100;
            }

            IQueryable<Activity> query = db.Activities
                .Include(x => x.Registrations)
                .Include(x => x.User)
                .Include(x => x.Guild);

            if (GuildId != null)
            {
                query = query.Where(x => x.GuildId == GuildId);
            }

            if (from.HasValue)
            {
                query = query.Where(x => x.Begin >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(x => x.Begin < to.Value);
            }

            if (status == ActivityStatus.Registering)
            {
                query = query.Where(x => x.Deadline >= DateTime.UtcNow);
            }
            else if (status == ActivityStatus.RegistrationClosed)
            {
                query = query.Where(x => x.Deadline < DateTime.UtcNow && x.Begin > DateTime.UtcNow);
            }
            else if (status == ActivityStatus.InProgress)
            {
                query = query.Where(x => x.Begin <= DateTime.UtcNow && x.Begin.AddHours(x.EstimatedDurationInHours) > DateTime.UtcNow);
            }
            else if (status == ActivityStatus.Ended)
            {
                query = query.Where(x => x.Begin.AddHours(x.EstimatedDurationInHours) < DateTime.UtcNow);
            }

            return await PagedApiResultAsync(
                query.OrderByDescending(x => x.Begin).Select(x => new ActivityViewModel 
                {
                    Begin = x.Begin,
                    Server = x.Server,
                    CreatedAt = x.CreatedAt,
                    Deadline = x.Deadline,
                    Description = x.Description,
                    EstimatedDurationInHours = x.EstimatedDurationInHours,
                    GuildId = x.GuildId,
                    GuildName = x.Guild.Name,
                    Name = x.Name,
                    RaidLeader = x.User.DisplayName,
                    Realm = x.Realm,
                    Raids = x.Raids,
                    RegisteredCount = x.Registrations.Count(),
                    Visibility = x.Visibility,
                    Faction = x.Guild.Faction,
                    Id = x.Id
                }), 
                page - 1, 
                pageSize, 
                cancellationToken);
        }

        [HttpGet("{id:long}")]
        public async ValueTask<ApiResult<Activity>> Get(
            [FromServices] WowContext db,
            [FromRoute] long id, 
            CancellationToken cancellationToken = default)
        {
            var activity = await db.Activities
                .Include(x => x.User)
                .Include(x => x.Registrations)
                .ThenInclude(x => x.Charactor)
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (activity == null)
            {
                return ApiResult<Activity>(404, "没有找到指定的活动");
            }

            activity.Registrations = activity.Registrations
                .OrderByDescending(x => x.Status)
                .ThenBy(x => x.Class)
                .ToList();

            return ApiResult(activity);
        }

        [HttpPost]
        public async ValueTask<ApiResult<Activity>> Post(
            [FromServices] WowContext db,
            [FromBody] Activity activity,
            CancellationToken cancellationToken = default)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return ApiResult<Activity>(403, "你没有权限创建活动");
            }

            if (string.IsNullOrWhiteSpace(activity.Name))
            {
                return ApiResult<Activity>(400, "活动名称不能为空");
            }

            if (GuildId == null)
            {
                return ApiResult<Activity>(400, "你必须在公会中创建活动");
            }

            if (await ValidateUserPermissionToCurrentGuildAsync(db, false, cancellationToken))
            {
                return ApiResult<Activity>(400, "你没有权限在这个公会中创建活动");
            }

            activity.GuildId = GuildId; 
            activity.UserId = Convert.ToInt32(User.Identity.Name);
            db.Activities.Add(activity);
            await db.SaveChangesAsync(cancellationToken);
            return ApiResult(activity);
        }

        [HttpDelete("{id:long}")]
        public async ValueTask<ApiResult> Delete(
            [FromServices] WowContext db,
            [FromRoute] long id,
             CancellationToken cancellationToken = default)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return ApiResult(403, "你没有权限删除这个活动");
            }

            var activity = await db.Activities.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (activity == null)
            {
                return ApiResult(404, "没有找到指定的活动");
            }

            db.Activities.Remove(activity);
            await db.SaveChangesAsync(cancellationToken);

            return ApiResult(200, "活动已删除");
        }

        [HttpPatch("{id:long}")]
        public async ValueTask<ApiResult<Activity>> Patch(
            [FromServices] WowContext db,
            [FromRoute] long id,
            [FromBody] Activity model,
            CancellationToken cancellationToken = default)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return ApiResult<Activity>(403, "你没有权限修改这个活动");
            }

            var activity = await db.Activities.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (activity == null)
            {
                return ApiResult<Activity>(404, "活动没有找到");
            }

            if (!string.IsNullOrEmpty(model.Name))
            {
                activity.Name = model.Name;
            }
            if (!string.IsNullOrEmpty(model.Description))
            {
                activity.Description = model.Description;
            }
            if (model.Deadline != default)
            {
                activity.Deadline = model.Deadline;
            }
            if (!string.IsNullOrEmpty(model.Raids))
            {
                activity.Raids = model.Raids;
            }
            if (!string.IsNullOrEmpty(model.Extension1))
            {
                activity.Extension1 = model.Extension1;
            }
            if (!string.IsNullOrEmpty(model.Extension2))
            {
                activity.Extension2 = model.Extension2;
            }
            if (!string.IsNullOrEmpty(model.Extension3))
            {
                activity.Extension3 = model.Extension3;
            }

            await db.SaveChangesAsync(cancellationToken);

            return ApiResult(activity);
        }
        #endregion

        #region Registration
        [HttpPost("{activityId:long}/registrations")]
        public async ValueTask<ApiResult<Registration>> Post(
            [FromServices] WowContext db,
            [FromRoute] long activityId,
            [FromBody] Registration registration,
            CancellationToken cancellationToken = default)
        {
            var activity = await db.Activities.SingleOrDefaultAsync(x => x.Id == activityId, cancellationToken);
            if (activity == null)
            {
                return ApiResult<Registration>(404, "Acitvity not found");
            }

            registration.RegisteredAt = DateTime.UtcNow;
            registration.Status = RegistrationStatus.Pending;
            registration.ActivityId = activityId;

            if (await db.Registrations.AnyAsync(x => x.ActivityId == activityId && x.Name == registration.Name, cancellationToken))
            {
                return ApiResult<Registration>(400, "Duplicated charactor found");
            }

            var charactor = await FetchCharactorAsync(db, _logger, registration.Name, activity.Realm);
            if (charactor != null)
            {
                registration.CharactorId = charactor.Id;
                registration.Class = registration.Class;
            }

            db.Registrations.Add(registration);
            await db.SaveChangesAsync(cancellationToken);

            return ApiResult(registration);
        }

        [HttpPatch("{activityId:long}/registrations/{id:Guid}")]
        public async ValueTask<ApiResult<Registration>> Patch(
            [FromServices] WowContext db,
            [FromRoute] long activityId,
            [FromRoute] Guid id,
            [FromBody] Registration model,
            CancellationToken cancellationToken = default)
        { 
            var registration = await db.Registrations.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (registration == null)
            {
                return ApiResult<Registration>(404, "Registration not found");
            }

            if (!string.IsNullOrWhiteSpace(model.Hint))
            {
                registration.Hint = model.Hint;
            }

            if (model.Status == RegistrationStatus.Leave || model.Status == RegistrationStatus.Pending || User.Identity.IsAuthenticated)
            {
                registration.Status = model.Status;
            }

            if (User.Identity.IsAuthenticated)
            {
                registration.Role = model.Role;
            }

            await db.SaveChangesAsync(cancellationToken);

            return ApiResult(registration);
        }

        [HttpDelete("{activityId:long}/registrations/{id:Guid}")]
        public async ValueTask<ApiResult> Delete(
            [FromServices] WowContext db,
            [FromRoute] long activityId,
            [FromRoute] Guid id,
            CancellationToken cancellationToken = default)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return ApiResult(403, "You don't have permission to modify an activity");
            }

            var registration = await db.Registrations.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (registration == null)
            {
                return ApiResult(404, "Registration not found");
            }

            db.Registrations.Remove(registration);
            await db.SaveChangesAsync(cancellationToken);

            return ApiResult(200, "Registration removed");
        }
        #endregion

        internal static async ValueTask<Charactor> FetchCharactorAsync(WowContext db, ILogger logger, string name, string realm)
        {
            try
            {
                var wclCharactorDps = await Fetcher.FetchAsync(name, realm, WCL.Models.CharactorRole.DPS);
                var wclCharactorHealer = await Fetcher.FetchAsync(name, realm, WCL.Models.CharactorRole.Healer);
                var charactor = await db.Charactors.SingleOrDefaultAsync(x => x.Name == name && x.Realm == realm);

                if (charactor == null)
                {
                    charactor = new Models.Charactor();
                    db.Charactors.Add(charactor);
                }

                charactor.Name = name;
                charactor.Realm = realm;
                charactor.HighestItemLevel = wclCharactorDps.HighestItemLevel;
                charactor.Equipments = String.Join(',', wclCharactorDps.Equipments);
                charactor.DpsBossRanks = JsonConvert.SerializeObject(wclCharactorDps.BossRanks);
                charactor.HpsBossRanks = JsonConvert.SerializeObject(wclCharactorHealer.BossRanks);

                await db.SaveChangesAsync();

                return charactor;
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Fetch charactor from WCL failed: {ex}");
                return null;
            }
        }
    }
}
