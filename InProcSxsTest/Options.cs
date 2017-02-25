using JetBrains.Annotations;
using Ookii.CommandLine;
using System;
using System.ComponentModel;

namespace InProcSxsTest
{
    [Description("Test appication to demonstrate InProc SxS.")]
    public class Options
    {
        /// <summary>
        /// The default URI that is used if none is passed in the arguments.
        /// </summary>
        [NotNull] private static readonly Uri _defaultUri = new Uri("https://google.com");

        /// <summary>
        /// The URI to test against, this should return an OK response.
        /// </summary>
        [NotNull] public readonly Uri Uri;

        /// <summary>
        /// If set the console should wait for a key press once complete.
        /// </summary>
        public readonly bool Pause;

        /// <summary>
        /// Initializes a new instance of the <see cref="Options" /> class.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="pause">if set to <see langword="true" /> console will pause on completion; otherwise
        /// <see langword="false"/>.</param>
        /// <exception cref="CommandLineArgumentException">Thrown if there are any issues with the options.</exception>
        public Options(
            [ArgumentName("uri"),
             Alias("u"),
             Description("A HTTP(S) URI that will return an OK response when hit, defaults to \"https://google.com\"."),
             ValueDescription("URI")] string uri = null,
            [ArgumentName("pause"),
             Alias("p"),
             Description("If set the console will wait for a key press once completed, defaults to not set.")]
             bool pause = false)
        {
            Pause = pause;

            /*
             * Validate the Uri.
             */
            uri = uri?.Trim();
            if (string.IsNullOrEmpty(uri))
                Uri = _defaultUri;
            else
            {
                Uri u;
                // Ensure URI is an absolute URI, has a valid syntax, and the scheme starts with 'http'. 
                if (!Uri.TryCreate(uri, UriKind.Absolute, out u) ||
                    !u.Scheme.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                    throw new CommandLineArgumentException($"\"{uri}\" is not a valid absolute HTTP(S) URI.",
                        "uri",
                        CommandLineArgumentErrorCategory.Unspecified);
                Uri = u;
            }
        }
    }
}