using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using COLID.SearchService.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using COLID.Identity.Extensions;
using Microsoft.Extensions.Logging;
using COLID.SearchService.DataModel.DTO;
using Newtonsoft.Json;
using COLID.Graph.Metadata.DataModels.FilterGroup;
using System.Net.Mime;
using COLID.Identity.Services;
using COLID.SearchService.Repositories.Configuration;

namespace COLID.SearchService.Services.Implementation
{
    public class RemoteCarrot2Service: IRemoteCarrot2Service
    {
        private readonly IConfiguration _configuration;
        private readonly bool _bypassProxy;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ITokenService<ColidRegistrationServiceTokenOptions> _registrationServiceTokenService;
        private readonly CancellationToken _cancellationToken;
        private readonly ILogger<RemoteCarrot2Service> _logger;

        public RemoteCarrot2Service(IConfiguration configuration, 
            IHttpClientFactory clientFactory,
            IHttpContextAccessor httpContextAccessor,
            ITokenService<ColidRegistrationServiceTokenOptions> registrationServiceTokenService,
            ILogger<RemoteCarrot2Service> logger)
        {
            _configuration = configuration;
            _bypassProxy = _configuration.GetValue<bool>("BypassProxy"); ;
            _clientFactory = clientFactory;
            _cancellationToken = httpContextAccessor?.HttpContext?.RequestAborted ?? CancellationToken.None;
            _registrationServiceTokenService = registrationServiceTokenService;
            _logger = logger;
        }

        public async Task<Carrot2ResponseDTO> Cluster(Carrot2RequestDTO clusterRequest)
        {          
            var strRequest = JsonConvert.SerializeObject(clusterRequest);
            var result = new Carrot2ResponseDTO();
            using (var httpClient = (_bypassProxy ? _clientFactory.CreateClient("NoProxy") : _clientFactory.CreateClient()))
            {
                var colidCarrot2erviceUrl = $"{_configuration.GetConnectionString("colidCarrot2ServiceUrl")}/cluster?indent=true";
                //var response = await httpClient.PostAsync(colidCarrot2erviceUrl, data);
                //string result = await response.Content.ReadAsStringAsync();
                try
                {
                    var accessToken = await _registrationServiceTokenService.GetAccessTokenForWebApiAsync();
                    
                    var response = await httpClient.SendRequestWithOptionsAsync(HttpMethod.Post, colidCarrot2erviceUrl, clusterRequest, accessToken, _cancellationToken);
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("Error requesting Carrot2 Service. Response Status Code {code}", response.StatusCode);
                    }

                    _logger.LogInformation("Filter group: successfully retrieved carrot2  clusters");

                    var jsonString = response.Content.ReadAsStringAsync();
                    result = JsonConvert.DeserializeObject<Carrot2ResponseDTO>(jsonString.Result);
                }
                catch (System.Exception ex)
                {
                    _logger.LogError("Error :" + (ex.InnerException == null ? ex.Message : ex.InnerException.Message));
                }
                return result;
            }
        }
    }
}
