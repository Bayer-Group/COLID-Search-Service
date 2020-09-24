using System.Collections.Generic;
using COLID.SearchService.DataModel.Search;
using COLID.SearchService.Repositories.Interface;
using COLID.SearchService.Repositories.Mapping.Constants;
using COLID.SearchService.Services.Interface;
using COLID.MessageQueue.Constants;
using Newtonsoft.Json.Linq;
using System;

namespace COLID.SearchService.Services.Implementation
{
    /// <summary>
    /// Services for handling of all search operations with DMP search engine.
    /// </summary>
    public class SearchService : ISearchService
    {
        private readonly IElasticSearchRepository _elasticSearchRepository;

        public SearchService(IElasticSearchRepository elasticSearchRepository)
        {
            _elasticSearchRepository = elasticSearchRepository;
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
        public object Search(SearchRequestDto searchRequest)
        {
            return _elasticSearchRepository.Search(searchRequest);
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
    }
}
