using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Web;
using System.Web.Configuration;

namespace DevelopmentWithADot.AspNetResourcesHandler
{
	public sealed class ResourcesHandler : IHttpHandler
	{
		void IHttpHandler.ProcessRequest(HttpContext context)
		{
			Assembly asm = null;
			Type appType = context.ApplicationInstance.GetType();
			IEnumerable<String> requestedResources = context.Request.PathInfo.Length != 0 ? context.Request.PathInfo.Substring(1).Split(',') : Enumerable.Empty<String>();

			context.Response.ContentType = "text/javascript";
			context.Response.Cache.SetCacheability(HttpCacheability.Public);

			if (appType.Namespace == "ASP")
			{
				asm = appType.BaseType.Assembly;
			}
			else
			{
				asm = appType.Assembly;
			}

			IEnumerable<Type> resources = asm.GetTypes().Where(x => x.Namespace == "Resources").ToList();

			var l = context.Request.UserLanguages;

			if (context.Request.UserLanguages.Any() == true)
			{
				try
				{
					Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture(context.Request.UserLanguages.First());
				}
				catch
				{
				}
			}

			context.Response.Write("var Resources = {};\n");

			foreach (Type resource in resources.Where(x => (requestedResources.Any() == false) || (requestedResources.Contains(x.Name, StringComparer.OrdinalIgnoreCase) == true)))
			{
				context.Response.Write(String.Format("Resources.{0} = {{}};\n", resource.Name));
				
				Dictionary<String, String> dict = resources.First().GetProperties(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetProperty).Where(x => x.PropertyType == typeof(String)).ToDictionary(x => x.Name, x => x.GetValue(null, null).ToString());

				foreach (String key in dict.Keys)
				{
					context.Response.Write(String.Format("Resources.{0}.{1} = '{2}';\n", resource.Name, key, dict[key].Replace("'", "\'")));
				}
			}
		}

		Boolean IHttpHandler.IsReusable
		{
			get
			{
				return (true);
			}
		}
	}
}