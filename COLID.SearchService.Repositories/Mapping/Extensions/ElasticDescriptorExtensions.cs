using System;
using System.Collections.Generic;
using COLID.SearchService.Repositories.Constants;
using COLID.SearchService.Repositories.Mapping.Constants;
using Nest;

namespace COLID.SearchService.Repositories.Mapping.Extensions
{
    public static class ElasticDescriptorExtensions
    {
        public static PropertiesDescriptor<dynamic> AddStaticFields(this PropertiesDescriptor<dynamic> ps)
        {
            return ps
                .AddResourceId()
                .AddResourceLinkedLifecycleStatusId()
                .AutocompleteFilter()
                .AutocompleteTerms()
                .DymTrigram()
                .HasVersions();
        }

        public static PropertiesDescriptor<dynamic> CustomNode(
            this PropertiesDescriptor<dynamic> pp,
            string nodeName,
            Func<ObjectTypeDescriptor<dynamic, dynamic>, ObjectTypeDescriptor<dynamic, dynamic>> mapping)
        {
            return pp.Object<dynamic>(oo => mapping(oo.Name(nodeName)));
        }

        public static PropertiesDescriptor<dynamic> DmpNgramAnalyzer(this PropertiesDescriptor<dynamic> pd)
        {
            return pd.Text(tt => tt.DmpNgramAnalyzer());
        }

        public static PropertiesDescriptor<dynamic> DmpStandardEnglishAnalyzer(this PropertiesDescriptor<dynamic> pd)
        {
            return pd.Text(tt => tt.DmpStandardEnglishAnalyzer());
        }

        public static PropertiesDescriptor<dynamic> DmpTaxonomyAnalyzer(this PropertiesDescriptor<dynamic> pd, string metadataPropertyKey)
        {
            return pd.Text(ft =>
                ft.Name(Strings.Taxonomy).Analyzer(metadataPropertyKey.GetPreparedAnalyzerName(ElasticAnalyzers.TaxonomyTextPrefix))
                    .SearchAnalyzer(metadataPropertyKey.GetPreparedAnalyzerName(ElasticAnalyzers.TaxonomyTextSearchPrefix)));
        }

        public static PropertiesDescriptor<dynamic> DmpNgramTaxonomyAnalyzer(this PropertiesDescriptor<dynamic> pd, string metadataPropertyKey)
        {
            return pd.Text(ft =>
                ft.Name("Ngram" + Strings.Taxonomy).Analyzer(metadataPropertyKey.GetPreparedAnalyzerName(ElasticAnalyzers.TaxonomyTextPrefix) + "_ngram")
                    .SearchAnalyzer(metadataPropertyKey.GetPreparedAnalyzerName(ElasticAnalyzers.TaxonomyTextSearchPrefix)));
        }

        /// <summary>
        /// This field is added to allow to find taxonomies terms and partial matching ones in the exaxt search of the term in the query string query.
        /// </summary>
        /// <param name="pd"></param>
        /// <param name="metadataPropertyKey"></param>
        /// <returns></returns>
        public static PropertiesDescriptor<dynamic> ExactTaxonomyAnalyzer(this PropertiesDescriptor<dynamic> pd, string metadataPropertyKey)
        {
            return pd.Text(ft =>
                ft.Name("ExactTaxo" + Strings.Taxonomy).Analyzer(metadataPropertyKey.GetPreparedAnalyzerName(ElasticAnalyzers.TaxonomyTextPrefix) + "_exact_taxo")
                    .SearchAnalyzer("keyword"));
        }

        public static PropertiesDescriptor<dynamic> AddNestedFields(this PropertiesDescriptor<dynamic> np,
            string nodeName)
        {
            return np
                .CustomNode(nodeName, obj => obj
                    .Properties(pp => pp
                        .CustomNode(NodeNames.Outbound, ob => ob
                            .NestedNodeProperties()
                        )
                    )
                );
        }

        public static PropertiesDescriptor<dynamic> AddNestedFields(this PropertiesDescriptor<dynamic> np,
           IEnumerable<string> properties)
        {
            foreach (var property in properties)
            {
                np.AddNestedFields(property);
            }
            return np;
        }

