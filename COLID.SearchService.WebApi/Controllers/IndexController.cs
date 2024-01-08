using System.Collections.Generic;
using COLID.SearchService.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace COLID.SearchService.WebApi.Controllers
{
    /// <summary>
    /// Controller for creating a new index.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class IndexController : Controller
    {
        private readonly IIndexService _indexService;

        public IndexController(IIndexService indexService)
        {
            _indexService = indexService;
        }

        /// <summary>
        /// Creates a new index with the given metadata.
        /// </summary>
        /// <param name="metadata">The metadata will define how the mapping of index.</param>
        [HttpPost]
        [Route("create")]
        [Authorize(Roles = "Resource.Index.All")]
        public IActionResult Post([FromBody] Dictionary<string, JObject> metadata)
        {
            _indexService.CreateAndApplyNewIndex(metadata);
            return Ok();
        }

        /// <summary>
        /// Check re-indexing progress status.
        /// </summary>        
        [HttpGet]
        [Route("IndexingStatus")]
        public IActionResult IndexingStatus()
        {            
            return Ok(_indexService.GetIndexingStatus());
        }
    }
}
