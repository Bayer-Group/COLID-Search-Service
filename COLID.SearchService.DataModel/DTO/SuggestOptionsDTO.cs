using Newtonsoft.Json;

namespace COLID.SearchService.DataModel.DTO
{
    public class SuggestOptionsDTO
    {
        [JsonProperty("text")]
        public string Test { get; set; }

        [JsonProperty("score")]
        public int Score { get; set; }
    }
}
