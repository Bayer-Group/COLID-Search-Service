using System.Collections.Generic;
using COLID.Cache.Services;
using COLID.Graph.Metadata.DataModels.FilterGroup;
using COLID.SearchService.DataModel.Search;
using COLID.SearchService.Repositories.Interface;
using COLID.SearchService.Services.Interface;
using Newtonsoft.Json.Linq;

namespace COLID.SearchService.Services.Implementation
{
    /// <summary>
    /// Services for handling of all search operations with DMP search engine.
    /// </summary>
    public class SearchService : ISearchService
    {
        private readonly IElasticSearchRepository _elasticSearchRepository;
        private readonly ICacheService _cacheService;

        public SearchService(IElasticSearchRepository elasticSearchRepository, ICacheService cacheService)
        {
            _elasticSearchRepository = elasticSearchRepository;
            _cacheService = cacheService;
        }

        /// <summary>
        /// <see cref="ISearchService.GetInitialAggregations"/>
        /// </summary>
        public object GetInitialAggregations(SearchIndex searchIndex)
        {
            var mappingProperties = _elasticSearchRepository.GetMappingProperties(searchIndex);
            return _elasticSearchRepository.GetFacets(mappingProperties, searchIndex);
        }

        /// <summary>
        /// <see cref="ISearchService.Search(SearchRequestDto)"/>
        /// </summary>
        public object Search(SearchRequestDto searchRequest, bool delay)
        {
            return _elasticSearchRepository.Search(searchRequest, delay);
        }

        /// <summary>
        /// <see cref="ISearchService.SearchLowLevel(JObject)"/>
        /// </summary>
        public object SearchLowLevel(JObject query, SearchIndex searchIndex)
        {
            return _elasticSearchRepository.ExecuteRawQuery(query, searchIndex);
        }

        public IList<string> Suggest(string searchText, SearchIndex searchIndex)
        {
            return _elasticSearchRepository.Suggest(searchText, searchIndex);
        }

        public IList<string> PhraseSuggest(string searchText, SearchIndex searchIndex)
        {
            return _elasticSearchRepository.PhraseSuggest(searchText, searchIndex);
        }

        public IList<FilterGroup> GetFilterGroupAndProperties()
        {
            //Get Filter Groups and properties
            var filterGrp = _cacheService.GetOrAdd($"AllFilterGroups", () => _elasticSearchRepository.GetFilterGroupAndProperties().Result);
            
            return filterGrp;
        }
    }
}
