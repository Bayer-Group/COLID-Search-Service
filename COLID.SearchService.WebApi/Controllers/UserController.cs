using System;
using System.Collections.Generic;
using COLID.SearchService.DataModel;
using COLID.SearchService.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace COLID.SearchService.WebApi.Controllers
{
    /// <summary>
    /// Controller for handling and fetching unique user operations and statistics from Elastic Search.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Searches unique users in the PID and DMP indices and writes to Elastic Search.
        /// </summary>
        /// <response code="200">Successfull request</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [Route("writepiddmpuniqueusers")]
        public IActionResult WriteUsersPIDDMP(indexEnum appName)
        {
            _userService.WritePIDDMPUniqueUsers(appName.ToString());
            return Ok();
        }

        /// <summary>
        /// Fetches Saved Search Filters Count and writes to Elastic Search.
        /// </summary>
        /// <response code="200">Successfull request</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [Route("writeAllSavedSearchFiltersCountToLogs")]
        public IActionResult WriteDmpAllSavedSearchFiltersCountToLogs([FromBody] Dictionary<string, int> allSavedSearchFilters)
        {
            _userService.WriteDmpAllSavedSearchFiltersCountToLogs(allSavedSearchFilters);
            return Ok();
        }

        /// <summary>
        /// Fetches Favorites List Count and writes to Elastic Search.
        /// </summary>
        /// <response code="200">Successfull request</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [Route("writeFavoritesListCountToLogs")]
        public IActionResult WriteFavoritesListCountToLogs(Dictionary<string, int> allFavoritesList)
        {
            _userService.WriteFavoritesListCountToLogs(allFavoritesList);
            return Ok();
        }

        /// <summary>
        /// Fetches Subscriptions Count and writes to Elastic Search.
        /// </summary>
        /// <response code="200">Successfull request</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [Route("writeAllSubscriptionsCountToLogs")]
        public IActionResult WriteAllSubscriptionsCountToLogs(Dictionary<string, int> allSubscriptions)
        {
            _userService.WriteAllSubscriptionsCountToLogs(allSubscriptions);
            return Ok();
        }


    }
}
