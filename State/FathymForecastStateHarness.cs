using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Fathym;
using LCU.Presentation.State.ReqRes;
using LCU.StateAPI.Utilities;
using LCU.StateAPI;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;
using System.Collections.Generic;
using LCU.Personas.Client.Enterprises;
using LCU.Personas.Client.Identity;
using LCU.Personas.Client.DevOps;
using LCU.Personas.Enterprises;
using LCU.Personas.Client.Applications;
using Fathym.API;

namespace LCU.State.API.NapkinIDE.NapkinIDE.FathymForecast.State
{
    public class FathymForecastStateHarness : LCUStateHarness<FathymForecastState>
    {
        #region Fields
        protected readonly string lcuApiSiteUrl;
        
        protected readonly string forecastEntLookup;
        #endregion

        #region Properties
        #endregion

        #region Constructors
        public FathymForecastStateHarness(FathymForecastState state)
            : base(state ?? new FathymForecastState())
        {
            lcuApiSiteUrl = Environment.GetEnvironmentVariable("LCU-API-SITE-URL");
            
            forecastEntLookup = Environment.GetEnvironmentVariable("LCU-ENTERPRISE-LOOKUP");
        }
        #endregion

        #region API Methods
        public virtual async Task<Status> CreateAPISubscription(EnterpriseArchitectClient entArch, string entApiKey, string username)
        {
            if (State.HasAccess)
            {
                var response = await entArch.EnsureForecastAPISubscription(new EnsureForecastAPISubscriptionRequset()
                {
                    SubscriptionType = $"{State.AccessLicenseType}-{State.AccessPlanGroup}".ToLower()
                }, forecastEntLookup, username);

                //  TODO:  Handle API error
            }

            return await LoadAPIKeys(entArch, entApiKey, username);
        }

        public virtual async Task<Status> GenerateAPIKeys(EnterpriseArchitectClient entArch, string entApiKey, string username, string keyType)
        {
            if (State.HasAccess)
            {
                var response = await entArch.GenerateForecastAPIKeys(new GenerateForecastAPIKeysRequset()
                {
                    KeyType = keyType
                }, forecastEntLookup, username);

                //  TODO:  Handle API error
            }

            return await LoadAPIKeys(entArch, forecastEntLookup, username);
        }

        public virtual async Task<Status> HasAccess(IdentityManagerClient idMgr, string entApiKey, string username)
        {
            var hasAccess = await idMgr.HasLicenseAccess(forecastEntLookup, username, Personas.AllAnyTypes.All, new List<string>() { "forecast" });

            State.HasAccess = hasAccess.Status;

            if (State.HasAccess)
            {
                State.AccessLicenseType = hasAccess.Model.Metadata["LicenseType"].ToString();

                State.AccessPlanGroup = hasAccess.Model.Metadata["PlanGroup"].ToString();
            }

            return Status.Success;
        }

        public virtual async Task<Status> LoadAPIKeys(EnterpriseArchitectClient entArch, string entApiKey, string username)
        {
            State.APIKeys = new Dictionary<string, string>();

            if (State.HasAccess)
            {
                var response = await entArch.LoadForecastAPIKeys(forecastEntLookup, username);

                //  TODO:  Handle API error

                State.APIKeys = response.Model?.Metadata.ToDictionary(m => m.Key, m => m.Value.ToString());
            }

            return Status.Success;
        }

        public virtual async Task<Status> Refresh(EnterpriseArchitectClient entArch, IdentityManagerClient idMgr, string entApiKey, string username)
        {
            Status status = await HasAccess(idMgr, entApiKey, username);

            if (status)
            {
                status = await LoadAPIKeys(entArch, entApiKey, username);

                if (State.APIKeys.IsNullOrEmpty())
                    status = await CreateAPISubscription(entArch, entApiKey, username);

                State.APISiteURL = lcuApiSiteUrl;
            }

            return status;
        }
        #endregion
    }
}
