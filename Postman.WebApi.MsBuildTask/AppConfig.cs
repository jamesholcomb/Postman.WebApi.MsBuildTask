// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Globalization;
using System.Reflection;
using Conditions;

namespace Postman.WebApi.MsBuildTask
{
	/// <summary>
	/// Represents a single &lt;bindingRedirect&gt; from the app.config file.
	/// </summary>
	internal sealed class BindingRedirect
	{
		/// <summary>
		/// The low end of the old version range.
		/// </summary>
		private Version _oldVersionLow = null;

		/// <summary>
		/// The high end of the old version range.
		/// </summary>
		private Version _oldVersionHigh = null;

		/// <summary>
		/// The new version number.
		/// </summary>
		private Version _newVersion = null;

		/// <summary>
		/// The low end of the old version range.
		/// </summary>
		internal Version OldVersionLow
		{
			set { _oldVersionLow = value; }
			get { return _oldVersionLow; }
		}

		/// <summary>
		/// The high end of the old version range.
		/// </summary>
		internal Version OldVersionHigh
		{
			set { _oldVersionHigh = value; }
			get { return _oldVersionHigh; }
		}

		/// <summary>
		/// The new version number.
		/// </summary>
		internal Version NewVersion
		{
			set { _newVersion = value; }
			get { return _newVersion; }
		}

		/// <summary>
		/// The reader is positioned on a &lt;bindingRedirect&gt; element--read it.
		/// </summary>
		/// <param name="reader"></param>
		internal void Read(XmlTextReader reader)
		{
			string oldVersion = reader.GetAttribute("oldVersion");

			// A badly formed assembly name.
			Condition.Requires(oldVersion).IsNotNullOrEmpty("AppConfig.BindingRedirectMissingOldVersion");

			int dashPosition = oldVersion.IndexOf('-');

				if (dashPosition != -1)
				{
					// This is a version range.
					_oldVersionLow = new Version(oldVersion.Substring(0, dashPosition));
					_oldVersionHigh = new Version(oldVersion.Substring(dashPosition + 1));
				}
				else
				{
					// This is a single version.
					_oldVersionLow = new Version(oldVersion);
					_oldVersionHigh = new Version(oldVersion);
				}
			string newVersionAttribute = reader.GetAttribute("newVersion");

			// A badly formed assembly name.
			Condition.Requires(newVersionAttribute).IsNotNullOrEmpty("AppConfig.BindingRedirectMissingNewVersion");

				_newVersion = new Version(newVersionAttribute);
		}
	}
	/// <summary>
	/// Represents a single &lt;dependentassembly&gt; from the app.config file.
	/// </summary>
	internal sealed class DependentAssembly
	{
		/// <summary>
		/// List of binding redirects. Type is BindingRedirect.
		/// </summary>
		private BindingRedirect[] _bindingRedirects = null;

		/// <summary>
		/// The partial assemblyname, there should be no version.
		/// </summary>
		private AssemblyName _partialAssemblyName = null;

		/// <summary>
		/// The partial assemblyname, there should be no version.
		/// </summary>
		internal AssemblyName PartialAssemblyName
		{
			set
			{
				_partialAssemblyName = (AssemblyName)value.Clone();
				_partialAssemblyName.Version = null;
			}
			get
			{
				if (_partialAssemblyName == null)
				{
					return null;
				}
				return (AssemblyName)_partialAssemblyName.Clone();
			}
		}

		/// <summary>
		/// The reader is positioned on a &lt;dependentassembly&gt; element--read it.
		/// </summary>
		/// <param name="reader"></param>
		internal void Read(XmlTextReader reader)
		{
			ArrayList redirects = new ArrayList();

			if (_bindingRedirects != null)
			{
				redirects.AddRange(_bindingRedirects);
			}

			while (reader.Read())
			{
				// Look for the end element.
				if (reader.NodeType == XmlNodeType.EndElement && AppConfig.StringEquals(reader.Name, "dependentassembly"))
				{
					break;
				}

				// Look for a <assemblyIdentity> element
				if (reader.NodeType == XmlNodeType.Element && AppConfig.StringEquals(reader.Name, "assemblyIdentity"))
				{
					string name = null;
					string publicKeyToken = "null";
					string culture = "neutral";

					// App.config seems to have mixed case attributes.
					while (reader.MoveToNextAttribute())
					{
						if (AppConfig.StringEquals(reader.Name, "name"))
						{
							name = reader.Value;
						}
						else
						if (AppConfig.StringEquals(reader.Name, "publicKeyToken"))
						{
							publicKeyToken = reader.Value;
						}
						else
						if (AppConfig.StringEquals(reader.Name, "culture"))
						{
							culture = reader.Value;
						}
					}

					string assemblyName = String.Format
					(
						 CultureInfo.InvariantCulture,
						 "{0}, Version=0.0.0.0, Culture={1}, PublicKeyToken={2}",
						 name,
						 culture,
						 publicKeyToken
					);

					_partialAssemblyName = new AssemblyName(assemblyName);
				}

				// Look for a <bindingRedirect> element.
				if (reader.NodeType == XmlNodeType.Element && AppConfig.StringEquals(reader.Name, "bindingRedirect"))
				{
					BindingRedirect bindingRedirect = new BindingRedirect();
					bindingRedirect.Read(reader);
					redirects.Add(bindingRedirect);
				}
			}
			_bindingRedirects = (BindingRedirect[])redirects.ToArray(typeof(BindingRedirect));
		}

