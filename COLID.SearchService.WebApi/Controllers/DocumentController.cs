using System;
using System.Collections.Generic;
using COLID.Exception.Models.Business;
using COLID.Graph.TripleStore.DataModels.Index;
using COLID.Identity.Requirements;
using COLID.SearchService.DataModel.DTO;
using COLID.SearchService.DataModel.Index;
using COLID.SearchService.DataModel.Search;
using COLID.SearchService.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace COLID.SearchService.WebApi.Controllers
{
    /// <summary>
    /// Controller to publish new documents to the search index.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api")]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentService _documentService;

        public DocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        /// <summary>
        /// Indexes multiple documents to the search index.
        /// </summary>
        /// <param name="documents"></param>
        /// <returns>The elastic search response for put index document: https://www.elastic.co/guide/en/elasticsearch/reference/current/docs-index_.html.</returns>
        [HttpPost]
        [Route("documentList")]
        [Authorize(Policy = nameof(SuperadministratorRequirement))]
        public IActionResult IndexDocuments([FromBody] Documents documents)
        {
            return Ok(_documentService.IndexDocuments(documents.Content, documents.Index));
        }

        /// <summary>
        /// Indexes a single document to the search index.
        /// </summary>
        /// <param name="document"></param>
        /// <returns>The elastic search response for put index document: https://www.elastic.co/guide/en/elasticsearch/reference/current/docs-index_.html.</returns>
        [HttpPost]
        [Route("document")]
        [Authorize(Policy = nameof(SuperadministratorRequirement))]
        public IActionResult IndexDocument([FromBody] IndexDocumentDto document)
        {
            return Ok(_documentService.IndexDocument(document.DocumentId, document));
        }

        /// <summary>
        /// Return the document for a given identifier
        /// </summary>
        /// <param name="identifier">The identifier for which a document is searched for</param>
        /// <param name="updateIndex">Specifies the index from which the document should be fetched</param>
        /// <returns>A elastic search document</returns>
        [HttpGet]
        [Route("document")]
        public IActionResult GetDocument([FromQuery(Name = "id")] string identifier, [FromQuery(Name = "index")] UpdateIndex updateIndex = UpdateIndex.Published)
        {
            try
            {
                return Ok(_documentService.GetDocument(identifier, updateIndex));
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Return the list of documents for a given identifier list
        /// </summary>
        /// <param name="identifiers">a list of identifiers to search for</param>
        /// <param name="includeDraft">Include drafts in the results</param>
        /// <returns>Elastic search documents</returns>
        [HttpPost]
        [Route("documentsByIds")]
        public IActionResult GetDocumentsByIds([FromBody] IEnumerable<string> identifiers, [FromQuery] bool includeDraft = false)
        {
            try
            {
                return Ok(_documentService.GetDocumentsByIds(identifiers, includeDraft));
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Return the document for a given identifier
        /// </summary>
        /// <param name="identifiers">The identifier for which a document is searched for</param>
        /// <returns>A elastic search document for schemaUi</returns>
        [HttpPost]
        [Route("getSchemaUIResource")]
        public IActionResult GetSchemaUIResource([FromBody] DisplayTableAndColumn identifiers)
        {
            try
            {
                return Ok(_documentService.GetSchemaUIResource(identifiers, UpdateIndex.Published));
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }


        /// <summary>
        /// Return hashes for each EntryLifecycleStatus to a given list of identifiers
        /// </summary>
        /// <param name="identifiers">a list of identifiers to search for</param>
        /// <returns>a dictionary containing identifiers and their hashes for each EntryLifecycleStatus. Hash is empty, if none exists or pid uri is not in elastic</returns>
        [HttpPost]
        [Route("documents/hash")]
        public IActionResult GetDocumentsHash([FromBody] IEnumerable<string> identifiers)
        {
            try
            {
                return Ok(_documentService.GetDocumentsHash(identifiers));
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
