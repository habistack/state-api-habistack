using System;
using System.IO;
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
using LCU.Personas.API;

namespace LCU.State.API.NapkinIDE.NapkinIDE.FathymForecast.State
{
    [Serializable]
    [DataContract]
    public class FathymForecastState
    {
        #region Constants
        public const string HUB_NAME = "fathymforecast";
        #endregion
        
        [DataMember]
        public virtual string AccessLicenseType { get; set; }
        
        [DataMember]
        public virtual string AccessPlanGroup { get; set; }
        
        [DataMember]
        public virtual List<APIAccessKeyData> APIKeys { get; set; }
        
        [DataMember]
        public virtual bool HasAccess { get; set; }
        
        [DataMember]
        public virtual bool Loading { get; set; }
        
        [DataMember]
        public virtual int MaxPointQueries { get; set; }
        
        [DataMember]
        public virtual string OpenAPISource { get; set; }
        
        [DataMember]
        public virtual UsageStateTypes UsageState { get; set; }
        
        [DataMember]
        public virtual string UserEnterpriseLookup { get; set; }
    }

    [DataContract]
    public enum UsageStateTypes
    {
        [EnumMember]
        Active,
        
        [EnumMember]
        Inactive,
        
        [EnumMember]
        Overage,
        
        [EnumMember]
        Revoked,
        
        [EnumMember]
        Warning        
    }
}
