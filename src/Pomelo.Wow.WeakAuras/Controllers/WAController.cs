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
        [HttpPost("encode")]
        public async ValueTask<string> PostEncode([FromServices] IConfiguration configuration)
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
                    Arguments = $"wa_encode.lua {weakTablePath} {waStringPath}",
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


        [HttpPost("decode")]
        public async ValueTask<string> PostDecode([FromServices] IConfiguration configuration)
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
            var waStringPath = Path.Combine("Temp", $"{id}.wa");
            var weakTablePath = Path.Combine("Temp", $"{id}.txt");
            System.IO.File.WriteAllText(waStringPath, weaktable);
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    FileName = configuration["LUA"],
                    Arguments = $"wa_decode.lua {waStringPath} {weakTablePath}",
                    CreateNoWindow = true
                };
                process.Start();
                process.WaitForExit();
                if (!System.IO.File.Exists(weakTablePath) || process.ExitCode != 0)
                {
                    if (System.IO.File.Exists(waStringPath))
                    {
                        System.IO.File.Delete(waStringPath);
                    }
                    return null;
                }

                var ret = System.IO.File.ReadAllText(weakTablePath);

                if (System.IO.File.Exists(waStringPath))
                {
                    System.IO.File.Delete(waStringPath);
                }

                if (System.IO.File.Exists(weakTablePath))
                {
                    System.IO.File.Delete(weakTablePath);
                }

                return ret.Replace("\\\\\\", "\\").Replace("\\\\", "\\");
            }
        }
    }
}
