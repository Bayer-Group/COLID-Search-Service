using System.Collections.Generic;
using Newtonsoft.Json;

namespace COLID.SearchService.DataModel.DTO
{
    public class SuggestDTO
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("offset")]
        public int Offset { get; set; }

        [JsonProperty("options")]
        public List<SuggestOptionsDTO> Options { get; set; }
    }
}
