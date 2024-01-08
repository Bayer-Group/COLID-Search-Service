using System.Collections.Generic;
using System.Linq;
using COLID.SearchService.Repositories.Constants;
using COLID.SearchService.Repositories.DataModel;
using OpenSearch.Net;
using OpenSearch.Client;

namespace COLID.SearchService.Repositories.Extensions
{
    internal static class ElasticRequestExtension
    {
        public static SearchDescriptor<dynamic> DateAggregationQuery(this SearchDescriptor<dynamic> searchDescriptor, IEnumerable<Facet> dateFacets)
        {
            return searchDescriptor.TypedKeys(null)
                                   .Aggregations(agg => agg.DateAggregationSelector(dateFacets)
                                   );
        }

        public static AggregationContainerDescriptor<dynamic> DateAggregationSelector(this AggregationContainerDescriptor<dynamic> agg, IEnumerable<Facet> dateFacets)
        {
            foreach (var dateFacet in dateFacets)
            {
                agg = agg.Min(dateFacet.Name + "_min", ter => ter.Field(dateFacet.Name + ".outbound.value"));
                agg = agg.Max(dateFacet.Name + "_max", ter => ter.Field(dateFacet.Name + ".outbound.value"));
            }

            return agg;
        }

        public static SearchDescriptor<dynamic> AggregationQuery(this SearchDescriptor<dynamic> searchDescriptor, IEnumerable<Facet> aggregationFacets)
        {
            return searchDescriptor.TypedKeys(null)
                    .Aggregations(agg => agg.AggregationSelector(aggregationFacets)
                    );
        }

        public static AggregationContainerDescriptor<dynamic> AggregationSelector(this AggregationContainerDescriptor<dynamic> agg, IEnumerable<Facet> aggregationFacets,
            IDictionary<string, QueryContainer> aggregationFilterQueries = null, QueryContainer dateRangeQuery = null)
        {
            foreach (var facet in aggregationFacets)
            {
                var name = System.Web.HttpUtility.UrlEncode(facet.Name);
                var aggFilterQuery = GetFilterQueryForAggregationByName(aggregationFilterQueries, facet.Name);

                if (facet.ContainsTaxonomy)
                {
                    // Facet with taxonomy will use named filters with query match queries for aggragations
                    agg.Filter(name, q => q
                      .Filter(aq => aggFilterQuery && dateRangeQuery)
                      .Aggregations(childAggs => childAggs
                          .Filters("result", filter => filter.NamedFilters(f =>
                          {
                              return AddNamedFiltersforTaxonomy(facet);
                          }
                          ))
                      )
                   );
                }
                else
                {
                    // Facet without taxonomies will use terms aggregations
                    agg.Filter(name, q => q
                       .Filter(aq => aggFilterQuery && dateRangeQuery)
                       .Aggregations(childAggs => childAggs
                           .Terms("result", ter => ter.Field(facet.Name + ".outbound.value").Size(1000).MinimumDocumentCount(0))
                       )
                    );
                }
            }

            return agg;
        }

        private static QueryContainer GetFilterQueryForAggregationByName(IDictionary<string, QueryContainer> aggregationFilterQueries, string aggName)
        {
            if (aggregationFilterQueries == null || aggregationFilterQueries.Count <= 0)
            {
                return new MatchAllQuery();
            }

            if (aggregationFilterQueries.TryGetValue(aggName, out var aggFilterQuery) && aggFilterQuery != null)
            {
                return aggFilterQuery;
            }

            if (aggregationFilterQueries.TryGetValue(Strings.AllFilters, out var aggAllFilterQuery) && aggAllFilterQuery != null)
            {
                return aggAllFilterQuery;
            }

            return new MatchAllQuery();
        }

        private static NamedFiltersContainerDescriptor<dynamic> AddNamedFiltersforTaxonomy(Facet facet)
        {
            var filterDescriptor = new NamedFiltersContainerDescriptor<dynamic>();
            if (facet.Taxonomy != null)
            {
                //Get name of unique taxonomy entries
                var taxonomyList = facet.Taxonomy.Select(x => x.Key).Select(x => x.Name).Distinct().ToList();

                foreach (var taxonomyEntry in taxonomyList)
                {
                    QueryContainer filterQuery = new MatchQuery
                    {
                        Field = facet.Name + ".outbound.value.taxonomy",
                        Query = taxonomyEntry
                    };
                    filterDescriptor.Filter(taxonomyEntry, filterQuery);
                }
            }
            return filterDescriptor;
        }

        public static SearchDescriptor<dynamic> SuggestQuery(this SearchDescriptor<dynamic> descriptor, string searchText)
        {
            return descriptor.TypedKeys(null)
                             .Suggest(ss => ss
                                  .Phrase(Strings.PhraseName, ph => ph
                                      .Text(searchText)
                                      .Field(Strings.DidYouMeanTrigram)
                                      .GramSize(3)
                                      .MaxErrors(5)
                                      .DirectGenerator(dd => dd
                                          .Field(Strings.DidYouMeanTrigram)
                                          .MaxEdits(2)
                                          .SuggestMode(SuggestMode.Always)
                                          )
                                      )
                                  );
        }

        public static SearchDescriptor<dynamic> SuggestAggregationQuery(this SearchDescriptor<dynamic> searchDescriptor, string searchText)
        {
            return searchDescriptor
                               .TypedKeys(null)
                               .Aggregations(agg => agg
                                    .Filter(Strings.Limiter, fil => fil
                                        .Filter(q => q.Match(mat => mat
                                            .Field(Strings.AutoCompleteFilter)
                                            .Query(searchText.ToLower())
                                            )
                                         )
                                         .Aggregations(aa => aa
                                            .Terms(Strings.DMPSuggestions, ter => ter
                                                .Field(Strings.AutoCompleteTerms)
                                                .Include(searchText.ToLower() + ".*")
                                                .Size(10)
                                            )
                                        )
                                     )
                                 ).Size(0);
        }

        public static SearchDescriptor<dynamic> AddHighlighting(this SearchDescriptor<dynamic> searchDescriptor)
        {
            return searchDescriptor.Highlight(h => h.PreTags("<b>").PostTags("</b>").Fields(fs => fs.Field("*.outbound.value.ngrams").NumberOfFragments(0)));
        }
    }
}
