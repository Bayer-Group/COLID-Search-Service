using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;

namespace COLID.SearchService.DataModel.Index
{
    public class Documents : DocumentBase
    {
        [Required]
        public IList<JObject> Content { get; set; }
    }
}
