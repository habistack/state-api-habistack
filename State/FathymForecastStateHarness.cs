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
using LCU.Personas.Client.DevOps;
using LCU.Personas.Enterprises;
using LCU.Personas.Client.Applications;
using Fathym.API;

namespace LCU.State.API.NapkinIDE.NapkinIDE.FathymForecast.State
{
    public class FathymForecastStateHarness : LCUStateHarness<FathymForecastState>
    {
        #region Fields
        #endregion

        #region Properties
        #endregion

        #region Constructors
        public FathymForecastStateHarness(FathymForecastState state)
            : base(state ?? new FathymForecastState())
        { }
        #endregion

        #region API Methods
        public virtual async Task<Status> CreateAPISubscription(EnterpriseArchitectClient entArch, string entApiKey, string username)
        {
            //  Create user record in API Management - User is not user, but rather entApiKey

            //  Create Product subscription in API Management
            //  New endpoint on Enterprise Architect for creating user record for enterprise and initial product subscriptin from Azure API Management
            // var response = await entArch.InitializeForecastAPIKeys(entApiKey, keyType);

            //  Note:  Initial subscription creation may create both keys, therefore both following lines would not be called
            await GenerateAPIKeys(entArch, entApiKey, "Primary");
            
            await GenerateAPIKeys(entArch, entApiKey, "Secondary");

            return Status.Success;
        }

        public virtual async Task<Status> GenerateAPIKeys(EnterpriseArchitectClient entArch, string entApiKey, string keyType)
        {
            //  Call generate api keys on app arch for key type (primary, secondary)
            //  New endpoint on Enterprise Architect for regenerating a specific key in the subscription from Azure API Management
            // var response = await entArch.GenerateForecastAPIKeys(entApiKey, keyType);

            return Status.Success;
        }
        #endregion
    }
}
