using Conditions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Postman.WebApi.MsBuildTask
{
	using Models;

	/// <summary>
	/// The Generate MSBuild Task
	/// </summary>
	/// <remarks>
	/// Assumes the .xml file is of the same name and in the same directory as the target assembly
	/// </remarks>
	[LoadInSeparateAppDomain]
	[Serializable]
	public class GenerateTask : AppDomainIsolatedTask
	{
		static GenerateTask()
		{
			// When executed as a task by MSBuild.exe, the configuration file is msbuild.exe.config
			// Manually redirect assembly bindings in case the host project has dependencies on differing versions
			var configFile = Assembly.GetExecutingAssembly().Location + Resources.ConfigFileExtension;

			var appConfig = new AppConfig();
			appConfig.Load(configFile);

			foreach (var assembly in appConfig.Runtime.DependentAssemblies)
			{
				RedirectAssembly(assembly.PartialAssemblyName.Name,
					assembly.BindingRedirects[0].NewVersion,
					assembly.PartialAssemblyName.GetPublicKeyToken().ToStringBase16());
			}
		}

		/// <summary>Gets or sets the assembly file path.</summary>
		/// <value>The assembly file path.</value>
		public string AssemblyFilePath { get; set; }

		/// <summary>Gets or sets the output directory.</summary>
		/// <value>The output path.</value>
		public string OutputDirectory { get; set; }

		/// <summary>Gets or sets the Postman environment key.</summary>
		/// <value>The environment key.</value>
		public string EnvironmentKey { get; set; } = Resources.DefaultEnvironmentKey;

		/// <summary>Gets or sets the http route template.</summary>
		/// <value>The route template.</value>
		public string RouteTemplate { get; set; } = Resources.DefaultRouteTemplate;

		public string OutputFileExtension { get; } = Resources.OutputFileExtension;
		public string OutputFilePath { get { return Path.Combine(OutputDirectory, AssemblyName + OutputFileExtension); } }
		public string AssemblyName { get { return Path.GetFileNameWithoutExtension(AssemblyFilePath); } }
		public string AssemblyDirectory { get { return Path.GetDirectoryName(AssemblyFilePath); } }

		private readonly string _messagePrefix = typeof(GenerateTask).Namespace + ": ";

		/// <summary>
		/// Adds an AssemblyResolve handler to redirect all failed assembly load attempts
		/// </summary>
		/// <remarks>
		/// http://blog.slaks.net/2013-12-25/redirecting-assembly-loads-at-runtime/
		/// </remarks>
		/// <param name="shortName">The short name.</param>
		/// <param name="targetVersion">The target version.</param>
		/// <param name="publicKeyToken">The public key token.</param>
		public static void RedirectAssembly(string shortName, Version targetVersion, string publicKeyToken)
		{
			ResolveEventHandler handler = null;

			handler = (sender, args) =>
			{
				// Use latest strong name & version when trying to load SDK assemblies
				var requestedAssembly = new AssemblyName(args.Name);
				if (requestedAssembly.Name != shortName)
				{
					return null;
				}

				requestedAssembly.Version = targetVersion;
				requestedAssembly.SetPublicKeyToken(new AssemblyName("x, PublicKeyToken=" + publicKeyToken).GetPublicKeyToken());
				requestedAssembly.CultureInfo = CultureInfo.InvariantCulture;

				AppDomain.CurrentDomain.AssemblyResolve -= handler;

				var assembly = Assembly.Load(requestedAssembly);

				return assembly;
			};

			AppDomain.CurrentDomain.AssemblyResolve += handler;
		}

		/// <summary>
		/// Builds a Postman collection from an assembly XML documentation file and writes the result to a json file.
		/// </summary>
		/// <returns></returns>
		public override bool Execute()
		{
			Condition.Requires(AssemblyFilePath).IsNotNullOrEmpty();
			Condition.Requires(OutputDirectory).IsNotNullOrEmpty();
			Condition.Requires(EnvironmentKey).IsNotNullOrEmpty();

			LogMessage(Resources.GeneratingPostmanCollection, AssemblyFilePath);

			var generator = new CollectionGenerator();
			var collection = generator.Create(AssemblyFilePath, EnvironmentKey, RouteTemplate);

			if (!collection.Folders.Any())
			{
				LogWarning(Resources.NoApiControllerClassesFound);
			}

			WriteFile(collection, OutputFilePath);

			LogMessage(Resources.PostmanCollectionCreated, OutputFilePath);

			return true;
		}

		private void LogMessage(string message, params object[] args)
		{
			Log.LogMessage(MessageImportance.High, _messagePrefix + message, args);
		}

		private void LogWarning(string message)
		{
			Log.LogWarning(_messagePrefix + message);
		}

		private void WriteFile(PostmanCollection collection, string targetFilename)
		{
			var jsonSerializer = new JsonSerializer() { ContractResolver = new CamelCasePropertyNamesContractResolver() };
			var json = JObject.FromObject(collection, jsonSerializer);

			File.WriteAllText(targetFilename, json.ToString());
		}

		private void LogError(Exception ex)
		{
			Log.LogErrorFromException(ex);
		}
	}
}