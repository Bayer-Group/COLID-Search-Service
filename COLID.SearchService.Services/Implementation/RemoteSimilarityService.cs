using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using COLID.SearchService.DataModel.Search;
using COLID.SearchService.Services.Interface;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace COLID.SearchService.Services.Implementation
{
    /// <summary>
    /// Services for handling of all search operations with DMP search engine.
    /// </summary>
    public class RemoteSimilarityService : IRemoteSimilarityService
    {
        private string _similarityServiceUrl;

        public RemoteSimilarityService(IConfiguration configuration)
        {
            _similarityServiceUrl = configuration.GetConnectionString("SimilarityServiceUrl");
        }

        /// <summary>
        /// <see cref="IRemoteSimilarityService.Similarity(SimilarityRequestDto)"/>
        /// </summary>
        public async Task<JObject> PerformRessourceSimilarity(SimilarityRequestDto query, double threshold, int limit, string model)
        {
            string json = JsonConvert.SerializeObject(query);
            var data = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

            var url = $"{_similarityServiceUrl}/api/v1/similarity/resource?limit={limit}&model={model}&threshold={threshold}";
            using var client = new HttpClient();

            var response = await client.PostAsync(url, data);
            string result = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<JObject>(result);
        }
    }
}
