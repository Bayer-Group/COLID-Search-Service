using COLID.SearchService.Repositories.Constants;
using COLID.SearchService.Repositories.Mapping.Base;
using COLID.SearchService.Repositories.Mapping.Constants;
using COLID.SearchService.Repositories.Mapping.Extensions;
using COLID.SearchService.Repositories.Mapping.Options;
using Nest;

namespace COLID.SearchService.Repositories.Mapping.Rules.NodeKind
{
    internal sealed class Iri : NodeKindRule
    {
        protected override string Value => Uris.ShaclIri;

        protected override ObjectTypeDescriptor<dynamic, dynamic> Mapping<T>(ObjectTypeDescriptor<dynamic, dynamic> o)
        {
            var metadataPropertyKey = Metadata.Key;

            return o
                .Properties(osp => osp
                    .UriKeyword()
                    .Keyword(ky =>
                    {
                        ky
                            .Name(NodeNames.Value)
                            .Fields(ff =>
                            {
                                ff.DmpStandardEnglishAnalyzer();
                                ff.DmpNgramAnalyzer();

                                if (Metadata.Properties.ContainsKey(Strings.Taxonomy))
                                {
                                    ff.DmpTaxonomyAnalyzer(metadataPropertyKey);
                                    ff.DmpNgramTaxonomyAnalyzer(metadataPropertyKey);
                                    ff.ExactTaxonomyAnalyzer(metadataPropertyKey);
                                }

                                return ff;
                            });

                        if (typeof(T) != typeof(LinkedTypesOptions))
                        {
                            ky.CopyTo(cc => cc.Fields(Strings.DidYouMeanTrigram));
                        }

                        return ky;
                    })
                );
        }
    }
}
