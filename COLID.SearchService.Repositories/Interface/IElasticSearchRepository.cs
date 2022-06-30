using System;
using System.Collections.Generic;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.SearchService.DataModel.DTO;
using COLID.SearchService.DataModel.Search;
using COLID.SearchService.Repositories.DataModel;
using Nest;
using Newtonsoft.Json.Linq;

namespace COLID.SearchService.Repositories.Interface
{
    /// <summary>
    /// Interface for classes capable of handling all operations and communication with the search index.
    /// </summary>
    public interface IElasticSearchRepository
    {
        /// <summary>
        /// Provides the functionality to execute a search on a low level with a custom search logic.
        /// </summary>
        /// <param name="query">A search request in accordance to the Elasticsearch JSON DSL: https://www.elastic.co/guide/en/elasticsearch/reference/current/search-request-body.html</param>
        /// <returns>Response from elasticsearch.</returns>
        /// <remarks>
        /// With this functionality a custom search logic can be used instead of the DMP default. This provides
        /// the flexibility to make use of the whole search capabilities of Elasticsearch. This
        /// functionality is only for users who are familiar with the Elasticsearch JSON DSL.
        /// </remarks>
        JObject ExecuteRawQuery(JObject jsonQuery, SearchIndex searchIndex);

        /// <summary>
        /// Return a document with the given id
        /// </summary>
        /// <param name="identifier">The identifier for which a document is searched for</param>
        /// <param name="searchIndex">The index from which the document should be fetched</param>
        /// <returns>Return a document with the given id</returns>
        object GetDocument(string identifier, UpdateIndex searchIndex);

        /// <summary>
        /// Return a document with the given id
        /// </summary>
        /// <param name="identifier">The identifier for which a document is searched for</param>
        /// <param name="searchIndex">The index from which the document should be fetched</param>
        /// <returns>Return a document with the given id</returns>
        IList<JObject> GetSchemaUIResource(IEnumerable<string> identifier, UpdateIndex searchIndex);

        /// <summary>
        /// Return specified fields to a list of identifiers based on the given index to consider.
        /// </summary>
        /// <param name="identifiers">the identifiers to return</param>
        /// <param name="fieldsToReturn">the fields to return</param>
        /// <returns></returns>
        IDictionary<string, IEnumerable<JObject>> GetDocuments(IEnumerable<string> identifiers, IEnumerable<string> fieldsToReturn);

        /// <summary>
        /// Deletes a single document with given ID from the index.
        /// </summary>
        /// <param name="id">Unique identifier of document.</param>
        /// <param name="updateIndex">The index to which the document should be indexed</param>
        object DeleteDocument(string id, UpdateIndex updateIndex);

        /// <summary>
        /// Adds a new document with specific ID to the index.
        /// </summary>
        /// <param name="id">Unique identifier <c>_id</c> of the document.</param>
        /// <param name="documentToIndex">Contents of the document.</param>
        /// <param name="updateIndex">The index to which the document should be indexed</param>
        /// <returns></returns>
        /// <remarks>Can be used to overwrite an existing document with the usage of an already existing ID.</remarks>
        object IndexDocument(string id, JObject documentToIndex, UpdateIndex updateIndex);

        IList<string> Suggest(string searchText, SearchIndex searchIndex);

        IList<string> PhraseSuggest(string searchText, SearchIndex searchIndex);

        /// <summary>
        /// Provides all properties of the index mapping for the current index.
        /// </summary>
        /// <returns>A properties of the index mapping as list of key value pairs.</returns>
        IList<MappingProperty> GetMappingProperties(SearchIndex searchIndex);

        /// <summary>
        /// Returns aggregations for all mapping properties based on the current metadata. In the metadata is
        /// defined which properties will be aggregated.
        /// </summary>
        /// <param name="mappingProperties">Mapping properties of the current index.</param>
        /// <returns> Returns aggragation buckets and date ranges dependent of the property type. Values will be returned only
        /// for properties which are defined as aggregatable in the metadata.</returns>
        FacetDTO GetFacets(IList<MappingProperty> mappingProperties, SearchIndex searchIndex);

        /// <summary>
        /// Adds multiple documents to the index.
        /// </summary>
        /// <param name="documents">Contents for each document</param>
        /// <param name="updateIndex">The index to which the documents should be indexed</param>
        object IndexDocuments(JObject[] documents, UpdateIndex updateIndex);

