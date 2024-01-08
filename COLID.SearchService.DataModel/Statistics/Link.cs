using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COLID.SearchService.DataModel.Statistics
{
    public class Link
    {
        public string Source { get; set; }
        public string Target { get; set; }
        public int Value { get; set; }
        public decimal Percentage { get; set; }
    }
}
