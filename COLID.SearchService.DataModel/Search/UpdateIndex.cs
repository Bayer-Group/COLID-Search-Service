using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace COLID.SearchService.DataModel.Search
{
    public enum UpdateIndex
    {
        [EnumMember(Value = "draft")]
        Draft,
        [EnumMember(Value = "published")]
        Published,
    }
}
