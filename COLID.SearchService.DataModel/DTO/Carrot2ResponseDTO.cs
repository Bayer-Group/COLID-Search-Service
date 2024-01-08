using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COLID.SearchService.DataModel.DTO
{
    public class Carrot2ResponseDTO
    {
        public IList<responseProperties> Clusters { get; set; }
        
    }

    public class responseProperties
    {
        public IList<string> Labels { get; set; }
        public IList<int> Documents { get; set; }
        public IList<DocuemntDetail> DocDetails { get; set; }
        public decimal Score { get; set; }
        public IList<responseProperties> Clusters { get; set; }
    }
    public class DocuemntDetail
    {
        public string PidUri { get; set; }
        //public string Label { get; set; }

    }
}
