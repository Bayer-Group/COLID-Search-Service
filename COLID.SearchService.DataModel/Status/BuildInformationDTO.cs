namespace COLID.SearchService.DataModel.Status
{
    public class BuildInformationDTO
    {
        public string VersionNumber { get; set; }
        public string JobId { get; set; }
        public string PipelineId { get; set; }
        public string CiCommitSha { get; set; }
    }
}
