using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using Amazon;
using Amazon.Runtime;
using COLID.Common.Enums;
using COLID.Common.Extensions;
using COLID.Exception.Models;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.TripleStore.Extensions;
using COLID.SearchService.DataModel.Configuration;
using COLID.SearchService.DataModel.DTO;
using COLID.SearchService.DataModel.Search;
using COLID.SearchService.Repositories.Constants;
using COLID.SearchService.Repositories.DataModel;
using COLID.SearchService.Repositories.Extensions;
using COLID.SearchService.Repositories.Indexing;
using COLID.SearchService.Repositories.Indexing.Extensions;
using COLID.SearchService.Repositories.Interface;
using COLID.SearchService.Repositories.Mapping.Constants;
using COLID.SearchService.Repositories.Mapping.Extensions;
using COLID.SearchService.Repositories.Mapping.Options;
using COLID.StatisticsLog.Services;

using Elasticsearch.Net;
using Elasticsearch.Net.Aws;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Microsoft.Extensions.Options;
using Nest;
using Nest.JsonNetSerializer;
using Newtonsoft.Json.Linq;


namespace COLID.SearchService.Repositories.Implementation
{
    /// <summary>
    /// The implementation for all operations with Elasticsearch. As client for sending requests
    /// and receiving responses the high level <c>NEST</c> client is used. This class implements also
    /// the centerpiece of DMP, the search logic.
    /// </summary>
    public class ElasticSearchRepository : IElasticSearchRepository
    {
        private const string _valueAccesor = ".outbound.value";
        private const string _uriAccesor = ".outbound.uri";

        private readonly Uri _baseUrl;
        private readonly string _resourceIndexPrefix;
        private readonly string _metadataIndexPrefix;
        private readonly string _documentSearchAlias;
        private readonly string _metadataSearchAlias;
        private readonly string _documentUpdateAlias;
        private readonly string _metadataUpdateAlias;
        private readonly string _awsRegion;
        private readonly IElasticClient _elasticClient;
        private readonly IGeneralLogService _statiticsLogService;
        private readonly ILogger<ElasticSearchRepository> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _defaultType = "_doc";
        private readonly IConfiguration _configuration;
#if DEBUG
        private readonly IList<string> _messages;
#endif

        public ElasticSearchRepository(
            IOptionsMonitor<ElasticSearchOptions> optionsAccessor,
            IHttpContextAccessor httpContextAccessor,
            IGeneralLogService statiticsLogService,
            ILogger<ElasticSearchRepository> logger,

            IMemoryCache memoryCache,
            IConfiguration configuration)
        {
            // Read and assing values from the appsettings for initalization
            var options = optionsAccessor.CurrentValue;
            _baseUrl = options.BaseUri;

            _resourceIndexPrefix = options.ResourceIndexPrefix;
            _metadataIndexPrefix = options.MetadataIndexPrefix;

            _documentSearchAlias = options.ResourceSearchAlias;
            _metadataSearchAlias = options.MetadataSearchAlias;
            _documentUpdateAlias = options.DocumentUpdateAlias;
            _metadataUpdateAlias = options.MetadataUpdateAlias;

            _awsRegion = options.AwsRegion;
            _statiticsLogService = statiticsLogService;
            _logger = logger;
            _configuration = configuration;

#if DEBUG
            _messages = new List<string>();
#endif

            _httpContextAccessor = httpContextAccessor;
            // Setup config for Elasticsearch on AWS
            AwsHttpConnection httpConnection;

            // if accessKey and secretKey are provided via configuration, use them. otherwise try to
            // determine by default AWS credentials resolution process see https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/net-dg-config-creds.html#creds-assign
            if (!string.IsNullOrWhiteSpace(options.AccessKey) && !string.IsNullOrWhiteSpace(options.SecretKey))
            {
                var creds = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
                var region = RegionEndpoint.GetBySystemName(_awsRegion);
                httpConnection = new AwsHttpConnection(creds, region);
            }
            else
            {
                httpConnection = new AwsHttpConnection(_awsRegion);
            }

            var pool = new SingleNodeConnectionPool(_baseUrl);
            var config = new ConnectionSettings(pool, httpConnection, sourceSerializer: JsonNetSerializer.Default);
            config.DefaultIndex(_documentSearchAlias);
            config.ThrowExceptions();

#if DEBUG
            config.DisableDirectStreaming(true)
                .OnRequestCompleted(request =>
                {
                    if (request.RequestBodyInBytes != null)
                    {
                        var message = System.Text.Encoding.UTF8.GetString(request.RequestBodyInBytes);
                        _messages.Add($"Request send at {GetCurrentTimeStamp()}");
                        _messages.Add(message);
                    }
                    if (request.ResponseBodyInBytes != null)
                    {
                        var message = System.Text.Encoding.UTF8.GetString(request.ResponseBodyInBytes);
                        _messages.Add($"Response received at {GetCurrentTimeStamp()}");
                    }
                });
#endif
            // Instantiate Elasticsearch Client
            _elasticClient = new ElasticClient(config);
        }

        /// <summary>
        /// Return a document with the given id
        /// </summary>
        /// <param name="identifier">The identifier for which a document is searched for</param>
        /// <returns>Return a document with the given id</returns>
        public object GetDocument(string identifier, UpdateIndex updateIndex)
        {
            // TODO: Search alias, update alias?
            var updateAlias = GetUpdateAlias(updateIndex);
            var response = _elasticClient.Get<dynamic>(identifier, doc => doc.Index(updateAlias));

            if (!response.Found)
            {
                var errorMessage = $"No document was found for the given identifier {identifier}";
                _logger.LogDebug(errorMessage);
                throw new EntityNotFoundException(errorMessage, identifier);
            }

            return response.Source;
        }


        /// <summary>
        /// Return a document with the given id
        /// </summary>
        /// <param name="identifier">The identifier for which a document is searched for</param>
        /// <returns>Return a document with the given id</returns>
        public IList<JObject> GetSchemaUIResource(IEnumerable<string> identifiers, UpdateIndex updateIndex)
        {
            // TODO: Search alias, update alias?
            List<JObject> searchresult = new List<JObject>();
            try
            {
                var documentIdentifiers = identifiers
                    .Select(HttpUtility.UrlEncode)
                    .AsEnumerable();

                var updateAlias = GetUpdateAlias(updateIndex);
                var response = _elasticClient.MultiGet(m => m.GetMany<object>(documentIdentifiers, (op, id) => op
                                  .Index(updateAlias)
                                     .Source(s => s
                                        .Includes(x => x))));

                if (!response.IsValid)
                {
                    var errorMessage = "No valid result was found for the given identifiers";
                    _logger.LogDebug(errorMessage);
                    throw new EntityNotFoundException(errorMessage);
                }

                if (response != null && response.Hits.Count > 0)
                {
                    foreach (var searchObj in response.Hits)
                    {
                        if (searchObj != null && searchObj.Source != null)
                        {
                            searchresult.Add((JObject)searchObj.Source);
                        }
                    }
                }

            }
            catch (System.Exception ex)
            {
                _logger.LogError("Exception while trying to get Elasticsearch error reason: " + ex.ToString());
                return searchresult;
            }

            return searchresult;
        }

