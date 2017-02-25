param ($dll = "WebRequest.dll")
$ErrorActionPreference = "Stop"

function Get-AbsolutePath($Path) {
    $Path = [System.IO.Path]::Combine( ((pwd).Path), ($Path) );

    # Strip out any relative path modifiers like '..' and '.'
    $Path = [System.IO.Path]::GetFullPath($Path);

    return $Path;
}

# Handles pause in a variety of PS versions
function Pause ($Message = "Press any key to continue . . . ") {
    if ((Test-Path variable:psISE) -and $psISE) {
        $Shell = New-Object -ComObject "WScript.Shell"
        $Button = $Shell.Popup("Click OK to continue.", 0, "Script Paused", 0)
    }
    else {     
        Write-Host -NoNewline $Message
        [void][System.Console]::ReadKey($true)
        Write-Host
    }
}

# HACK: Should really locate this dynamically...
$RegAsm = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe"
$TheGac = "c:\windows\microsoft.net\assembly\gac_32"

If (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator"))
{   
    throw "Administrator permissions required, please relaunch as admin."
}

# NOTE: This assumes the dll is in the same directory as this script
$psGACPath = Get-AbsolutePath "PowerShellGac.dll"

# Make sure the dll path is absolute
$dll = Get-AbsolutePath $dll

if (!(test-path $TheGac))
{
    "Unable to register, gac_32 folder does not exist."
    Pause
    exit
}

$dllPathRes = (Resolve-Path -LiteralPath $dll).ProviderPath
$psGACPathRes = (Resolve-Path -LiteralPath $psGACPath).ProviderPath

if (!(Test-Path $dllPathRes))
{
    "Unable to locate the DLL. Please check the path is correct: "
    $dllPathRes
    Pause
    exit
}

if (!(Test-Path $psGACPathRes))
{
    "Incorrect location of the PowerShellGAC.dll."
    Pause
    exit
}

# First run regasm
.$RegAsm -codebase -tlb $dllPathRes

# Add the .dll to the current PS session
Add-Type -Path $psGACPath

# Can now use it
[PowerShellGac.GlobalAssemblyCache]::InstallAssembly($dllPathRes, $null, $false)

# Output success
$asmName = [System.Reflection.AssemblyName]::GetAssemblyName($dllPathRes)
"Installed the following into the GAC:"
$asmName
Pause