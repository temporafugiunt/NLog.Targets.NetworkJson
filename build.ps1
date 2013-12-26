properties {
    $ProductName = "Gelf4NLog.Target"
    $BaseDir = Resolve-Path "."
    $SolutionFile = "$BaseDir\Gelf4NLog.sln"
    $OutputDir = "$BaseDir\Deploy\Package\"
    # Gets the number of commits since the last tag. 
    $Version = "1.0.0.4"
    $BuildConfiguration = "Release"
    
    $NuGetPackageName = "Gelf4NLog.Target"
    $NuGetPackDir = "$OutputDir" + "Pack"
    $NuSpecFileName = "Gelf4NLog.Target.nuspec"
    $NuGetPackagePath = "$OutputDir" + $NuGetPackageName + "." + $Version + ".nupkg"
    
    $ArchiveDir = "$OutputDir" + "Archive"
}

Framework '4.0'

task default -depends Pack

task Init {
}

task Clean -depends Init {
    if (Test-Path $OutputDir) {
        ri $OutputDir -Recurse
    }
    
    ri "$NuGetPackageName.*.nupkg"
    ri "$NuGetPackageName.zip" -ea SilentlyContinue
}

task Build -depends Init,Clean {
    exec { msbuild $SolutionFile "/p:OutDir=$OutputDir" "/p:Configuration=$BuildConfiguration" }
}

task Pack -depends Build {
    mkdir $NuGetPackDir
    cp "$NuSpecFileName" "$NuGetPackDir"

    mkdir "$NuGetPackDir\lib\net40"
    cp "$OutputDir\Gelf4NLog.Target.dll" "$NuGetPackDir\lib\net40"

    $Spec = [xml](get-content "$NuGetPackDir\$NuSpecFileName")
    $Spec.package.metadata.version = ([string]$Spec.package.metadata.version).Replace("{Version}",$Version)
    $Spec.Save("$NuGetPackDir\$NuSpecFileName")

    exec { .\.nuget\nuget pack "$NuGetPackDir\$NuSpecFileName" -OutputDirectory "$OutputDir" }
}

task Publish -depends Pack {
    exec { .\.nuget\nuget push $NuGetPackagePath }
}
