namespace COLID.SearchService.DataModel.DTO
{
    public class SearchResultDTO : FacetDTO
    {
        public string OriginalSearchTerm { get; set; }

        public string SuggestedSearchTerm { get; set; }

        public HitDTO Hits { get; set; }

        public dynamic Suggest { get; set; }
    }
}
