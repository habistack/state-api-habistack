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
    [DataContract]
    public class GenerateAPIKeysRequest : BaseRequest
    {
        [DataMember]
        public virtual string KeyType { get; set; }
    }

    public class GenerateAPIKeys
    {
        protected EnterpriseArchitectClient entArch;

        public GenerateAPIKeys(EnterpriseArchitectClient entArch)
        {
            this.entArch = entArch;
        }

        [FunctionName("GenerateAPIKeys")]
        public virtual async Task<Status> Run([HttpTrigger] HttpRequest req, ILogger log,
            [SignalR(HubName = FathymForecastState.HUB_NAME)] IAsyncCollector<SignalRMessage> signalRMessages,
            [Blob("state-api/{headers.lcu-ent-api-key}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob)
        {
            return await stateBlob.WithStateHarness<FathymForecastState, GenerateAPIKeysRequest, FathymForecastStateHarness>(req, signalRMessages, log,
                async (harness, dataReq, actReq) =>
            {
                log.LogInformation($"GenerateAPIKeys");

                var stateDetails = StateUtils.LoadStateDetails(req);

                return await harness.GenerateAPIKeys(entArch, stateDetails.EnterpriseAPIKey, stateDetails.Username, dataReq.KeyType);
            });
        }
    }
}
