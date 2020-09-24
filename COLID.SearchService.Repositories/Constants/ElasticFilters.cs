using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.SearchService.Repositories.Constants
{
    public static class ElasticFilters
    {
        public const string AutoPhrasePrefix = "autophrase_syn_{0}";
        public const string VocabularyPrefix = "vocab_taxonomy_{0}";
        public const string Lowercase = "lowercase";
        public const string Standard = "standard";
        public const string AsciiFolding = "asciifolding";
        public const string EnglishStopwords = "english_stopwords";
        public const string AutocompleteNgram = "filter_autocomplete_ngram";
        public const string AutocompleteShingle = "filter_autocomplete_shingle";
        public const string Ngram = "filter_ngram";
        public const string FilterWordDelimiter = "filter_word_delimiter";
    }
}
