using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Services;
using System.Xml;

namespace InProcSxSTest
{
    /// <summary>
    /// Summary description for WebService
    /// </summary>
    [WebService(Namespace = "http://localhost/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]

    public class WebService : System.Web.Services.WebService
    {
        /// <summary>
        /// The CLSID of the HttpWebRequestWrapper in the WebRequest project.
        /// </summary>
        private static readonly Guid _clsid = new Guid("73A7A013-2249-4A59-8E3C-594E70A2D3C4");

        /// <summary>
        /// The method name to invoke on the HttpWebRequestWrapper.
        /// </summary>
        private static string _method = "Execute";

        /// <summary>
        /// Tests the web request using the .NET 2.0 <see cref="WebRequest"/>.
        /// </summary>
        /// <param name="input">The optional input that will be sent to the <see cref="Ping"/> web method.</param>
        /// <returns><see langword="true" /> if we successfully contact the <see cref="Ping"/> method and parsed it's
        /// response, <see langword="false" /> otherwise.</returns>
        [WebMethod(Description= "This web method will call the <a href=\"WebService.asmx?op=Ping\">Ping</a> web method using the <a href=\"https://msdn.microsoft.com/en-us/library/system.net.webrequest(v=vs.80).aspx\">.NET 2.0 System.Net.WebRequest</a>.")]
        public bool TestWebRequest(string input = null)
        {
            // If we don't get given a string, generate a random one.
            if (string.IsNullOrEmpty(input))
                input = RandomString();

            // Create request body
            string body = PingBody(input);

            // Create request and write body to stream
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(GetWebMethodUri("Ping"));
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.ContentLength = body.Length;
            using (StreamWriter sw = new StreamWriter(webRequest.GetRequestStream()))
                sw.Write(body);

            // Get response as string
            string response;
            using (HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse())
            using (StreamReader sr = new StreamReader(webResponse.GetResponseStream()))
                response = sr.ReadToEnd();

            // Parse response, and check we got the same input as requested
            return string.Equals(input, ParsePingResponse(response));
        }


        /// <summary>
        /// Tests the web request using the .NET 4.0 <see cref="WebRequest"/> via InProc SxS.
        /// </summary>
        /// <param name="input">The optional input that will be sent to the <see cref="Ping"/> web method.</param>
        /// <returns><see langword="true" /> if we successfully contact the <see cref="Ping"/> method and parsed it's
        /// response, <see langword="false" /> otherwise.</returns>
        [WebMethod(Description = "This web method will call the <a href=\"WebService.asmx?op=Ping\">Ping</a> web method using <a href=\"https://blogs.msdn.microsoft.com/carlos/2013/08/23/loading-multiple-clr-runtimes-inproc-sxs-sample-code/\">InProc SxS</a> to call the <a href=\"https://msdn.microsoft.com/en-us/library/system.net.webrequest(v=vs.110).aspx\">.NET 4.0 System.Net.WebRequest</a>.  Note, you must first register the WebRequest COM Object as described in the <a href=\"https://github.com/webappsuk/InProcSxSTest\">README.md</a>.")]
        public bool TestInProcSxSWebRequest(string input = null)
        {
            // If we don't get given a string, generate a random one.
            if (string.IsNullOrEmpty(input))
                input = RandomString();

            // Create an instance of .NET 4.5.2 HttpWebRequestWrapper 
            Type type = Type.GetTypeFromCLSID(_clsid);
            object instance = Activator.CreateInstance(type);

            // Invoke the member and get the response
            object response = type.InvokeMember(
                _method,
                BindingFlags.InvokeMethod,
                null,
                instance,
                new object[]
                {
                    GetWebMethodUri("Ping").ToString(),
                    PingBody(input),
                    "POST",
                    "application/x-www-form-urlencoded"
                });

            // Parse response, and check we got the same input as requested
            return string.Equals(input, ParsePingResponse(response as string));
        }

        #region Ping - Simple echo web method
            /// <summary>
            /// Echoes the specified input.
            /// </summary>
            /// <param name="input">The input.</param>
            /// <returns>The <paramref name="input"></paramref>.</returns>
        [WebMethod(Description = "Simple web method that echoes the specified input.")]
        public string Ping(string input) => input;

        /// <summary>
        /// Creates a body for a <see cref="Ping"/> Command.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>A properly formated POST body.</returns>
        private static string PingBody(string input)
        {
            return $"input={HttpUtility.UrlEncode(input)}";
        }

        /// <summary>
        /// Parses a ping response.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <returns>The string returned from a <see cref="Ping"/>.</returns>
        private static string ParsePingResponse(string response)
        {
            // Parse response as XML
            XmlDocument xmlResponse = new XmlDocument();
            xmlResponse.LoadXml(response);

            // Get the contents of the document element.
            return xmlResponse.DocumentElement?.InnerText ?? string.Empty;
        }
        #endregion

        /// <summary>
        /// Gets an URI for a web method.
        /// </summary>
        /// <param name="name">The web method name.</param>
        /// <param name="queryString">The query string (optional).</param>
        /// <returns>An absolute URI.</returns>
        private static Uri GetWebMethodUri(string name, string queryString = null)
        {
            // Get the request Url, which will also contain the method and any query string
            Uri url = HttpContext.Current.Request.Url;

            // Create a new builder from the Url
            UriBuilder builder = new UriBuilder(url);

            // Change the last segment's name
            string[] segments = url.Segments;
            segments[segments.Length - 1] = name;
            builder.Path = string.Concat(segments);

            // Update query string
            builder.Query = queryString ?? string.Empty;

            // The remaining Uri is this service's URL
            return builder.Uri;
        }

        #region Random Generation
        /// <summary>
        /// Generates a random string.
        /// </summary>
        /// <param name="maxLength">Maximum length.</param>
        /// <param name="unicode">if set to <see langword="true" /> string is UTF16; otherwise it uses ASCII.</param>
        /// <param name="nullProbability">The probability of a null being returned (0.0 for no nulls).</param>
        /// <param name="supplementaryPlaneProbability">The probability of the character coming from a random 
        /// supplementary plane (0.0 for a Basic Multilingual Plane character).</param>
        /// <param name="minLength">The minimum length.</param>
        /// <returns>A random <see cref="System.String" />.</returns>
        public static string RandomString(
            int maxLength = 500,
            bool unicode = true,
            double nullProbability = 0.0,
            double supplementaryPlaneProbability = 0.1,
            int minLength = 1)
        {
            Random random = new Random();

            // Check for random nulls
            if ((nullProbability > 0.0) &&
                (random.NextDouble() < nullProbability))
                return null;

            // Get string length, if there's no maximum then use 8001 (as 8000 is max specific size in SQL Server).
            if (maxLength < 0)
                maxLength = 8001;
            if (minLength < 0)
                minLength = 0;
            int length = random.Next(maxLength - minLength) + minLength;
            if (length < 1)
                return string.Empty;

            if (!unicode)
            {
                byte[] bytes = new byte[length];
                random.NextBytes(bytes);
                return new ASCIIEncoding().GetString(bytes);
            }

            StringBuilder stringBuilder = new StringBuilder(length);
            for (int charIndex = 0; charIndex < length; ++charIndex)
                stringBuilder.Append(RandomUnicodeCharacter(random));
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Generates a random Unicode character.
        /// </summary>
        /// <param name="random">The random generator.</param>
        /// <param name="supplementaryPlaneProbability">The probability of the character coming from a random supplementary plane (0.0 for a Basic Multilingual Plane character).</param>
        /// <returns>A random unicode character.</returns>
        private static char[] RandomUnicodeCharacter(Random random, double supplementaryPlaneProbability = 0.1)
        {
            if (supplementaryPlaneProbability > 0.0 &&
                random.NextDouble() < supplementaryPlaneProbability)
                return new[]
                {
                    (char)random.Next(0xD800, 0xDBFF),
                    (char)random.Next(0xDC00, 0xDFFF)
                };
            int character = random.Next(0xF7E1);
            switch (character)
            {
                case 0:
                    character = 0x0009;
                    break;
                case 1:
                    character = 0x000A;
                    break;
                case 2:
                    character = 0x000D;
                    break;
                default:
                    // Other valid characters are 0x0020-0xD7FF and 0xE000-0xFFFD:
                    character += character < 0xD7E3 ? 0x001D : 0x081D;
                    break;
            }
            return new[] { (char)character };
        }
        #endregion
    }
}
