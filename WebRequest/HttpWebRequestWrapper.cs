using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;

namespace WebRequest
{
    /// <summary>
    /// Exposes System.Net.WebRequest via COM.
    /// </summary>
    [ComVisible(true)]
    [Guid("73A7A013-2249-4A59-8E3C-594E70A2D3C4")]
    public class HttpWebRequestWrapper
    {
        /// <summary>
        /// Executes a web request against the specified URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>The response from the URL.</returns>
        /// <exception cref="System.ArgumentNullException">url - No URL was specfied.</exception>
        /// <exception cref="System.ArgumentException">url - URL is not a valid absolute HTTP(S) URI.</exception>
        /// <exception cref="System.ApplicationException">
        /// Failed to create web request
        /// or
        /// URI did not return an OK response.
        /// </exception>
        public string Execute(string url)
        { 
            /*
             * Validate the Uri.
             */
            url = url?.Trim();
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException(nameof(url), "No URL was specfied.");

            Uri uri;
            // Ensure URI is an absolute URI, has a valid syntax, and the scheme starts with 'http'. 
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri) ||
                !uri.Scheme.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException($"\"{url}\" is not a valid absolute HTTP(S) URI.", nameof(url));

            Console.WriteLine($"Attempting to contact \"{uri}\" using .NET 4.0 HttpWebRequest.");

            // Create a .NET 4 web request, to ensure we can connect without InProc SxS
            HttpWebRequest webRequest = System.Net.WebRequest.Create(uri) as HttpWebRequest;
            if (webRequest == null)
                throw new ApplicationException("Failed to create web request");

            // Now we can get a response
            string response;
            using (HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse())
            {
                Console.WriteLine($"\"{uri}\" returned status code '{webResponse.StatusCode}'.");
                if (webResponse.StatusCode != HttpStatusCode.OK)
                    throw new ApplicationException(
                        $"Please specify a Uri that returns a status code of '{HttpStatusCode.OK}'.");

                using (StreamReader reader = new StreamReader(webResponse.GetResponseStream()))
                    response = reader.ReadToEnd();
            }

            Console.WriteLine($"Successfully contacted \"{uri}\" using .NET 4.0 HttpWebRequest.");
            return response;
        }
    }
}
