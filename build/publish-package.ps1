param(
	[Parameter(Mandatory=$true)]
	[string]$nugetApiKey
)

function TestRelease(){
	param(
		[Parameter(Mandatory=$true)]
		[int]$promMajorVer
	) 
	
	echo "Running tests for v$promMajorVer.."
	dotnet test ..\src\prometheus-net.DotNetRuntime.Tests -c "ReleaseV$promMajorVer"
	
	if ($LastExitCode -ne 0){
		throw "Tests failed for V$promMajorVer, exiting.."
	}
}

function BuildAndPublishRelease(){
	param(
		[Parameter(Mandatory=$true)]
		[int]$promMajorVer
	) 
	
	echo "Packing and publishing for v$promMajorVer.."
	rm "$PSScriptRoot\nupkgs\*.*" 

	dotnet pack ..\src\prometheus-net.DotNetRuntime --include-symbols -c "ReleaseV$promMajorVer" --output "$PSScriptRoot\nupkgs"

	if ($LastExitCode -ne 0){
		throw "Creating nuget package for V$promMajorVer failed, exiting.."
	}

	dotnet nuget push "$PSScriptRoot\nupkgs\prometheus-net.DotNetRuntime.$promMajorVer.*.symbols.nupkg" -k $nugetApiKey -s https://api.nuget.org/v3/index.json 

	if ($LastExitCode -ne 0){
		throw "pushing nuget package for V$promMajorVer failed, exiting.."
	}
}

	
# Ensure tests are green for both versions before we continue
TestRelease(2)
TestRelease(3)

# Build and release each version
BuildAndPublishRelease(2)
BuildAndPublishRelease(3) 

