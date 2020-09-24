using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;

namespace COLID.SearchService.DataModel.Index
{
    public class Document : DocumentBase
    {
        [Required]
        public string Id { get; set; }

        [Required]
        public JObject Content { get; set; }
    }
}
