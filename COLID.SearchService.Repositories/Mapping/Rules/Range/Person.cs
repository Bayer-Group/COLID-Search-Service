using COLID.SearchService.Repositories.Constants;
using COLID.SearchService.Repositories.Mapping.Base;
using COLID.SearchService.Repositories.Mapping.Constants;
using COLID.SearchService.Repositories.Mapping.Extensions;
using COLID.SearchService.Repositories.Mapping.Options;
using Nest;

namespace COLID.SearchService.Repositories.Mapping.Rules.Range
{
    internal sealed class Person : RangeRule
    {
        protected override string Value => Uris.Person;

        protected override ObjectTypeDescriptor<dynamic, dynamic> Mapping<T>(ObjectTypeDescriptor<dynamic, dynamic> o)
        {
            return o
                .Properties(osp => osp
                    .Keyword(ky =>
                    {
                        ky
                            .Name(NodeNames.Value)
                            .Normalizer("lowercase")
                            .Fields(ff => ff
                                .DmpStandardEnglishAnalyzer()
                                .DmpNgramAnalyzer()
                            );

                        if (typeof(T) != typeof(LinkedTypesOptions))
                        {
                            ky.CopyTo(ct => ct.Fields(Strings.DidYouMeanTrigram));
                        }

                        return ky;
                    })
                );
        }
    }
}
