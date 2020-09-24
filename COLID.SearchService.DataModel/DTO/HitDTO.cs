using Newtonsoft.Json;

namespace COLID.SearchService.DataModel.DTO
{
    public class HitDTO
    {
        [JsonProperty("total")]
        public long Total { get; set; }

        [JsonProperty("max_score")]
        public double MaxScore { get; set; }

        [JsonProperty("hits")]
        public dynamic Hits { get; set; }
    }
}
