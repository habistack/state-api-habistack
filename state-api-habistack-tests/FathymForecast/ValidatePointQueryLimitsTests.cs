using Fathym.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace state_api_fathym_forecast_tests
{
    [TestClass]
    public class ValidatePointQueryLimitsTests : AzFunctionTestBase
    {
        
        public ValidatePointQueryLimitsTests() : base()
        {
            APIRoute = "api/ValidatePointQueryLimits";                
        }

        [TestMethod]
        public async Task TestValidatePointQueryLimits()
        {
            LcuentLookup = "";            
            PrincipalId = "";

            addRequestHeaders();

            var url = $"{HostURL}/{APIRoute}";            

            var response = await httpGet(url); 

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var model = getContent<dynamic>(response);

            dynamic result = model.Result;            

            throw new NotImplementedException("Implement me!");                  
        }

        [TestMethod]
        public async Task TestValidatePointQueryLimitsTimer()
        {
            LcuentLookup = "";            
            PrincipalId = "";

            addRequestHeaders();

            var url = $"{HostURL}/{APIRoute}Timer";            

            var response = await httpGet(url); 

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var model = getContent<dynamic>(response);

            dynamic result = model.Result;            

            throw new NotImplementedException("Implement me!");                  
        }        
    }
}
