using System.Collections.Generic;
using COLID.Graph.TripleStore.DataModels.Taxonomies;
using Newtonsoft.Json.Linq;

namespace COLID.SearchService.Repositories.Indexing
{
    public static class TaxonomyTransformer
    {
        /// <summary>
        /// creates a dictionary where for all children all parent nodes are in one list.
        /// </summary>
        /// <param name="taxonmysJArray"></param>
        /// <returns></returns>
        public static IDictionary<TaxonomyResultDTO, IList<TaxonomyResultDTO>> BuildFlatDictionary(JArray taxonmysJArray)
        {
            IList<TaxonomyResultDTO> taxonomyResults = taxonmysJArray?.ToObject<List<TaxonomyResultDTO>>();

            var dict = new Dictionary<TaxonomyResultDTO, IList<TaxonomyResultDTO>>();

            if (taxonomyResults == null)
            {
                return dict;
            }

            foreach (var taxonomy in taxonomyResults)
            {
                dict.Add(taxonomy, new List<TaxonomyResultDTO>());

                if (taxonomy.HasChild)
                {
                    HandleChildNodes(taxonomy.Children, dict, new List<TaxonomyResultDTO>() { taxonomy });
                }
            }

            return dict;
        }

        private static void HandleChildNodes(IList<TaxonomyResultDTO> children, Dictionary<TaxonomyResultDTO, IList<TaxonomyResultDTO>> dict, IList<TaxonomyResultDTO> parentsList)
        {
            foreach (var child in children)
            {
                if (!dict.ContainsKey(child))
                {
                    dict.Add(child, parentsList);
                }

                if (!child.HasChild)
                {
                    continue;
                }

                var childParentList = new List<TaxonomyResultDTO>(parentsList) { child };
                HandleChildNodes(child.Children, dict, childParentList);
            }
        }
    }
}
