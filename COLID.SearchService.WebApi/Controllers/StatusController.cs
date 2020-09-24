using System.Threading.Tasks;
using COLID.SearchService.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace COLID.SearchService.WebApi.Controllers
{
    /// <summary>
    /// Controller to report status of the service.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StatusController : Controller
    {
        private readonly IStatusService _statusService;

        /// <summary>
        /// API endpoint for status information.
        /// </summary>
        /// <param name="statusService">The service for status information</param>
        public StatusController(IStatusService statusService)
        {
            _statusService = statusService;
        }

        /// <summary>
        /// Returns the status with build informations of the running web api.
        /// </summary>
        /// <response code="200">Returns the status of the build</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        public IActionResult GetBuildInformation()
        {
            return Ok(_statusService.GetBuildInformation());
        }

    }
}
