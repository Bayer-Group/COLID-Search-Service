using Newtonsoft.Json;

namespace COLID.SearchService.DataModel.DTO
{
    public class BucketDTO
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("doc_count")]
        public long? DocCount { get; set; }
    }
}