        public IDictionary<string, IEnumerable<JObject>> GetDocuments(IEnumerable<string> identifiers, IEnumerable<string> fieldsToReturn, bool includeDraft = false)
        {
            IDictionary<string, IEnumerable<JObject>> resultDict = new Dictionary<string, IEnumerable<JObject>>();
            try
            {
                var searchAlias_Draft = GetSearchAlias(SearchIndex.Draft);
                var searchAlias_Public = GetSearchAlias(SearchIndex.Published);
                MultiGetResponse response;

                if (includeDraft)
                {
                    response = _elasticClient.MultiGet(m => m
                        .GetMany<object>(identifiers, (op, id) => op
                            .Index(searchAlias_Draft)
                            .Source(s => s
                                .Includes(x => x
                                    .Fields(fieldsToReturn.ToArray()
                                    ))
                                ))
                        .GetMany<object>(identifiers, (op, id) => op
                            .Index(searchAlias_Public)
                            .Source(s => s
                                .Includes(x => x
                                    .Fields(fieldsToReturn.ToArray()
                                    )))));
                }
                else
                {
                    response = _elasticClient.MultiGet(m => m
                        .GetMany<object>(identifiers, (op, id) => op
                            .Index(searchAlias_Public)
                            .Source(s => s
                                .Includes(x => x
                                    .Fields(fieldsToReturn.ToArray()
                                    )))));
                }

                if (!response.IsValid)
                {
                    var errorMessage = "No valid result was found for the given identifiers";
                    _logger.LogDebug(errorMessage);
                    throw new EntityNotFoundException(errorMessage);
                }

                var groupedHits = response.Hits.GroupBy(h => h.Id);

                resultDict = groupedHits.ToDictionary(
                    gh => gh.Key,
                    gh => gh.Select(doc => doc.Source == null ? null : JObject.Parse(doc.Source.ToString()!)));
            }
            catch (ElasticsearchClientException ex)
            {
                _logger.LogError("Exception while trying to get Elasticsearch error reason: " + ex.ToString() + "DebugInformation : " + ex.DebugInformation);
                return resultDict;
            }
            return resultDict;
        }

        /// <summary>
        /// <see cref="IElasticSearchRepository.ExecuteRawQuery(JObject)"/>
        /// </summary>
        /// <exception cref="InvalidRequestException">Elasticsearch failed to parse the query.</exception>
        public JObject ExecuteRawQuery(JObject jsonQuery, SearchIndex searchIndex)
        {
            // Set the highlighting for hits in documents.
            var highlight = JObject.FromObject(new { pre_tags = new[] { "<strong>" }, post_tags = new[] { "</strong>" }, fields = JObject.Parse("{\"*\": {\"number_of_fragments\": 0} }") });
            jsonQuery.Add("highlight", highlight);
            var jsonString = jsonQuery.ToString();

            _logger.LogDebug($"Sending search_request={jsonString} to ES");

            // Execute search
            var searchAlias = GetSearchAlias(searchIndex);
            var lowLevelResponse = _elasticClient.LowLevel.Search<StringResponse>(searchAlias, jsonString);

            _logger.LogDebug($"Retrieved response with debug_information={lowLevelResponse.DebugInformation}");

            var response = JObject.Parse(lowLevelResponse.Body);

            // Try to find an error from response
            if (response["error"] != null)
            {
                string reason = "";
                try
                {
                    reason = response["error"]["root_cause"][0]["reason"].ToString();
                }
                catch (System.Exception e)
                {
                    _logger.LogError("Exception while trying to get Elasticsearch error reason: " + e.ToString());
                }
                var isInvalidQuery = reason.StartsWith("Failed to parse query");
                if (isInvalidQuery)
                {
                    throw new ArgumentException(reason);
                }
            }
            return response;
        }

        /// <summary>
        /// <see cref="IElasticSearchRepository.GetMappingProperties"/>
        /// </summary>
        public IList<MappingProperty> GetMappingProperties(SearchIndex searchIndex)
        {
            IList<MappingProperty> mappingProperties = null;
            var searchAlias = GetSearchAlias(searchIndex);
            CallWithTimeStampLog(() =>
            {
                var indexName = _elasticClient.GetIndicesPointingToAlias(Names.Parse(searchAlias))?.FirstOrDefault();

                if (indexName != null)
                {
                    var response = _elasticClient.Indices.GetMapping<object>(g => g.Index(indexName));
                    var properties = response.Indices[indexName].Mappings.Properties;
                    mappingProperties = properties.Select(m => new MappingProperty(m.Key.Name, m.Value.Type)).ToList();
                    //var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromHours(12));
                }
            }, nameof(GetMappingProperties));

            return mappingProperties;
        }

        /// <summary>
        /// <see cref="IElasticSearchRepository.GetFacets(IList{MappingProperty})"/>
        /// </summary>
        public FacetDTO GetFacets(IList<MappingProperty> mappingProperties, SearchIndex searchIndex)
        {
#if DEBUG
            _messages.Clear();
#endif
            var facets = GetAllFacets();
            var response = new FacetDTO();

            CallWithTimeStampLog(() =>
            {
                // cache IEnumerable by calling ToList()
                var onlySearchFacet = facets.Where(x => x.FacetType == FacetType.Always).ToList();

                var aggregationFacets = onlySearchFacet.Where(x => !x.IsRangeFilter);
                var dateFacets = onlySearchFacet.Where(x => x.IsRangeFilter);

                CallWithTimeStampLog(() =>
                {
                    // Query to get buckets for aggregation
                    var aggregation = GetAggregationBuckets(aggregationFacets, searchIndex);
                    var enrichedResponse = GetEnrichedResponseForFacets(aggregation);
                    response.Aggregations = enrichedResponse;
                }, "EnrichingFacetsAggregation");

                CallWithTimeStampLog(() =>
                {
                    // Query to get date ranges for aggregation of the type date
                    var dateAggregations = GetDateAggregationBuckets(dateFacets, searchIndex);
                    var enrichedResponseDate = GetEnrichedResponseForDateFacets(dateAggregations, dateFacets);
                    response.RangeFilters = enrichedResponseDate;
#if DEBUG
                    response.Messages = _messages;
#endif
                }, "EnrichingFacetsDateAggregation");
            }, "ProcessingGetFacets");

            return response;
        }

        private AggregateDictionary GetAggregationBuckets(IEnumerable<Facet> aggregationFacets, SearchIndex searchIndex)
        {
            var searchAlias = GetSearchAlias(searchIndex);
            var aggregationResponse = _elasticClient.Search<dynamic>(s => s.Index(searchAlias).AggregationQuery(aggregationFacets));
            return aggregationResponse.Aggregations;
        }

        private AggregateDictionary GetDateAggregationBuckets(IEnumerable<Facet> dateFacets, SearchIndex searchIndex)
        {
            var searchAlias = GetSearchAlias(searchIndex);
            var dateAggregationsResponse = _elasticClient.Search<dynamic>(s => s.Index(searchAlias).DateAggregationQuery(dateFacets));
            return dateAggregationsResponse.Aggregations;
        }

