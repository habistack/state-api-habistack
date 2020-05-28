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
            if (State.HasAccess)
            {
                //  Create user record in API Management - User is not user, but rather entApiKey...  Ensure only one user created per enterprise
                //  Create Product subscription in API Management
                //  New endpoint on Enterprise Architect for creating user record for enterprise and initial product subscriptin from Azure API Management
                //  var response = await entArch.EnsureForecastAPISubscription(entApiKey, keyType);

                //  Note:  Initial subscription creation may create both keys, therefore both following lines would not be called
                // await GenerateAPIKeys(entArch, entApiKey, "Primary");

                // await GenerateAPIKeys(entArch, entApiKey, "Secondary");
            }

            return await LoadAPIKeys(entArch, entApiKey);
        }

        public virtual async Task<Status> GenerateAPIKeys(EnterpriseArchitectClient entArch, string entApiKey, string keyType)
        {
            if (State.HasAccess)
            {
                //  Call generate api keys on app arch for key type (primary, secondary)
                //  New endpoint on Enterprise Architect for regenerating a specific key in the subscription from Azure API Management
                // var response = await entArch.GenerateForecastAPIKeys(entApiKey, keyType);
            }

            return Status.Success;
        }

        public virtual async Task<Status> HasAccess(IdentityManagerClient idMgr, string entApiKey)
        {
            //  Verify that a user has a forecast license, and prevent call if none
            //  await idMgr.HasLicenseAccess()

            State.HasAccess = true;

            return Status.Success;
        }

        public virtual async Task<Status> LoadAPIKeys(EnterpriseArchitectClient entArch, string entApiKey)
        {
                State.APIKeys = new Dictionary<string, string>();

            if (State.HasAccess)
            {
                //  Call generate api keys on app arch for key type (primary, secondary)
                //  New endpoint on Enterprise Architect for regenerating a specific key in the subscription from Azure API Management
                // var response = await entArch.LoadForecastAPIKeys(entApiKey);

                State.APIKeys.Add("Primary", "as;ldfjas;dlkfasd;fkjasdlkf");
                
                State.APIKeys.Add("Secndary", "pqwoieurpqwoeirua,zmxcnqp");
            }

            return Status.Success;
        }

        public virtual async Task<Status> Refresh(EnterpriseArchitectClient entArch, IdentityManagerClient idMgr, string entApiKey, string username)
        {
            Status status = await HasAccess(idMgr, entApiKey);

            if (!State.APIKeys.IsNullOrEmpty())
                status = await CreateAPISubscription(entArch, entApiKey, username);
            else
                status =await LoadAPIKeys(entArch, entApiKey);

            return status;
        }
        #endregion
    }
}
