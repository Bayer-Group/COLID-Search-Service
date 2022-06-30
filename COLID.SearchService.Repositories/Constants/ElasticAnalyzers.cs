namespace COLID.SearchService.Repositories.Constants
{
    public static class ElasticAnalyzers
    {
        public const string TaxonomyTextPrefix = "taxonomy_text_{0}";
        public const string TaxonomyTextSearchPrefix = "taxonomy_text_search_{0}";
        public const string Ngram = "dmp_ngram_analyzer";
        public const string StandardEnglish = "dmp_standard_english";
        public const string AutoCompleteNgram = "autocomplete_ngram";
        public const string SearchAutoComplete = "search_autocomplete";
        public const string DidYouMeanTrigram = "dym_trigram";
        public const string AutoCompleteShingle = "autocomplete_shingle";
    }
}
