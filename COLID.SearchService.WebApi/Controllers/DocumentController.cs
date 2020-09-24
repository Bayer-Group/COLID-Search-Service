using System;
using COLID.SearchService.DataModel.Index;
using COLID.SearchService.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using COLID.Exception.Models.Business;
using COLID.Identity.Requirements;
using COLID.SearchService.DataModel.Search;

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
        public IActionResult IndexDocument([FromBody] Document document)
        {
            return Ok(_documentService.IndexDocument(document.Id, document.Content, document.Index));
        }

        /// <summary>
        /// Return the document for a given identifier
        /// </summary>
        /// <param name="identifier">The identifier for which a document is searched for</param>
        /// <param name="searchIndex">Specifies the index from which the document should be fetched</param>
        /// <returns>A elastic search document</returns>
        [HttpGet]
        [Route("document")]
        public IActionResult GetDocument([FromQuery(Name="id")] string identifier, [FromQuery(Name = "index")] UpdateIndex updateIndex = UpdateIndex.Published)
        {
            try
            {
                return Ok(_documentService.GetDocument(identifier, updateIndex));
            }
            catch(ArgumentNullException ex)
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
