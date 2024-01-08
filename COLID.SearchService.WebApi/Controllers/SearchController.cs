using System.Threading.Tasks;
using COLID.SearchService.DataModel.Search;
using COLID.SearchService.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace COLID.SearchService.WebApi.Controllers
{
    /// <summary>
    /// Controller for handling of all search operations to the search engine.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;
        private readonly IRemoteSimilarityService _similarityService;

        public SearchController(ISearchService searchService, IRemoteSimilarityService similarityService)
        {
            _searchService = searchService;
            _similarityService = similarityService;
        }

        /// <summary>
        /// Searches the index for the exact query in Elasticsearch JSON DSL given in the request body.
        /// </summary>
        /// <param name="searchIndex">Index to search in</param>
        /// <param name="searchRequest">A search request in accordance to the spec of Elasticsearch: https://www.elastic.co/guide/en/elasticsearch/reference/current/search-request-body.html </param>
        /// <returns>A status code and search results.</returns>
        /// <response code="200">Successfull request</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [Route("lowlevel")]
        public IActionResult Query([FromBody] JObject searchRequest, [FromQuery] SearchIndex searchIndex = SearchIndex.Published)
        {
            return Ok(_searchService.SearchLowLevel(searchRequest, searchIndex));
        }

        /// <summary>
        /// Searches the index for the query given in the request body.
        /// </summary>
        /// <param name="searchRequest">A search request in accordance to the DTO <see cref="COLID.SearchService.DataModel.Search.SearchRequestDto"/> for handling of search requests. </param>
        /// <returns>A status code and search results. </returns>
        /// <response code="200">Successfull request</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [Route("")]
        public IActionResult Search([FromBody] SearchRequestDto searchRequest)
        {
            var delay = searchRequest.Delay;
            return Ok(_searchService.Search(searchRequest, delay));
        }

        /// <summary>
        /// Searches the given term in opensearch, and returns clustered result using Carrot2.
        /// </summary>
        /// <param name="searchRequest">A search request in accordance to the DTO <see cref="COLID.SearchService.DataModel.Search.SearchRequestDto"/> for handling of search requests. </param>
        /// <response code="200">Successfull request</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [Route("clusterSearchResult")]
        public IActionResult GetClusteredSearchResult(SearchRequestDto searchRequest)
        {            
            return Ok(_searchService.GetClusteredSearchResult(searchRequest));
        }

        /// <summary>
        /// Returns buckets of documents for all fields in the index which are marked as initial aggregation.
        /// </summary>
        /// <param name="searchIndex">Index to search in</param>
        /// <returns>A status code and all inital aggreagations.</returns>
        /// <response code="200">Successfull request</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [Route("aggregations")]
        public IActionResult GetInitialAggregations([FromQuery] SearchIndex searchIndex = SearchIndex.Published)
        {
            return Ok(_searchService.GetInitialAggregations(searchIndex));
        }

        /// <summary>
        /// Gets the suggestion for the search text.
        /// </summary>
        /// <param name="searchText">The searchText to get suggestion list.</param>
        /// <param name="searchIndex">Index to search in</param>
        /// <returns>Status code along with list of suggest text.</returns>
        [HttpGet]
        [Route("suggest")]
        public IActionResult Suggest([FromQuery(Name = "q")] string searchText, [FromQuery] SearchIndex searchIndex = SearchIndex.Published)
        {
            return Ok(_searchService.Suggest(searchText, searchIndex));
        }

        /// <summary>
        /// Gets the similar resources of a partial PID resource, using the Similarity Service
        /// </summary>
        /// <param name="similarityRequest">A search request in accordance to the DTO <see cref="COLID.SearchService.DataModel.Search.SimilarityRequestDto"/> for handling of search requests. </param>
        /// <param name="threshold">The threshold of the similarity score.</param>
        /// <param name="limit">Limits the returned resources.</param>
        /// <param name="model">The model which should be used to calculate the similarity of resources.</param>
        [HttpPost]
        [Route("similarity")]
        public async Task<IActionResult> Similarity([FromBody] SimilarityRequestDto similarityRequest,
                                                    [FromQuery(Name = "threshold")] double threshold = 0.0,
                                                    [FromQuery(Name = "limit")] int limit = 10,
                                                    [FromQuery(Name = "model")] string model = "ft")
        {
            var result = await _similarityService.PerformRessourceSimilarity(similarityRequest, threshold, limit, model);
            return Ok(result);
        }

        /// <summary>
        /// Get the suggestion for a phrase.
        /// </summary>
        /// <param name="searchText">The pharse for suggestion.</param>
        /// <param name="searchIndex">Index to search in</param>
        /// <returns>Status code along with list of pharse suggestion.</returns>
        [HttpGet]
        [Route("phraseSuggest")]
        public IActionResult PhraseSuggest([FromQuery(Name = "q")] string searchText, [FromQuery] SearchIndex searchIndex = SearchIndex.Published)
        {
            return Ok(_searchService.PhraseSuggest(searchText, searchIndex));
        }

        /// <summary>
        /// Get Filter Group and Properties
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("filterGroup")]
        public IActionResult FilterGroupAndProperties()
        {
            var result = _searchService.GetFilterGroupAndProperties();
            return Ok(result);
        }
    }
}
