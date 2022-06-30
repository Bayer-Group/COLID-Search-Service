using System.Runtime.Serialization;

namespace COLID.SearchService.DataModel.Search
{
    public enum SearchIndex
    {
        [EnumMember(Value = "draft")]
        Draft,

        [EnumMember(Value = "published")]
        Published,

        [EnumMember(Value = "all")]
        All
    }
}
