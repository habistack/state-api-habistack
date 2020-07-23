using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Fathym;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Runtime.Serialization;
using Fathym.API;
using System.Collections.Generic;
using System.Linq;
using LCU.Personas.Client.Applications;
using LCU.StateAPI.Utilities;
using System.Security.Claims;
using LCU.Personas.Client.Enterprises;
using LCU.State.API.NapkinIDE.NapkinIDE.FathymForecast.State;

namespace LCU.State.API.NapkinIDE.NapkinIDE.FathymForecast.Host
{
    [Serializable]
    [DataContract]
    public class CreateAPISubscriptionRequest : BaseRequest
    { 

    }

    public class CreateAPISubscription
    {
        protected EnterpriseArchitectClient entArch;

        public CreateAPISubscription(EnterpriseArchitectClient entArch)
        {
            this.entArch = entArch;
        }

        [FunctionName("CreateAPISubscription")]
        public virtual async Task<Status> Run([HttpTrigger] HttpRequest req, ILogger log,
            [SignalR(HubName = FathymForecastState.HUB_NAME)] IAsyncCollector<SignalRMessage> signalRMessages,
            [Blob("state-api/{headers.lcu-ent-api-key}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob)
        {
            return await stateBlob.WithStateHarness<FathymForecastState, CreateAPISubscriptionRequest, FathymForecastStateHarness>(req, signalRMessages, log,
                async (harness, refreshReq, actReq) =>
            {
                log.LogInformation($"CreateAPISubscription");

                var stateDetails = StateUtils.LoadStateDetails(req);

                return await harness.CreateAPISubscription(entArch, stateDetails.EnterpriseAPIKey, stateDetails.Username);
            });
        }
    }
}
