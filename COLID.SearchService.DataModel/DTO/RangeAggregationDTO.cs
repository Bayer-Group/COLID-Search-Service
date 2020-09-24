using Newtonsoft.Json;

namespace COLID.SearchService.DataModel.DTO
{
    public class RangeAggregationDTO
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }
    }
}
