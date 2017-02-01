//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////
#tool "nuget:?package=xunit.runner.console"
#tool "nuget:?package=OpenCover"
#tool "nuget:?package=ReportGenerator"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument<string>("target", "Default");
var configuration = Argument<string>("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories
var buildDir = Directory("./AnotherCakeExampleApp/bin") + Directory(configuration);
var testCoverageReportDir = Directory("./test-coverage-report");
var testResultsDir = Directory("./test-results");
var assemblyInfoPath = File("./AnotherCakeExampleApp/Properties/AssemblyInfo.cs");

//////////////////////////////////////////////////////////////////////
// HELPERS
//////////////////////////////////////////////////////////////////////

private void GenerateAssemblyInfo(string assemblyVersion, string semanticVersion)
{
    CreateAssemblyInfo(assemblyInfoPath,
        new AssemblyInfoSettings {
            Product = "AnotherCakeExampleApp",
            Title = "Another Cake Demo Application",
            Company = "Some Company",
            Copyright = string.Format("Copyright (c) Alexey Solomatin 2017 - {0}", DateTime.Now.Year),
            Version = assemblyVersion,            
            FileVersion = assemblyVersion,
            InformationalVersion = semanticVersion,                        
            Guid = "7ffdf4f3-1dac-4bbd-8976-22e7a118f922",
            ComVisible = false
    });
}

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Bump-Version")
    .Does(() => 
{
    if (!HasArgument("assemblyVersion"))
    {
        throw new Exception("The assembly version is not specified.");
    }
    var assemblyVersion = Argument<string>("assemblyVersion");
    if (!System.Text.RegularExpressions.Regex.IsMatch(assemblyVersion, @"^(\d+).(\d+).(\d|).(\d+)$")) 
    {
        throw new Exception("The assembly version is in a wrong format.");
    }
    var semanticVersion = HasArgument("semanticVersion") 
        ? Argument<string>("semanticVersion")
        : assemblyVersion;
    GenerateAssemblyInfo(assemblyVersion, semanticVersion);
});

Task("Clean")
    .Does(() =>
{
    CleanDirectory(buildDir);
	CleanDirectory(testResultsDir);
});

Task("Clean-Test-Coverage-Report")
    .Does(() =>
{    
	CleanDirectory(testCoverageReportDir);
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{        
    NuGetRestore("./AnotherCakeExampleApp.sln");
});

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")    
    .Does(() =>
{
    if (IsRunningOnWindows())
    {
      // Use MSBuild
      MSBuild("./AnotherCakeExampleApp.sln", settings =>
        settings.SetConfiguration(configuration));
    }
    else
    {
      // Use XBuild
      XBuild("./AnotherCakeExampleApp.sln", settings =>
        settings.SetConfiguration(configuration));
    }
});

Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .Does(() =>
{   
    XUnit2("./**/bin/" + configuration + "/*.Tests.dll",
        new XUnit2Settings {
            Parallelism = ParallelismOption.All,
            HtmlReport = true,
            XmlReport = true,
            NoAppDomain = true,
            OutputDirectory = testResultsDir
    });     
});

Task("Calculate-Test-Coverage")
    .IsDependentOn("Clean-Test-Coverage-Report")
    .IsDependentOn("Build")
    .Does(() =>
{
    if (IsRunningOnWindows()) 
    {
        OpenCover(tool => {
        tool.XUnit2("./**/bin/" + configuration + "/*.Tests.dll",
            new XUnit2Settings {
                Parallelism = ParallelismOption.All,
                HtmlReport = true,
                XmlReport = true,
                NoAppDomain = true,
                OutputDirectory = testResultsDir
            });
        },
        testCoverageReportDir + File("test-coverage-results.xml"),
        new OpenCoverSettings()
            .WithFilter("+[AnotherCakeExampleApp]*")
            .WithFilter("-[*.Tests]*"));
        ReportGenerator(testCoverageReportDir + File("test-coverage-results.xml"), testCoverageReportDir);
    }
    else
    {
        Information("Cannot use OpenCover on OS that is not MS Windows.");
    }
});

// TODO add signing 
// TODO demonstrate using environment variables
// TODO use GitReleaseManager
// TODO use GitReleaseNotes
// TODO investigate other possibilities of integration with Git
// TODO demostrate HTTP operations
// TODO should be used Octopus Deploy?
// TODO parse and display Release Notes
// TODO use ReSharper
// TODO publish NuGet package
// TODO create installer using Wix?

Task("Usage-Information")
    .Does(() =>
{
    Information("Cake script for building the Content Service.");
    Information("Usage:");
    Information("powershell -ExecutionPolicy ByPass -File build.ps1 -target Run-Unit-Tests|Perform-Release [-configuration Debug|Release] [-assemblyVersion\"\"\"<ASSEMBLY_VERSION>\"\"\"] [-semanticVersion\"\"\"<SEMANTIC_VERSION>\"\"\"]");
    Information("Switches:");
    Information("-target                      Mandatory, specifies the build target for Cake: Run-Unit-Tests - for continuous integration, Release - for releasing.");
    Information("-configuration               Optional, specifies the build configuration for MSBuild, defaults to Release.");    
    Information("-assemblyVersion=\"\"\"...\"\"\"   Optional, if it's specified, the SharedAssemblyVersion.cs wiil be updated with this version before building the solution.");    
    throw new Exception("In order to build the solution please specify the correct target and other parameters in case of need.");
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Usage-Information");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
