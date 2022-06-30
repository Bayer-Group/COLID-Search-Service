using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using COLID.Common.Extensions;
using COLID.Graph.TripleStore.DataModels.Index;
using COLID.Graph.TripleStore.DataModels.Resources;
using COLID.MessageQueue.Configuration;
using COLID.MessageQueue.Services;
using COLID.SearchService.DataModel.DTO;
using COLID.SearchService.DataModel.Search;
using COLID.SearchService.Repositories.Interface;
using COLID.SearchService.Services.Interface;
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

        public DocumentService(IOptionsMonitor<ColidMessageQueueOptions> messageQueuingOptionsAccessor, IElasticSearchRepository elasticSearchRepository,
           ILogger<DocumentService> logger)
        {
            _mqOptions = messageQueuingOptionsAccessor.CurrentValue;
            _elasticSearchRepository = elasticSearchRepository;
            _serializerSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            _logger = logger;
        }

        public IDictionary<string, Action<string>> OnTopicReceivers => new Dictionary<string, Action<string>>() {
            {_mqOptions.Topics["IndexingResourceDocument"], IndexDocument},
        };

        // TODO: Add update index to delete
        /// <summary>
        /// <see cref="IDocumentService.DeleteDocument(string)"/>
        /// </summary>
        /// <param name="rawDocumentId">Document with field <c>resoruceID</c>.</param>
        public void DeleteDocument(Uri id, IndexDocumentDto document)
        {
            var encodedId = HttpUtility.UrlEncode(id.ToString());

            if (string.IsNullOrWhiteSpace(encodedId))
            {
                Console.WriteLine($"No ID in Resource Deletion Request found: {document}");
                return;
            }

            _elasticSearchRepository.DeleteDocument(encodedId, GetIndexToUpdate(document));
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
        public object GetSchemaUIResource(DisplayTableAndColumn identifiers, UpdateIndex searchIndex)
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

                schemaUI.columns = _elasticSearchRepository.GetSchemaUIResource(colresult, searchIndex)?.ToList<object>();

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

                var tableDocuments = _elasticSearchRepository.GetSchemaUIResource(identifierList, searchIndex);

                var columnIDs = identifiers.tables.Where(x => x.linkedTableFiled != null).SelectMany(x => x.linkedTableFiled).Select(y => y.pidURI).AsEnumerable();

                var columnDocuments = _elasticSearchRepository.GetSchemaUIResource(columnIDs, searchIndex);

                foreach (var table in identifiers.tables)
                {
                    try
                    {
                        Table tableObj = new Table();

                        tableObj.resourceDetail = tableDocuments.Where(x => GetPidUrl(x) == table.pidURI).FirstOrDefault();
                        tableObj.linkedColumnResourceDetail = table.linkedTableFiled
                            .Where(x => x != null && columnDocuments.Any(y => GetPidUrl(y) == x.pidURI))
                            .Select(col =>
                            {
                                return columnDocuments.Where(x => GetPidUrl(x) == col.pidURI).FirstOrDefault();

                            }).ToList<object>();
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
                        _logger.LogInformation(ex, ex.Message);
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
            return document["http://pid.bayer.com/kos/19014/hasPID"]?["outbound"]?[0]?["value"]?.ToString();
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
        public object IndexDocument(Uri id, IndexDocumentDto document)
        {
            Console.WriteLine("[Indexing] Indexing document with id: " + id);
            var encodedId = HttpUtility.UrlEncode(id.ToString());

            JObject jObjectDocument = JObject.FromObject(document.Document, JsonSerializer.Create(_serializerSettings));
            return _elasticSearchRepository.IndexDocument(encodedId, jObjectDocument, GetIndexToUpdate(document));
        }

        private UpdateIndex GetIndexToUpdate(IndexDocumentDto document)
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
        public void IndexDocument(string rawDocument)
        {
            var document = JsonConvert.DeserializeObject<IndexDocumentDto>(rawDocument);

            if (document.Action == ResourceCrudAction.Deletion)
            {
                DeleteDocument(document.DocumentId, document);
                return;
            }

            IndexDocument(document.DocumentId, document);
        }

        /// <summary>
        /// <see cref="IDocumentService.IndexDocuments(JObject[])"/>
        /// </summary>
        public object IndexDocuments(JObject[] documents, UpdateIndex updateIndex)
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
    }
}
