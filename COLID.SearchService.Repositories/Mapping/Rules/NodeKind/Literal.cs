using COLID.SearchService.Repositories.Constants;
using COLID.SearchService.Repositories.Mapping.Base;
using COLID.SearchService.Repositories.Mapping.Constants;
using COLID.SearchService.Repositories.Mapping.Extensions;
using COLID.SearchService.Repositories.Mapping.Options;
using Nest;

namespace COLID.SearchService.Repositories.Mapping.Rules.NodeKind
{
    internal sealed class Literal : NodeKindRule
    {
        protected override string Value => Uris.ShaclLiteral;

        protected override ObjectTypeDescriptor<dynamic, dynamic> Mapping<T>(ObjectTypeDescriptor<dynamic, dynamic> o)
        {
            return o
                .Properties(osp => osp
                    .Keyword(ky =>
                    {
                        ky
                            .Name(NodeNames.Value)
                            .Fields(ff => ff
                                .DmpStandardEnglishAnalyzer()
                                .DmpNgramAnalyzer()
                            );

                        if (typeof(T) != typeof(LinkedTypesOptions))
                        {
                            ky.CopyTo(cc => cc
                                .Fields(
                                    Strings.DidYouMeanTrigram,
                                    Strings.AutoCompleteFilter,
                                    Strings.AutoCompleteTerms
                                )
                            );
                        }

                        return ky;
                    })
                );
        }
    }
}
