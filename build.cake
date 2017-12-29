
// Load external file and tools
#tool "nuget:?package=GitVersion.CommandLine"

//Read script parameter
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");

//Declare variable
GitVersion versionInfo;
string NuGetUrl = "http://proget/nuget/Nuget/";
var csProjPathArray =  GetFiles("./**/*.csproj");

//--source will use only your specified source, and override any sources in your NuGet.config
//--fallbacksource will append your specified source with what is in NuGet.config
var NuGetRestoreSource = new DotNetCoreRestoreSettings{
        Sources = new[] { NuGetUrl },
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
    .IsDependentOn("PackageNuget")
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

        Information(versionInfo.FullSemVer);

        XmlPoke(file.FullPath, "/Project/PropertyGroup/Version", versionInfo.FullSemVer.ToString());
        XmlPoke(file.FullPath, "/Project/PropertyGroup/FileVersion", versionInfo.AssemblySemVer.ToString());
         XmlPoke(file.FullPath, "/Project/PropertyGroup/PackageVersion", versionInfo.NuGetVersionV2.ToString());


        DotNetCoreBuild(
            file.FullPath,
            new DotNetCoreBuildSettings{
                Configuration = configuration,
                //ArgumentCustomization = args => args.Append("--no-restore /p:Version=" + versionInfo.FullSemVer + " /p:FileVersion=" + versionInfo.AssemblySemVer )
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
     .Does(() => {   
        DotNetCoreRestore(".", new DotNetCoreRestoreSettings {
          
        });
     });

Task("Clean")
    .DoesForEach(csProjPathArray, (file) => {
   
        DotNetCoreClean(file.FullPath);
        CleanDirectory("./artifacts/");
    });

Task("PackageNuget")
     .DoesForEach(csProjPathArray, (file) => {
   
        DotNetCorePack(file.FullPath,new DotNetCorePackSettings{
           Configuration = configuration,
           OutputDirectory = "./artifacts/",
           NoBuild = true
           //ArgumentCustomization = args => args.Append(" /p:PackageVersion=" + versionInfo.NuGetVersionV2 )
          
        });
    }).DeferOnError();




RunTarget(target);