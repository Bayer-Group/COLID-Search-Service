using COLID.SearchService.Repositories.Mapping.Base;
using COLID.SearchService.Repositories.Mapping.Constants;
using COLID.SearchService.Repositories.Mapping.Extensions;
using Nest;

namespace COLID.SearchService.Repositories.Mapping.Rules.Range
{
    /// <summary>
    /// Class for handling of permanent identifers such as URIs.
    /// </summary>
    internal sealed class PermanentIdentifer : RangeRule
    {
        protected override string Value => Uris.PermanentIdentifer;

        protected override ObjectTypeDescriptor<dynamic, dynamic> Mapping<T>(ObjectTypeDescriptor<dynamic, dynamic> o)
        {
            return o
                .Properties(osp => osp
                    .UriKeyword()
                    .Keyword(ky =>
                    {
                        ky
                            .Name(NodeNames.Value)
                            .Fields(ff => ff
                                .DmpStandardEnglishAnalyzer()
                            );
                        return ky;
                    })
                );
        }
    }
}
