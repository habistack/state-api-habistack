using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using LCU.StateAPI.Hosting;

[assembly: FunctionsStartup(typeof(LCU.State.API.NapkinIDE.NapkinIDE.FathymForecast.Host.Startup))]

namespace LCU.State.API.NapkinIDE.NapkinIDE.FathymForecast.Host
{
    public class Startup : StateAPIStartup
    {
        #region Fields
        #endregion

        #region Constructors
        public Startup()
        { }
        #endregion

        #region API Methods
        #endregion
    }
}