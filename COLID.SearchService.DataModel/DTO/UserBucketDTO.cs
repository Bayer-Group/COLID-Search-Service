using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace COLID.SearchService.DataModel.DTO
{
    public class UserBucketDTO
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("doc_count")]
        public long? DocCount { get; set; }

        [JsonProperty("first_visit_hit")]
        public JObject FirstVisitHit { get; set; }
    }
}
