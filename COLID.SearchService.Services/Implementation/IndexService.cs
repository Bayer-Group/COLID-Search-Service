using System;
using COLID.SearchService.Services.Interface;
using COLID.MessageQueue.Constants;
using Newtonsoft.Json;
using COLID.SearchService.Repositories.Interface;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Linq.Expressions;
using System.Linq;
using Elasticsearch.Net;
using Amazon.Runtime.Internal.Util;
using COLID.SearchService.DataModel.Search;
using Microsoft.Extensions.Logging;

namespace COLID.SearchService.Services.Implementation
{
    /// <summary>
    /// Services for handling of all index operations.
    /// </summary>
    public class IndexService : IIndexService
    {
        private readonly IElasticSearchRepository _elasticSearchRepository;
        private readonly ILogger<IndexService> _logger;

        public IndexService(IElasticSearchRepository repository, ILogger<IndexService> logger)
        {
            _elasticSearchRepository = repository;
            _logger = logger;
        }

        /// <summary>
        /// <see cref="IIndexService.CreateAndApplyNewIndex(string)"/>
        /// </summary>
        public void CreateAndApplyNewIndex(Dictionary<string, JObject> metadataObject)
        {
            IList<Action> rollbackActions = new List<Action>();

            try
            {
                _elasticSearchRepository.CreateDocumentIndex(metadataObject, UpdateIndex.Draft, rollbackActions, out IEnumerable<string> oldDraftDocumentIndices);
                _elasticSearchRepository.CreateDocumentIndex(metadataObject, UpdateIndex.Published, rollbackActions, out IEnumerable<string> oldPublishedDocumentIndices);

                /// No rollback function needed, because the mapping is on the index, 
                /// and therefore the index must be deleted
                _elasticSearchRepository.CreateDocumentMapping(metadataObject, UpdateIndex.Draft);
                _elasticSearchRepository.CreateDocumentMapping(metadataObject, UpdateIndex.Published);

                _elasticSearchRepository.CreateMetadataIndex(rollbackActions, out IEnumerable<string> oldMetadataIndices);

                try
                {
                    /// No rollback function needed, because the document is in the index, 
                    /// and therefore the index will be deleted on rollback
                    _elasticSearchRepository.IndexMetadata(JObject.FromObject(metadataObject));
                }
                catch (ElasticsearchClientException)
                {
                    // It makes sense from a technical point of view to try the indexing of metadata again.
                    _elasticSearchRepository.IndexMetadata(JObject.FromObject(metadataObject));
                }

                _elasticSearchRepository.UpdateMetadataSearchAlias(rollbackActions);
                _elasticSearchRepository.UpdateDocumentSearchAlias(rollbackActions, UpdateIndex.Draft, SearchIndex.Draft);
                _elasticSearchRepository.UpdateDocumentSearchAlias(rollbackActions, UpdateIndex.Published, SearchIndex.Published);



                /// At this point the actual indexing process is completed. 
                /// The following processes are there to clean up the elastic. 
                /// It is ignored whether the deletion was successful or not.
                var oldIndices = oldDraftDocumentIndices.Concat(oldPublishedDocumentIndices).Concat(oldMetadataIndices);
                foreach (var oldIndex in oldIndices)
                {
                    _elasticSearchRepository.DeleteIndex(oldIndex);
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                try
                {
                    foreach (var rollbackAction in rollbackActions.Reverse())
                    {
                        rollbackAction.Invoke();
                    }
                }
                catch (System.Exception innerEx)
                {
                    _logger.LogError(innerEx, innerEx.Message);

                }
                
                throw;
            }
        }
    }
}
