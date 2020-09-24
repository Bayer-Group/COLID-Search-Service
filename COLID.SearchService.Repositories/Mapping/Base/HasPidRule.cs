using COLID.SearchService.Repositories.Mapping.Constants;

namespace COLID.SearchService.Repositories.Mapping.Base
{
    internal abstract class HasPidRule : BaseRule, IRule
    {
        public string Priority => "1";

        protected override string Key => Uris.HasPid;
    }
}
