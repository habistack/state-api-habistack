using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DurableTask.Core.Exceptions;
using Fathym;
using LCU.Personas.Client.Enterprises;
using LCU.Personas.Client.Identity;
using LCU.State.API.NapkinIDE.NapkinIDE.FathymForecast.State;
using LCU.StateAPI;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace LCU.State.API.Habistack.FathymForecast
{
    public class ValidatePointQueryLimitsOrchestration : ActionOrchestration
    {
        #region Fields
        protected readonly EnterpriseArchitectClient entArch;

        protected readonly IdentityManagerClient idMgr;
        #endregion

        #region Constructors
        public ValidatePointQueryLimitsOrchestration(EnterpriseArchitectClient entArch, IdentityManagerClient idMgr)
        {
            this.entArch = entArch;

            this.idMgr = idMgr;
        }
        #endregion

        [FunctionName("ValidatePointQueryLimitsOrchestration")]
        public virtual async Task<Status> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            string entLookup;

            try
            {
                var actionArgs = context.GetInput<ExecuteActionArguments>();

                entLookup = actionArgs.StateDetails.EnterpriseLookup;
            }
            catch
            {
                entLookup = context.GetInput<string>();
            }

            if (!context.IsReplaying)
                log.LogInformation($"ValidatePointQueryLimits");

            var genericRetryOptions = new RetryOptions(TimeSpan.FromSeconds(1), 3)
            {
                BackoffCoefficient = 1.5,
                Handle = handleRetryException
            };

            var status = Status.Initialized;

            var entsWithLicense = await context.CallActivityWithRetryAsync<List<string>>(
                "ValidatePointQueryLimitsOrchestration_FindEnterprisesWithForecastLicense", genericRetryOptions, entLookup);

            var invalidEnts = new List<Tuple<string, Status>>();

            if (!entsWithLicense.IsNullOrEmpty())
            {
                // if (!context.IsReplaying)
                //     log.LogInformation($"...: {stateCtxt.ToJSON()}");

                var entValidateTasks = entsWithLicense.Select(entLookup =>
                {
                    return context.CallActivityWithRetryAsync<Tuple<string, Status>>(
                        "ValidatePointQueryLimitsOrchestration_ValidateEnterprisesForecastPointQueries", genericRetryOptions, entLookup);
                }).ToList();

                invalidEnts = (await Task.WhenAll(entValidateTasks)).Where(s => !s.Item2).ToList();
            }
            // else if (!context.IsReplaying)
            //     log.LogError($"...: {stateCtxt.ToJSON()}");

            if (invalidEnts.Any())
            {
                // if (!context.IsReplaying)
                //     log.LogInformation($"...: {stateCtxt.ToJSON()}");

                var revokeTasks = invalidEnts.Select(invalidEnt =>
                {
                    return context.CallActivityWithRetryAsync<Status>("ValidatePointQueryLimitsOrchestration_RevokeEnterpriseForecastLicense",
                        genericRetryOptions, invalidEnt.Item1);
                }).ToList();

                var revokeStati = await Task.WhenAll(revokeTasks);

                status = revokeStati.All(rs => rs) ? Status.Success : Status.GeneralError.Clone("Not all licenses properly revoked");

                if (!status)
                    status.Metadata["RevokeStati"] = revokeStati.JSONConvert<JArray>();
            }
            // else if (!context.IsReplaying)
            //     log.LogError($"...: {stateCtxt.ToJSON()}");

            return status;
        }

        [FunctionName("ValidatePointQueryLimitsOrchestration_FindEnterprisesWithForecastLicense")]
        public virtual async Task<List<string>> FindEnterprisesWithForecastLicense([ActivityTrigger] string parententLookup, ILogger log)
        {
            log.LogInformation($"FindEnterprisesWithForecastLicense...");

            // var response = await idMgr.ListLicenseAccessTokensForEnterprise(parententLookup, new List<string>() { "forecast" });

            //  key:string is the entLookup....
            return new List<string>();
        }

        [FunctionName("ValidatePointQueryLimitsOrchestration_ValidateEnterprisesForecastPointQueries")]
        public virtual async Task<Tuple<string, Status>> ValidateEnterprisesWithForecastLicense([ActivityTrigger] string entLookup, ILogger log)
        {
            log.LogInformation($"ValidateEnterprisesWithForecastLicense...");

            // var response = await entArch.ValidateForecastPointQueries(entLookup);

            return new Tuple<string, Status>(entLookup, Status.Success);
        }

        [FunctionName("ValidatePointQueryLimitsOrchestration_RevokeEnterpriseForecastLicense")]
        public virtual async Task<Status> RevokeEnterpriseForecastLicense([ActivityTrigger] string entLookup, ILogger log)
        {
            var enterprises = new List<string>();

            log.LogInformation($"RevokeEnterpriseForecastLicense...");

            //  New endpoint on the Identity Manager persona to revoke all licenses for all users under an enterprise
            // var response = await idMgr.RevokeLicenseAccessTokensForEnterprise(entLookup, new List<string>() { "forecast" });

            //  New endpoint on Enterprise Architect for revoking the subscription from Azure API Management
            // var response = await entArch.RevokeForecastAPIKeys(entLookup);

            return Status.Success;
        }

        #region Helpers
        #endregion
    }
}