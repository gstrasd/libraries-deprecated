using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog.Core;
using Serilog.Events;

namespace Library.Hosting.AspNetCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly LoggingLevelSwitch _loggingLevelSwitch;

        public AdminController(LoggingLevelSwitch loggingLevelSwitch)
        {
            if (loggingLevelSwitch == null) throw new ArgumentNullException(nameof(loggingLevelSwitch));
            
            _loggingLevelSwitch = loggingLevelSwitch;
        }

        [HttpGet("logging/level")]
        [Produces("application/json")]
        public async Task<IActionResult> GetLogLevel()
        {
            var level = _loggingLevelSwitch.MinimumLevel.ToString().ToLower();
            var result = new
            {
                message = $"Log level is currently set to {level}.",
                success = true,
                data = new { level }
            };

            return new JsonResult(result);
        }

        [HttpPut("logging/level/{level}")]
        [Produces("application/json")]
        public IActionResult SetLogLevel(string level)
        {
            level = level?.ToLower().Trim();
            if (!Enum.TryParse<LogEventLevel>(level, true, out var logLevel))
            {
                var result = new
                {
                    message =   $"{level} is not a valid log level. Please use verbose, debug, information, warning, error or fatal.",
                    success = false,
                    data = new { }
                };
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return new JsonResult(result);
            }
            else
            {
                _loggingLevelSwitch.MinimumLevel = logLevel;

                var result = new
                {
                    message = $"Logging level successfully set to {level}.",
                    success = true,
                    data = new { level }
                };

                return new JsonResult(result);
            }
        }

        //[HttpGet("workers")]
        //[Produces("application/json")]
        //public IActionResult GetWorkers()
        //{

        //}
    }
}
