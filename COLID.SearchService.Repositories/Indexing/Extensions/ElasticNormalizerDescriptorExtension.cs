using COLID.SearchService.Repositories.Constants;
using OpenSearch.Client;

namespace COLID.SearchService.Repositories.Indexing.Extensions
{
    public static class ElasticNormalizerDescriptorExtension
    {
        public static NormalizersDescriptor AddLowercaseNormalizer(this NormalizersDescriptor nd)
        {
            nd.Custom(ElasticFilters.Lowercase, l => l.Filters(ElasticFilters.Lowercase));
            return nd;
        }
    }
}
