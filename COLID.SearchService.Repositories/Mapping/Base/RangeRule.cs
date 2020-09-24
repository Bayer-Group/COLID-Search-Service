using COLID.SearchService.Repositories.Mapping.Constants;

namespace COLID.SearchService.Repositories.Mapping.Base
{
    internal abstract class RangeRule : BaseRule, IRule
    {
        public string Priority => "2";

        protected override string Key => Uris.RdfSchemeRange;
    }
}
