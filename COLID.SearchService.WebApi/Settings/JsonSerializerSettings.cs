using System;
using System.Collections.Generic;
using System.Linq;

using System.Threading.Tasks;
using COLID.Cache.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace COLID.SearchService.WebApi.Settings
{
    /// <summary>
    /// 
    /// </summary>
    public static class JsonSerializerSettings
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
#pragma warning disable CA1024 // Use properties where appropriate
        public static CachingJsonSerializerSettings GetSerializerSettings()
#pragma warning restore CA1024 // Use properties where appropriate
        {
            var serializerSettings = new CachingJsonSerializerSettings
            {
                Converters = new List<JsonConverter>(),
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                },
                Formatting = Formatting.Indented
            };

            return serializerSettings;
        }
    }
}
