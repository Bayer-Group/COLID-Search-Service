using COLID.SearchService.DataModel.Search;

namespace COLID.SearchService.DataModel.Index
{
    public abstract class DocumentBase
    {
        public UpdateIndex Index { get; set; }

        protected DocumentBase()
        {
            Index = UpdateIndex.Published;
        }
    }
}
