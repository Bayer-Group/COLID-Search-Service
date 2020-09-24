using System;

namespace COLID.SearchService.DataModel.Configuration
{
    public class ElasticSearchOptions
    {
        public Uri BaseUri { get; set; }
        public string ResourceIndexPrefix { get; set; }
        public string MetadataIndexPrefix { get; set; }
        public string ResourceSearchAlias { get; set; }
        public string MetadataSearchAlias { get; set; }
        public string DocumentUpdateAlias { get; set; }
        public string MetadataUpdateAlias { get; set; }
        public string AwsRegion { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }

        public ElasticSearchOptions()
        {
        }
    }
}
