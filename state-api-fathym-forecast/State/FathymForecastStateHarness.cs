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
using LCU.Personas.Client.Security;
using Fathym.Design;
using LCU.Personas.API;

namespace LCU.State.API.NapkinIDE.NapkinIDE.FathymForecast.State
{
    public class FathymForecastStateHarness : LCUStateHarness<FathymForecastState>
    {
        #region Fields
        #endregion

        #region Properties
        #endregion

        #region Constructors
        public FathymForecastStateHarness(FathymForecastState state, ILogger log)
            : base(state ?? new FathymForecastState(), log)
        {
        }
        #endregion

        #region API Methods
        public virtual async Task<Status> EnsureAPISubscription(EnterpriseArchitectClient entArch, string entLookup, string username)
        {
            await DesignOutline.Instance.Retry()
                .SetActionAsync(async () =>
                {
                    try
                    {
                        var resp = await entArch.EnsureAPISubscription(new EnsureAPISubscriptionRequset()
                        {
                            SubscriptionType = buildSubscriptionType()
                        }, entLookup, username);

                        //  TODO:  Handle API error

                        return !resp.Status;
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, "Failed ensuring API subscription");

                        return true;
                    }
                })
                .SetCycles(5)
                .SetThrottle(25)
                .SetThrottleScale(2)
                .Run();

            return await LoadAPIKeys(entArch, entLookup, username);
        }

        public virtual async Task EnsureUserEnterprise(EnterpriseArchitectClient entArch, EnterpriseManagerClient entMgr,
            SecurityManagerClient secMgr, string parentEntLookup, string username)
        {
            if (State.UserEnterpriseLookup.IsNullOrEmpty())
            {
                await DesignOutline.Instance.Retry()
                    .SetActionAsync(async () =>
                    {
                        try
                        {
                            var hostLookup = $"{parentEntLookup}|{username}";

                            log.LogInformation($"Ensuring user enterprise for {hostLookup}...");

                            var getResp = await entMgr.ResolveHost(hostLookup, false);

                            if (!getResp.Status || getResp.Model == null)
                            {
                                var createResp = await entArch.CreateEnterprise(new CreateEnterpriseRequest()
                                {
                                    Name = username,
                                    Description = username,
                                    Host = hostLookup
                                }, parentEntLookup, username);

                                if (createResp.Status)
                                    State.UserEnterpriseLookup = createResp.Model.EnterpriseLookup;
                            }
                            else
                                State.UserEnterpriseLookup = getResp.Model.EnterpriseLookup;

                            return State.UserEnterpriseLookup.IsNullOrEmpty();
                        }
                        catch (Exception ex)
                        {
                            log.LogError(ex, "Failed ensuring user enterprise");

                            return true;
                        }
                    })
                    .SetCycles(5)
                    .SetThrottle(25)
                    .SetThrottleScale(2)
                    .Run();
            }

            if (State.UserEnterpriseLookup.IsNullOrEmpty())
                throw new Exception("Unable to establish the user's enterprise, please try again.");
        }

        public virtual async Task<Status> HasLicenseAccess(IdentityManagerClient idMgr, string entLookup, string username)
        {
            await DesignOutline.Instance.Retry()
                .SetActionAsync(async () =>
                {
                    try
                    {
                        var hasAccess = await idMgr.HasLicenseAccess(entLookup, username, Personas.AllAnyTypes.All, new List<string>() { "forecast" });

                        State.HasAccess = hasAccess.Status;

                        if (State.HasAccess)
                        {
                            if (hasAccess.Model.Metadata.ContainsKey("LicenseType"))
                                State.AccessLicenseType = hasAccess.Model.Metadata["LicenseType"].ToString();

                            if (hasAccess.Model.Metadata.ContainsKey("PlanGroup"))
                                State.AccessPlanGroup = hasAccess.Model.Metadata["PlanGroup"].ToString();

                            if (hasAccess.Model.Metadata.ContainsKey("PointQueries"))
                                State.MaxPointQueries = hasAccess.Model.Metadata["PointQueries"].ToString().As<int>();
                        }
                        else
                        {
                            State.AccessLicenseType = "forecast";

                            State.AccessPlanGroup = "hobby";

                            State.MaxPointQueries = 10000;
                        }

                        return false;
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, "Failed checking has license access type");

                        return true;
                    }
                })
                .SetCycles(5)
                .SetThrottle(25)
                .SetThrottleScale(2)
                .Run();

            return Status.Success;
        }

        public virtual async Task<Status> LoadAPIKeys(EnterpriseArchitectClient entArch, string entLookup, string username)
        {
            State.APIKeys = new List<APIAccessKeyData>();

            await DesignOutline.Instance.Retry()
                .SetActionAsync(async () =>
                {
                    try
                    {
                        var resp = await entArch.LoadAPIKeys(entLookup, buildSubscriptionType(), username);

                        //  TODO:  Handle API error

                        log.LogInformation($"Load API Keys response: {resp.ToJSON()}");

                        State.APIKeys = resp.Model?.Metadata.Select(m => new APIAccessKeyData()
                        {
                            Key = m.Value.ToString(),
                            KeyName = m.Key
                        }).ToList();

                        return !resp.Status;
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, "Failed loading API Keys");

                        return true;
                    }
                })
                .SetCycles(5)
                .SetThrottle(25)
                .SetThrottleScale(2)
                .Run();

            return Status.Success;
        }

        public virtual async Task<Status> LoadAPIOptions()
        {
            State.OpenAPISource = "https://www.habistack.com/open-api/habistack-ground-weather.openapi.json";

            return Status.Success;
        }

        public virtual async Task<Status> Refresh(EnterpriseArchitectClient entArch, EnterpriseManagerClient entMgr,
            IdentityManagerClient idMgr, SecurityManagerClient secMgr, StateDetails stateDetails)
        {
            await EnsureUserEnterprise(entArch, entMgr, secMgr, stateDetails.EnterpriseLookup, stateDetails.Username);

            await Task.WhenAll(
                HasLicenseAccess(idMgr, stateDetails.EnterpriseLookup, stateDetails.Username)
            );

            await Task.WhenAll(
                EnsureAPISubscription(entArch, stateDetails.EnterpriseLookup, stateDetails.Username),
                LoadAPIOptions()
            );

            State.Loading = false;

            return Status.Success;
        }
        #endregion

        #region Helpers
        protected virtual string buildSubscriptionType()
        {
            return $"{State.AccessLicenseType}-{State.AccessPlanGroup}".ToLower();
        }
        #endregion
    }
}
