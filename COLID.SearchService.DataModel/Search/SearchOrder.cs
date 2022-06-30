using System.Runtime.Serialization;

namespace COLID.SearchService.DataModel.Search
{
    public enum SearchOrder
    {
        [EnumMember(Value = "asc")]
        Asc,

        [EnumMember(Value = "desc")]
        Desc
    }
}