        /// <summary>
        /// Enriches the standard response for date range aggregations with additional metadata.
        /// </summary>
        /// <param name="dateAggregations">Response from Elasticsearch with date aggregations.</param>
        /// <param name="dateFacets">List of all properties with date type.</param>
        private List<RangeAggregationDTO> GetEnrichedResponseForDateFacets(AggregateDictionary dateAggregations, IEnumerable<Facet> dateFacets)
        {
            var enrichedAggregations = new List<RangeAggregationDTO>();

            foreach (var dateFacet in dateFacets)
            {
                var key = dateFacet.Name;

                try
                {
                    if (dateAggregations.GetValueOrDefault(key + "_min", null) is ValueAggregate fromAggregate
                        && dateAggregations.GetValueOrDefault(key + "_max", null) is ValueAggregate toAggregate)
                    {
                        var properties = GetMetadataCollection()[key].Properties;
                        var label = properties.GetValueOrDefault(Graph.Metadata.Constants.Shacl.Name, string.Empty);
                        string from = string.Empty;
                        string to = string.Empty;

                        // Add date property only, if a date aggregation is available in the document set.
                        if (!string.IsNullOrEmpty(fromAggregate.ValueAsString)
                            && !string.IsNullOrEmpty(toAggregate.ValueAsString))
                        {
                            from = fromAggregate.ValueAsString;
                            to = toAggregate.ValueAsString;

                            // Explicit naming used to allow easy variable refactoring.
                            enrichedAggregations.Add(new RangeAggregationDTO
                            {
                                Key = key,
                                Label = label,
                                From = from,
                                To = to
                            }
                            );
                        }
                    }
                }
                catch (System.Exception)
                {
                    _logger.LogError($"Could not get proper label from metadata for the following datefacet {key}");
                }
            }

            return enrichedAggregations;
        }

        /// <summary>
        /// Enriches the standard response for bucket aggregations with additional metadata.
        /// </summary>
        /// <param name="nestAggregations">Response from Elasticsearch with bucket aggregations.</param>
        private List<AggregationDTO> GetEnrichedResponseForFacets(AggregateDictionary nestAggregation, IDictionary<string, List<string>> searchAggregations = null)
        {
            var enrichedAggregations = new List<AggregationDTO>();
            var metadataCollection = GetMetadataCollection();
            foreach (var aggregate in nestAggregation)
            {
                string key = aggregate.Key;
                string metadataKey = HttpUtility.UrlDecode(key);
                if (!(aggregate.Value is SingleBucketAggregate))
                {
                    continue;
                }

                var aggValue = aggregate.Value as SingleBucketAggregate;
                var properties = GetMetadataPropertiesByKey(metadataKey, metadataCollection);
                string label = properties.GetValueOrDefault(Graph.Metadata.Constants.Shacl.Name, string.Empty);
                // Always show facets on top if it is type i.e. Resource Type.
                int order = Uris.RdfSyntaxType.Equals(metadataKey) ? 0 : properties.GetValueOrDefault(Graph.Metadata.Constants.Shacl.Order, 99999);
                bool isTaxonomy = !Uris.RdfSyntaxType.Equals(metadataKey) && properties.ContainsKey("taxonomy");

                foreach (var (aggKey, aggBuckets) in aggValue)
                {
                    // DateAggregation returns ValueAggregate object, because it is different query.
                    // Here we are interested in buckets with at least one record for filter.

                    var buckets = new List<BucketDTO>();

                    switch (aggBuckets)
                    {
                        // Check if aggregation is for a taxonomy
                        // Aggregations for taxonomies are of type FiltersAggregate
                        case FiltersAggregate filtersAggregate:
                            {
                                var taxonomyAggregations = filtersAggregate;
                                foreach (var entry in taxonomyAggregations)
                                {
                                    var value = entry.Value as SingleBucketAggregate;
                                    buckets.Add(new BucketDTO { Key = entry.Key, DocCount = value.DocCount });
                                }

                                break;
                            }
                        case BucketAggregate bucketAggregate:
                            {
                                var termsBucket = bucketAggregate;
                                foreach (var bucket in termsBucket.Items)
                                {
                                    var keyedBucket = bucket as KeyedBucket<object>;
                                    buckets.Add(new BucketDTO { Key = keyedBucket.Key.ToString(), DocCount = keyedBucket.DocCount });
                                }

                                break;
                            }
                        default:
                            continue;
                    }

                    var aggregationKey = HttpUtility.UrlDecode(aggregate.Key);
                    if (!buckets.Any() || (buckets.All(b => b.DocCount == 0) && searchAggregations != null && !searchAggregations.ContainsKey(aggregationKey)))
                    {
                        continue;
                    }

                    enrichedAggregations.Add(new AggregationDTO { Key = metadataKey, Label = label, Order = order, Taxonomy = isTaxonomy, Buckets = buckets });
                }
            }

            return enrichedAggregations;
        }

        /// <summary>
        /// Returns metadata properties for specified key.
        /// </summary>
        /// <returns>Returns propertiens for key as dicitionary.</returns>
        private IDictionary<string, dynamic> GetMetadataPropertiesByKey(string key, MetadataCollection metadataCollection = null)
        {

            IDictionary<string, dynamic> properties = new Dictionary<string, dynamic>();

            //Check which level of metadata
            var is2ndLevel = key.Contains(_valueAccesor);
            if (is2ndLevel)
            {
                var splittedKey = key.Split(_valueAccesor + ".");
                if (!splittedKey.IsNullOrEmpty())
                {
                    var firstLevelKey = splittedKey[0];
                    var secondLevelKey = splittedKey[1];
                    var nestedMetadata = metadataCollection[firstLevelKey].NestedMetadata;
                    foreach (var nestedProperty in nestedMetadata)
                    {
                        var nestedEntry = nestedProperty.Properties.FirstOrDefault(x => string.Equals(x.Properties[Uris.HasPid], secondLevelKey));
                        // Check if this propertey contains the key, if not check the next nested property in the list
                        if (nestedEntry == null)
                            continue;
                        properties = nestedEntry.Properties;
                        break;
                    }
                }
            }
            else
            {
                properties = metadataCollection[key].Properties;
            }

            return properties;
        }

        /// <summary>
        /// Returns all properties which are aggregatable by metadata definition.
        /// </summary>
        /// <returns>Returns a list with all aggregateable Properties.</returns>
        private IList<Facet> GetAllFacets()
        {
            IList<Facet> facetList = null;
            CallWithTimeStampLog(() =>
            {
                var metadataCollection = GetMetadataCollection();

                facetList = new List<Facet>();
                foreach (var metadata in metadataCollection)
                {
                    // All values greater than 0 are considerd as facet
                    if (metadata.Value.Properties.TryGetValue(Graph.Metadata.Constants.Shacl.IsFacet, out var mainIsFacet) && mainIsFacet > 0)
                    {
                        var containsTaxonomy = ContainsTaxonomy(metadata);
                        var facet = new Facet
                        {
                            FacetType = (FacetType)mainIsFacet,
                            Name = metadata.Key,
                            IsRangeFilter = IsDateTimeDatatype(metadata),
                            ContainsTaxonomy = containsTaxonomy,
                            Taxonomy = containsTaxonomy ? TaxonomyTransformer.BuildFlatDictionary(metadata.Value.Properties[Strings.Taxonomy]) : null
                        };
                        facetList.Add(facet);
                    }
                    // Iterate for nested metadata
                    foreach (var nestedMetadata in metadata.Value.NestedMetadata)
                    {
                        // Get only properties which are facets
                        foreach (var nestedProperty in nestedMetadata.Properties)
                        {
                            var name = $"{metadata.Key}.outbound.value.{nestedProperty.Properties[Uris.HasPid]}";

                            // Check if nested property is facet and not included in the facet list already
                            if (nestedProperty.Properties.TryGetValue(Graph.Metadata.Constants.Shacl.IsFacet, out var isFacet) && isFacet > 0 && !facetList.Any(x => x.Equals(name)))
                            {
                                var containsTaxonomy = ContainsTaxonomy(metadata);
                                var facet = new Facet
                                {
                                    FacetType = (FacetType)nestedProperty.Properties[Graph.Metadata.Constants.Shacl.IsFacet],
                                    Name = name,
                                    IsRangeFilter = false,
                                    ContainsTaxonomy = ContainsTaxonomy(metadata),
                                    Taxonomy = containsTaxonomy ? TaxonomyTransformer.BuildFlatDictionary(metadata.Value.Properties[Strings.Taxonomy]) : null
                                };
                                facetList.Add(facet);
                            }
                        }
                    }
                }
            }, nameof(GetAllFacets));

            return facetList;
        }

