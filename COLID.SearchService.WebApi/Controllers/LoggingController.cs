using System.Linq;
using COLID.StatisticsLog.DataModel;
using COLID.StatisticsLog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace COLID.SearchService.WebApi.Controllers
{
    /// <summary>
    /// API endpoint for logging.
    /// </summary>
    [Produces("application/json")]
    [Route("api/log")]
    [Authorize]
    public class LoggingController : Controller
    {
        private readonly IGeneralLogService _logService;

        /// <summary>
        /// API endpoint for logging.
        /// </summary>
        public LoggingController(IGeneralLogService logService)
        {
            _logService = logService;
        }

        /// <summary>
        /// Logs the given entry to the database used for logging.
        /// </summary>
        /// <returns>A status code</returns>
        /// <response code="200">Returns true, if the log as been processed. Otherwise false</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [Route("{logLevel}")]
        public IActionResult LogMessage([FromRoute] int logLevel, [FromBody] LogEntry logEntry)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Select(x => x.Value.Errors)
                              .Where(y => y.Count > 0)
                              .ToList();
                return BadRequest(errors);
            }

            _logService.Log(logEntry, (Serilog.Events.LogEventLevel)logLevel);

            return Ok();
        }
    }
}
