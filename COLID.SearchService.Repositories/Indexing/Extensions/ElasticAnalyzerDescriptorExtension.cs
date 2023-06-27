using System.Collections.Generic;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.TripleStore.DataModels.Taxonomies;
using COLID.SearchService.Repositories.Constants;
using COLID.SearchService.Repositories.Mapping.Extensions;
using Nest;

namespace COLID.SearchService.Repositories.Indexing.Extensions
{
    public static class ElasticAnalyzerDescriptorExtension
    {
        public static AnalyzersDescriptor AddTaxonomyAnalyzers(this AnalyzersDescriptor ad, IEnumerable<MetadataProperty> metadataProperties)
        {
            foreach (var metadataProperty in metadataProperties)
            {
                if (metadataProperty == null || !metadataProperty.Properties.ContainsKey(Strings.Taxonomy))
                {
                    continue;
                }

                var taxonomyTextKey = metadataProperty.Key.GetPreparedAnalyzerName(ElasticAnalyzers.TaxonomyTextPrefix);
                var taxonomyTextSearchKey = metadataProperty.Key.GetPreparedAnalyzerName(ElasticAnalyzers.TaxonomyTextSearchPrefix);

                string autoPhraseKey = metadataProperty.Key.GetPreparedAnalyzerName(ElasticFilters.AutoPhrasePrefix);
                string vocabularyKey = metadataProperty.Key.GetPreparedAnalyzerName(ElasticFilters.VocabularyPrefix);

                ad.AddStandardTokenizerFilter(taxonomyTextKey, ElasticFilters.Lowercase, autoPhraseKey, vocabularyKey);
                ad.AddStandardTokenizerFilter(taxonomyTextKey + "_ngram", ElasticFilters.Lowercase, autoPhraseKey, vocabularyKey, ElasticFilters.Ngram);
                ad.AddStandardTokenizerFilter(taxonomyTextKey + "_exact_taxo", ElasticFilters.Lowercase, autoPhraseKey, vocabularyKey, ElasticFilters.FilterWordDelimiter, ElasticFilters.Ngram);
                ad.AddStandardTokenizerFilter(taxonomyTextSearchKey, ElasticFilters.Lowercase, autoPhraseKey);
            }
            return ad;
        }

        public static AnalyzersDescriptor AddStandardTokenizerFilter(this AnalyzersDescriptor ad, string name, params string[] filters)
        {
            return ad.Custom(name, azs => azs.Tokenizer(ElasticTokenizers.Standard).Filters(filters));
        }

        // Analyzers for standard text analysis
        public static AnalyzersDescriptor AddDmpNgramAnalyzer(this AnalyzersDescriptor ad)
        {
            return ad.Custom(ElasticAnalyzers.Ngram, nga => nga
                .Tokenizer(ElasticTokenizers.Ngram)
                .Filters(ElasticFilters.Lowercase, ElasticFilters.AsciiFolding));
        }

        public static AnalyzersDescriptor AddDmpStandardEnglishAnalyzer(this AnalyzersDescriptor ad)
        {
            return ad.Custom(ElasticAnalyzers.StandardEnglish, nga => nga
                .Tokenizer(ElasticTokenizers.Standard)
                .Filters(ElasticFilters.Lowercase, ElasticFilters.EnglishStopwords, ElasticFilters.AsciiFolding));
        }

        // Analyzers for auto complete and did you mean functionalities
        public static AnalyzersDescriptor AddAutoCompleteNgramAnalyzer(this AnalyzersDescriptor ad)
        {
            return ad.Custom(ElasticAnalyzers.AutoCompleteNgram, ang => ang
                .Tokenizer(ElasticTokenizers.Lowercase)
                .Filters(ElasticFilters.Lowercase, ElasticFilters.AsciiFolding, ElasticFilters.AutocompleteNgram)
            );
        }

        public static AnalyzersDescriptor AddSearchAutoCompleteAnalyzer(this AnalyzersDescriptor ad)
        {
            return ad.Custom(ElasticAnalyzers.SearchAutoComplete, ang => ang
                .Tokenizer(ElasticTokenizers.Lowercase)
                .Filters(ElasticFilters.Lowercase, ElasticFilters.AsciiFolding)
            );
        }

        public static AnalyzersDescriptor AddDymTrigramAnalyzer(this AnalyzersDescriptor ad)
        {
            return ad.Custom(ElasticAnalyzers.DidYouMeanTrigram, ang => ang
                .Tokenizer(ElasticTokenizers.Standard)
                .Filters(ElasticFilters.Lowercase, ElasticFilters.AsciiFolding, ElasticFilters.AutocompleteShingle)
            );
        }

        public static AnalyzersDescriptor AddAutoCompleteShingleAnalyzer(this AnalyzersDescriptor ad)
        {
            return ad.Custom(ElasticAnalyzers.AutoCompleteShingle, ang => ang
                .Tokenizer(ElasticTokenizers.Lowercase)
                .Filters(ElasticFilters.Lowercase, ElasticFilters.AsciiFolding, ElasticFilters.AutocompleteShingle)
            );
        }
    }
}
