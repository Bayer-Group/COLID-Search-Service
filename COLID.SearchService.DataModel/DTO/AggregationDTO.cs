using System.Collections.Generic;
using Newtonsoft.Json;

namespace COLID.SearchService.DataModel.DTO
{
    public class AggregationDTO
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("order")]
        public int Order { get; set; }

        [JsonProperty("taxonomy")]
        public bool Taxonomy { get; set; }

        [JsonProperty("buckets")]
        public IList<BucketDTO> Buckets { get; set; }
    }
}
