# InProcSxSTest
Test code for simulating InProc SxS to allow NewRelic to identify issues.

## What it does
The test program is written using .NET 2.0 and first attempts to hit a URL (by default Google), and look for an 'OK'
response.  Having ensured that the URL is active, it then attempts to instantiate the HttpWebRequestWrapper COM Object
found in the .NET 4.5.2 WebRequest project.  It then invokes the Execute method on an instance of the object, which will
repeat the URL test, this time using the .NET 4.0 version of System.Net.WebRequest.  This is an example of InProc SxS,
where a .NET domain is hosting a .NET 4 COM Object.

The important code is in Program.cs and is well commented.

## Building
The solution should be built using VS 2015.  There are 2 projects, InProcSxSTest and WebRequest, and a dependancy has
been specified that ensures WebRequest is built prior to InProcSxSTest.  You should restore NuGet packages, as
InProcSxSTest uses the 'Ookii.CommandLine' NuGet, however NuGet restore should happen on first build anyway.  The
solution only specifies a x86 Debug configuration, and this is the one that will be built.

## Deploying
After the first build, the executable and class libraries are automatically deployed to the root Release folder.
You should open an Adminsistrator Powershell prompt and run `.\RunTests.ps1` the first time at least, as this will
register the WebRequest class library for COM, and then run the tests.

You should see the following:
```
Microsoft .NET Framework Assembly Registration Utility version 4.6.1586.0
for Microsoft .NET Framework version 4.6.1586.0
Copyright (C) Microsoft Corporation.  All rights reserved.

Types registered successfully
Assembly exported to 'C:\SourceControl\InProcSxSTest\Release\WebRequest.tlb', and the type library was registered successfully
Installed the following into the GAC:

Version        Name
-------        ----
1.0.0.0        WebRequest
Executing tests.
Attempting to contact "https://google.com/" using .NET 2.0 HttpWebRequest.
"https://google.com/" returned status code 'OK'.
Successfully contacted "https://google.com/" using .NET 2.0 HttpWebRequest.

Attempting to find COM Object using CLSID '73a7a013-2249-4a59-8e3c-594e70a2d3c4'.
Found COM Object using CLSID '73a7a013-2249-4a59-8e3c-594e70a2d3c4'.
Attempting to create instance of 'System.__ComObject'.
Create instance of 'System.__ComObject'.
Attempting to invoke method 'Execute' on instance of 'System.__ComObject'.
Attempting to contact "https://google.com/" using .NET 4.0 HttpWebRequest.
"https://google.com/" returned status code 'OK'.
Successfully contacted "https://google.com/" using .NET 4.0 HttpWebRequest.
Successfully invoked method 'Execute' on instance of 'System.__ComObject', and received a response of length 46013.

All tests passed.
Press any key to continue . . .
```

After which you can press any key to finish.

## Running
Once the WebRequest COM Library is registered correctly (see above), you can Debug the InProcSxSTest executable inside
Visual Studio.

Both the Powershell script and the executable accept a URL argument, e.g.:
```
PS> .\RunTests.ps1 -url https://www.webappuk.com
```
or:
```
> InProcSxSTest "https://www.webappuk.com"
```