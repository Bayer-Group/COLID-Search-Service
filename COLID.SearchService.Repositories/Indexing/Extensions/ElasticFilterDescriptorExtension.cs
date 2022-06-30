using System.Collections.Generic;
using System.Linq;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.TripleStore.DataModels.Taxonomies;
using COLID.SearchService.Repositories.Constants;
using COLID.SearchService.Repositories.Mapping.Extensions;
using Nest;

namespace COLID.SearchService.Repositories.Indexing.Extensions
{
    public static class ElasticFilterDescriptorExtension
    {
        public static TokenFiltersDescriptor AddTaxonomyFilters(this TokenFiltersDescriptor fd, IEnumerable<MetadataProperty> metadataProperties)
        {
            foreach (var metadataProperty in metadataProperties)
            {
                if (metadataProperty == null || !metadataProperty.Properties.ContainsKey(Strings.Taxonomy))
                {
                    continue;
                }
                IDictionary<TaxonomyResultDTO, IList<TaxonomyResultDTO>> dictionary = TaxonomyTransformer.BuildFlatDictionary(metadataProperty.Properties[Strings.Taxonomy]);

                string autoPhraseKey = metadataProperty.Key.GetPreparedAnalyzerName(ElasticFilters.AutoPhrasePrefix);
                string vocabularyKey = metadataProperty.Key.GetPreparedAnalyzerName(ElasticFilters.VocabularyPrefix);

                fd.AddAutoPhraseFilter(dictionary, autoPhraseKey);
                fd.AddVocabularyFilter(dictionary, vocabularyKey);
            }
            return fd;
        }

        public static TokenFiltersDescriptor AddAutoPhraseFilter(this TokenFiltersDescriptor fd, IDictionary<TaxonomyResultDTO, IList<TaxonomyResultDTO>> dictionary, string autoPhraseKey)
        {
            if (string.IsNullOrEmpty(autoPhraseKey))
            {
                return fd;
            }

            return fd.Synonym(autoPhraseKey, s => s.Tokenizer(ElasticTokenizers.Keyword).Synonyms(BuildAutoPhraseList(dictionary)));
        }

        public static TokenFiltersDescriptor AddVocabularyFilter(this TokenFiltersDescriptor fd, IDictionary<TaxonomyResultDTO, IList<TaxonomyResultDTO>> dictionary, string vocabularyKey)
        {
            if (string.IsNullOrEmpty(vocabularyKey))
            {
                return fd;
            }

            return fd.Synonym(vocabularyKey, s => s.Tokenizer(ElasticTokenizers.Keyword).Synonyms(BuildVocabularyList(dictionary)));
        }

        public static TokenFiltersDescriptor AddEnglishStopwordsFilter(this TokenFiltersDescriptor fd)
        {
            return fd.Stop(ElasticFilters.EnglishStopwords, s => s.StopWords("_english_"));
        }

        public static TokenFiltersDescriptor AddNgramFilter(this TokenFiltersDescriptor fd)
        {
            return fd.NGram(ElasticFilters.Ngram, ng => ng.MinGram(2).MaxGram(15));
        }

        public static TokenFiltersDescriptor AddAutoCompleteNgramFilter(this TokenFiltersDescriptor fd)
        {
            return fd.EdgeNGram(ElasticFilters.AutocompleteNgram, eng => eng.MinGram(3).MaxGram(15));
        }

        public static TokenFiltersDescriptor AddAutoCompleteShingleFilter(this TokenFiltersDescriptor fd)
        {
            return fd.Shingle(ElasticFilters.AutocompleteShingle, sh => sh.MinShingleSize(2).MaxShingleSize(4));
        }

        public static TokenFiltersDescriptor AddWordDelimeter(this TokenFiltersDescriptor fd)
        {
            return fd.PatternReplace(ElasticFilters.FilterWordDelimiter, wd => wd.Pattern("_").Replacement(" "));
        }

        private static IList<string> BuildAutoPhraseList(IDictionary<TaxonomyResultDTO, IList<TaxonomyResultDTO>> dictionary)
        {
            return dictionary
                .Where(d => d.Key.Name.Contains(" "))
                .Select(d => $"{d.Key.Name} => {PrepareNameWithUnderScore(d.Key)}")
                .ToList();
        }

        /// <summary>
        /// A synonym list is created for each entry in the dictionary.
        /// </summary>
        /// <param name="dictionary">Dictionary of all nodes of a taxonomy. Key is a node and the value contains all parent elements of the node.</param>
        /// <returns>List of synonyms of all nodes</returns>
        private static IList<string> BuildVocabularyList(
            IDictionary<TaxonomyResultDTO, IList<TaxonomyResultDTO>> dictionary)
        {
            return dictionary.Select(d =>
                $"{PrepareNameWithUnderScore(d.Key)} => {BuildVocabularyString(d.Key, d.Value)}").ToList();
        }

        /// <summary>
        /// Replaces the names of the taxonomy with underscore and connects them to a string, joined by a comma
        /// </summary>
        /// <param name="taxonomy">Taxonomy for which a synonym list is to be created</param>
        /// <param name="parents">List of taxonmies</param>
        /// <returns>Names of the taxonomies as string</returns>
        private static string BuildVocabularyString(TaxonomyResultDTO taxonomy, IList<TaxonomyResultDTO> parents)
        {
            // Replace spaces in name with underscore and connect to list
            var synonyms = parents.Any() ? string.Join(", ", parents.Select(v => PrepareNameWithUnderScore(v))) : "";

            return string.IsNullOrWhiteSpace(synonyms) ? PrepareNameWithUnderScore(taxonomy) : $"{PrepareNameWithUnderScore(taxonomy)}, {synonyms}";
        }

        private static string PrepareNameWithUnderScore(TaxonomyResultDTO taxonomy)
        {
            return taxonomy.Name.Replace(" ", "_");
        }
    }
}