        private bool IsDateTimeDatatype(KeyValuePair<string, MetadataProperty> attribute)
        {
            return attribute.Value.Properties.Values.Contains(Graph.Metadata.Constants.DataTypes.DateTime);
        }

        private bool ContainsTaxonomy(KeyValuePair<string, MetadataProperty> attribute)
        {
            return attribute.Value.Properties.Keys.Contains(Strings.Taxonomy) && !attribute.Value.Key.Equals(Uris.RdfSyntaxType);
        }

        /// <summary>
        /// <see cref="IElasticSearchRepository.IndexDocument(string, JObject)"/>
        /// New document will be written to the update alias.
        /// </summary>
        public object IndexDocument(string id, JObject documentToIndex, UpdateIndex updateIndex)
        {
            var updateAlias = GetUpdateAlias(updateIndex);
#if DEBUG
            //_logger.LogInformation($"Indexing document with id={id} and content={documentToIndex} to index={updateAlias} with type={_defaultType}");
#endif
            //_logger.LogInformation("Indexing document with:\nid={id}\ncontent={document}\nto index={documentUpdateAlias}\nwith type={defaultType}", id, documentToIndex.ToString(), updateAlias, _defaultType);

            var indexedDocument = _elasticClient.Index(documentToIndex, idx => idx.Index(updateAlias)
                                                                               .Id(id));
            return indexedDocument;
        }

        /// <summary>
        /// <see cref="IElasticSearchRepository.IndexDocuments(JObject[])"/>
        /// New documents will be written to the update alias.
        /// </summary>
        public object IndexDocuments(JObject[] documents, UpdateIndex updateIndex)
        {
            var descriptor = new BulkDescriptor();
            var index = GetUpdateAlias(updateIndex);

            foreach (var doc in documents)
            {
                descriptor = descriptor.Index<JObject>(idx => idx
                                                        .Index(index)
                                                        .Id(doc["resourceId"]["outbound"][0]["uri"].ToString())
                                                        .Document(doc)
                                                     );
            }

            return _elasticClient.Bulk(descriptor);
        }

        private TimeSpan GetCurrentTimeStamp()
        {
            return DateTime.UtcNow.TimeOfDay;
        }

        /// <summary>
        /// <see cref="IElasticSearchRepository.Search(SearchRequestDto)"/>
        /// </summary>
        public SearchResultDTO Search(SearchRequestDto searchRequest, bool delay)
        {
            // Tranform search term with special logic
            searchRequest.SearchTerm = ApplySearchTermTransformations(searchRequest.SearchTerm);
            var result = new SearchResultDTO();
#if DEBUG
            _messages.Clear();
#endif

            var receivedTime = GetCurrentTimeStamp();

            var originalSearchTerm = searchRequest.SearchTerm;
            if (delay)
            {
                Thread.Sleep(3500);
            }
            ISearchResponse<dynamic> esSearchRequest = null;
            // Excute search with DMP search logic
            CallWithTimeStampLog(() => esSearchRequest = BuildSearchObject(searchRequest), nameof(BuildSearchObject));

            CallWithTimeStampLog(() =>
           {
               // auto-correction part, only if allowed
               if (esSearchRequest.IsValid && !searchRequest.NoAutoCorrect && searchRequest.EnableSuggest)
               {
                   // In case of no result try to fetch result for suggested term.
                   var totalHits = esSearchRequest.Hits.Count;
                   if (totalHits <= 0)
                   {
                       string phraseSuggest = esSearchRequest.PhraseNames().FirstOrDefault();

                       if (!string.IsNullOrEmpty(phraseSuggest))
                       {
                           // if phrase suggest is available, search for this again
                           searchRequest.SearchTerm = phraseSuggest;
                           esSearchRequest = BuildSearchObject(searchRequest);

                           result.OriginalSearchTerm = originalSearchTerm;
                           result.SuggestedSearchTerm = phraseSuggest;
                       }
                   }
               }
           }, "AutoCorrectionAndSuggestion");

            // Query to get metadata for facets and enrich elasticsearch standard response
            if (searchRequest.EnableAggregation)

            {

                CallWithTimeStampLog(() => { EnrichElasticsearchResponseForAggregationBuckets(esSearchRequest, searchRequest, ref result); }, "Enriching");

            }



            result.Suggest = esSearchRequest.Suggest;
            result.Hits = new HitDTO
            {
                Hits = esSearchRequest.Hits,
                Total = esSearchRequest.HitsMetadata != null ? esSearchRequest.HitsMetadata.Total.Value : 0,
                MaxScore = esSearchRequest.HitsMetadata.MaxScore != null ? esSearchRequest.HitsMetadata.MaxScore.Value : 0
            };

            WriteLogsAfterSearch(esSearchRequest, searchRequest);
#if DEBUG
            _messages.Insert(0, $"Api call made from client at {searchRequest.ApiCallTime} ");
            _messages.Insert(1, $"Api call received by server at {receivedTime} ");
            _messages.Add($"Respsone send from server at {GetCurrentTimeStamp()}");
            result.Messages = _messages;
#endif
            return result;
        }

        private void EnrichElasticsearchResponseForAggregationBuckets(ISearchResponse<dynamic> esSearchRequest, SearchRequestDto searchRequest, ref SearchResultDTO result)
        {
            // Query to get metadata for facets
            IList<Facet> facets = null;
            GetAllFacets();
            CallWithTimeStampLog(() => { facets = GetAllFacets(); }, nameof(GetAllFacets));
            // Enrich Elasticsearch standard responses
            var dateFacets = facets.Where(x => x.IsRangeFilter);
            result.Aggregations = GetEnrichedResponseForFacets(esSearchRequest.Aggregations, searchRequest.AggregationFilters);
            result.RangeFilters = GetEnrichedResponseForDateFacets(esSearchRequest.Aggregations, dateFacets);
            result.Suggest = esSearchRequest.Suggest;
        }

        #region Write logs

        private void WriteLogsAfterSearch(ISearchResponse<dynamic> esSearchRequest, SearchRequestDto searchRequest)
        {
            // Create unique search ID for logging purposes
            var searchID = Guid.NewGuid();

            //Write search to logging
            var additionalInfoSearch = new Dictionary<string, object>();
            additionalInfoSearch.Add("searchText", searchRequest.SearchTerm);
            additionalInfoSearch.Add("searchID", searchID.ToString());
            // Write to logs
            _statiticsLogService.Info("DMP_SEARCH", additionalInfoSearch);

            WriteDmpAggregationsToLogs(searchRequest, searchID);
            WriteDmpTopSearchResultsToLogs(searchRequest, esSearchRequest, searchID);

        }

