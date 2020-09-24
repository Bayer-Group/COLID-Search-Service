using System.Collections.Generic;
using COLID.SearchService.Repositories.Mapping.Constants;
using COLID.SearchService.Repositories.Mapping.Options;
using Nest;
using Newtonsoft.Json.Linq;

namespace COLID.SearchService.Repositories.Mapping.Extensions
{
    public static class LinkedTypes
    {
        public static PropertiesDescriptor<dynamic> AddLinkedTypes(this PropertiesDescriptor<dynamic> pd,
            Dictionary<string, JObject> metadataObject)
        {
            pd.Object<dynamic>(o => o
                .Name(Uris.LinkTypes)
                .Properties(pp => pp
                    .LinkedTypeNode(NodeNames.Inbound, metadataObject)
                    .LinkedTypeNode(NodeNames.Outbound, metadataObject)
                )
            );
            return pd;
        }

        private static PropertiesDescriptor<dynamic> LinkedTypeNode(this PropertiesDescriptor<dynamic> pp,
            string nodeName, Dictionary<string, JObject> metadataObject)
        {
            return pp
                .Nested<dynamic>(nst => nst
                    .Name(nodeName)
                    .Properties(pr => pr
                        .CustomNode(NodeNames.Value, ob => ob
                            .Properties(op =>
                                {
                                    op.AddResourceId();
                                    op.AddObject<LinkedTypesOptions>(metadataObject);
                                    return op;
                                }
                            )
                        )
                        .EdgeKeyword()
                        .UriKeyword()
                    )
                );
        }
    }
}
