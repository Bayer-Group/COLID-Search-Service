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
    }
}