        private void WriteDmpAggregationsToLogs(SearchRequestDto searchRequest, Guid searchID)
        {
            var metadataCollection = GetMetadataCollection();
            foreach (var filter in searchRequest.AggregationFilters)
            {
                var additionalInfoSearch = new Dictionary<string, object>();
                // Arrange the data needed for logs
                var metadataKey = System.Web.HttpUtility.UrlDecode(filter.Key);
                var properties = GetMetadataPropertiesByKey(metadataKey, metadataCollection);
                string label = properties.GetValueOrDefault(Graph.Metadata.Constants.Shacl.Name, string.Empty);

                // Create additional info for log entry
                additionalInfoSearch.Add("searchID", searchID.ToString());
                additionalInfoSearch.Add("searchTerm", searchRequest.SearchTerm);
                additionalInfoSearch.Add("categoryKey", filter.Key);
                additionalInfoSearch.Add("categoryLabel", label);
                additionalInfoSearch.Add("pageFrom", searchRequest.From);
                additionalInfoSearch.Add("size", searchRequest.Size);

                // Write aggregation category to logs
                _statiticsLogService.Info("DMP_Aggregation_Category", additionalInfoSearch);

                // Write aggregation instances to logs
                foreach (var instance in filter.Value)
                {
                    additionalInfoSearch.Add("aggregationValue", instance);
                    // Write aggregation instances/values to logs
                    _statiticsLogService.Info("DMP_Aggregation_Value", additionalInfoSearch);
                    additionalInfoSearch.Remove("aggregationValue");
                }
            }
        }

        private void WriteDmpTopSearchResultsToLogs(SearchRequestDto searchRequest, ISearchResponse<dynamic> esSearchRequest, Guid searchID)
        {
            var rank = 1;
            // Write top search results to logs
            foreach (var hit in esSearchRequest.Hits)
            {
                var additionalInfo = new Dictionary<string, object>();

                var document = hit.Source;
                var pidUri = (string)document[Uris.HasPid]["outbound"][0]["uri"];
                var label = (string)document[Uris.HasLabel]["outbound"][0]["value"];

                // Add additional info to logging
                additionalInfo.Add("score", hit.Score);
                additionalInfo.Add("rank", rank);
                additionalInfo.Add("pidUri", pidUri);
                additionalInfo.Add("resourceLabel", label);
                additionalInfo.Add("searchText", searchRequest.SearchTerm);
                additionalInfo.Add("searchID", searchID.ToString());
                // Write to logs
                _statiticsLogService.Info("DMP_TOP_RESULTS", additionalInfo);
                //Increase rank
                rank++;
            }
        }

        public void WriteSavedSearchFavoritesListSubscriptionsToLogs(Dictionary<string, int> requestData, string type, string statiticsName)
        {
            // Write top search results to logs
            foreach (var data in requestData)
            {
                var additionalInfo = new Dictionary<string, object>();

                // Add additional info to logging
                additionalInfo.Add("userId", data.Key);
                additionalInfo.Add(type, data.Value);
                // Write to logs
                _statiticsLogService.Info(statiticsName, additionalInfo);
            }
        }

        #endregion Write logs

        /// <summary>
        /// Apply special transformations on search terms. Transformations can be to escape or remove special characters.
        /// </summary>
        /// <returns> Transformed search term. </returns>
        private string ApplySearchTermTransformations(string searchTerm)
        {
            string newSearchTerm;
            // Escape Brackets ([]) for query string query.
            // Brackets are used in business context and has to be escaped, because in elastic they are used to query ranges.
            newSearchTerm = searchTerm.Replace("[", @"\[");
            newSearchTerm = newSearchTerm.Replace("]", @"\]");
            newSearchTerm = string.IsNullOrEmpty(searchTerm) ? newSearchTerm : (newSearchTerm.Contains("/") ? Regex.Replace(newSearchTerm, @"\/?$", @"\/") : newSearchTerm);

            return newSearchTerm;
        }

        private void CallWithTimeStampLog(Action a, string methodName)
        {
#if DEBUG
            var start = GetCurrentTimeStamp();
            //_messages.Add(LogMessage("started at", methodName));
            a.Invoke();
            var end = GetCurrentTimeStamp();
            // _messages.Add(LogMessage("finished at", methodName));
            _messages.Add($"{methodName} execution time {(end - start).Milliseconds} ms");
#else
            a.Invoke();
#endif
        }

        /// <summary>
        /// Takes a search request DTO and tranforms it into a valid Elasticsearch JSON DSL request followed by DMP search logic.
        /// </summary>
        /// <param name="searchRequest">A search request in accordance to the DTO <see cref="SearchRequestDto"/> for handling of search requests. </param>>
        /// <returns>Elasticsearch standard response.</returns>
        private ISearchResponse<dynamic> BuildSearchObject(SearchRequestDto searchRequest)
        {
            var facets = GetAllFacets();

            // Get all facets except dateTime.
            var aggregationFacets = facets.Where(x => !x.IsRangeFilter).ToList();
            var rangeFacets = facets.Where(x => x.IsRangeFilter);



            QueryContainer dateRangeQuery = null;
            CallWithTimeStampLog(() =>

            {

                // Add date range filters to query

                if (searchRequest.RangeFilters != null)

                {

                    // Date fields are not available in all documents. If a date field is not avaialable for a document,

                    // it should be returned anyhow in the results. Therefore a must not exists query will be combined

                    // with the date range query. Queries are linked with boolean OR operator.

                    foreach (var rangeFilter in searchRequest.RangeFilters)

                    {

                        var fromDate = Convert.ToDateTime(rangeFilter.Value.GetValueOrDefault("from"));

                        var toDate = Convert.ToDateTime(rangeFilter.Value.GetValueOrDefault("to"));

                        // Set to date to end of the day

                        toDate = toDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

                        // Container to combine date range and must not exist query

                        QueryContainer rangeQuery = null;

                        rangeQuery |= new DateRangeQuery

                        {

                            Field = rangeFilter.Key + _valueAccesor,

                            GreaterThanOrEqualTo = fromDate,

                            LessThanOrEqualTo = toDate

                        };

                        // Link must not exists query with boolean OR operator

                        rangeQuery |= !new ExistsQuery

                        {

                            Field = rangeFilter.Key + _valueAccesor

                        };

                        // Link combined query for date field with boolean AND operator

                        dateRangeQuery &= rangeQuery;

                    }

                }

            }, $"{nameof(BuildSearchObject)}.QueryString");

            // A container to build search query with NEST API for Elasticsearch

            var queryString = new QueryStringQuery { Query = searchRequest.SearchTerm };
            var queryContainer = new QueryContainer(queryString);

            queryContainer |= new NestedQuery
            {
                Path = "http://pid.bayer.com/kos/19050/LinkTypes.outbound",
                ScoreMode = NestedScoreMode.Sum,
                Boost = 0.5,
                Query = queryString
            };

            queryContainer |= new NestedQuery
            {
                Path = "http://pid.bayer.com/kos/19050/LinkTypes.inbound",
                ScoreMode = NestedScoreMode.Sum,
                Boost = 0.5,
                Query = queryString
            };

            var query = new BoolQuery { Must = new List<QueryContainer>() { queryContainer } };

            if (searchRequest.SearchIndex == SearchIndex.All)
            {
                query.MustNot = new List<QueryContainer>()
                {
                    new TermQuery()
                    {
                        Field = $"resourceLinkedLifecycleStatus{_uriAccesor}",
                        Value = COLID.Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Draft
                    }
                };
            }

            ISearchResponse<dynamic> searchResult = null;

            // Get queries for aggregation filters

            var aggregationFilterQueries = GetFilterQueriesForAggregations(searchRequest.AggregationFilters, aggregationFacets);

            // Set the right filter query for the search query. Filters will be set as post filter in the search query.

            var aggregationPostFilter = aggregationFilterQueries != null ? aggregationFilterQueries[Strings.AllFilters] : new MatchAllQuery();



            // Finally, execute build query

            CallWithTimeStampLog(() =>
                        {
                            searchResult = _elasticClient.Search<dynamic>(s =>
                {
                    s
                    .Index(GetSearchAlias(searchRequest))
                    .From(searchRequest.From)
                    .Size(searchRequest.Size)
                    .Query(q => query)
                    .PostFilter(q => aggregationPostFilter && dateRangeQuery);

                    if (!searchRequest.FieldsToReturn.IsNullOrEmpty())
                    {
                        s.Source(sf => sf
                            .Includes(i => i.Fields(searchRequest.FieldsToReturn.ToArray<string>()))
                        );
                    }

                    if (searchRequest.EnableSuggest)
                    {
                        s.SuggestQuery(searchRequest.SearchTerm.ToLower());
                    }

                    if (searchRequest.EnableAggregation)
                    {
                        s.Aggregations(agg => agg.AggregationSelector(aggregationFacets, aggregationFilterQueries, dateRangeQuery)

                                            .DateAggregationSelector(rangeFacets));
                    }

                    if (searchRequest.EnableHighlighting)
                    {
                        s.AddHighlighting();
                    }

                    if (!string.IsNullOrWhiteSpace(searchRequest.OrderField))
                    {
                        var orderField = searchRequest.OrderField == "_score"
                            ? SortSpecialField.Score.GetStringValue()
                            : $"{searchRequest.OrderField}{_valueAccesor}";

                        s = s.Sort(ss => searchRequest.Order == SearchOrder.Asc ? ss.Ascending(orderField) : ss.Descending(orderField));
                    }

                    return s;
                }

                                                        );

                        }, $"{nameof(BuildSearchObject)}.ElasticSearchResult");

            return searchResult;
        }

