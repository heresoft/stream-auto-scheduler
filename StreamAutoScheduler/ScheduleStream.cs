using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;
using System.Data;
using System.Web.Http;

namespace StreamAutoScheduler
{
    public class ScheduleStream
    {
        private readonly ILogger<ScheduleStream> _logger;

        public ScheduleStream(ILogger<ScheduleStream> log)
        {
            _logger = log;
        }

        [FunctionName("ScheduleStream")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiRequestBody("application/json", typeof(ScheduleStreamRequestBodyModel), Description = "JSON request body containing {Title, Descritpion, StartTime}")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            string title;
            string description;
            DateTime startTime;
            ClientSecrets secrets;
            TokenResponse token;
            UserCredential credentials;
            GoogleAuthorizationCodeFlow codeFlow;
            GoogleAuthorizationCodeFlow.Initializer authInit;
            BaseClientService.Initializer serviceInit;
            YouTubeService service;
            LiveBroadcast broadcast;
            LiveBroadcastSnippet broadcastSnippet;
            LiveBroadcastStatus broadcastStatus;
            LiveBroadcastsResource.InsertRequest request;
            LiveBroadcast response;
            DateTimeOffset broadcastStartTime;

            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            if (data == null)
            {
                return new BadRequestObjectResult("The request body does not contain a valid json object.");
            }

            title = data.title;
            if (string.IsNullOrEmpty(title))
            {
                return new BadRequestObjectResult("Connection string is not set.");
            }

            CourtDirectoryAndFileName = data.courtDirectoryAndFileName;
            if (string.IsNullOrEmpty(CourtDirectoryAndFileName))
            {
                return new BadRequestObjectResult("The court directory and file name is not set.");
            }

            FileName = data.fileName;
            if (string.IsNullOrEmpty(FileName))
            {
                return new ExceptionResult(new Exception("The file name is not set"), true);
            }

            secrets = new ClientSecrets()
            {
                ClientId = "Client ID String",
                ClientSecret = "Client Secret"
            };

            token = new TokenResponse { RefreshToken = "Refresh Token" };
            authInit = new GoogleAuthorizationCodeFlow.Initializer { ClientSecrets = secrets };
            codeFlow = new GoogleAuthorizationCodeFlow(authInit);
            credentials = new UserCredential(codeFlow, "user", token);

            serviceInit = new BaseClientService.Initializer
            {
                HttpClientInitializer = credentials,
                ApplicationName = "your-app-name"
            };

            service = new YouTubeService(serviceInit);

            broadcastStartTime = new DateTimeOffset(startTime);

            broadcastSnippet = new LiveBroadcastSnippet
            {
                Title = title,
                ScheduledStartTimeDateTimeOffset = broadcastStartTime
            };

            broadcastStatus = new LiveBroadcastStatus { PrivacyStatus = "public" };

            broadcast = new LiveBroadcast
            {
                Kind = "youtube#liveBroadcast",
                Snippet = broadcastSnippet,
                Status = broadcastStatus
            };

            request = service.LiveBroadcasts.Insert(broadcast, "id,snippet,status");
            response = request.Execute();
            //return response.Id;

            return new OkObjectResult(responseMessage);
        }
    }

    public class ScheduleStreamRequestBodyModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartTime { get; set; }
    }
}

