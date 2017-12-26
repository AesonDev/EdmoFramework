
#load ./Config.cs


var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");

var IsLocalbuild = Argument("IsLocalbuild", "true");


// Get the solution file in a generic way. The .sln file containes the dependencies ans so the order to vuild the projects
 var solutionFilePath = GetFiles("../**/*.sln").FirstOrDefault().ToString();
 var csProjPathArray =  GetFiles("../**/*.csproj");


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
    .IsDependentOn("Restore")
    .IsDependentOn("IncrementBuildNumber")
    .Does(() =>
{
   
    DotNetCoreBuild(solutionFilePath,
     new DotNetCoreBuildSettings{
       Configuration = configuration,
       //Since .Net Core CLI version 2, dotnet build also do a restore by default. But we already did a restore
       //Dotnet build use the default package source to do the restore. 
       //Therefore we do a dotnet restore in a different task with the desired source.
       //https://github.com/cake-build/cake/issues/1836
       ArgumentCustomization = args => args.Append("--no-restore")
       
    });
});

Task("IncrementBuildNumber") 
.DoesForEach(csProjPathArray, (file) => 
{
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
    .Does(() =>
{
    DotNetCoreRestore("../");
   
    //NuGetRestoreSource);
});

Task("Clean")
    .Does(() =>
{
    DotNetCoreClean(solutionFilePath);
});

RunTarget(target);