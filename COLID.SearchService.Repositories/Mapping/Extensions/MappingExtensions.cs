using System;
using System.Collections.Generic;
using System.Linq;
using COLID.SearchService.Repositories.Constants;
using COLID.SearchService.Repositories.Mapping.Base;
using COLID.SearchService.Repositories.Mapping.Constants;
using COLID.SearchService.Repositories.Mapping.Options;
using Nest;
using Newtonsoft.Json.Linq;

namespace COLID.SearchService.Repositories.Mapping.Extensions
{
    internal static class MappingExtensions
    {
        public static PropertiesDescriptor<dynamic> AddObject<T>(
            this PropertiesDescriptor<dynamic> ps,
            Dictionary<string, JObject> metadataObject) where T : IOptions
        {
            var rules = GetRules();
            var isRuleApplied = false;

            foreach (var item in metadataObject.Where(x => x.Value.IsNotLinkedType()))
            {
                var metadata = item.Value;
                var properties = metadata.SelectToken(Strings.Properties);

                foreach (var rule in rules)
                {
                    foreach (var jToken in properties)
                    {
                        var prop = (JProperty)jToken;
                        if (rule.TryExecute<T>(item.Key, prop, ps, metadata))
                        {
                            isRuleApplied = true;
                            break;
                        }
                    }

                    if (isRuleApplied)
                    {
                        isRuleApplied = false;
                        break;
                    }
                }
            }

            return ps;
        }

        internal static bool IsNotLinkedType(this JObject metaItem)
        {
            foreach (var item in metaItem.SelectToken(Strings.Properties))
            {
                if (item is JProperty itemProperty && itemProperty.Name == Uris.ShaclGroup)
                {
                    foreach (var jToken in itemProperty.Value)
                    {
                        if (jToken is JProperty propertyItem
                            && propertyItem.Name == Strings.Key
                            && propertyItem.Value.ToString() == Uris.LinkTypes)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static IList<IRule> GetRules()
        {
            var baseAssembly = typeof(IRule).Assembly;
            var typeList = baseAssembly.DefinedTypes
                .Where(type =>
                    !type.IsAbstract &&
                    type.ImplementedInterfaces.Any(imp => imp == typeof(IRule)))
                .ToList();

            var rules = typeList.Select(item => (IRule)Activator.CreateInstance(item))
                .OrderBy(x => x.Priority)
                .ToList();
            return rules;
        }
    }
}
