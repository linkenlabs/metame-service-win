using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Owin;
using System;
using System.Collections.Generic;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace MetaMe.WindowsClient
{

    public class Startup
    {
        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        public void Configuration(IAppBuilder appBuilder)
        {
            appBuilder.UseCors(Microsoft.Owin.Cors.CorsOptions.AllowAll);

            //Make these shorter
            //https://docs.microsoft.com/en-us/aspnet/signalr/overview/guide-to-the-api/handling-connection-lifetime-events#clientdisconnect
            GlobalHost.Configuration.ConnectionTimeout = TimeSpan.FromSeconds(110);
            GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromSeconds(6);
            GlobalHost.Configuration.KeepAlive = TimeSpan.FromSeconds(2);
            GlobalHost.Configuration.DefaultMessageBufferSize = 1;

            var hubConfig = new HubConfiguration()
            {
                EnableDetailedErrors = true,
                EnableJavaScriptProxies = false
            };
            appBuilder.MapSignalR(hubConfig);

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new SignalRContractResolver()
            };
            var serializer = JsonSerializer.Create(settings);
            GlobalHost.DependencyResolver.Register(typeof(JsonSerializer), () => serializer);

            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();

            //GlobalConfiguration.Configure(WebApiConfig.Register);
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            appBuilder.UseWebApi(config);

            config.Formatters.Clear();
            config.Formatters.Add(new JsonMediaTypeFormatter());
            config.Formatters.JsonFormatter.SerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            config.EnsureInitialized();

        }
    }
}
