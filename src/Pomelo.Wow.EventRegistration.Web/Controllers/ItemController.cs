using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pomelo.Wow.EventRegistration.WCL;
using Pomelo.Wow.EventRegistration.Web.Models;
using Pomelo.Wow.EventRegistration.Web.Models.ViewModels;

namespace Pomelo.Wow.EventRegistration.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemController : ControllerBase
    {
        ILogger<ItemController> _logger;

        public ItemController(ILogger<ItemController> logger)
        {
            _logger = logger;
        }

        [HttpGet("{id:int}")]
        public async ValueTask<ApiResult<Item>> Get(
            [FromServices] WowContext db,
            [FromRoute] int id,
            CancellationToken cancellationToken = default)
        {
            var item = await db.Items.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (item == null)
            {
                var equipment = await Fetcher.FetchEquipmentAsync(id);
                item = new Item
                { 
                    Id = equipment.Id,
                    ImageUrl = equipment.ImageUrl,
                    ItemLevel = equipment.ItemLevel,
                    Name = equipment.Name,
                    Position = equipment.Position,
                    Quality = equipment.Quality
                };
                db.Items.Add(item);
                await db.SaveChangesAsync(cancellationToken);
            }

            return ApiResult(item);
        }
    }
}