        public static PropertiesDescriptor<dynamic> DymTrigram(this PropertiesDescriptor<dynamic> ps)
        {
            return ps
                .Text(tt => tt
                    .Name(Strings.DidYouMeanTrigram)
                    .Analyzer(ElasticAnalyzers.DidYouMeanTrigram)
                );
        }

        public static PropertiesDescriptor<dynamic> AutocompleteTerms(this PropertiesDescriptor<dynamic> ps)
        {
            return ps
                .Text(tt => tt
                    .Name(Strings.AutoCompleteTerms)
                    .Fielddata()
                    .Analyzer(ElasticAnalyzers.AutoCompleteShingle)
                );
        }

        public static PropertiesDescriptor<dynamic> AutocompleteFilter(this PropertiesDescriptor<dynamic> ps)
        {
            return ps
                .Text(tt => tt
                    .Name(Strings.AutoCompleteFilter)
                    .Analyzer(ElasticAnalyzers.AutoCompleteNgram)
                    .SearchAnalyzer(ElasticAnalyzers.SearchAutoComplete)
                );
        }

        public static PropertiesDescriptor<dynamic> AddResourceId(this PropertiesDescriptor<dynamic> ps)
        {
            return ps
                .Object<dynamic>(ob => ob
                    .Name(Strings.ResourceId)
                    .Properties(ps1 => ps1
                        .Object<dynamic>(ob1 => ob1
                            .Outbound()
                            .Properties(pd => pd
                                .UriKeyword()
                            )
                        )
                    )
                );
        }

        public static PropertiesDescriptor<dynamic> AddResourceLinkedLifecycleStatusId(this PropertiesDescriptor<dynamic> ps)
        {
            return ps
                .Object<dynamic>(ob => ob
                    .Name(Strings.ResourceLinkedLifecycleStatus)
                    .Properties(ps1 => ps1
                        .Object<dynamic>(ob1 => ob1
                            .Outbound()
                            .Properties(pd => pd
                                .UriKeyword()
                            )
                        )
                    )
                );
        }

        public static PropertiesDescriptor<dynamic> UriKeyword(this PropertiesDescriptor<dynamic> pd)
        {
            return pd.Keyword(kk => kk.Name(NodeNames.Uri));
        }

        public static PropertiesDescriptor<dynamic> ValueKeyword(this PropertiesDescriptor<dynamic> pd)
        {
            return pd.Keyword(kk => kk.Name(NodeNames.Value));
        }

        public static PropertiesDescriptor<dynamic> EdgeKeyword(this PropertiesDescriptor<dynamic> pd)
        {
            return pd.Keyword(kk => kk.Name(NodeNames.Edge));
        }

        public static ObjectTypeDescriptor<dynamic, dynamic> Outbound(this ObjectTypeDescriptor<dynamic, dynamic> otb)
        {
            return otb.Name(NodeNames.Outbound);
        }

        public static TextPropertyDescriptor<dynamic> DmpStandardEnglishAnalyzer(
            this TextPropertyDescriptor<dynamic> tt)
        {
            return tt
                .Name(Strings.Text)
                .Analyzer(Strings.DmpStandardEnglish);
        }

        public static TextPropertyDescriptor<dynamic> DmpNgramAnalyzer(this TextPropertyDescriptor<dynamic> tt)
        {
            return tt
                .Name(Strings.Ngrams)
                .Analyzer(Strings.DmpNgramAnalyzer)
                .SearchAnalyzer(Strings.DmpStandardEnglish);
        }

        public static ObjectTypeDescriptor<dynamic, dynamic> NestedNodeProperties(
            this ObjectTypeDescriptor<dynamic, dynamic> o)
        {
            return o
                .Properties(op => op
                    .UriKeyword()
                    .Keyword(ky => ky.Name(NodeNames.Value)
                        .Fields(ff => ff.DmpNgramAnalyzer().DmpStandardEnglishAnalyzer())
                    )
                );
        }
    }
}
