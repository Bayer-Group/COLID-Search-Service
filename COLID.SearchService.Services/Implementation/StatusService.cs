using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using COLID.SearchService.DataModel.Status;
using COLID.SearchService.Services.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace COLID.SearchService.Services.Implementation
{
    public class StatusService : IStatusService
    {
        private readonly IConfiguration _configuration;

        public StatusService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public BuildInformationDTO GetBuildInformation()
        {
            return new BuildInformationDTO
            {
                VersionNumber = _configuration["Build:VersionNumber"],
                JobId = _configuration["Build:CiJobId"],
                PipelineId = _configuration["Build:CiPipelineId"],
                CiCommitSha = _configuration["Build:CiCommitSha"]
            };
        }
    }
}
