namespace COLID.SearchService.Repositories.DataModel
{
    public class MappingProperty
    {
        public string Key { get; set; }
        public string Type { get; set; }

        public MappingProperty(string key, string type)
        {
            Key = key;
            Type = type;
        }
    }
}
