﻿using System;
using System.Collections.Generic;
using System.Text;
using COLID.SearchService.Repositories.Constants;
using Nest;

namespace COLID.SearchService.Repositories.Indexing.Extensions
{
    public static class ElasticTokenizerDescriptorExtension
    {
        public static TokenizersDescriptor AddDmpNgramTokenizer(this TokenizersDescriptor td)
        {
            td.NGram(ElasticTokenizers.Ngram, ng => ng.TokenChars(TokenChar.Letter).MinGram(3).MaxGram(15));
            return td;
        }
    }
}
