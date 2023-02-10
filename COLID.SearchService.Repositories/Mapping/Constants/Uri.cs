using System.IO;
using Microsoft.Extensions.Configuration;

namespace COLID.SearchService.Repositories.Mapping.Constants
{
    public class Uris
    {
        // getting the service url ex: pid.bayer..
        private static readonly string _basePath = Path.GetFullPath("appsettings.json");
        private static readonly string _filePath = _basePath.Substring(0, _basePath.Length - 16);
        private static IConfigurationRoot _configuration = new ConfigurationBuilder()
                     .SetBasePath(_filePath)
                    .AddJsonFile("appsettings.json")
                    .Build();
        public static readonly string _serviceUrl = _configuration.GetValue<string>("ServiceUrl");
        public static readonly string _httpServiceUrl = _configuration.GetValue<string>("HttpServiceUrl");


        // Pid related

        public static readonly string Person =  _httpServiceUrl + @"kos/19014/Person";
        public static readonly string HasNetworkAddress = _httpServiceUrl + @"kos/19014/hasNetworkAddress";
        public static readonly string HasPid = _httpServiceUrl + @"kos/19014/hasPID";
        public static readonly string HasBaseUri = _serviceUrl + @"kos/19050/hasBaseURI";
        public static readonly string HasVersion = _serviceUrl + @"kos/19050/hasVersion";
        public static readonly string HasVersions = _serviceUrl + @"kos/19050/hasVersions";
        public static readonly string HasContactPerson = _serviceUrl + @"kos/19050/hasContactPerson";
        public static readonly string HasLabel = _serviceUrl + @"kos/19050/hasLabel";
        public static readonly string HasNetworkedResourceLabel = _serviceUrl + @"kos/19050/hasNetworkedResourceLabel";
        public static readonly string LinkTypes = _httpServiceUrl + @"kos/19050/LinkTypes";
        public static readonly string Distribution = _serviceUrl + @"kos/19050/distribution";
        public static readonly string DistributionEndpointLifecycleStatus = _serviceUrl + @"kos/19050/hasDistributionEndpointLifecycleStatus";
        public static readonly string PermanentIdentifer = _httpServiceUrl + @"kos/19014/PermanentIdentifier";
        public static readonly string LastChangeDatetime = _serviceUrl + @"kos/19050/lastChangeDateTime";

        // Topbraid related

        public const string EditWidget = @"http://topbraid.org/tosh#editWidget";
        public const string ObjectEditor = @"http://topbraid.org/swa#NestedObjectEditor";

        // Rdf related

        public const string RdfSyntaxType = @"http://www.w3.org/1999/02/22-rdf-syntax-ns#type";
        public const string RdfSchemeRange = @"http://www.w3.org/2000/01/rdf-schema#range";

        // XML related

        public const string XmlFloat = @"http://www.w3.org/2001/XMLSchema#float";
        public const string XmlDateTime = @"http://www.w3.org/2001/XMLSchema#dateTime";
        public const string XmlDate = @"http://www.w3.org/2001/XMLSchema#date";
        public const string XmlBoolean = @"http://www.w3.org/2001/XMLSchema#boolean";
        public const string XmlAnyUri = @"http://www.w3.org/2001/XMLSchema#anyURI";

        // Shacl related

        public const string ShaclLiteral = @"http://www.w3.org/ns/shacl#Literal";
        public const string ShaclGroup = @"http://www.w3.org/ns/shacl#group";
        public const string ShaclNodeKind = @"http://www.w3.org/ns/shacl#nodeKind";
        public const string ShaclIri = @"http://www.w3.org/ns/shacl#IRI";
    }
}
