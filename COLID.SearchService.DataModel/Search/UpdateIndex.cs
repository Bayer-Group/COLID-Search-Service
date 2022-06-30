using System.Runtime.Serialization;

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
