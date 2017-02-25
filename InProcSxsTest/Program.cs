using JetBrains.Annotations;
using Ookii.CommandLine;
using System;
using System.Net;
using System.Reflection;

namespace InProcSxsTest
{
    /// <summary>
    /// Class Program implements the main execution code.
    /// </summary>
    class Program
    {
        /// <summary>
        /// The CLSID of the HttpWebRequestWrapper in the WebRequest project.
        /// </summary>
        private static readonly Guid _clsid = new Guid("73A7A013-2249-4A59-8E3C-594E70A2D3C4");

        /// <summary>
        /// The method name to invoke on the HttpWebRequestWrapper.
        /// </summary>
        [NotNull]
        private static string _method = "Execute";

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        static void Main(string[] args)
        {
            bool pause = false;
            CommandLineParser parser = new CommandLineParser(typeof(Options));
            try
            {
                // Parse options
                Options options = (Options)parser.Parse(args);
                pause = options.Pause;

                // Test .NET 2 WebRequest
                TestWebRequest(options);
                Console.WriteLine();

                TestWebRequestInProcSxS(options);
                Console.WriteLine();
                Console.WriteLine("All tests passed.");
            }
            catch (CommandLineArgumentException exception)
            {
                Console.Error.WriteLine(exception.Message);
                parser.WriteUsageToConsole();
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
            }

            if (!pause) return;

            // Pause
            Console.WriteLine("Press any key to continue.");
            Console.ReadKey(true);
        }

        /// <summary>
        /// Tests the web request using the .NET 2 version of HttpWebRequest.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <exception cref="System.ApplicationException">
        /// Failed to create web request
        /// or
        /// Response from specified URI was not OK.
        /// </exception>
        private static void TestWebRequest(Options options)
        {
            Uri uri = options.Uri;
            Console.WriteLine($"Attempting to contact \"{uri}\" using .NET 2.0 HttpWebRequest.");

            // Create a .NET 2 web request, to ensure we can connect without InProc SxS
            HttpWebRequest webRequest = WebRequest.Create(uri) as HttpWebRequest;
            if (webRequest == null)
                throw new ApplicationException("Failed to create web request");

            // Now we can get a response
            using (HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse())
            {
                Console.WriteLine($"\"{uri}\" returned status code '{webResponse.StatusCode}'.");
                if (webResponse.StatusCode != HttpStatusCode.OK)
                    throw new ApplicationException(
                        $"Please specify a Uri that returns a status code of '{HttpStatusCode.OK}'.");
            }

            Console.WriteLine($"Successfully contacted \"{uri}\" using .NET 2.0 HttpWebRequest.");
        }

        private static void TestWebRequestInProcSxS(Options options)
        {
            Console.WriteLine($"Attempting to find COM Object using CLSID '{_clsid}'.");
            Type type = Type.GetTypeFromCLSID(_clsid);
            if (type == null)
                throw new ApplicationException($"Failed to retrieve COM object using CLSID '{_clsid}', have you registered the WebRequest assembly?");
            Console.WriteLine($"Found COM Object using CLSID '{_clsid}'.");

            Console.WriteLine($"Attempting to create instance of '{type}'.");
            object instance = Activator.CreateInstance(type);
            if (instance == null)
                throw new ApplicationException($"Failed to create instance of '{type}'.");
            Console.WriteLine($"Create instance of '{type}'.");

            Console.WriteLine($"Attempting to invoke method '{_method}' on instance of '{type}'.");
            string result = (string) type.InvokeMember(
                _method,
                BindingFlags.InvokeMethod,
                null,
                instance,
                new object[] {options.Uri.ToString()});
            if (result == null)
                throw new ApplicationException($"The method '{_method}' on instance of '{type}' returned an empty response.");


            Console.WriteLine(
                $"Successfully invoked method '{_method}' on instance of '{type}', and received a response of length {result.Length}.");
        }
    }
}
