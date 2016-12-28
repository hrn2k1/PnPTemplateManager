using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using PnPTemplateManager.BLL.Managers;

namespace PnPTemplateManager.Web
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Configure webapi to use our Ioc container to resolve dependencies            
            //config.DependencyResolver = new IocResolver();            
            config.DependencyResolver = new NinjectResolver();
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
