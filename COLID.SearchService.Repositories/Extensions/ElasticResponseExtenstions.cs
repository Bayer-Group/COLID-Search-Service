using System.Collections.Generic;
using System.Linq;
using COLID.SearchService.Repositories.Constants;
using OpenSearch.Client;

namespace COLID.SearchService.Repositories.Extensions
{
    internal static class ElasticResponseExtenstions
    {
        internal static List<string> PhraseNames(this ISearchResponse<dynamic> response)
        {
            return response.Suggest[Strings.PhraseName][0].Options.Select(opt => opt.Text).ToList();
        }

        internal static IList<string> Suggestions(this ISearchResponse<dynamic> output)
        {
            return output.Aggregations.Filter(Strings.Limiter).Terms(Strings.DMPSuggestions).Buckets.Select(x => x.Key).ToList();
        }
    }
}
