using System.Threading.Tasks;
using COLID.SearchService.DataModel.Search;
using Newtonsoft.Json.Linq;

namespace COLID.SearchService.Services.Interface
{
    /// <summary>
    /// Interface for classes connecting to the similarity service api
    /// </summary>
    public interface IRemoteSimilarityService
    {
        /// <summary>
        /// Gets the similar resources of a partial PID resource, using the Similarity Service
        /// </summary>
        /// <param name="query">A search request in accordance to the DTO <see cref="COLID.SearchService.DataModel.Search.SimilarityRequestDto"/> for handling of search requests. </param>
        /// <param name="threshold">The threshold of the similarity score.</param>
        /// <param name="limit">Limits the returned resources.</param>
        /// <param name="model">The model which should be used to calculate the similarity of resources.</param>
        Task<JObject> PerformRessourceSimilarity(SimilarityRequestDto query, double threshold, int limit, string model);
    }
}
