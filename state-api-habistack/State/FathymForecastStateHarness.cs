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
using LCU.State.API.Habistack.Host.TempRefit;

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
        public virtual async Task<Status> EnsureAPISubscription(IEnterprisesAPIManagementService entApiArch, string entLookup, string username)
        {
            await DesignOutline.Instance.Retry()
                .SetActionAsync(async () =>
                {
                    try
                    {
                        var resp = await entApiArch.EnsureAPISubscription(new EnsureAPISubscriptionRequest()
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

            return await LoadAPIKeys(entApiArch, entLookup, username);
        }

        public virtual async Task EnsureUserEnterprise(IEnterprisesAsCodeService eacSvc, IEnterprisesHostingManagerService hostMgrSvc,
            ISecurityDataTokenService dataTokenSvc, string parentEntLookup, string username)
        {

            if (State.UserEnterpriseLookup.IsNullOrEmpty())
            {
                await DesignOutline.Instance.Retry()
                    .SetActionAsync(async () =>
                    {
                        try
                            {
                            var userHost = $"{parentEntLookup}|{username}";

                            log.LogInformation($"Ensuring child enterprise for {userHost}.");

                            var hostResp = await hostMgrSvc.ResolveHost(userHost);

                            if (hostResp.Model == null)
                            {
                                var commitReq = new CommitEnterpriseAsCodeRequest()
                                {
                                    EaC = new EnterpriseAsCode()
                                    {
                                        Enterprise = new EaCEnterpriseDetails()
                                        {
                                            Name = $"{username} Enterprise",
                                            Description = $"{username} Enterprise",
                                            ParentEnterpriseLookup = parentEntLookup,
                                            PrimaryEnvironment = userHost,
                                            PrimaryHost = userHost
                                        },
                                        Hosts = new Dictionary<string, EaCHost>()
                                        {
                                            {userHost, new EaCHost()}                                           
                                        },
                                        AccessRights = new Dictionary<string, EaCAccessRight>()
                                        {
                                            {
                                                "Fathym.Global.Admin",
                                                new EaCAccessRight()
                                                {
                                                    Name = "Fathym.Global.Admin",
                                                    Description = "Fathym.Global.Admin",
                                                }
                                            },
                                            {
                                                "Fathym.User",
                                                new EaCAccessRight()
                                                {
                                                    Name = "Fathym.User",
                                                    Description = "Fathym.User",
                                                }
                                            }
                                        },
                                        Modifiers = new Dictionary<string, EaCDFSModifier>()
                                        {
                                            {
                                                "html-base",
                                                new EaCDFSModifier()
                                                {
                                                    Type = "LCU.Runtime.Applications.Modifiers.HTMLBaseDFSModifierManager, LCU.Runtime, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                                                    Name = "HTML Base",
                                                    Priority = 10000,
                                                    Enabled = true,
                                                    Details = new {}.ToJSON(),
                                                    PathFilterRegex = ".*index.html"
                                                }
                                            },
                                            {
                                                "lcu-reg",
                                                new EaCDFSModifier()
                                                {
                                                    Type = "LCU.Runtime.Applications.Modifiers.LCURegDFSModifierManager, LCU.Runtime, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                                                    Name = "LCU Reg",
                                                    Priority = 9000,
                                                    Enabled = true,
                                                    Details = new { StateDataToken = "lcu-state-config" }.ToJSON(),
                                                    PathFilterRegex = ".*index.html"
                                                }
                                            }
                                        },
                                        DataTokens = new Dictionary<string, EaCDataToken>()
                                        {
                                            {
                                                "EMULATED_DEVICE_ENABLED",
                                                new EaCDataToken()
                                                {
                                                    Value = "false",
                                                    Name = "EMULATED_DEVICE_ENABLED"                                                   
                                                }
                                            },
                                            {
                                                "TELEMETRY_SYNC_ENABLED",
                                                new EaCDataToken()
                                                {
                                                    Value = "false",
                                                    Name = "TELEMETRY_SYNC_ENABLED"                                                   
                                                }                                                
                                            }
                                        },
                                        Providers = new Dictionary<string, EaCProvider>()
                                        {
                                            {
                                                "ADB2C", 
                                                new EaCProvider()
                                                {
                                                    Name = "ADB2C",
                                                    Description = "ADB2C Provider",
                                                    Type = "ADB2C"
                                                } 
                                            }
                                        },
                                        Environments = new Dictionary<string, EaCEnvironmentAsCode>()
                                        {
                                            {
                                                userHost,
                                                new EaCEnvironmentAsCode()
                                                {
                                                    Environment = new EaCEnvironmentDetails()
                                                    {
                                                        Name = $"{username} Environment",
                                                        Description = $"{username} Environment"
                                                    }
                                                }
                                            }
                                        }
                                    },

                                    Username = username
                                };

                                // string adb2cAppId = null;

                                // if (hostResp.Status)
                                // {
                                //     var adB2cAppIdToken = await dataTokenSvc.GetDataToken(EnterpriseContext.AD_B2C_APPLICATION_ID_LOOKUP, entLookup: hostResp.Model?.Lookup);

                                //     adb2cAppId = adB2cAppIdToken?.Model?.Value;
                                // }

                                // if (adb2cAppId.IsNullOrEmpty() && !parentEntLookup.IsNullOrEmpty())
                                // {
                                //     //  TODO:  Create unique application in ADB2C to allow for multi tenant control of sign in

                                //     var adB2cAppIdToken = await dataTokenSvc.GetDataToken(EnterpriseContext.AD_B2C_APPLICATION_ID_LOOKUP, entLookup: parentEntLookup);

                                //     adb2cAppId = adB2cAppIdToken?.Model?.Value;

                                //     commitReq.EaC.Providers.Add("ADB2C", new EaCProvider()
                                //     {
                                //         Name = "ADB2C",
                                //         Description = "ADB2C Provider",
                                //         Type = "ADB2C",
                                //         Metadata = new Dictionary<string, JToken>()
                                //         {
                                //             { "ApplicationID", EnterpriseContext.AD_B2C_APPLICATION_ID_LOOKUP },
                                //             { "Authority", "fathymcloudprd.onmicrosoft.com" }
                                //         }
                                //     });

                                //     commitReq.EaC.DataTokens[EnterpriseContext.AD_B2C_APPLICATION_ID_LOOKUP] = new EaCDataToken()
                                //     {
                                //         Value = adb2cAppId,
                                //         Name = "AD B2C Application ID",
                                //         Description = "The AD B2C application ID used with authentication."
                                //     };
                                // }

                                var commitResp = await eacSvc.Commit(commitReq);

                                if (commitResp.Status)
                                {
                                    log.LogInformation($"Ensured child enterprise for {userHost}.");

                                    hostResp = await hostMgrSvc.ResolveHost(userHost);

                                    var parentGitHubDataToken = await dataTokenSvc.GetDataToken("LCU-GITHUB-ACCESS-TOKEN", entLookup: parentEntLookup, email: username);

                                    if (parentGitHubDataToken.Model != null)
                                    {
                                        log.LogInformation($"Transferring GitHub access to child enterprise for {hostResp.Model.Lookup}.");

                                        var setDTResp = await dataTokenSvc.SetDataToken(new DataToken()
                                        {
                                            Name = parentGitHubDataToken.Model.Name,
                                            Description = parentGitHubDataToken.Model.Description,
                                            Lookup = parentGitHubDataToken.Model.Lookup,
                                            Value = parentGitHubDataToken.Model.Value
                                        }, entLookup: hostResp.Model.Lookup, email: username);
                                    }
                                    State.UserEnterpriseLookup = hostResp.Model.Lookup;
                                }
                            }

                            log.LogInformation($"Ensuring child enterprise for {userHost}");

                            State.UserEnterpriseLookup = hostResp.Model.Lookup;

                            return true;
                        }

                        catch (Exception ex)
                        {
                            log.LogError(ex, "Failed ensuring user enterprise");

                            return false;
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

        public virtual async Task<Status> HasLicenseAccess(IIdentityAccessService idMgr, string entLookup, string username)
        {
            await DesignOutline.Instance.Retry()
                .SetActionAsync(async () =>
                {
                    try
                    {
                        var hasAccess = await idMgr.HasLicenseAccess(entLookup, username, AllAnyTypes.All, new List<string>() { "forecast" });

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

        public virtual async Task<Status> LoadAPIKeys(IEnterprisesAPIManagementService entApiArch, string entLookup, string username)
        {
            State.APIKeys = new List<APIAccessKeyData>();

            await DesignOutline.Instance.Retry()
                .SetActionAsync(async () =>
                {
                    try
                    {
                        var resp = await entApiArch.LoadAPIKeys(entLookup, buildSubscriptionType(), username);

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
            State.OpenAPISource = Environment.GetEnvironmentVariable("OPEN-API-SOURCE-URL");

            return Status.Success;
        }

        public virtual async Task<Status> Refresh(StateDetails stateDetails, IEnterprisesAPIManagementService entApiArch, IEnterprisesAsCodeService eacSvc, 
            IEnterprisesHostingManagerService entHostMgr, IIdentityAccessService idMgr, ISecurityDataTokenService secMgr)
        {
            await EnsureUserEnterprise(eacSvc, entHostMgr, secMgr, stateDetails.EnterpriseLookup, stateDetails.Username);

            await Task.WhenAll(
                HasLicenseAccess(idMgr, stateDetails.EnterpriseLookup, stateDetails.Username)
            );

            await Task.WhenAll(
                EnsureAPISubscription(entApiArch, stateDetails.EnterpriseLookup, stateDetails.Username),
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
