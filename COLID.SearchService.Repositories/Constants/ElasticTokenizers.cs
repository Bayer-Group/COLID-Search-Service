using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.SearchService.Repositories.Constants
{
    public static class ElasticTokenizers
    {
        public const string Standard = "standard";
        public const string Lowercase = "lowercase";
        public const string Ngram = "dmp_ngram_tokenizer";
        public const string Keyword = "keyword";
    }
}
