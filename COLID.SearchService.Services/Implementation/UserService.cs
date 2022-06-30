using System.Collections.Generic;
using COLID.SearchService.DataModel.Search;
using COLID.SearchService.Repositories.Interface;
using COLID.SearchService.Repositories.Mapping.Constants;
using COLID.SearchService.Services.Interface;
using COLID.MessageQueue.Constants;
using Newtonsoft.Json.Linq;
using System;
using Nest;
using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Logging;
using COLID.SearchService.DataModel.DTO;
using Microsoft.Extensions.Configuration;
using COLID.SearchService.Repositories.Constants;

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
    }
}
