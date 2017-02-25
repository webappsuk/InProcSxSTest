using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

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
        ///  Regular Expression to parses content type and detect encodings
        /// </summary>
        private static readonly Regex _charSetRegex = new Regex(@"charset\s?\=\s?(?<charset>[^;]+);?", 
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        /// <summary>
        /// Executes a web request against the specified URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="content">The content.</param>
        /// <param name="method">The method.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="encoding">The encoding.</param>
        /// <returns>The response from the URL.</returns>
        /// <exception cref="System.ArgumentNullException">url - No URL was specified.</exception>
        /// <exception cref="System.ArgumentException">url - URL is not a valid absolute HTTP(S) URI.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Cannot specify content data when method is GET.</exception>
        /// <exception cref="System.ApplicationException">Failed to create web request
        /// or
        /// URI did not return an OK response.</exception>
        public string Execute(
            string url,
            string content = null,
            string method = null,
            string contentType = null,
            string encoding = null)
        {
            /*
             * Validate the Uri.
             */
            url = url?.Trim();
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException(nameof(url), "No URL was specified.");

            Uri uri;
            // Ensure URI is an absolute URI, has a valid syntax, and the scheme starts with 'http'. 
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri) ||
                !uri.Scheme.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException($"\"{url}\" is not a valid absolute HTTP(S) URI.", nameof(url));

            // Normalise method
            method = string.IsNullOrWhiteSpace(method) ? "POST" : method.Trim();

            // Ensure content is null if attempting a GET.
            if (content != null && string.Equals(method, "GET", StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentOutOfRangeException(nameof(content), "Cannot specify content data when method is GET.");

            // Normalise content Type. Dismiss the Encoding.
            GetEncoding(ref contentType, ref encoding);

            // Create a .NET 4 web request, to ensure we can connect without InProc SxS
            HttpWebRequest webRequest = System.Net.WebRequest.Create(uri) as HttpWebRequest;
            if (webRequest == null)
                throw new ApplicationException("Failed to create web request");
            webRequest.Method = method;

            // If we have content then write it.
            if (content != null)
            {
                webRequest.ContentType = contentType;
                webRequest.ContentLength = content.Length;

                /*
                 * STACK OVERFLOW OCCURS WHILST CALLING GetRequestStream() BELOW
                 */
                using (StreamWriter sw = new StreamWriter(webRequest.GetRequestStream()))
                    sw.Write(content);
            }

            // Get the response
            string response;
            using (HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse())
            using (StreamReader reader = new StreamReader(webResponse.GetResponseStream()))
                response = reader.ReadToEnd();

            return response;
        }

        /// <summary>
        /// Gets the encoding, and normalises the content type and encoding name.
        /// </summary>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="encoding">The encoding.</param>
        /// <returns></returns>
        private Encoding GetEncoding(ref string contentType, ref string encoding)
        {
            Encoding result = null;
            if (!string.IsNullOrWhiteSpace(encoding))
            {
                // We have a named encoding - try to find it.
                result = Encoding.GetEncoding(encoding);

                // Correct any encoding in the content type
                encoding = result.WebName;
                string charSet = $"charset={encoding}";
                if (string.IsNullOrWhiteSpace(contentType))
                    contentType = $"application/x-www-form-urlencoded;{charSet}";
                else
                {
                    bool matched = false;
                    // ReSharper disable once PossibleNullReferenceException
                    contentType = _charSetRegex.Replace(contentType, m =>
                    {
                        if (matched) return string.Empty;
                        matched = true;
                        // ReSharper disable once PossibleNullReferenceException
                        return charSet + (m.Value[m.Value.Length - 1] == ';' ? ";" : string.Empty);
                    });
                    // ReSharper disable once PossibleNullReferenceException
                    if (!matched) contentType += (contentType[contentType.Length - 1] == ';' ? string.Empty : ";") + charSet;
                }
            }
            else if (!string.IsNullOrWhiteSpace(contentType))
            {
                // See if the encoding is in the content type
                bool matched = false;
                // ReSharper disable once PossibleNullReferenceException
                contentType = _charSetRegex.Replace(contentType, m =>
                {
                    if (matched) return string.Empty;
                    matched = true;
                    string enc = m.Groups["charset"].Value.Trim().ToLowerInvariant();
                    result = Encoding.GetEncoding(enc);
                    return $"charset={result.WebName}" + (m.Value[m.Value.Length - 1] == ';' ? ";" : string.Empty);
                });
                if (!matched)
                {
                    // Use default encoding
                    result = Encoding.UTF8;
                    contentType += (contentType[contentType.Length - 1] == ';' ? string.Empty : ";") + "charset=utf-8";
                }
                // ReSharper disable once PossibleNullReferenceException
                encoding = result.WebName;
            }
            else
            {
                // Use defaults
                result = Encoding.UTF8;
                encoding = result.WebName;
                contentType = "application/x-www-form-urlencoded;charset=utf-8";
            }
            return result;
        }
    }
}
