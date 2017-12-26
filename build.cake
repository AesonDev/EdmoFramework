
// Load external file and tools
#load ./Config.cs
#tool "nuget:?package=GitVersion.CommandLine"

//Read script parameter
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");

//Declare variable
GitVersion versionInfo;

// Get the solution file in a generic way. The .sln file containes the dependencies ans so the order to vuild the projects
 var solutionFilePath = GetFiles("../**/*.sln").FirstOrDefault().ToString();
 var csProjPathArray =  GetFiles("./**/*.csproj");

//--source will use only your specified source, and override any sources in your NuGet.config
//--fallbacksource will append your specified source with what is in NuGet.config
var NuGetRestoreSource = new DotNetCoreRestoreSettings{
        Sources = new[] { MyConfig.NuGetUrl},
        Verbosity = DotNetCoreVerbosity.Normal       
     };

Setup(ctx =>
    {
    // Executed BEFORE the first task.
    Information("Running tasks...");
    });


Teardown(ctx =>{
        // Executed AFTER the last task.
        Information("Finished running tasks.");
    });

Task("Default")
    .IsDependentOn("Build")
    .Does(() =>{
         Information("Running Build...");    
    });

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .DoesForEach(csProjPathArray, (file) => { 

        versionInfo = GitVersion(
            new GitVersionSettings {
                RepositoryPath = ".",
                UpdateAssemblyInfo = true 
            }
        );

        DotNetCoreBuild(
            file.FullPath,
            new DotNetCoreBuildSettings{
                Configuration = configuration,
                ArgumentCustomization = args => args.Append("--no-restore /p:SemVer=" + versionInfo.NuGetVersionV2)
            }
        );
    });


Task("IncrementBuildNumber")
    .DoesForEach(csProjPathArray, (file) => {
        Information("Incrementing build version on project : " + file.FullPath);  
        var currentStr = XmlPeek(File(file.FullPath),  "/Project/PropertyGroup/BuildNumber/text()");
        Information("Previous BuildNumber : " + currentStr);
        var version = Int32.Parse(currentStr);
        version ++;
        Information("New BuildNumber : " + version);
        XmlPoke(file.FullPath, "/Project/PropertyGroup/BuildNumber", version.ToString());

    }).DeferOnError();

Task("Restore")   
    .IsDependentOn("Clean")
    .Does(() =>{
      DotNetCoreRestore(".");
    });

Task("Clean")
    .DoesForEach(csProjPathArray, (file) => {
   
        DotNetCoreClean(file.FullPath);
    });

RunTarget(target);