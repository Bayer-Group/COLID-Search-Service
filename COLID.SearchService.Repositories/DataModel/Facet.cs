using System.Collections.Generic;
using COLID.Common.Enums;
using COLID.Graph.TripleStore.DataModels.Taxonomies;

namespace COLID.SearchService.Repositories.DataModel
{
    public class Facet
    {
        public string Name { get; set; }

        public FacetType FacetType { get; set; }

        public bool IsRangeFilter { get; set; }

        public bool ContainsTaxonomy { get; set; }

        public Dictionary<TaxonomyResultDTO, IList<TaxonomyResultDTO>> Taxonomy { get; set; }
    }
}
