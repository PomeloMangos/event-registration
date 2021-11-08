﻿using System;
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
        public async ValueTask<PagedApiResult<Activity>> Get(
            [FromServices] WowContext db,
            [FromQuery] int pageSize = 10,
            [FromQuery] int page = 1,
            CancellationToken cancellationToken = default)
        { 
            if (pageSize > 100)
            {
                pageSize = 100;
            }

            return await PagedApiResultAsync(db.Activities
                .Include(x => x.Registrations)
                .Include(x => x.User)
                .OrderByDescending(x => x.Id), page - 1, pageSize, cancellationToken);
        }

        [HttpGet("{id:long}")]
        public async ValueTask<ApiResult<Activity>> Get(
            [FromServices] WowContext db,
            [FromQuery] long id, 
            CancellationToken cancellationToken = default)
        {
            var activity = await db.Activities
                .Include(x => x.User)
                .Include(x => x.Registrations)
                .ThenInclude(x => x.Charactor)
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (activity == null)
            {
                return ApiResult<Activity>(404, "Acitvity not found");
            }

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
                return ApiResult<Activity>(403, "You don't have permission to create an activity");
            }

            if (string.IsNullOrWhiteSpace(activity.Name))
            {
                return ApiResult<Activity>(400, "Activity name is null or white space");
            }

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
                return ApiResult(403, "You don't have permission to delete an activity");
            }

            var activity = await db.Activities.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (activity == null)
            {
                return ApiResult(404, "Acitvity not found");
            }

            db.Activities.Remove(activity);
            await db.SaveChangesAsync(cancellationToken);

            return ApiResult(200, "Activity removed");
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
                return ApiResult<Activity>(403, "You don't have permission to modify an activity");
            }

            var activity = await db.Activities.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (activity == null)
            {
                return ApiResult<Activity>(404, "Acitvity not found");
            }

            activity.Name = model.Name;
            activity.Description = model.Description;
            activity.Deadline = model.Deadline;
            activity.Raids = model.Raids;

            await db.SaveChangesAsync(cancellationToken);

            return ApiResult(activity);
        }
        #endregion

        #region Registration
        [HttpPost("{activityId:long}")]
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

            var charactor = await FetchCharactorAsync(db, registration.Name, activity.Realm);
            if (charactor != null)
            {
                registration.CharactorId = charactor.Id;
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

            registration.Hint = model.Hint;
            if (model.Status == RegistrationStatus.Leave || User.Identity.IsAuthenticated)
            { 
                registration.Status = RegistrationStatus.Leave;
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

        private async ValueTask<Charactor> FetchCharactorAsync(WowContext db,  string name, string realm)
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
                _logger.LogWarning($"Fetch charactor from WCL failed: {ex}");
                return null;
            }
        }
    }
}
