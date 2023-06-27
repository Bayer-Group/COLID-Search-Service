using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.SearchService.Repositories.Mapping.Constants;
using COLID.SearchService.Repositories.Mapping.Extensions;
using COLID.SearchService.Repositories.Mapping.Options;
using Nest;
using Newtonsoft.Json.Linq;

namespace COLID.SearchService.Repositories.Mapping.Base
{
    internal abstract class BaseRule
    {
        protected abstract string Key { get; }
        protected abstract string Value { get; }
        protected MetadataProperty Metadata { get; set; }

        public bool TryExecute<T>(string key, JProperty prop, PropertiesDescriptor<dynamic> ps, JObject metadata) where T : IOptions
        {
            if (!IsMatch(prop.Name, prop.Value.ToString()))
            {
                return false;
            }

            // Set metadata for rules below
            Metadata = metadata.ToObject<MetadataProperty>();

            ps.Object<dynamic>(ob => ob.Name(key)
                .Properties(pp => pp
                    .CustomNode(NodeNames.Outbound,
                        Mapping<T>
                    )
                )
            );

            return true;
        }

        protected abstract ObjectTypeDescriptor<dynamic, dynamic> Mapping<T>(ObjectTypeDescriptor<dynamic, dynamic> o) where T : IOptions;

        protected bool IsMatch(string key, string value)
        {
            return (key == Key && value == Value);
        }
    }
}
