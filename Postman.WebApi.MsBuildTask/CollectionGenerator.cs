using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Http.Dispatcher;

namespace Postman.WebApi.MsBuildTask
{
	using Conditions;
	using Models;
	using SampleGeneration;

	/// <summary>
	/// Resolves the ApiController assembly from a file path
	/// </summary>
	public class AssemblyResolver : IAssembliesResolver
	{
		public string AssemblyFilePath { get; private set; }
		public Version Version { get; private set; } = new Version();

		public AssemblyResolver(string assemblyFilePath) : base()
		{
			AssemblyFilePath = assemblyFilePath;
		}

		public ICollection<Assembly> GetAssemblies()
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
			var target = Path.GetFileNameWithoutExtension(AssemblyFilePath);

			// do not load the assembly if it is already loaded
			if (assemblies.Any(a => a.GetName().Name == target))
			{
				return assemblies;
			}

			var controllersAssembly = Assembly.LoadFrom(AssemblyFilePath);
			if (controllersAssembly == null)
			{
				throw new Exception(string.Format(Resources.AssemblyLoadFailure, AssemblyFilePath));
			}

			Version = controllersAssembly.GetName().Version;
			assemblies.Add(controllersAssembly);

			return assemblies;
		}
	}

	/// <summary>
	/// The Postman collection generator
	/// </summary>
	public class CollectionGenerator
	{
		private readonly Regex _pathVariableRegEx = new Regex("\\{([A-Za-z0-9-_]+)\\}", RegexOptions.ECMAScript | RegexOptions.Compiled);
		private readonly Regex _urlParameterVariableRegEx = new Regex("=\\{([A-Za-z0-9-_]+)\\}", RegexOptions.ECMAScript | RegexOptions.Compiled);

		/// <summary>Creates a <see cref="PostmanCollection" />.</summary>
		/// <param name="assemblyFilePath">The assembly file path.</param>
		/// <param name="baseUrl">The base URL.</param>
		/// <returns></returns>
		public PostmanCollection Create(string assemblyFilePath, string environmentKey, string routeTemplate)
		{
			Condition.Requires(assemblyFilePath).IsNotNullOrEmpty();
			Condition.Requires(environmentKey).IsNotNullOrEmpty();
			Condition.Requires(routeTemplate).IsNotNullOrEmpty();

			if (!File.Exists(assemblyFilePath))
			{
				throw new FileNotFoundException(assemblyFilePath);
			}

			var baseUrl = "{{" + environmentKey + "}}/";
			var assemblyName = Path.GetFileNameWithoutExtension(assemblyFilePath);
			var xmlPath = assemblyFilePath
				.ToLowerInvariant()
				.Replace(Resources.DllFileExtension, Resources.XmlFileExtension);

			if (!File.Exists(xmlPath))
			{
				throw new FileNotFoundException(Resources.XmlCommentsFileNotFound);
			}

			var assemblyResolver = new AssemblyResolver(assemblyFilePath);
			var config = new HttpConfiguration();
			config.MapHttpAttributeRoutes();
			config.Routes.MapHttpRoute("DefaultApi", routeTemplate);
			config.Services.Replace(typeof(IAssembliesResolver), assemblyResolver);
			config.SetDocumentationProvider(new XmlDocumentationProvider(xmlPath));
			config.EnsureInitialized();

			var apiExplorer = config.Services.GetApiExplorer();
			var provider = config.Services.GetDocumentationProvider();

			var sampleGenerator = new HelpPageSampleGenerator();

			var controllerDescriptors = apiExplorer.ApiDescriptions
				.GroupBy(description => description
					.ActionDescriptor
					.ActionBinding
					.ActionDescriptor
					.ControllerDescriptor);

			var x = apiExplorer.ApiDescriptions.ToList();

			var postManCollection = new PostmanCollection
			{
				Id = Guid.NewGuid(),
				Name = assemblyName,
				Description = assemblyName + " Api v" + assemblyResolver.Version.ToString(),
				Order = new Collection<Guid>(),
				Timestamp = DateTime.Now.ToUnixMilliseconds(),
				Folders = new Collection<PostmanFolder>(),
				Requests = new Collection<PostmanRequest>()
			};

			foreach (var apiDescriptionsByControllerGroup in controllerDescriptors)
			{
				var controllerName = apiDescriptionsByControllerGroup.Key.ControllerName;
				var controllerDocumentation = provider.GetDocumentation(apiDescriptionsByControllerGroup.Key);

				dynamic jsonControllerDocumentation = JObject.Parse(controllerDocumentation);
				var controllerDescription = string.Format("{0}  \n_{1}_",
					jsonControllerDocumentation.summary,
					jsonControllerDocumentation.remarks ?? " ");

				var postManFolder = new PostmanFolder
				{
					Id = Guid.NewGuid(),
					CollectionId = postManCollection.Id,
					Name = controllerName,
					Description = controllerDescription,
					Order = new Collection<Guid>()
				};

				foreach (var apiDescription in apiDescriptionsByControllerGroup
					 .OrderBy(description => description.HttpMethod, new HttpMethodComparator())
					 .ThenBy(description => description.RelativePath)
					 .ThenBy(description => description.Documentation))
				{
					TextSample sampleData = null;
					var sampleDictionary = sampleGenerator.GetSample(apiDescription, SampleDirection.Request);

					MediaTypeHeaderValue mediaTypeHeader;
					if (MediaTypeHeaderValue.TryParse("application/json", out mediaTypeHeader)
						 && sampleDictionary.ContainsKey(mediaTypeHeader))
					{
						sampleData = sampleDictionary[mediaTypeHeader] as TextSample;
					}

					// scrub curly braces from url parameter values
					var pathTokens = apiDescription.RelativePath.Split(new char[] { '?' }, 2);
					var path = _pathVariableRegEx.Replace(pathTokens[0], ":$1");
					var queryString = pathTokens.Length > 1 ? _urlParameterVariableRegEx.Replace(pathTokens[1], "=") : string.Empty;

					// prefix url with postman environment key variable
					var url = path + (pathTokens.Length > 1 ? "?" + queryString : string.Empty);

					var postmanRequest = new PostmanRequest
					{
						CollectionId = postManCollection.Id,
						Id = Guid.NewGuid(),
						Name = url,
						Description = ToMarkdown(apiDescription),
						Url = baseUrl + url,
						Method = apiDescription.HttpMethod.Method,
						Headers = "Content-Type: application/json",
						RawModeData = sampleData == null ? null : sampleData.Text,
						DataMode = "raw",
						Time = postManCollection.Timestamp,
						DescriptionFormat = "markdown",
						Responses = new Collection<string>(),
						Folder = postManFolder.Id
					};

					postManFolder.Order.Add(postmanRequest.Id); // add to the folder
					postManCollection.Requests.Add(postmanRequest);
				}

				postManCollection.Folders.Add(postManFolder);
			}

			return postManCollection;
		}

		private string ToMarkdown(ApiDescription apiDescription)
		{
			var doc = JObject.Parse(apiDescription.Documentation);

			var summary = doc.Value<string>("summary");
			var remarks = doc.Value<string>("remarks");
			var returns = doc.Value<string>("returns");
			var authorize = doc.Value<bool>("authorize");
			var oDataQuery = doc.Value<bool>("oDataQuery");
			var authorizedUsers = doc.Value<string>("authorizeUsers");
			var authorizedRoles = doc.Value<string>("authorizeRoles");
			var obsolete = doc.Value<bool>("obsolete");
			var obsoleteMessage = doc.Value<string>("obsoleteMessage");

			var response = apiDescription.ResponseDescription;
			var responseType = response.ResponseType ?? response.DeclaredType;

			var dw = new DocumentationWriter();

			dw.WriteSummary(summary);
			dw.WriteRemarks(remarks);
			dw.WriteParameters(apiDescription);
			dw.WriteReturns(returns, responseType.GetFriendlyTypeName());
			dw.WriteAuthorization(authorize, authorizedUsers, authorizedRoles);
			dw.WriteObsolete(obsolete, obsoleteMessage);
			dw.WriteODataQuery(oDataQuery);

			return dw.Writer.ToString();
		}
	}

	/// <summary>
	/// Quick comparer for ordering http methods for display
	/// </summary>
	internal class HttpMethodComparator : IComparer<HttpMethod>
	{
		private readonly string[] _order =
		{
			"GET",
			"POST",
			"PUT",
			"DELETE"
		};

		public int Compare(HttpMethod x, HttpMethod y)
		{
			return Array.IndexOf(this._order, x.ToString()).CompareTo(Array.IndexOf(this._order, y.ToString()));
		}
	}
}
