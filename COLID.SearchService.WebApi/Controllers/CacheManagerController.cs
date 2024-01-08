using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using COLID.Cache.Services;

namespace COLID.SearchService.WebApi.Controllers
{
    /// <summary>
    /// Cache Manager.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CacheManagerController : ControllerBase
    {

        private readonly ICacheService _cacheService;

        /// <summary>
        /// API endpoint to manage cache.
        /// </summary>
        /// <param name="cacheService">The service to manage cache</param>
        public CacheManagerController(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }

        /// <summary>
        /// Flush all cache.
        /// </summary>        
        [HttpDelete("deleteAll")]
        public IActionResult ClearCache()
        {
            _cacheService.Clear();
            return Ok("Cache cleared");
        }

    }             
}
