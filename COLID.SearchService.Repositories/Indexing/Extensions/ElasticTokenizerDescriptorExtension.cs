using COLID.SearchService.Repositories.Constants;
using OpenSearch.Client;

namespace COLID.SearchService.Repositories.Indexing.Extensions
{
    public static class ElasticTokenizerDescriptorExtension
    {
        public static TokenizersDescriptor AddDmpNgramTokenizer(this TokenizersDescriptor td)
        {
            td.NGram(ElasticTokenizers.Ngram, ng => ng.TokenChars(TokenChar.Letter, TokenChar.Digit).MinGram(3).MaxGram(15));
            return td;
        }
    }
}
