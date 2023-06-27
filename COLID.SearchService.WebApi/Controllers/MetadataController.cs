using COLID.Identity.Requirements;
using COLID.SearchService.DataModel.Index;
using COLID.SearchService.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace COLID.SearchService.WebApi.Controllers
{
    /// <summary>
    /// Controller for handling document CRUD operations.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class MetadataController : ControllerBase
    {
        private readonly IDocumentService _documentService;

        public MetadataController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        /// <summary>
        /// Creates new metadata document and stores it in dedicated index. This function is only
        /// needed in case of reindexing due to changes on metadata ontology.
        /// </summary>
        /// <param name="metadata">Metadata to be stored.</param>
        /// <returns>The elastic search response for put index document: https://www.elastic.co/guide/en/elasticsearch/reference/current/docs-index_.html.</returns>
        [HttpPost]
        [Route("")]
        [Authorize(Policy = nameof(SuperadministratorRequirement))]
        public IActionResult IndexMetadata(MetadataDocument metadata)
        {
            return Ok(_documentService.IndexMetadata(metadata.Content));
        }

        /// <summary>
        /// Provides current metadata for all fields of a document in the index.
        /// </summary>
        /// <returns>Actual metadata of all fields for Data Marketplace.</returns>
        [HttpGet]
        [Route("")]
        public IActionResult GetMetadata()
        {
            return Ok(_documentService.GetMetadata());
        }

        /// <summary>
        /// Provides all current resource types from the metadata.
        /// </summary>
        /// <returns>Actual resourse types for Data Marketplace.</returns>
        [HttpGet]
        [Route("types")]
        public IActionResult GetResourceTypes()
        {
            return Ok(_documentService.GetResourceTypes());
        }
    }
}
