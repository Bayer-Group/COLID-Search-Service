﻿using COLID.SearchService.Repositories.Mapping.Base;
using COLID.SearchService.Repositories.Mapping.Constants;
using COLID.SearchService.Repositories.Mapping.Extensions;
using OpenSearch.Client;

namespace COLID.SearchService.Repositories.Mapping.Rules.Range
{
    internal sealed class Boolean : RangeRule
    {
        protected override string Value => Uris.XmlBoolean;

        protected override ObjectTypeDescriptor<dynamic, dynamic> Mapping<T>(ObjectTypeDescriptor<dynamic, dynamic> o)
        {
            return o
                .Properties(osp => osp
                    .ValueKeyword()
                );
        }
    }
}
