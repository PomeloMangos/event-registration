using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Pomelo.Wow.WeakAuras.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WAController : ControllerBase
    {
        [HttpPost]
        public async ValueTask<string> Post([FromServices] IConfiguration configuration)
        {
            string weaktable = "";
            using (var sr = new StreamReader(Request.Body))
            {
                weaktable = await sr.ReadToEndAsync();
            }
            if (!Directory.Exists("Temp"))
            {
                Directory.CreateDirectory("Temp");
            }

            var id = Guid.NewGuid();
            var weakTablePath = Path.Combine("Temp", $"{id}.txt");
            var waStringPath = Path.Combine("Temp", $"{id}.wa");
            System.IO.File.WriteAllText(weakTablePath, weaktable);
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo 
                {
                    UseShellExecute = false,
                    FileName = configuration["LUA"],
                    Arguments = $"wa_convert.lua {weakTablePath} {waStringPath}",
                    CreateNoWindow = true
                };
                process.Start();
                process.WaitForExit();
                if (!System.IO.File.Exists(waStringPath) || process.ExitCode != 0)
                {
                    if (System.IO.File.Exists(weakTablePath))
                    {
                        System.IO.File.Delete(weakTablePath);
                    }
                    return null;
                }

                var ret = System.IO.File.ReadAllText(waStringPath);

                if (System.IO.File.Exists(waStringPath))
                {
                    System.IO.File.Delete(waStringPath);
                }

                if (System.IO.File.Exists(weakTablePath))
                {
                    System.IO.File.Delete(weakTablePath);
                }

                return ret;
            }
        }
    }
}