		/// <summary>
		/// The binding redirects.
		/// </summary>
		/// <value></value>
		internal BindingRedirect[] BindingRedirects
		{
			set { _bindingRedirects = value; }
			get { return _bindingRedirects; }
		}
	}
	/// <summary>
	/// Wraps the &lt;runtime&gt; section of the .config file.
	/// </summary>
	internal sealed class RuntimeSection
	{
		/// <summary>
		/// List of dependent assemblies. Type is DependentAssembly.
		/// </summary>
		private ArrayList _dependentAssemblies = new ArrayList();

		/// <summary>
		/// The reader is positioned on a &lt;runtime&gt; element--read it.
		/// </summary>
		/// <param name="reader"></param>
		internal void Read(XmlTextReader reader)
		{
			while (reader.Read())
			{
				// Look for the end element.
				if (reader.NodeType == XmlNodeType.EndElement && AppConfig.StringEquals(reader.Name, "runtime"))
				{
					return;
				}

				// Look for a <dependentAssembly> element
				if (reader.NodeType == XmlNodeType.Element && AppConfig.StringEquals(reader.Name, "dependentAssembly"))
				{
					DependentAssembly dependentAssembly = new DependentAssembly();
					dependentAssembly.Read(reader);

					// Only add if there was an <assemblyIdentity> tag.
					// Otherwise, this section is no use.
					if (dependentAssembly.PartialAssemblyName != null)
					{
						_dependentAssemblies.Add(dependentAssembly);
					}
				}
			}
		}

		/// <summary>
		/// Return the collection of dependent assemblies for this runtime element.
		/// </summary>
		/// <value></value>
		internal DependentAssembly[] DependentAssemblies
		{
			get { return (DependentAssembly[])_dependentAssemblies.ToArray(typeof(DependentAssembly)); }
		}
	}
	// Copyright (c) Microsoft. All rights reserved.
	// Licensed under the MIT license. See LICENSE file in the project root for full license information.

	/// <summary>
	/// An exception thrown while parsing through an app.config.
	/// </summary>
	[Serializable]
	internal class AppConfigException : System.ApplicationException
	{
		/// <summary>
		/// The name of the app.config file.
		/// </summary>
		private string fileName = String.Empty;
		internal string FileName
		{
			get
			{
				return fileName;
			}
		}


		/// <summary>
		/// The line number with the error. Is initialized to zero
		/// </summary>
		private int line;
		internal int Line
		{
			get
			{
				return line;
			}
		}

		/// <summary>
		/// The column with the error. Is initialized to zero
		/// </summary>
		private int column;
		internal int Column
		{
			get
			{
				return column;
			}
		}


		/// <summary>
		/// Construct the exception.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="fileName"></param>
		/// <param name="line"></param>
		/// <param name="column"></param>
		/// <param name="inner"></param>
		public AppConfigException(string message, string fileName, int line, int column, System.Exception inner) : base(message, inner)
		{
			this.fileName = fileName;
			this.line = line;
			this.column = column;
		}

	}

	/// <summary>
	/// Read information from application .config files.
	/// </summary>
	internal sealed class AppConfig
	{
		/// <summary>
		/// Corresponds to the contents of the &lt;runtime&gt; element.
		/// </summary>
		private RuntimeSection _runtime = new RuntimeSection();

		/// <summary>
		/// Read the .config from a file.
		/// </summary>
		/// <param name="appConfigFile"></param>
		internal void Load(string appConfigFile)
		{
			XmlTextReader reader = null;
			try
			{
				reader = new XmlTextReader(appConfigFile);
				reader.DtdProcessing = DtdProcessing.Ignore;
				Read(reader);
			}
			catch (XmlException e)
			{
				throw new AppConfigException(e.Message, appConfigFile, (reader != null ? reader.LineNumber : 0), (reader != null ? reader.LinePosition : 0), e);
			}
			catch (Exception e) // Catching Exception, but rethrowing unless it's an IO related exception.
			{
				throw new AppConfigException(e.Message, appConfigFile, (reader != null ? reader.LineNumber : 0), (reader != null ? reader.LinePosition : 0), e);
			}
			finally
			{
				if (reader != null) reader.Close();
			}
		}

		/// <summary>
		/// Read the .config from an XmlReader
		/// </summary>
		/// <param name="reader"></param>
		internal void Read(XmlTextReader reader)
		{
			// Read the app.config XML
			while (reader.Read())
			{
				// Look for the <runtime> section
				if (reader.NodeType == XmlNodeType.Element && StringEquals(reader.Name, "runtime"))
				{
					_runtime.Read(reader);
				}
			}
		}

		/// <summary>
		/// Access the Runtime section of the application .config file.
		/// </summary>
		/// <value></value>
		internal RuntimeSection Runtime
		{
			get { return _runtime; }
		}

		/// <summary>
		/// App.config files seem to come with mixed casing for element and attribute names.
		/// If the fusion loader can handle this then this code should too.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		static internal bool StringEquals(string a, string b)
		{
			return String.Compare(a, b, StringComparison.OrdinalIgnoreCase) == 0;
		}
	}
}