        private IDictionary<string, QueryContainer> GetFilterQueriesForAggregations(IDictionary<string, List<string>> aggregationFilters, IList<Facet> aggregationFacets)
        {
            IDictionary<string, QueryContainer> aggFiltersQueries = new Dictionary<string, QueryContainer>();

            // Add filters to query
            if (aggregationFilters == null)
            {
                return aggFiltersQueries;
            }

            var aggregationFilterKeys = aggregationFilters.Keys.ToList();

            aggregationFilterKeys.Add(Strings.AllFilters);

            foreach (var aggregationFilterKey in aggregationFilterKeys)
            {
                var filteredAggregationFilters = aggregationFilters.Where(ag => ag.Key != aggregationFilterKey);

                // If no aggregation filter exists (e.g. AggFilter count 1), add match all query for filter query.

                QueryContainer filterQuery = filteredAggregationFilters.IsNullOrEmpty() ? new MatchAllQuery() : null;

                foreach (var aggFilter in filteredAggregationFilters)
                {
                    QueryContainer queryContainerForKey = null;

                    var containsTaxonomy = aggregationFacets.Any(x => x.Name == aggFilter.Key && x.ContainsTaxonomy);

                    // Add all aggregation filter values for key as match queries with boolean OR operator
                    foreach (var value in aggFilter.Value)
                    {
                        if (containsTaxonomy)
                        {
                            queryContainerForKey |= new BoolQuery()
                            {
                                Should = new List<QueryContainer>
                                {
                                    new MatchQuery { Field = aggFilter.Key + _valueAccesor + ".taxonomy", Query = value },
                                    new MatchQuery {Field = aggFilter.Key + _uriAccesor, Query = value}
                                }
                            };
                        }
                        else
                        {
                            queryContainerForKey |= new BoolQuery()
                            {
                                Should = new List<QueryContainer>
                                {
                                    new TermQuery { Field = aggFilter.Key + _valueAccesor, Value = value },
                                    new TermQuery {Field = aggFilter.Key + _uriAccesor, Value = value}
                                }
                            };
                        }
                    }

                    // Add queries of key and combine them with other keys using boolean AND operator
                    filterQuery &= queryContainerForKey;
                }

                // Add filter queries for key to dictionary
                aggFiltersQueries.Add(aggregationFilterKey, filterQuery ?? new MatchAllQuery());
            }

            return aggFiltersQueries;
        }

        /// <summary>
        /// Autocomplete a given Input
        /// </summary>
        /// <param name="searchText"></param>
        /// <returns></returns>
        public IList<string> Suggest(string searchText, SearchIndex searchIndex)
        {
            try
            {
                var searchAlias = GetSearchAlias(searchIndex);
                var output = _elasticClient.Search<dynamic>(s => s.Index(searchAlias).SuggestAggregationQuery(searchText));

                return output.Suggestions();
            }
            catch (System.Exception e)
            {
                _logger.LogError(e.ToString());
            }
            return new List<string>();
        }

        public IList<string> PhraseSuggest(string searchText, SearchIndex searchIndex)
        {
            try
            {
                var searchAlias = GetSearchAlias(searchIndex);
                var output = _elasticClient.Search<dynamic>(s => s.Index(searchAlias).SuggestQuery(searchText));
                return output.PhraseNames();
            }
            catch (System.Exception e)
            {
                _logger.LogError(e.ToString());
            }

            // TODO return error
            return new List<string>();
        }

        /// <summary>
        /// Delete a document from the index
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public object DeleteDocument(string id, UpdateIndex updateIndex)
        {
            var updateAlias = GetUpdateAlias(updateIndex);

            _logger.LogInformation("Deleting document with id={id} from index={documentUpdateAlias} with type={defaultType}", id, updateAlias, _defaultType);

            var response = _elasticClient.Delete<StringResponse>(id, doc => doc.Index(updateAlias));
            return response;
        }

        /// <summary>
        /// Index new metadata as single document in a dedicated index only for metadata storage. The
        /// index will store only a single document which will be updated during reindexing due to
        /// changes on metadata ontology.
        /// </summary>
        /// <param name="metadata"></param>
        /// <returns></returns>
        public object IndexMetadata(JObject metadata)
        {
#if DEBUG
            _logger.LogInformation("Indexing metadata with content={metadata} to index={metadataUpdateAlias} with type={defaultType}", metadata, _metadataUpdateAlias, _defaultType);

#endif
            // Usage of the same document id will ensure that only one document is stored in index.
            // Document will be always overwritten with latest metadata.
            var indexedDocument = _elasticClient.Index(metadata, idx => idx.Index(_metadataUpdateAlias).Id("1"));

            LogElasticsearchResponse(indexedDocument, "Index metadata");

            return indexedDocument;
        }

        /// <summary>
        /// Provides the metadata from current metadata index.
        /// </summary>
        /// <returns>Collection of current metadata for DMP.</returns>
        public MetadataCollection GetMetadataCollection()
        {
            // Index with metadata holds only one document with the _id 1, which represents the current metadata.
            var metadata = _elasticClient.Get<MetadataCollection>("1", req => req.Index(_metadataSearchAlias)).Source;

            return metadata;
        }

        public object GetResourceTypes()
        {
            var resourcetypes = _elasticClient.Search<dynamic>(s => s
                .Index(_metadataSearchAlias)
                .Query(q => q
                    .Term(t => t
                        .Field("_id")
                        .Value("1")
                    )
                )
                .Source(sf => sf
                    .Includes(i => i
                        .Field("http://www.w3.org/1999/02/22-rdf-syntax-ns#type.properties.https://pid.bayer.com/kos/19050#ControlledVocabulary")
                    )
                )
            );

