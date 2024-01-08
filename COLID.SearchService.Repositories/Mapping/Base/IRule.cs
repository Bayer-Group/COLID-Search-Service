using COLID.SearchService.Repositories.Mapping.Options;
using OpenSearch.Client;
using Newtonsoft.Json.Linq;

namespace COLID.SearchService.Repositories.Mapping.Base
{
    internal interface IRule
    {
        bool TryExecute<T>(string key, JProperty prop, PropertiesDescriptor<dynamic> ps, JObject nestedMetadata) where T : IOptions;

        string Priority { get; }
    }
}
