param(
	[Parameter(Mandatory=$true)]
	[string]$nugetApiKey
) 

# ensure tests are green
dotnet test ..\src\prometheus-net.DotNetRuntime.Tests

if ($LastExitCode -ne 0){
	throw "Tests failed, exiting.."
}

rm "$PSScriptRoot\nupkgs\*.*" 

dotnet pack ..\src\prometheus-net.DotNetRuntime --include-symbols -c Release --output "$PSScriptRoot\nupkgs"

if ($LastExitCode -ne 0){
	throw "Creating nuget package failed, exiting.."
}

dotnet nuget push "$PSScriptRoot\nupkgs\prometheus-net.DotNetRuntime.*.symbols.nupkg" -k $nugetApiKey -s https://api.nuget.org/v3/index.json 

if ($LastExitCode -ne 0){
	throw "pushing nuget package failed, exiting.."
}