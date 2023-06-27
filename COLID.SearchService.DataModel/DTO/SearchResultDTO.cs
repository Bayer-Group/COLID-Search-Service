namespace COLID.SearchService.DataModel.DTO
{
    public class SearchResultDTO : FacetDTO
    {
        public long Took { get; set; }

        public string OriginalSearchTerm { get; set; }

        public string SuggestedSearchTerm { get; set; }

        public HitDTO Hits { get; set; }

        public dynamic Suggest { get; set; }
    }
}