            return resourcetypes.Hits;
        }

        /// <summary>
        /// <see cref="IElasticSearchRepository.CreateMetadataIndex"/>
        /// </summary>
        public string CreateMetadataIndex(IList<Action> rollbackActions, out IEnumerable<string> oldIndexNames)
        {
            if (rollbackActions == null)
            {
                throw new ArgumentNullException(nameof(rollbackActions));
            }

            var newIndexName = _metadataIndexPrefix + DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
            var indexResponse = _elasticClient.Indices.Create(newIndexName);

            // Function to delete new index, if something goes wrong in the further process
            rollbackActions.Add(() => DeleteIndex(newIndexName));

            LogElasticsearchResponse(indexResponse, "Create metadata index");

            // Is necessary for cleaning up after successful creation of the index
            oldIndexNames = _elasticClient.GetIndicesPointingToAlias(Names.Parse(_metadataUpdateAlias));

            // Delete all old and add new index name
            UpdateAlias(_metadataUpdateAlias, newIndexName, oldIndexNames, rollbackActions);

            var disabledMapping = new JObject { { "enabled", false } };

            var indicesPutMappingResponse = _elasticClient.LowLevel.Indices.PutMapping<StringResponse>(_metadataUpdateAlias, disabledMapping.ToString());

            return newIndexName;
        }

        /// <summary>
        /// <see cref="IElasticSearchRepository.CreateDocumentIndex"/>
        /// </summary>
        public string CreateDocumentIndex(Dictionary<string, JObject> metadataObject, UpdateIndex updateIndex, IList<Action> rollbackActions, out IEnumerable<string> oldIndexNames)
        {
            if (rollbackActions == null)
            {
                throw new ArgumentNullException(nameof(rollbackActions));
            }

            var newIndexName = $"{_resourceIndexPrefix}-{updateIndex}-".ToLower() + DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");

            var metadataProperties = metadataObject.Select(metadata => metadata.Value.ToObject<MetadataProperty>()).Where(p => p != null);

            // Configures index settings with analyzers for text analysis on DMP search index. Custom analyzers will be configured
            // using built-in and custom token filter. Custom token filter will be also created.
            var indexResponse = _elasticClient.Indices.Create(newIndexName, c => c
.Settings(s => s
.NumberOfReplicas(0)
.NumberOfShards(1)
.Setting(UpdatableIndexSettings.MaxNGramDiff, 13)
.Setting("index.mapping.total_fields.limit", 3000)
.Analysis(a => a
.TokenFilters(tf => tf
.AddEnglishStopwordsFilter()
.AddAutoCompleteNgramFilter()
.AddAutoCompleteShingleFilter()
.AddTaxonomyFilters(metadataProperties)
.AddNgramFilter()
.AddWordDelimeter()
)
.Analyzers(an => an
.AddDmpNgramAnalyzer()
.AddDmpStandardEnglishAnalyzer()
.AddAutoCompleteNgramAnalyzer()
.AddSearchAutoCompleteAnalyzer()
.AddDymTrigramAnalyzer()
.AddAutoCompleteShingleAnalyzer()
.AddTaxonomyAnalyzers(metadataProperties)
)
.Normalizers(n => n
.AddLowercaseNormalizer()
)
.Tokenizers(t => t
.AddDmpNgramTokenizer()
)
)
)
);

            // Funktion to delete new index, if something goes wrong in the further process
            rollbackActions.Add(() => DeleteIndex(newIndexName));

            LogElasticsearchResponse(indexResponse, "Create document index");

            // Is necessary for cleaning up after successful creation of the index
            var updateAlias = GetUpdateAlias(updateIndex);

            // Delete all old and add new index name
            oldIndexNames = _elasticClient.GetIndicesPointingToAlias(Names.Parse(updateAlias)).ToList();

            UpdateAlias(updateAlias, newIndexName, oldIndexNames, rollbackActions);

            return newIndexName;
        }

        public bool DeleteIndex(string index)
        {
            try
            {
                _elasticClient.Indices.Delete(index);

                return true;
            }
            catch (ElasticsearchClientException)
            {
                return false;
            }
        }

        /// <summary>
        /// <see cref="IElasticSearchRepository.CreateDocumentMapping(Dictionary{string, JObject})"/>
        /// </summary>
        public object CreateDocumentMapping(Dictionary<string, JObject> metadataObject, UpdateIndex updateIndex)
        {
            var updateAlias = GetUpdateAlias(updateIndex);
            var result = _elasticClient.Map<dynamic>(map => map
                    .Index(updateAlias)
                    .Dynamic(false)
                    .Properties(ps => ps
                        .AddStaticFields()
                        .AddLinkedTypes(metadataObject)
                        .AddObject<NoopOptions>(metadataObject)
                    )
                );

            LogElasticsearchResponse(result, "Create document mapping");

            return result;
        }

        /// <summary>
        /// Sets the current search alias to the newest index existing, which has been used for reindexing before.
        /// </summary>
        public void UpdateMetadataSearchAlias(IList<Action> rollbackActions)
        {
            if (rollbackActions == null)
            {
                throw new ArgumentNullException(nameof(rollbackActions));
            }

            var newIndexName = _elasticClient.GetIndicesPointingToAlias(Names.Parse(_metadataUpdateAlias))?.FirstOrDefault();
            var oldIndexNames = _elasticClient.GetIndicesPointingToAlias(Names.Parse(_metadataSearchAlias)).ToList();

            // Delete all old and add new index name
            UpdateAlias(_metadataSearchAlias, newIndexName, oldIndexNames, rollbackActions);
        }

        /// <summary>
        /// Sets the current search alias to the newest index existing, which has been used for reindexing before.
        /// </summary>
        public void UpdateDocumentSearchAlias(IList<Action> rollbackActions, UpdateIndex updateIndex, SearchIndex searchIndex)
        {
            if (rollbackActions == null)
            {
                throw new ArgumentNullException(nameof(rollbackActions));
            }

            var updateAlias = GetUpdateAlias(updateIndex);
            var newIndexName = _elasticClient.GetIndicesPointingToAlias(Names.Parse(updateAlias))?.FirstOrDefault();

            var searchAlias = GetSearchAlias(searchIndex);
            var searchAllAlias = GetSearchAlias(SearchIndex.All);

            var oldIndexNames = _elasticClient.GetIndicesPointingToAlias(Names.Parse(searchAlias)).ToList();

            // Delete all old and add new index name
            UpdateAlias(searchAllAlias, newIndexName, oldIndexNames, rollbackActions);
            UpdateAlias(searchAlias, newIndexName, oldIndexNames, rollbackActions);
        }

        private void UpdateAlias(string alias, string newIndexName, IEnumerable<string> oldIndexNames, IList<Action> rollbackActions)
        {
            var aliasResponse = _elasticClient.Indices.BulkAlias(a =>
            {
                if (!string.IsNullOrWhiteSpace(newIndexName))
                {
                    a.Add(add => add.Alias(alias).Index(newIndexName));
                }

                if (oldIndexNames != null)
                {
                    foreach (var oldIndexName in oldIndexNames.Where(oin => oin != newIndexName))
                    {
                        a.Remove(remove => remove.Alias(alias).Index(oldIndexName));
                    }
                }

                return a;
            });

            var newIndexNames = _elasticClient.GetIndicesPointingToAlias(Names.Parse(alias)).ToList();

            rollbackActions.Add(() => UpdateAlias(alias, oldIndexNames.FirstOrDefault(), newIndexNames, new List<Action>()));

            if (!CheckUpdatedAlias(alias, newIndexName))
            {
                throw new TechnicalException($"Alias \"{alias}\" was not updated");
            }

            LogElasticsearchResponse(aliasResponse, $"Alias \"{alias}\" was updated");
        }

