using System;
using System.Collections.Generic;
using COLID.SearchService.DataModel.DTO;
using COLID.SearchService.DataModel.Statistics;
using static COLID.SearchService.Services.Implementation.UserService;

namespace COLID.SearchService.Services.Interface
{
    /// <summary>
    /// Interface for getting uniques user information and writing them in Index.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Searches unique users in the PID and DMP index and writes to Elastic Search.
        /// </summary>
        /// <param name="appName">PID or DMP App Name</param>
        void WritePIDDMPUniqueUsers(string appName);
        void WriteDmpAllSavedSearchFiltersCountToLogs(Dictionary<string, int> allSavedSearchFilters);
        void WriteFavoritesListCountToLogs(Dictionary<string, int> allFavoritesList);
        void WriteAllSubscriptionsCountToLogs(Dictionary<string, int> allSubscriptions);
        HierarchicalData UserDepartmentsFlowView();

    }
}
