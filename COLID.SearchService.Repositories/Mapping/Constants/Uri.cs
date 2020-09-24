namespace COLID.SearchService.Repositories.Mapping.Constants
{
    public class Uris
    {
        // Pid related

        public const string Person = @"http://pid.bayer.com/kos/19014/Person";
        public const string HasNetworkAddress = @"http://pid.bayer.com/kos/19014/hasNetworkAddress";
        public const string HasPid = @"http://pid.bayer.com/kos/19014/hasPID";
        public const string HasBaseUri = @"https://pid.bayer.com/kos/19050/hasBaseURI";
        public const string HasVersion = @"https://pid.bayer.com/kos/19050/hasVersion";
        public const string HasVersions = @"https://pid.bayer.com/kos/19050/hasVersions";
        public const string HasContactPerson = @"https://pid.bayer.com/kos/19050/hasContactPerson";
        public const string HasLabel = @"https://pid.bayer.com/kos/19050/hasLabel";
        public const string HasNetworkedResourceLabel = @"https://pid.bayer.com/kos/19050/hasNetworkedResourceLabel";
        public const string LinkTypes = @"http://pid.bayer.com/kos/19050/LinkTypes";
        public const string Distribution = @"https://pid.bayer.com/kos/19050/distribution";
        public const string DistributionEndpointLifecycleStatus = @"https://pid.bayer.com/kos/19050/hasDistributionEndpointLifecycleStatus";
        public const string PermanentIdentifer = @"http://pid.bayer.com/kos/19014/PermanentIdentifier";
        public const string LastChangeDatetime = @"https://pid.bayer.com/kos/19050/lastChangeDateTime";

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
