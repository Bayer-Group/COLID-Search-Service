using System.Collections.Generic;
using COLID.SearchService.DataModel.DataTypes;

namespace COLID.SearchService.DataModel.DTO
{
    public class FacetDTO : Tracking
    {
        public List<AggregationDTO> Aggregations { get; set; }

        public List<RangeAggregationDTO> RangeFilters { get; set; }
    }
}
