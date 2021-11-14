﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Pomelo.Wow.EventRegistration.Web.Models;
using Pomelo.Wow.EventRegistration.Web.Models.ViewModels;

namespace Pomelo.Wow.EventRegistration.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CharactorController : ControllerBase
    {
        ILogger<CharactorController> _logger;

        public CharactorController(ILogger<CharactorController> logger)
        {
            _logger = logger;
        }

        [HttpPost("batch")]
        public async ValueTask<ApiResult> Post(
            [FromServices] WowContext db,
            [FromBody] BatchUpdateWclRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return ApiResult(403, "Permission Denied");
            }

            foreach (var x in request.Names)
            {
                try
                {
                    await ActivityController.FetchCharactorAsync(db, _logger, x, request.Realm);
                }
                catch (Exception ex) 
                {
                    _logger.LogError(ex.ToString());
                }
            }

            return ApiResult(200, "Done");
        }
    }
}
