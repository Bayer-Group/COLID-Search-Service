using System;
using System.Collections.Generic;

namespace COLID.SearchService.DataModel.Search
{
    public class SimilarityRequestDto
    {
        public string id { get; set; }

        public IDictionary<string, string> Properties { get; set; }

        public SimilarityRequestDto()
        {
            Properties = new Dictionary<string, string>();
            id = String.Empty;
        }
    }
}
