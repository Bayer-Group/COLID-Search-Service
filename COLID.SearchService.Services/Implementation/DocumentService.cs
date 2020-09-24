using System;
using System.Collections.Generic;
using System.Web;
using COLID.SearchService.Repositories.Interface;
using COLID.SearchService.Services.Interface;
using COLID.MessageQueue.Configuration;
using COLID.MessageQueue.Services;
using COLID.SearchService.DataModel.Search;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace COLID.SearchService.Services.Implementation
{
    /// <summary>
    /// Services for handling of CRUD operations of documents within an index.
    /// </summary>
    public class DocumentService : IDocumentService, IMessageQueueReceiver
    {
        private readonly ColidMessageQueueOptions _mqOptions;
        private readonly IElasticSearchRepository _elasticSearchRepository;

        public DocumentService(IOptionsMonitor<ColidMessageQueueOptions> messageQueuingOptionsAccessor, IElasticSearchRepository elasticSearchRepository)
        {
            _mqOptions = messageQueuingOptionsAccessor.CurrentValue;
            _elasticSearchRepository = elasticSearchRepository;
        }

        public IDictionary<string, Action<string>> OnTopicReceivers => new Dictionary<string, Action<string>>() {
            {_mqOptions.Topics["TopicResourcePublished"], IndexDocument},
            {_mqOptions.Topics["TopicResourceDeleted"], DeleteDocument}
        };

        // TODO: Add update index to delete
        /// <summary>
        /// <see cref="IDocumentService.DeleteDocument(string)"/>
        /// </summary>
        /// <param name="rawDocumentId">Document with field <c>resoruceID</c>.</param>
        public void DeleteDocument(string rawDocumentId)
        {
            var document = JObject.Parse(rawDocumentId);
            // Extract resourceID from raw document, which is also used an unique identifer of a document.
            var id = document["resourceId"]["outbound"][0]["uri"].ToString();

            id = HttpUtility.UrlEncode(id);
            Console.WriteLine(rawDocumentId);
            Console.WriteLine(document);
            Console.WriteLine(id);

            if (string.IsNullOrWhiteSpace(id))
            {
                Console.WriteLine($"No ID in Resource Deletion Request found: {document}");
                return;
            }
            //Delete document with the unique identifer.
            // TODO: Add mq dto with index variable
            _elasticSearchRepository.DeleteDocument(id, UpdateIndex.Published);
            //_elasticSearchRepository.DeleteDocument(id, UpdateIndex.Draft);
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
        public object IndexDocument(string id, JObject document, UpdateIndex updateIndex)
        {
            Console.WriteLine("[Indexing] Indexing document with id: " + id);
            id = HttpUtility.UrlEncode(id);

            return _elasticSearchRepository.IndexDocument(id, document, updateIndex);
        }

        /// <summary>
        /// <see cref="IDocumentService.IndexDocument(string)"/>
        /// </summary>
        /// <param name="rawDocument"></param>
        public void IndexDocument(string rawDocument)
        {
            var document = JObject.Parse(rawDocument);

            // TODO: Update pipeline and add index to pipeline document
            IndexDocument(document["resourceId"]["outbound"][0]["uri"].ToString(), document, UpdateIndex.Published);
            //IndexDocument(document["resourceId"]["outbound"][0]["uri"].ToString(), document, UpdateIndex.Draft);
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
