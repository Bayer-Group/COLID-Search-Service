using COLID.SearchService.Repositories.Mapping.Constants;

namespace COLID.SearchService.Repositories.Mapping.Base
{
    internal abstract class NodeKindRule : BaseRule, IRule
    {
        public string Priority => "3";

        protected override string Key => Uris.ShaclNodeKind;
    }
}
