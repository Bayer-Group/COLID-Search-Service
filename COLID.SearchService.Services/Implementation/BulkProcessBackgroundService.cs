using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace COLID.SearchService.Services.Implementation
{
    public class BulkProcessBackgroundService: BackgroundService
    {
        private readonly ILogger<BulkProcessBackgroundService> _logger;
        private readonly DocumentService _documentService;
        /// <summary>
        /// Constructer to initialize
        /// </summary>        
        public BulkProcessBackgroundService(ILogger<BulkProcessBackgroundService> logger, DocumentService documentService)
        {
            _logger = logger;
            _documentService = documentService;
        }

        /// <summary>
        /// Background service invoked automatically at startup, for bulk validation and linking resources
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BackgroundService: Started ");
            //await Task.Yield();
            while (!stoppingToken.IsCancellationRequested)
            {
                //_logger.LogInformation("BackgroundService: Running");
                _documentService.ReindexDocumentsFromQueue();
                await Task.Delay(10000, stoppingToken);
            }
            _logger.LogInformation("BackgroundService: Stopped for some reason");
        }
    }
}
