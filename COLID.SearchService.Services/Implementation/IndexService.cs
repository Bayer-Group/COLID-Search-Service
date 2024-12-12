using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using COLID.Exception.Models.Business;
using COLID.MessageQueue.Configuration;
using COLID.MessageQueue.Services;
using COLID.SearchService.DataModel.Index;
using COLID.SearchService.DataModel.Search;
using COLID.SearchService.Repositories.Interface;
using COLID.SearchService.Services.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace COLID.SearchService.Services.Implementation
{
    /// <summary>
    /// Services for handling of all index operations.
    /// </summary>
    public class IndexService : IIndexService//, IMessageQueueReceiver
    {
        private readonly IElasticSearchRepository _elasticSearchRepository;
        private readonly ColidMessageQueueOptions _mqOptions;
        private readonly ILogger<IndexService> _logger;
        private readonly bool _reindexingSwitch;
        private readonly IConfiguration _configuration;

        public IndexService(IElasticSearchRepository repository, IOptionsMonitor<ColidMessageQueueOptions> messageQueuingOptionsAccessor, ILogger<IndexService> logger, IConfiguration configuration)
        {
            _elasticSearchRepository = repository;
            _mqOptions = messageQueuingOptionsAccessor.CurrentValue;
            _logger = logger;
            _configuration = configuration;
            _reindexingSwitch = _configuration.GetValue<bool>("ReindexSwitch");
            //_logger.LogInformation("ReindexSwitch is allowed {ReindexingSwitch}", _reindexingSwitch);
        }

        //public IDictionary<string, Action<string>> OnTopicReceivers => new Dictionary<string, Action<string>>() {
        //    {_mqOptions.Topics["ReindexingSwitch"], ReindexingSwitch},
        //};

        public void ReindexingSwitch()
        {
            if (_reindexingSwitch)
            {            
                SwitchAndDeleteOldIndex();
            }
        }

        private void SwitchAndDeleteOldIndex()
        {
            IList<Action> rollbackActions = new List<Action>();
            try
            {
                _logger.LogInformation("Switching Indicies Now");
                _elasticSearchRepository.UpdateDocumentSearchAlias(rollbackActions, UpdateIndex.Draft, SearchIndex.Draft);
                _elasticSearchRepository.UpdateDocumentSearchAlias(rollbackActions, UpdateIndex.Published, SearchIndex.Published);
                /// At this point the actual indexing process is completed.
                /// The following processes are there to clean up the elastic.
                /// It is ignored whether the deletion was successful or not.
                //var oldIndices = oldDraftDocumentIndices.Concat(oldPublishedDocumentIndices).Concat(oldMetadataIndices);
                //foreach (var oldIndex in oldIndices)
                //{
                //    _elasticSearchRepository.DeleteIndex(oldIndex);
                //}
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "{Message}", ex.Message);

                try
                {
                    foreach (var rollbackAction in rollbackActions.Reverse())
                    {
                        rollbackAction.Invoke();
                    }
                }
                catch (System.Exception innerEx)
                {
                    _logger.LogError(innerEx, "{Message}", innerEx.Message);
                }

            }


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
                catch (System.Exception ex)
                {
                    // It makes sense from a technical point of view to try the indexing of metadata again.
                    _elasticSearchRepository.IndexMetadata(JObject.FromObject(metadataObject));
                }
                _elasticSearchRepository.UpdateMetadataSearchAlias(rollbackActions);
                //_elasticSearchRepository.UpdateDocumentSearchAlias(rollbackActions, UpdateIndex.Draft, SearchIndex.Draft);
                //_elasticSearchRepository.UpdateDocumentSearchAlias(rollbackActions, UpdateIndex.Published, SearchIndex.Published);

                /// At this point the actual indexing process is completed.
                /// The following processes are there to clean up the elastic.
                /// It is ignored whether the deletion was successful or not.
                if (!_reindexingSwitch)
                {
                    _logger.LogInformation($"We do not need to wait for switching index in Local environment");
                    _elasticSearchRepository.UpdateDocumentSearchAlias(rollbackActions, UpdateIndex.Draft, SearchIndex.Draft);
                    _elasticSearchRepository.UpdateDocumentSearchAlias(rollbackActions, UpdateIndex.Published, SearchIndex.Published);
                    var oldIndices = oldDraftDocumentIndices.Concat(oldPublishedDocumentIndices).Concat(oldMetadataIndices);
                    foreach (var oldIndex in oldIndices)
                    {
                        _elasticSearchRepository.DeleteIndex(oldIndex);
                    }
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "{Message}", ex.Message);

                try
                {
                    foreach (var rollbackAction in rollbackActions.Reverse())
                    {
                        rollbackAction.Invoke();
                    }
                }
                catch (System.Exception innerEx)
                {
                    _logger.LogError(innerEx, "{Message}", innerEx.Message);
                }

                throw;
            }
        }

        public IndexStaus GetIndexingStatus()
        {
            return _elasticSearchRepository.GetIndexingStatus();
        }
    }
}
