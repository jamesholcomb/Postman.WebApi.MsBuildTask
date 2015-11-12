using NUnit.Framework;
using FluentAssertions;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System;
using Moq;
using Microsoft.Build.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using Microsoft.Build.Logging;
using Microsoft.Build.Evaluation;
using System.Linq;

namespace Postman.WebApi.MsBuildTask.Tests
{
	[TestFixture]
	public class GeneratorTests
	{
		static int _folderCount = 2;
		static int _requestCount = 6;

		[Test]
		public void creates_postman_collection()
		{
			// Arrange
			var codeBase = new Uri(Assembly.GetExecutingAssembly().CodeBase);
			var assemblyFilePath = codeBase.LocalPath;

			var generator = new CollectionGenerator();

			// Act
			var actual = generator.Create(assemblyFilePath, Resources.DefaultEnvironmentKey, Resources.DefaultRouteTemplate);

			// Assert
			var jsonSerializer = new JsonSerializer() { ContractResolver = new CamelCasePropertyNamesContractResolver() };
			Debug.WriteLine(JObject.FromObject(actual, jsonSerializer).ToString());

			actual.Should().NotBeNull();
			actual.Requests.Count.Should().Be(_requestCount);
			actual.Folders.Count.Should().Be(_folderCount);
			actual.Description.Should().NotBeNull();
		}

		[Test]
		public void executes_postman_generatetask()
		{
			// Arrange
			var mock = new Mock<IBuildEngine>();
			var codeBase = new Uri(Assembly.GetExecutingAssembly().CodeBase);
			var assemblyFilePath = codeBase.LocalPath;

			var task = new GenerateTask
			{
				AssemblyFilePath = assemblyFilePath,
				OutputDirectory = @".\",
				BuildEngine = mock.Object
			};

			File.Delete(task.OutputFilePath);

			// Act
			var actual = task.Execute();

			// Assert
			actual.Should().BeTrue();
			File.Exists(task.OutputFilePath).Should().BeTrue();

			AssertOutputFile(task.OutputFilePath);
		}

		[Test]
		public void throws_filenotfoundexception_when_no_xml_doc_file_found()
		{
			// Arrange
			var codeBase = new Uri(Assembly.GetExecutingAssembly().CodeBase);
			var assemblyFilePath = codeBase.LocalPath;

			var mock = new Mock<IBuildEngine>();
			GenerateTask task = new GenerateTask
			{
				AssemblyFilePath = assemblyFilePath,
				OutputDirectory = @".\",
				BuildEngine = mock.Object
			};

			var xmlFile = task.AssemblyFilePath.Replace(Resources.DllFileExtension, Resources.XmlFileExtension);
			var tmpFile = @"tmp.xml";
			File.Move(xmlFile, tmpFile);

			// Act/Assert
			Assert.That(() => task.Execute(), Throws.TypeOf<FileNotFoundException>());
			File.Move(tmpFile, xmlFile);
		}

		[Test]
		public void throws_filenotfoundexception_when_no_assembly_file_found()
		{
			// Arrange
			var mock = new Mock<IBuildEngine>();
			GenerateTask task = new GenerateTask
			{
				AssemblyFilePath = "c:\badpath\file.dll",
				OutputDirectory = @".\",
				BuildEngine = mock.Object
			};

			// Act/Assert
			Assert.That(() => task.Execute(), Throws.TypeOf<FileNotFoundException>());
		}

		[Test]
		public void throws_argumentnullexception_when_assemblyfilepath_unspecified()
		{
			// Arrange
			var mock = new Mock<IBuildEngine>();
			GenerateTask task = new GenerateTask
			{
				BuildEngine = mock.Object
			};

			// Act/Assert
			Assert.That(() => task.Execute(), Throws.TypeOf<ArgumentNullException>());
		}

		[Test]
		public void executes_msbuild_postman_generatetask()
		{
			// arrange
			var loggers = new List<ILogger>();
			loggers.Add(new ConsoleLogger());

			var projectCollection = new ProjectCollection();
			projectCollection.RegisterLoggers(loggers);

			var projectDir = Path.GetDirectoryName(Path.GetDirectoryName(TestContext.CurrentContext.TestDirectory));
			var assemblyFile = Path.Combine(projectDir, @"bin\debug\Postman.WebApi.MsBuildTask.dll");
			var projectFile = Path.Combine(projectDir, @"Postman.WebApi.MsBuildTask.Tests.csproj");
			var task = @"Postman.WebApi.MsBuildTask.GenerateTask";

			var project = projectCollection.LoadProject(projectFile);

			project.Xml.AddUsingTask(task, assemblyFile, string.Empty);
			var target = project.Xml.AddTarget("AfterBuild");
			var el = target.AddTask(task);
			el.SetParameter("AssemblyFilePath", "$(TargetPath)");
			el.SetParameter("EnvironmentKey", "myhost");
			el.SetParameter("RouteTemplate", "myapi/v2/{controller}");
			el.SetParameter("OutputDirectory", Path.GetTempPath());

			// act
			var actual = project.Build("AfterBuild");

			// assert
			actual.Should().BeTrue();
			AssertOutputFile(Path.Combine(Path.GetTempPath(), @"Postman.WebApi.MsBuildTask.Tests.postman.json"));
		}

		private void AssertOutputFile(string file)
		{
			File.Exists(file).Should().BeTrue();

			var s = File.OpenText(file).ReadToEnd();

			Debug.WriteLine(s);

			var json = JObject.Parse(s);

			json["folders"].Children().Count().Should().Be(_folderCount);
			json["requests"].Children().Count().Should().Be(_requestCount);
		}
	}
}