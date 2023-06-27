using System.Collections.Generic;
using COLID.Graph.Metadata.DataModels.FilterGroup;
using COLID.SearchService.DataModel.Search;
using Newtonsoft.Json.Linq;

namespace COLID.SearchService.Services.Interface
{
    /// <summary>
    /// Interface for classes capable of handling search operations.
    /// </summary>
    public interface ISearchService
    {
        /// <summary>
        /// Provides the functionality to execute a search on a low level with a custom search logic.
        /// </summary>
        /// <param name="query">A search request in accordance to the Elasticsearch JSON DSL: https://www.elastic.co/guide/en/elasticsearch/reference/current/search-request-body.html</param>
        /// <returns>Response from elasticsearch.</returns>
        /// <remarks>
        /// With this functionality a custom search logic can be used instead of the Data Marketplace default. This provides
        /// the felixibilty to make use of the whole search capabilites of Elasticsearch. This
        /// functionality is only for users who are familar with the Elasticsearch JSON DSL.
        /// </remarks>
        object SearchLowLevel(JObject query, SearchIndex searchIndex);

        /// <summary>
        /// Returns buckets of documents for all fields in the index which are marked as initial aggregation.
        /// </summary>
        /// <returns>All available buckets for inital aggreagations.</returns>
        object GetInitialAggregations(SearchIndex searchIndex);

        IList<string> Suggest(string searchText, SearchIndex searchIndex);

        IList<string> PhraseSuggest(string searchText, SearchIndex searchIndex);

        /// <summary>
        /// Executes a search on the current index with the given query with the Data Marketplace default search logic.
        /// </summary>
        /// <param name="searchRequest">A search request in accordance to the DTO <see cref="COLID.SearchService.DataModel.Search.SearchRequestDto"/> for handling of search requests. </param>
        /// <returns>Enrichend Data Marketplace Elasticsearch response.</returns>
        /// <remarks>
        /// The search request DTO simplifies the usage of the Data Marketplace search. The DTO will be transformed by following the Data Marketplace search logic
        /// into an Elasticsearch JSON DSL.
        /// </remarks>
        object Search(SearchRequestDto searchRequest, bool delay = false);

        /// <summary>
        /// Get List of Filter Group
        /// </summary>
        /// <returns></returns>
        IList<FilterGroup> GetFilterGroupAndProperties();
    }
}
