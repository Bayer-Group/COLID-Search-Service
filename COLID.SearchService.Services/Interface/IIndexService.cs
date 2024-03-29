﻿using System.Collections.Generic;
using COLID.SearchService.DataModel.Index;
using Newtonsoft.Json.Linq;

namespace COLID.SearchService.Services.Interface
{
    /// <summary>
    /// Interface for classes capable of handling operations for indices.
    /// </summary>
    public interface IIndexService
    {
        /// <summary>
        /// Creates a new index with automatic mapping creation based on the given metadata. After successfull
        /// creation the new index is set as default index in Data Marketplace.
        /// </summary>
        /// <param name="metadataObject">Metadata of all fields which should be added to the index mapping.</param>
        void CreateAndApplyNewIndex(Dictionary<string, JObject> metadataObject);

        /// <summary>
        /// Check re-indexing progress ststus.
        /// </summary>
        /// <returns></returns>
        IndexStaus GetIndexingStatus();
    }
}
