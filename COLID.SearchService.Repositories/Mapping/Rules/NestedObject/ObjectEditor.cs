using System.Collections.Generic;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.SearchService.Repositories.Mapping.Base;
using COLID.SearchService.Repositories.Mapping.Constants;
using COLID.SearchService.Repositories.Mapping.Extensions;
using COLID.SearchService.Repositories.Mapping.Options;
using Nest;

namespace COLID.SearchService.Repositories.Mapping.Rules.NestedObject
{
    internal sealed class ObjectEditor : NestedObjectRule
    {
        protected override string Value => Uris.ObjectEditor;

        protected override ObjectTypeDescriptor<dynamic, dynamic> Mapping<T>(ObjectTypeDescriptor<dynamic, dynamic> o)
        {
            var currentOptionType = typeof(T).ToString();
            // Check if rule is applied in linked types.
            // If true, do not apply all nested fields of this property
            if (currentOptionType.Equals(typeof(LinkedTypesOptions).ToString()))
            {
                return o.Properties(pn => pn
                     .UriKeyword()
                     .EdgeKeyword()
                     .ValueKeyword()
                );
            }
            else
            {
                var nestedProperties = CollectProperties(Metadata.NestedMetadata);
                return o.Properties(pn => pn
                 .UriKeyword()
                 .EdgeKeyword()
                 .CustomNode(NodeNames.Value, od => od
                     .Properties(pd => pd
                         .AddNestedFields(nestedProperties)
                     )
                 )
                );
            }
        }

        /// <summary>
        /// This methos iterates over all properties for each distributin endpointtype.
        /// </summary>
        /// <param name="nestedMetadata"></param>
        /// <returns> A list with all properties from each distribution endpoint type, whereby properties are unique in the list.</returns>
        private IEnumerable<string> CollectProperties(IList<Metadata> nestedMetadata)
        {
            var nestedProperties = new HashSet<string>();
            if (nestedMetadata != null)
            {
                // Iterate over all distirbution Endpoint types
                foreach (var distributionEndpointType in nestedMetadata)
                {
                    // Add property
                    foreach (var property in distributionEndpointType.Properties)
                    {
                        var propertyKey = property.Key;
                        if (!string.IsNullOrEmpty(propertyKey))
                        {
                            nestedProperties.Add(propertyKey);
                        }
                    }
                }
            }
            return nestedProperties;
        }
    }
}
