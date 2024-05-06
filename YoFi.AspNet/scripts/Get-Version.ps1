#
# Used by the build process to inject the current version of software
# being built as an assembly resource
#

if (Test-Path env:SOLUTION_VERSION) 
{
    $Version = "$env:SOLUTION_VERSION"
}
else 
{   
    $User = $env:USERNAME
    $Commit = git describe --always
    $Time = $(Get-Date -Format "MMddHHmm")

    $Version = "$Commit-$User-$Time"
}

Write-Output $Version
