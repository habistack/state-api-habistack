using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fathym;
using LCU.Presentation.State.ReqRes;
using LCU.StateAPI;
using LCU.StateAPI.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Linq;

namespace LCU.State.API.Habistack.FathymForecast
{
    public class GenericValidatePointQueryLimits
    {
        #region Helpers
        public virtual async Task<string> runAction(IDurableOrchestrationClient starter, ILogger log)
        {
            var entApiKey = Environment.GetEnvironmentVariable("LCU-ENTERPRISE-LOOKUP");

            try
            {
                var instanceId = await starter.StartAction("ValidatePointQueryLimitsOrchestration", new StateDetails()
                {
                    EnterpriseAPIKey = entApiKey
                }, new ExecuteActionRequest()
                {
                    Arguments = new
                    {
                        EnterpriseAPIKey = entApiKey
                    }.JSONConvert<MetadataModel>()
                }, log);

                return instanceId;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        #endregion
    }

    public class ValidatePointQueryLimitsTimer : GenericValidatePointQueryLimits
    {
        #region API Methods
        [FunctionName("ValidatePointQueryLimitsTimer")]
        public virtual async Task RunTimer([TimerTrigger("0 0 1 * * *", RunOnStartup = true)]TimerInfo myTimer,
            [DurableClient] IDurableOrchestrationClient starter, ILogger log)
        {
            log.LogInformation($"ValidatePointQueryLimits: {DateTime.Now}");

            var instanceId = await runAction(starter, log);
        }
        #endregion
    }

    public class ValidatePointQueryLimitsAPI : GenericValidatePointQueryLimits
    {
        #region API Methods
        [FunctionName("ValidatePointQueryLimits")]
        public virtual async Task<IActionResult> RunAPI([HttpTrigger]HttpRequest req, [DurableClient] IDurableOrchestrationClient starter, ILogger log)
        {
            log.LogInformation($"ValidatePointQueryLimits");

            var instanceId = await runAction(starter, log);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
        #endregion
    }
}
