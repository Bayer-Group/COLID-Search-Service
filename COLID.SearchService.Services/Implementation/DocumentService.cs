using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using COLID.AWS.Interface;
using COLID.Common.Extensions;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.TripleStore.DataModels.Index;
using COLID.Graph.TripleStore.DataModels.Resources;
using COLID.MessageQueue.Configuration;
using COLID.MessageQueue.Services;
using COLID.SearchService.DataModel.DTO;
using COLID.SearchService.DataModel.Search;
using COLID.SearchService.Repositories.Interface;
using COLID.SearchService.Services.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace COLID.SearchService.Services.Implementation
{
    /// <summary>
    /// Services for handling of CRUD operations of documents within an index.
    /// </summary>
    public class DocumentService : IDocumentService, IMessageQueueReceiver
    {
        private readonly ColidMessageQueueOptions _mqOptions;
        private readonly IElasticSearchRepository _elasticSearchRepository;
        private readonly JsonSerializerSettings _serializerSettings;
        private readonly ILogger<DocumentService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IIndexService _indexService;

        // getting the service url ex: pid.bayer..
        private static readonly string _basePath = Path.GetFullPath("appsettings.json");
        private static readonly string _filePath = _basePath[..^16];
        private static readonly IConfigurationRoot _configurationRoot = new ConfigurationBuilder()
                     .SetBasePath(_filePath)
                    .AddJsonFile("appsettings.json")
                    .Build();
        public static readonly string _httpServiceUrl = _configurationRoot.GetValue<string>("HttpServiceUrl");

        private readonly IAmazonSQSExtendedService _amazonSQSExtService;
        private readonly string _indexingOpensearchDocInputQueueUrl;
        private readonly string _indexingOpensearchDocInputS3;
        private bool _reIndexRunning = false;
        private readonly object _reIndexlock = new object();

        public DocumentService(IOptionsMonitor<ColidMessageQueueOptions> messageQueuingOptionsAccessor, IElasticSearchRepository elasticSearchRepository,
           ILogger<DocumentService> logger, IConfiguration configuration, IIndexService indexService, IAmazonSQSExtendedService amazonSQSExtService)
        {
            _mqOptions = messageQueuingOptionsAccessor.CurrentValue;
            _elasticSearchRepository = elasticSearchRepository;
            _serializerSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            _logger = logger;
            _configuration = configuration;
            _indexService = indexService;
            _amazonSQSExtService = amazonSQSExtService;
            _indexingOpensearchDocInputQueueUrl = _configuration.GetConnectionString("IndexingOpensearchDocInputQueueUrl");
            _indexingOpensearchDocInputS3 = _configuration.GetConnectionString("IndexingOpensearchDocInputS3");
            _serializerSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };

        }

        public IDictionary<string, Action<string>> OnTopicReceivers => new Dictionary<string, Action<string>>() {
            {_mqOptions.Topics["IndexingResourceDocument"], async (message) => await IndexDocument(message)},
        };

        // TODO: Add update index to delete
        /// <summary>
        /// <see cref="IDocumentService.DeleteDocument(string)"/>
        /// </summary>
        /// <param name="rawDocumentId">Document with field <c>resoruceID</c>.</param>
        public async Task DeleteDocument(Uri id, IndexDocumentDto document)
        {
            var encodedId = HttpUtility.UrlEncode(id.ToString());

            if (string.IsNullOrWhiteSpace(encodedId))
            {
                Console.WriteLine($"No ID in Resource Deletion Request found: {document}");  
                return;
            }

            await _elasticSearchRepository.DeleteDocument(encodedId, GetIndexToUpdate(document));
        }

        /// <summary>
        /// <see cref="IDocumentService.GetDocument(string)(string)"/>
        /// </summary>
        /// <param name="identifier">Document with field <c>resoruceID</c>.</param>
        /// <param name="searchIndex">The index from which the document should be fetched.</param>
        public object GetDocument(string identifier, UpdateIndex searchIndex)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                throw new ArgumentNullException(nameof(identifier), "The identifier must not be null or empty.");
            }

            var documentIdentifier = HttpUtility.UrlEncode(identifier);

            //Delete document with the unique identifer.
            return _elasticSearchRepository.GetDocument(documentIdentifier, searchIndex);
        }


        /// <summary>
        /// <see cref="IDocumentService.GetDocument(string)(string)"/>
        /// </summary>
        /// <param name="identifier">Document with field <c>resoruceID</c>.</param>
        /// <param name="searchIndex">The index from which the document should be fetched.</param>
        public object GetSchemaUIResource(DisplayTableAndColumn identifiers, UpdateIndex updateIndex)
        {
            if (identifiers == null)
            {
                throw new ArgumentNullException(nameof(identifiers), "The identifier must not be null or empty.");
            }


            SchemaUI schemaUI = new SchemaUI();
            if (identifiers.columns != null && identifiers.columns.Count > 0)
            {
                var colresult = identifiers.columns.Select
                     (x => x.pidURI).AsEnumerable();

                schemaUI.columns = _elasticSearchRepository.GetSchemaUIResource(colresult, updateIndex)?.ToList<object>();

                for (int i = 0; i < schemaUI.columns.Count; i++) 
                {
                    var columnObject = identifiers.columns.Where(x => x.pidURI == GetPidUrl((JObject)schemaUI.columns[i])).FirstOrDefault();
                    if (columnObject.subColumns.Count > 0)
                    {
                        JObject jsonColumnDocument = JObject.Parse(schemaUI.columns[i].ToString());
                        jsonColumnDocument.Add(new JProperty("hasSubColumns", GetSubcolumns(columnObject)));
                        schemaUI.columns[i] = jsonColumnDocument;
                    }
                }
            }

            if (identifiers.tables != null && identifiers.tables.Count > 0)
            {

                var identifierList = identifiers.tables.Select(x => x.pidURI).AsEnumerable();

                var tableDocuments = _elasticSearchRepository.GetSchemaUIResource(identifierList, updateIndex);

                var columnIDs = identifiers.tables.Where(x => x.linkedTableFiled != null).SelectMany(x => x.linkedTableFiled).Select(y => y.pidURI).AsEnumerable();

                var columnDocuments = _elasticSearchRepository.GetSchemaUIResource(columnIDs, updateIndex);

                foreach (var table in identifiers.tables)
                {
                    try
                    {
                        Table tableObj = new Table
                        {
                            resourceDetail = tableDocuments.Where(x => GetPidUrl(x) == table.pidURI).FirstOrDefault(),
                            linkedColumnResourceDetail = table.linkedTableFiled
                            .Where(x => x != null && columnDocuments.Any(y => GetPidUrl(y) == x.pidURI))
                            .Select(col =>
                            {
                                return columnDocuments.Where(x => GetPidUrl(x) == col.pidURI).FirstOrDefault();

                            }).ToList<object>()
                        };
                        for (int i = 0; i < tableObj.linkedColumnResourceDetail.Count; i++)
                        {
                            var columnObject = table.linkedTableFiled.Where(x=>x.pidURI == GetPidUrl((JObject)tableObj.linkedColumnResourceDetail[i])).FirstOrDefault();
                            if (columnObject.subColumns.Count > 0)
                            {
                                JObject jsonColumnDocument = JObject.Parse(tableObj.linkedColumnResourceDetail[i].ToString());
                                jsonColumnDocument.Add(new JProperty("hasSubColumns", GetSubcolumns(columnObject)));
                                tableObj.linkedColumnResourceDetail[i] = jsonColumnDocument;
                            }
                        } 
                        if (tableObj.resourceDetail != null)
                        {
                            schemaUI.tables.Add(tableObj);
                        }


                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogInformation(ex, "{Message}", ex.Message);
                        continue;
                    }
                }


            }

            return schemaUI;
        }

        private List<object> GetSubcolumns(Filed column) 
        {
            var subColumnIDs = column.subColumns.Where(x => x.subColumns != null).Select(y => y.pidURI).AsEnumerable();
            var subColumnDocuments = _elasticSearchRepository.GetSchemaUIResource(subColumnIDs, UpdateIndex.Published).ToList<object>();
            List<object> filledColumns = new List<object>();
            foreach (var item in subColumnDocuments)
            {
                var subColumnObject = column.subColumns.Where(x=>x.pidURI == GetPidUrl((JObject)item)).FirstOrDefault();
                if (subColumnObject.subColumns.Count > 0)
                {
                    JObject jsonColumnDocument = JObject.Parse(item.ToString());
                    jsonColumnDocument.Add(new JProperty("hasSubColumns", GetSubcolumns(subColumnObject)));
                    filledColumns.Add(jsonColumnDocument);
                }
                else
                {
                    filledColumns.Add(item);
                }
            }
            return filledColumns;
        }
        /// <summary>
        /// Get the hashes for a list of identifiers. If no hash or identifier was found for the given ones, an empty hash value will be returned.
        /// </summary>
        /// <param name="identifiers">the ids to search for</param>
        public IDictionary<string, Dictionary<string, string>> GetDocumentsHash(IEnumerable<string> identifiers)
        {
            if (identifiers.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(identifiers), "The identifiers must not be null or empty.");
            }

            var documentIdentifiers = identifiers
                .Select(HttpUtility.UrlEncode)
                .AsEnumerable();

            var fieldsToReturn = new HashSet<string> { "resourceHash.outbound.value", COLID.Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus };

            var documents = _elasticSearchRepository.GetDocuments(documentIdentifiers, fieldsToReturn);

            var hashDict = documents
                .ToDictionary(
                    gh => HttpUtility.UrlDecode(gh.Key),
                    gh => gh
                        .Value
                        .Where(g => g != null)
                        .ToDictionary(
                        g => GetOutboundUri(COLID.Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus, g),
                        g => GetOutboundValue("resourceHash", g)));

            return hashDict;
        }

        private static string GetOutboundValue(string key, JObject document)
        {
            return document[key]?["outbound"]?[0]?["value"]?.ToString();
        }

        private static string GetPidUrl(JObject document)
        {
            return document[_httpServiceUrl + "kos/19014/hasPID"]?["outbound"]?[0]?["value"]?.ToString();
        }

        private static string GetOutboundUri(string key, JObject document)
        {
            return document[key]?["outbound"]?[0]?["uri"]?.ToString();
        }

        /// <summary>
        /// <see cref="IDocumentService.GetMetadata"/>
        /// </summary>
        public object GetMetadata()
        {
            return _elasticSearchRepository.GetMetadataCollection();
        }

        public object GetResourceTypes()
        {
            return _elasticSearchRepository.GetResourceTypes();
        }

        /// <summary>
        /// <see cref="IDocumentService.IndexDocument(string, JObject)"/>
        /// </summary>
        public async Task IndexDocument(Uri id, IndexDocumentDto document)
        {
            Console.WriteLine("[Indexing] Indexing document with id: " + id);
            var encodedId = HttpUtility.UrlEncode(id.ToString());

            JObject jObjectDocument = JObject.FromObject(document.Document, JsonSerializer.Create(_serializerSettings));
            await _elasticSearchRepository.IndexDocument(encodedId, jObjectDocument, GetIndexToUpdate(document));
        }

        private static UpdateIndex GetIndexToUpdate(IndexDocumentDto document)
        {
            return document.DocumentLifecycleStatus ==
                                COLID.Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Draft
                ? UpdateIndex.Draft
                : UpdateIndex.Published;
        }

        /// <summary>
        /// <see cref="IDocumentService.IndexDocument(string)"/>
        /// </summary>
        /// <param name="rawDocument"></param>
        public async Task IndexDocument(string rawDocument)
        {
            try
            {
                var rawDocString = System.Text.Json.JsonSerializer.Deserialize<string>(rawDocument);
                var document = JsonConvert.DeserializeObject<IndexDocumentDto>(rawDocString, _serializerSettings);

                if (document.Action == ResourceCrudAction.Deletion)
                {
                    await DeleteDocument(document.DocumentId, document);
                    return;
                }

                await IndexDocument(document.DocumentId, document);
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"[Indexing] Something went wrong: " + (ex.InnerException == null ? ex.Message : ex.InnerException.Message));
            }
            
        }

        /// <summary>
        /// <see cref="IDocumentService.IndexDocuments(IList<JObject>)"/>
        /// </summary>
        public object IndexDocuments(IList<JObject> documents, UpdateIndex updateIndex)
        {
            return _elasticSearchRepository.IndexDocuments(documents, updateIndex);
        }

        /// <summary>
        /// <see cref="IDocumentService.IndexMetadata(JObject)"/>
        /// </summary>
        public object IndexMetadata(JObject metadata)
        {
            return _elasticSearchRepository.IndexMetadata(metadata);
        }

        /// <summary>
        /// Document with field resoruceIDs
        /// </summary>
        /// <param name="identifiers">the ids to search for</param>
        public IDictionary<string, IEnumerable<JObject>> GetDocumentsByIds(IEnumerable<string> identifiers, bool includeDraft = false)
        {
            if (identifiers.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(identifiers), "The identifiers must not be null or empty.");
            }

            var documentIdentifiers = identifiers.Distinct()
                .Select(HttpUtility.UrlEncode)
                .AsEnumerable();

            var fieldsToReturn = new HashSet<string> ();

            return _elasticSearchRepository.GetDocuments(documentIdentifiers, fieldsToReturn, includeDraft);
        }

        /// <summary>
        /// Fetch OpenSearch Document from SQS and start Indexing
        /// </summary>        
        public async void ReindexDocumentsFromQueue()
        {
            //Lock method so that multiple call from Background service does not invoke this method when its busy processing messages
            lock (_reIndexlock)
            {
                if (_reIndexRunning)
                {
                    return;
                }
                else
                {
                    _reIndexRunning = true;
                }
            }

            try
            {
                //Check for msgs in a loop           
                int msgcount = 0;
                bool msgProcessed = false;
                do
                {
                    //Check msgs available in SQS                      
                    var msgs = await _amazonSQSExtService.ReceiveMessageAsync(_indexingOpensearchDocInputQueueUrl, _indexingOpensearchDocInputS3, 2, 10);
                    msgcount = msgs.Count;

                    //Iterate on each msg which containing a pidUri
                    foreach (var msg in msgs)
                    {
                        msgProcessed = true;
                        try
                        {
                            //Delete the msg from SQS Queue before it times out
                            await _amazonSQSExtService.DeleteMessageAsync(_indexingOpensearchDocInputQueueUrl, _indexingOpensearchDocInputS3, msg.ReceiptHandle);
                            //Then process the msg
                            await IndexDocument(msg.Body);
                        }
                        catch (System.Exception ex)
                        {
                            _logger.LogError("[Reindexing] Something went wrong : " + (ex.InnerException == null ? ex.Message : ex.InnerException.Message));
                        }
                    }

                } while (msgcount > 0);                
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"[Reindexing] Something went wrong: " + (ex.InnerException == null ? ex.Message : ex.InnerException.Message));
            }

            _reIndexRunning = false;
        }        
    }
}
