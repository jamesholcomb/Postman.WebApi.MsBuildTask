#Postman.WebApi.MsBuildTask
[![Nuget version](https://img.shields.io/nuget/v/Postman.WebApi.MsBuildTask.svg)](https://www.nuget.org/packages/Postman.WebApi.MsBuildTask)
[![Build status](https://ci.appveyor.com/api/projects/status/fbbjue07o913v0n7/branch/master?svg=true)](https://ci.appveyor.com/project/jamesholcomb/postman-webapi-msbuildtask/branch/master)
####What is it?
An MSBuild [task](https://msdn.microsoft.com/en-us/library/t9883dzc.aspx) to automatically generate documented [Postman 3](http://www.getpostman.com) collections from your ApiController derived classes
####What does it do?
MSBuild executes `Postman.WebApi.MsBuildTask.Generator` as an _AfterBuild_ task to create a JSON Postman collection file containing folders (ApiControllers) and requests (HTTP verbs) within a project.  It utilizes [standard .NET documentation](http://msdn.microsoft.com/en-us/library/5ast78ax.aspx) comments and attributes to document your Postman requests.

* Runs automatically after project build so your APIs and test requests stay in sync
* No run time dependency (only compile time)
* Won't muck up your project with MVC or `Microsoft.AspNet.WebApi.HelpPage` dependencies
* Documents`ResponseType` attributes so you can use `IHttpAction` responses
* Documents `Authorize` and `Obsolete` attributes
* Documents OData `EnableQuery` attribute
* You can use tags like `<c>`, `<code>`, `<para>`, `<example>`, `<exception>`, `<paramref>`, `<see>`, `<seealso>`, `<typeparamref>`

####How do I use it?
1. Install the [NuGet package](https://www.nuget.org/packages/Postman.WebApi.MsBuildTask)
1. Make sure you have checked the "XML Documentation file" checkbox in the build properties for the project containing your ApiControllers
1. Build your project to create the `<AssemblyName>.postman.json` file in your project root
1. Open Postman and import the collection file
1. Add a new Postman [environment](https://www.getpostman.com/docs/environments) and set _key_ to `url` and _value_ to your http api endpoint root e.g. `http://localhost:3000/myapp`

####How do I configure it?
You can override the default configuration by modifying the `.targets` file located in the `packages` directory.  __Note that removing/upgrading the package will set the configuration back to the defaults.__

| Option | Description | Default | Required |
| ------ | ----------- | ------- | -------- |
| `OutputDirectory`   | The directory to write the output Postman .json file | `$(ProjectDir)` | Yes
| `AssemblyFilePath` | The complete path and file name to the project target assembly | `$(TargetPath)` | Yes
| `EnvironmentKey`    | The Postman [environment](https://www.getpostman.com/docs/environments) key variable   | `url` | No
| `RouteTemplate` | The HTTP route template used to setup route maps | `api/{controller}` | No

#### Can I contribute?
Please do!  PRs are welcome.  Just follow the conventions in the source code.

#### Usage notes
1. The XML documentation file must be located in the same folder as the project assembly target _and_ have the same name.  For example, if your project assembly is called `myproject.dll`, the XML documentation file must be named `myproject.xml` and be located in the same directory (e.g. `bin\debug`).
1. Errors, warnings and messages from the Postman build task can be viewed in the Output | Build window and the build Error List
1. Only Postman version 3 is supported at this time
1. There are some limitations with Postman markdown that prevent the use of certain Markdown (tables, icons and inline html).

[1]: Thanks to @azzlack for the source to his [XmlDocumentationProvider](https://github.com/azzlack/Microsoft.AspNet.WebApi.HelpPage.Ex)

Please open an issue if you have any problems or questions.
