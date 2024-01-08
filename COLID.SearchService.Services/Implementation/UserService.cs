using System.Collections.Generic;
using COLID.SearchService.Repositories.Interface;
using COLID.SearchService.Services.Interface;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using COLID.SearchService.Repositories.Constants;
using COLID.SearchService.DataModel.DTO;
using COLID.SearchService.DataModel.Search;
using OpenSearch.Client;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Xml.Linq;
using System.Linq;
using Microsoft.Identity.Client;
using VDS.RDF.Shacl.Validation;
using COLID.SearchService.DataModel.Statistics;

namespace COLID.SearchService.Services.Implementation
{
    /// <summary>
    /// Services for handling of all fetching unique user operations from Elastic Search.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IElasticSearchRepository _elasticSearchRepository;
        private readonly ILogger<UserService> _logger;
        private readonly IConfiguration _configuration;

        public UserService(IElasticSearchRepository repository, ILogger<UserService> logger, IConfiguration configuration)
        {
            _elasticSearchRepository = repository;
            _logger = logger;
            _configuration = configuration;
        }

        public void WritePIDDMPUniqueUsers(string appName)
        {
            var sourceIndex = _configuration.GetSection("Indices").GetValue<string>(appName);

            var uniqueUserIndexName = _configuration.GetSection("StatisticsUniqueUsersIndices").GetValue<string>(appName);

            bool checkIfIndexEmpty = _elasticSearchRepository.IsIndexEmpty(uniqueUserIndexName);
            if (checkIfIndexEmpty)
            {
                _logger.LogInformation("index is empty and thus we load data from start time {Strings.statisticsStartDate}", Strings.StatisticsStartDate);
                _elasticSearchRepository.CountAndWriteUniqueUsers(sourceIndex, uniqueUserIndexName, DateTime.Parse(Strings.StatisticsStartDate), false);
            }
            else
            {
                _logger.LogInformation("index is not empty and thus we load data from previous day");
                _elasticSearchRepository.CountAndWriteUniqueUsers(sourceIndex, uniqueUserIndexName, DateTime.Now.Date.AddDays(-1), true);
            }
        }

        public void WriteDmpAllSavedSearchFiltersCountToLogs(Dictionary<string, int> allSavedSearchFilters)
        {
            _elasticSearchRepository.WriteSavedSearchFavoritesListSubscriptionsToLogs(allSavedSearchFilters, "savedSearchCount", "DMP_SAVEDSEARCH_FILTERS");
        }

        public void WriteFavoritesListCountToLogs(Dictionary<string, int> allFavoritesList)
        {
            _elasticSearchRepository.WriteSavedSearchFavoritesListSubscriptionsToLogs(allFavoritesList, "favoritesListCount", "DMP_FAVORITESLIST");
        }

        public void WriteAllSubscriptionsCountToLogs(Dictionary<string, int> allSubscriptions)
        {
            _elasticSearchRepository.WriteSavedSearchFavoritesListSubscriptionsToLogs(allSubscriptions, "userSubscriptionsCount", "DMP_USER_SUBSCRIPTIONS");
        }

        public HierarchicalData UserDepartmentsFlowView()
        {
            var departmentBuckets = new List<BucketDTO>();

            string jsonString = @"
            {
              ""query"": {
                ""bool"": {
                  ""should"": [
                    {
                      ""match"": {
                        ""fields.logEntry.Message"": ""PID_WELCOME_PAGE_OPENED""
                      }
                    },
                    {
                      ""match"": {
                        ""fields.logEntry.Message"": ""DMP_WELCOME_PAGE_OPENED""
                      }
                    }
                  ]
                }
              },
              ""size"": 0, 
                ""aggs"": {
                ""departments"": {
                  ""terms"": {
                    ""field"": ""fields.logEntry.Department.keyword"",
                    ""size"": 50000
    
                  }
                }
              }
            }";

            var elasticQueryResults = _elasticSearchRepository.ExecuteRawQuery(JObject.Parse(jsonString), SearchIndex.Log);

            var aggregationsBuckets = elasticQueryResults["aggregations"]["departments"]["buckets"];

            if (aggregationsBuckets.Any())
            {
                departmentBuckets = aggregationsBuckets.Select(bucket =>
                {
                    return new BucketDTO
                    {
                        Key = bucket["key"].ToString(),
                        DocCount = bucket["doc_count"].Value<int>()
                    };
                }).ToList();
            }

            var nodes = new List<Node>();
            var links = new List<Link>();
            
        foreach (var data in departmentBuckets)
        {
                var levelHierarchy = data.Key.Split('-').Length;
                for (int i = levelHierarchy; i > 0; i--)
                {
                    var name = String.Join('-', data.Key.Split('-').Take(i));

                    var lastNode = i == levelHierarchy;
                    var node = nodes.FirstOrDefault(n => n.Name == name);
                    if (node == null)
                    {
                        node = new Node { Name = name, Id = name };
                        nodes.Add(node);
                    }

                    // If node is not the last elemnt in chain, we need to create a link
                    if (!lastNode)   
                    {
                        var targetName = String.Join('-', data.Key.Split('-').Take(i+1));
                        var existingLink = links.FirstOrDefault(link => link.Source == name && link.Target == targetName);
                        if (existingLink != null)
                        {
                            existingLink.Value += (int)data.DocCount;
                        } 
                        else
                        {
                            links.Add(new Link { Source = name, Target = targetName, Value = (int)data.DocCount });
                        }
                    }
                }
        }

            var nodeTotalValues = links.GroupBy(l => l.Source).ToDictionary(g => g.Key, g => g.Sum(v => v.Value));

            foreach (var link in links)
            {
                nodeTotalValues.TryGetValue(link.Source, out var totalUsage);
                link.Percentage = decimal.Divide(link.Value, totalUsage) * 100;
            }

            var hierarchicalData = new HierarchicalData { Nodes = nodes, Links = links };
        
            return hierarchicalData;
        }
         
    }
}
