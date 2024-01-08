using COLID.SearchService.Repositories.Mapping.Constants;
using OpenSearch.Client;

namespace COLID.SearchService.Repositories.Mapping.Extensions
{
    public static class HasVersionsExtension
    {
        public static PropertiesDescriptor<dynamic> HasVersions(this PropertiesDescriptor<dynamic> pd)
        {
            pd.CustomNode(Uris.HasVersions, ob => ob
                .Properties(op => op
                    .BoundNode(NodeNames.Outbound)
                    .BoundNode(NodeNames.Inbound)
                )
            );

            return pd;
        }

        private static PropertiesDescriptor<dynamic> BoundNode(this PropertiesDescriptor<dynamic> pd, string boundType)
        {
            return pd
                .CustomNode(boundType, ob => ob
                    .Properties(op => op
                        .CustomNode(NodeNames.Value, ob2 => ob2
                            .Properties(op1 => op1
                                .HasVersionValueNode(Uris.HasPid)
                                .HasVersionValueNode(Uris.HasBaseUri)
                                .HasVersionValueNode(Uris.HasVersion)
                            )
                        )
                       .UriKeyword()
                       .EdgeKeyword()
                    )
                );
        }

        private static PropertiesDescriptor<dynamic> HasVersionValueNode(
            this PropertiesDescriptor<dynamic> pd,
            string nodeName)
        {
            return pd
                .CustomNode(nodeName, ob => ob
                    .Properties(op => op
                        .ValueKeyword()
                        .UriKeyword()
                    )
                );
        }
    }
}