        /// <summary>
        /// Executes a search on the current index with the given query with the DMP default search logic.
        /// </summary>
        /// <param name="searchRequest">A search request in accordance to the DTO <see cref="COLID.SearchService.DataModel.Search.SearchRequestDto"/> for handling of search requests. </param>
        /// <returns>Enrichend DMP Elasticsearch response.</returns>
        /// <remarks>
        /// The search request DTO simplifies the usage of the DMP search. The DTO will be transformed by following the DMP search logic
        /// into an Elasticsearch JSON DSL.
        /// </remarks>
        SearchResultDTO Search(SearchRequestDto searchRequest, bool delay = false);

        /// <summary>
        /// Adds new document with metadata to the metadata index and overrides the old metadata.
        /// </summary>
        /// <param name="metadata">Current metadata to be used for DMP.</param>
        /// <returns></returns>
        object IndexMetadata(JObject metadata);

        /// <summary>
        /// Provides actual metadata for all fields of a document in elasticsearch.
        /// </summary>
        /// <returns>Metadata collection for all fields in elasticsearch.</returns>
        MetadataCollection GetMetadataCollection();

        object GetResourceTypes();

        /// <summary>
        /// Creates a new index for DMP metadata and updates the <c>update alias</c>.
        /// </summary>
        /// <param name="rollbackActions">Rollback functions, which are necessary to undo the current process</param>
        /// <param name="oldIndexNames">The old indices as pointed to by the update alias. </param>
        /// <returns>The new index name</returns>
        string CreateMetadataIndex(IList<Action> rollbackActions, out IEnumerable<string> oldIndexNames);

        /// <summary>
        /// Creates a new search index for DMP and updates the <c>update alias</c>.
        /// </summary>
        /// <param name="rollbackActions">Rollback functions, which are necessary to undo the current process</param>
        /// <param name="oldIndexNames">The old indices as pointed to by the update alias. </param>
        /// <returns>The new index name</returns>
        string CreateDocumentIndex(Dictionary<string, JObject> metadataObject, UpdateIndex updateIndex, IList<Action> rollbackActions, out IEnumerable<string> oldIndexNames);

        /// <summary>
        /// Creates index mapping for current index based on given metadata and defined mapping rules.
        /// </summary>
        /// <param name="metadataObject"></param>
        /// <returns></returns>
        object CreateDocumentMapping(Dictionary<string, JObject> metadataObject, UpdateIndex updateIndex);

        /// <summary>
        /// Sets the current metadata alias to the newest metadata index existing, which has been used for reindexing before.
        /// </summary>
        /// <param name="rollbackActions">Rollback functions, which are necessary to undo the current process</param>
        void UpdateMetadataSearchAlias(IList<Action> rollbackActions);

        /// <summary>
        /// Sets the current search alias to the newest search index existing, which has been used for reindexing before.
        /// </summary>
        /// <param name="rollbackActions">Rollback functions, which are necessary to undo the current process</param>
        void UpdateDocumentSearchAlias(IList<Action> rollbackActions, UpdateIndex updateIndex, SearchIndex searchIndex);

        /// <summary>
        /// Deletes an index 
        /// </summary>
        /// <param name="index">Index to be deleted</param>
        /// <returns>True if the index was deleted, otherwise false</returns>
        bool DeleteIndex(string index);

        /// <summary>
        /// Checks if the given Index is Empty
        /// </summary>
        /// <returns>True if the index is empty, otherwise false</returns>
        /// <param name="index">Index to be searched</param>
        bool IsIndexEmpty(string index);

        /// <summary>
        /// Checks if the given userIds are part of the given index
        /// and returns the list of present user IDs 
        /// </summary>
        /// <param name="sourceIndex">Source Index where users are to be written to</param>
        /// <param name="uniqueUserIndexName">List of Unique Users</param>
        /// <param name="dateTime">Date from which users need to be written</param>
        /// <param name="isDeltaLoad">Delta Load value for first or periodic insert</param>
        void CountAndWriteUniqueUsers(string sourceIndex, string uniqueUserIndexName, DateTime dateTime, bool isDeltaLoad);
    }
}
