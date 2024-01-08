using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using COLID.SearchService.DataModel.DTO;

namespace COLID.SearchService.Services.Interface
{
    /// <summary>
    /// Interface for clustering search result using remote carrot2 service api
    /// </summary>
    public interface IRemoteCarrot2Service
    {
        Task<Carrot2ResponseDTO> Cluster(Carrot2RequestDTO clusterRequest);
    }
}
