using COLID.SearchService.Repositories.Mapping.Base;
using COLID.SearchService.Repositories.Mapping.Constants;
using Nest;

namespace COLID.SearchService.Repositories.Mapping.Rules.Range
{
    internal sealed class DateTime : RangeRule
    {
        protected override string Value => Uris.XmlDateTime;

        protected override ObjectTypeDescriptor<dynamic, dynamic> Mapping<T>(ObjectTypeDescriptor<dynamic, dynamic> o)
        {
            return o
                .Properties(osp => osp
                    .Date(ky => ky
                        .Name(NodeNames.Value)
                    )
                );
        }
    }
}
