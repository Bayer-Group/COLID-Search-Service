using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COLID.SearchService.DataModel.Index
{
    public class IndexStaus
    {
        public bool InProgress { get; set; }
        public long TotalDocCount { get; set; }
        public long CurrentDocCount { get; set; }

    }
}