        /// <summary>
        /// Get new index names to check if the alias reassignment was successful
        /// </summary>
        /// <param name="alias">alias to be checked</param>
        /// <param name="indexName">Index name to which the alias should point</param>
        /// <returns>true, if the pointing is correct, otherwise false</returns>
        private bool CheckUpdatedAlias(string alias, string indexName)
        {
            var newCreatedIndexNames = _elasticClient.GetIndicesPointingToAlias(Names.Parse(alias));

            return !newCreatedIndexNames.IsNullOrEmpty() && newCreatedIndexNames.Any(t => t == indexName);
        }

        private void LogElasticsearchResponse(IElasticsearchResponse response, string message)
        {
            var additionalInformation = new Dictionary<string, object>
            {
                {"requestId", (_httpContextAccessor.HttpContext == null) ? null :_httpContextAccessor.HttpContext.TraceIdentifier},
                {nameof(response.ApiCall), response.ApiCall}
            };

            if (response.ApiCall.Success)
            {
                _logger.LogInformation(message, additionalInformation);
            }
            else
            {
                _logger.LogError(message, additionalInformation);
            }
        }

        private string GetSearchAlias(SearchRequestDto searchRequest)
        {
            return GetSearchAlias(searchRequest.SearchIndex);
        }

        private string GetSearchAlias(SearchIndex index)
        {
            return $"{_documentSearchAlias}-{index}".ToLower();
        }

        private string GetUpdateAlias(UpdateIndex index)
        {
            return $"{_documentUpdateAlias}-{index}".ToLower();
        }

        private List<UserBucketDTO> GetUniqueUsers(string index, DateTime date)
        {
            try
            {
                var response = _elasticClient.Search<dynamic>(s => s
                  .Index(index)
                  .Size(0)
                  .Query(q => q
                     .Bool(b => b
                        .Must(m => m
                           .DateRange(rgQuery => rgQuery
                               .Field("fields.logEntry.Timestamp")
                               .GreaterThanOrEquals(DateMath.Anchored(date))
                           )
                        )
                        .MustNot(mn => mn
                           .MatchPhrase(mp => mp
                                  .Field("fields.logEntry.UserId.keyword")
                                  .Query("")
                               )
                        )
                     )
                  )
                  .Aggregations(aggs => aggs
                     .Terms("all_users", au => au
                        .Field("fields.logEntry.UserId.keyword")
                        .Size(100000)
                        .Aggregations(agg2 => agg2
                           .TopHits("first_visit", th => th
                               .Sort(srt => srt
                                  .Field(sf => sf
                                      .Field("fields.logEntry.Timestamp")
                                      .Order(SortOrder.Ascending)
                                  )
                               )
                               .Source(src => src
                                   .Includes(fs => fs
                                       .Field("fields.logEntry.Timestamp")
                                       .Field("fields.logEntry.UserId.keyword")
                                   )
                               )
                               .Size(1)
                           )
                        )
                     )
                  )
               );
                var uniqueUserBuckets = response.Aggregations.Terms("all_users");
                var uniqueUsers = new List<UserBucketDTO>();
                if (uniqueUserBuckets != null)
                {
                    foreach (var user in uniqueUserBuckets.Buckets)
                    {
                        var firstVisits = user.TopHits("first_visit");
                        var hits = firstVisits.Hits<JObject>();
                        if (!user.Key.Equals(""))
                        {
                            foreach (var hit in hits)
                            {
                                uniqueUsers.Add(new UserBucketDTO { Key = user.Key, DocCount = user.DocCount, FirstVisitHit = hit.Source });
                            }
                        }
                    }
                }
                return uniqueUsers;
            }
            catch (System.Exception ex)
            {

                _logger.LogInformation(ex, ex.Message);
                return new List<UserBucketDTO>();

            }
        }

        public bool IsIndexEmpty(string index)
        {
            try
            {
                var response = _elasticClient.Search<dynamic>(s => s
                       .Index(index)
                       .MatchAll()
                       .Size(0)
                    );
                return !(response.Total > 0);
            }
            catch (System.Exception ex)
            {
                _logger.LogInformation(ex, ex.Message);
                return true;
            }
        }

        public List<string> GetUserIDsInIndex(string index, List<string> userIds)
        {
            try
            {
                var response = _elasticClient.Search<dynamic>(s => s
                       .Index(index)
                       .Size(10000)
                       .Source(false)
                       .Query(q => q
                            .Terms(t => t
                                .Field("_id")
                                .Terms<string>(userIds)
                            )
                        )
                    );
                var userHits = new List<string>();
                if (response != null)
                {
                    foreach (var hit in response.Hits)
                    {
                        userHits.Add(hit.Id);
                    }
                }
                return userHits;
            }
            catch (System.Exception ex)
            {
                _logger.LogInformation(ex, ex.Message);
                return null;
            }
        }

        public bool WriteUserToIndex(JObject document, string index, string userID)
        {
            try
            {
                var indexedDocument = _elasticClient.Index<JObject>(document, idx => idx.Index(index).Id(userID));
                _logger.LogInformation("Added unique user={userID} document={document} to index={index}", index, document, userID);
                return true;
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Failed to add user={userID} to index={index}", index, userID, ex, ex.Message);
                return false;
            }
        }

        public void CountAndWriteUniqueUsers(string sourceIndex, string uniqueUserIndexName, DateTime dateTime, bool isDeltaLoad)
        {
            var response = GetUniqueUsers(sourceIndex, dateTime);
            var userIds = new List<string>();
            if (response != null)
            {
                userIds = response.Where(x => !x.Key.Equals("")).Select(x => x.Key).ToList();

                if (isDeltaLoad)
                {
                    var userIdsToWrite = GetOnlyNewUserIDs(uniqueUserIndexName, userIds);
                    if (userIdsToWrite.Count > 0)
                    {
                        foreach (var user in response)
                        {
                            var userID = user.Key;
                            if (userIdsToWrite.Contains(userID))
                            {
                                var userDocument = CreateDocumetForUniqueUser(userID, user.FirstVisitHit);
                                WriteUserToIndex(userDocument, uniqueUserIndexName, userID);
                            }
                        }
                    }
                }
                else
                {
                    foreach (var user in response)
                    {
                        var userID = user.Key;
                        var userDocument = CreateDocumetForUniqueUser(userID, user.FirstVisitHit);
                        WriteUserToIndex(userDocument, uniqueUserIndexName, userID);
                    }
                }
            }
        }

        private List<string> GetOnlyNewUserIDs(string uniqueUserIndexName, List<string> userIds)
        {
            var response = GetUserIDsInIndex(uniqueUserIndexName, userIds);
            if (response != null)
            {
                foreach (var hit in response)
                {
                    userIds.Remove(hit);
                }
                return userIds;
            }
            return new List<string>();
        }

        private JObject CreateDocumetForUniqueUser(string userID, JObject firstVisitHit)
        {
            var timestamp = firstVisitHit["fields"]["logEntry"]["Timestamp"];
            var newDocument = JObject.FromObject(new
            {
                UserID = userID,
                Timestamp = timestamp
            }
                );
            return newDocument;
        }
    }
}
