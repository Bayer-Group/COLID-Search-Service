using System;
using COLID.Logging.Services;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Serialization;
using System.Threading.Tasks;
using System.Net.Mime;
using System.Collections.Generic;
using COLID.SearchService.Exceptions.Models;
using Elasticsearch.Net;

namespace COLID.SearchService.Exceptions
{
    /// <summary>
    /// Central exception handler Middleware
    /// </summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IGeneralLogService _generalLogService;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly IHostingEnvironment _env;
        private readonly string _applicationId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next.</param>
        public ExceptionMiddleware(RequestDelegate next, IGeneralLogService generalLogService, IHostingEnvironment env, IConfiguration configuration)
        {
            _next = next;
            _generalLogService = generalLogService;
            _jsonSerializerSettings = new JsonSerializerSettings();
            _jsonSerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            _jsonSerializerSettings.Formatting = Formatting.Indented;
            _applicationId = configuration["AzureAd:ClientId"];
            _env = env;
        }

        /// <summary>
        /// Invokes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (GeneralException exception)
            {
                await HandleExceptionAsync(httpContext, exception);
            }
            catch (ArgumentException exception)
            {
                var businessException = new BusinessException(exception.Message, exception);

                await HandleExceptionAsync(httpContext, businessException);
            }
            catch (ElasticsearchClientException exception)
            {
                var technicalException = new TechnicalException("An error has occurred in the request against elasticsearch", exception);
                technicalException.Data.Add("additionalMessage", exception.Message);

                await HandleExceptionAsync(httpContext, technicalException);
            }
            catch (Exception exception)
            {
                var generalException = new GeneralException("An unhandled exception has occurred.", exception);
                await HandleExceptionAsync(httpContext, generalException);
            }
        }

        /// <summary>
        /// Handles all exceptions that are thrown by COLID and could not be treated.
        /// </summary>
        /// <param name="httpContext">the context of request.</param>
        /// <param name="generalException">New COLID exception that is passed on to the user.</param>
        /// <param name="exception">The untreated expcetion.</param>
        /// <returns></returns>
        private async Task HandleExceptionAsync(HttpContext httpContext, GeneralException generalException)
        {
            httpContext.Response.ContentType = MediaTypeNames.Application.Json;
            httpContext.Response.StatusCode = generalException.Code;
            generalException.RequestId = httpContext.TraceIdentifier;
            generalException.ApplicationId = _applicationId;

            if (generalException.InnerException != null)
            {
                generalException.Data.Add("type", generalException.InnerException.GetType().Name);
                generalException.Data.Add("message", generalException.InnerException.Message);
            }

            if (generalException.Code == 500)
            {
                var additionalInformation = new Dictionary<string, object>()
                {
                    { "Exception", generalException.InnerException }, // Necessary because all properties of the general exception base classes are ignored during normal serialization.
                    { "ExceptionType", generalException.Type }, // Important for filtering in kibana
                    { "TraceIdentifier", httpContext.TraceIdentifier }
                };

                if (_env.IsDevelopment() || _env.IsEnvironment("Local") || _env.IsEnvironment("OnPrem") || _env.IsEnvironment("Testing"))
                {
                    if (generalException?.InnerException?.Message != null)
                    {
                        additionalInformation.Add("ExceptionDetails", generalException.InnerException.Message);
                    }
                }

                _generalLogService.Error(generalException, generalException.Message, additionalInformation);
            }

            await httpContext.Response.WriteAsync(JsonConvert.SerializeObject(generalException, _jsonSerializerSettings));
        }
    }
}
