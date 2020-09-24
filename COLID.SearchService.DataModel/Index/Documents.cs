using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;

namespace COLID.SearchService.DataModel.Index
{
    public class Documents : DocumentBase
    {
        [Required]
        public JObject[] Content { get; set; }
    }
}
