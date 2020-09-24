using System.Threading.Tasks;
using COLID.SearchService.DataModel.Status;

namespace COLID.SearchService.Services.Interface
{
    public interface IStatusService
    {
        BuildInformationDTO GetBuildInformation();
    }
}
