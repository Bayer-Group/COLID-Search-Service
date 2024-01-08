using System.Collections.Generic;
using System.Text;
using COLID.Cache.Services;
using COLID.Graph.Metadata.DataModels.FilterGroup;
using COLID.SearchService.DataModel.DTO;
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
        private readonly IRemoteCarrot2Service _carrot2Service;

        public SearchService(IElasticSearchRepository elasticSearchRepository, ICacheService cacheService, IRemoteCarrot2Service carrot2Service)
        {
            _elasticSearchRepository = elasticSearchRepository;
            _cacheService = cacheService;
            _carrot2Service = carrot2Service;
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
        /// Get clustered search result
        /// </summary>
        /// <param name="searchTerm"></param>
        /// <param name="maxRecord"></param>
        /// <returns></returns>
        public Carrot2ResponseDTO GetClusteredSearchResult(SearchRequestDto searchRequest)
        {
            //Ignore List of Properties
            List<string> ignoredProperties = new List<string>();
            ignoredProperties.Add(Graph.Metadata.Constants.Resource.Distribution);
            ignoredProperties.Add(Graph.Metadata.Constants.Resource.MainDistribution);            
            ignoredProperties.Add(Graph.Metadata.Constants.Resource.Groups.LinkTypes);
            ignoredProperties.Add(Graph.Metadata.Constants.Resource.HasVersion);
            ignoredProperties.Add(Graph.Metadata.Constants.Resource.HasVersions);
            ignoredProperties.Add(Graph.Metadata.Constants.Resource.Attachment);
            ignoredProperties.Add(Graph.Metadata.Constants.Resource.Author);
            ignoredProperties.Add(Graph.Metadata.Constants.Resource.LastChangeUser);
            ignoredProperties.Add(Graph.Metadata.Constants.Resource.hasBusinessOwner);
            ignoredProperties.Add(Graph.Metadata.Constants.Resource.hasApplicationManager);
            ignoredProperties.Add(Graph.Metadata.Constants.Resource.hasSystemOwner);
            ignoredProperties.Add(Graph.Metadata.Constants.Resource.HasDataSteward);
            ignoredProperties.Add(Graph.Metadata.Constants.Resource.HasLaterVersion);
            ignoredProperties.Add(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus);
            ignoredProperties.Add(Graph.Metadata.Constants.Resource.LifecycleStatus);
            ignoredProperties.Add(Graph.Metadata.Constants.Resource.IsPersonalData);
            ignoredProperties.Add(Graph.Metadata.Constants.Resource.ContainsLicensedData);
            ignoredProperties.Add(Graph.Metadata.Constants.Resource.HasConsumerGroup);

            ////create search request DTO and call opensearch 
            //SearchRequestDto searchRequest = new SearchRequestDto { 
            //    From = 0, 
            //    Size = maxRecord,
            //    SearchTerm = searchTerm,
            //    EnableHighlighting = false,
            // EnableAggregation = false,
            // EnableSuggest = false,
            //};

            var searchResult = _elasticSearchRepository.Search(searchRequest, false);
            
            //Convert opensearch response to carrot2 request dto
            var carrot2Request = new Carrot2RequestDTO();     
            
            foreach(var hit in searchResult.Hits.Hits)
            {                
                Dictionary<string, string> hitRecord = new Dictionary<string, string>();
                foreach (JProperty source in hit.Source)
                {                                        
                    string name = source.Name;
                    if (!ignoredProperties.Contains(name))                    
                    {
                        if ((bool)(JValue)source.Value["outbound"].HasValues)
                        {
                            if (((JValue)source.Value["outbound"][0]["value"]).Value != null)
                            {
                                string val = ((JValue)source.Value["outbound"][0]["value"]).Value.ToString();
                                hitRecord.Add(name, val);                                
                            }
                        }
                    }                                  
                }                
                carrot2Request.documents.Add(hitRecord);
            }
            
            //Cluster opensearch response to carrot2 clsuter
            var carrot2response = _carrot2Service.Cluster(carrot2Request).Result;
            
            //find piduri and label based on doc index returned by carrot2
            foreach (var cluster in carrot2response.Clusters)
            {
                cluster.DocDetails = new List<DocuemntDetail>();
                foreach ( int doc in cluster.Documents)
                {
                    if (carrot2Request.documents[doc].ContainsKey(Graph.Metadata.Constants.Resource.hasPID))
                    {
                        cluster.DocDetails.Add(new DocuemntDetail
                        {
                            PidUri = carrot2Request.documents[doc][Graph.Metadata.Constants.Resource.hasPID]
                        });
                    }
                }
                cluster.Documents.Clear();
            }
            return carrot2response;            
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
