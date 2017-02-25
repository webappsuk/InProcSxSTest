# InProcSxSTest
Test code for simulating 
[InProc SxS](https://blogs.msdn.microsoft.com/carlos/2013/08/23/loading-multiple-clr-runtimes-inproc-sxs-sample-code/)
to allow NewRelic to identify issues.

## What it does
The InProcSxSTest solution creates an ASP.NET WebService and allows you to hit the same service using 
[InProc SxS](https://blogs.msdn.microsoft.com/carlos/2013/08/23/loading-multiple-clr-runtimes-inproc-sxs-sample-code/).
This reliably crashes New Relic's .NET Agent when enabled, throwing a 
[StackOverflowException](https://msdn.microsoft.com/en-us/library/system.stackoverflowexception(v=vs.110).aspx).

## Building
There are two solutions in the project that should be opened using Visual Studio 2015.

### WebRequest
The WebRequest solution defines a .NET 4.5.2 wrapper to the 
[.NET 4.0 `System.Net.WebRequest` class](https://blogs.msdn.microsoft.com/carlos/2013/08/23/loading-multiple-clr-runtimes-inproc-sxs-sample-code/)
called [`HttpWebRequestWrapper`](WebRequest/HttpWebRequestWrapper.cs) that implements a single method called `Execute`.  It exposes this method via COM.  You 
should build this solution first and then open a Powershell prompt using 'Run as administrator'.  Navigate to the 
`WebRequest\bin` directory and then type `.\Register.ps1`.  This is only necessary the first time you build the COM
object, however you should repeat the exercise if you make any changes to the WebRequest solution.

On running `.\Register.ps1` you should see the following:
```
Microsoft .NET Framework Assembly Registration Utility version 4.6.1586.0
for Microsoft .NET Framework version 4.6.1586.0
Copyright (C) Microsoft Corporation.  All rights reserved.

Types registered successfully
Assembly exported to 'C:\SourceControl\InProcSxSTest\Release\WebRequest.tlb', and the type library was registered successfully
Installed the following into the GAC:

Version        Name
-------        ----
1.0.???.???    WebRequest
Press any key to continue . . .
```

### InProcSxSTest
The InProcSxSTest solution creates a simple ASP.NET 2.0 Web Service which exposes three methods.  You must follow the
above instructions in the [WebRequest Section](#WebRequest) first to ensure the WebRequest COM Object is correctly
registered in the GAC.  The Web Service should build without any further requirements, and all the important code can
be found in [`WebService.asmx.cs`](InProcSxSTest/WebService.asmx.cs).

## Debugging in Visual Studio
As the projects are set to build in x86, it's important that IIS Express is also run in x86.  Further, the .NET 2.0
code uses [`XmlDocument`](https://msdn.microsoft.com/en-us/library/system.xml.xmldocument(v=vs.80).aspx), which can
cause problems with Visual Studio 2015, so I recommend setting the following options:

```
 Tools
  -> Options
   -> Projects and Solutions
    -> Web Projects
     -> Uncheck "Use the 64 bit version of IIS Express for web sites and projects"
   -> Debugging
    -> General
     -> Check "Use Managed Compatability Mode"
     -> Check "Use the legacy C# and VB expression evaluators"
```

The .NET 2.0 code can be debugged by starting the InProcSxSTest Web Service in Visual Studio directly.  Debugging 
should start IIS Express and open a website on your localhost at `http://localhost:<port>/WebService.asmx`.  I 
recommend disabling the New Relic .NET Agent the first time you test to ensure everything is installed and working 
correctly.

There are three web methods exposed:

### Ping
A simple web method that echoes the specified input.  This is used by the other two Web Methods as an endpoint to 
contact whilst testing.

To test click the `Invoke` button, any string you specify in the input box will be echoed back.

### TestWebRequest
This web method will call the [Ping](#Ping) web method using the [.NET 2.0 System.Net.WebRequest](https://msdn.microsoft.com/en-us/library/system.net.webrequest(v=vs.80).aspx\),
as such it can be used to ensure the InProcSxSTest is correctly installed and working, as it does not rely on the
WebRequest COM Object already being installed.

To test click the `Invoke` button, you can optionally add an input
string, which can be useful if you wish to find the string in a Memory Dump; however, if you leave the input empty
a randomly generated Unicode string is created, which is more likely to identify encoding issues.

### TestInProcSxSWebRequest 
This web method will call the [Ping](#Ping) web method using 
[InProc SxS](https://blogs.msdn.microsoft.com/carlos/2013/08/23/loading-multiple-clr-runtimes-inproc-sxs-sample-code/)
to call the
[.NET 4.0 `System.Net.WebRequest` class](https://blogs.msdn.microsoft.com/carlos/2013/08/23/loading-multiple-clr-runtimes-inproc-sxs-sample-code/).
When the New Relic .NET Agent is running this will cause a 
[StackOverflowException](https://msdn.microsoft.com/en-us/library/system.stackoverflowexception(v=vs.110).aspx).  Note
that this requires the WebRequest COM Object to be installed, as described in the [WebRequest Section](#WebRequest)
above.

Again, to test click the `Invoke` button, you can optionally add an input
string, which can be useful if you wish to find the string in a Memory Dump; however, if you leave the input empty
a randomly generated Unicode string is created, which is more likely to identify encoding issues.

## Debugging the WebRequest COM Object
If you wish to Debug the .NET 4.0 WebRequest project then it is a little more complex as Visual Studio can only have one
instance of a debugger running at the same time 
(see [Debugging Multiple CLRs (InProc SxS)](https://blogs.msdn.microsoft.com/carlos/2013/09/06/debugging-multiple-clrs-inproc-sxs/)
for more details). Open a second instance of Visual Studio, and load the WebRequest solution.  Start the InProcSxSTest
solution *without debugging* and once IIS Express has started, go the WebRequest solution and attach to the IIS Express
proces directly:

```
  Debug
   -> Attach to Process
     -> Attach to: Managed (v4.6, v4.5, v4.0)
     -> Available processes: iisexpress.exe
```

It is critical that you set the 'Attach to:' option to `Managed (v4.6, v4.5, v4.0)` as the primary CLR is .NET 2.0.
Note that, unfortunately, you can only attach one debugger to a process at a time, so you cannot have both instances of
Visual Studio set to debug IIS Express.
