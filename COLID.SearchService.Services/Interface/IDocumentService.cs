using System;
using System.Collections.Generic;
using COLID.Graph.TripleStore.DataModels.Index;
using COLID.SearchService.DataModel.DTO;
using COLID.SearchService.DataModel.Search;
using Newtonsoft.Json.Linq;

namespace COLID.SearchService.Services.Interface
{
    /// <summary>
    /// Interface for classes capable of handling document CRUD operations on indices.
    /// </summary>
    public interface IDocumentService
    {
        /// <summary>
        /// Adds new document with specific ID to the index.
        /// </summary>
        /// <param name="id">Unique identifer <c>_id</c> of the document.</param>
        /// <param name="document">Document to index.</param>
        /// <returns></returns>
        /// <remarks>Can be used to overwrite an existing document with the usage of an already existing ID.</remarks>
        object IndexDocument(Uri id, IndexDocumentDto document);

        /// <summary>
        /// Adds new document to the index.
        /// </summary>
        /// <param name="document">Contents of the document.</param>
        void IndexDocument(string document);

        /// <summary>
        /// Return a document with the given id
        /// </summary>
        /// <param name="identifier">The identifier for which a document is searched for</param>
        /// <param name="searchIndex">Specifies the index from which the document should be fetched</param>
        /// <returns>Return a document with the given id</returns>
        object GetDocument(string identifier, UpdateIndex searchIndex);


        /// <summary>
        /// Return a document with the given id
        /// </summary>
        /// <param name="identifier">The identifier for which a document is searched for</param>
        /// <param name="searchIndex">Specifies the index from which the document should be fetched</param>
        /// <returns>Return a document with the given id</returns>
        object GetSchemaUIResource(DisplayTableAndColumn identifier, UpdateIndex searchIndex);

        /// <summary>
        /// Get the hashes for a list of identifiers. If no hash or identifier was found for the given ones, an empty hash value will be returned.
        /// </summary>
        /// <param name="identifiers">the ids to search for</param>
        IDictionary<string, Dictionary<string, string>> GetDocumentsHash(IEnumerable<string> identifiers);

        /// <summary>
        /// Adds multiple documents to the index.
        /// </summary>
        /// <param name="documents">Contents for each document</param>
        /// <param name="updateIndex">The index to which the document should be indexed.</param>
        /// <returns></returns>
        object IndexDocuments(JObject[] documents, UpdateIndex updateIndex);

        /// <summary>
        /// Deletes a single document with given ID from the index.
        /// </summary>
        /// <param name="documentId">Raw document which should be deleted. Resource ID will be extracted.</param>
        void DeleteDocument(Uri id, IndexDocumentDto document);

        /// <summary>
        /// Adds new document with metadata to the metadata index and overrides the old metadata.
        /// </summary>
        /// <param name="metadata">Current metadata to be used for DMP.</param>
        /// <returns></returns>
        object IndexMetadata(JObject metadata);

        /// <summary>
        /// Returns a document with the current metadata in the metadata index.
        /// </summary>
        /// <returns></returns>
        object GetMetadata();

        object GetResourceTypes();

        /// <summary>
        /// Document with field resoruceIDs
        /// </summary>
        /// <param name="identifiers">the ids to search for</param>
        public IDictionary<string, IEnumerable<JObject>> GetDocumentsByIds(IEnumerable<string> identifiers, bool includeDraft = false);
    }
}
