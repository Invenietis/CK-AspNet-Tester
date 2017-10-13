using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace CK.AspNet.Tester
{
    /// <summary>
    /// Brings useful extensions to cookie container.
    /// </summary>
    public static class CookieContainerExtensions
    {
        /// <summary>
        /// Clears cookies from a base path and optional sub paths.
        /// </summary>
        /// <param name="container">The cookie container to update.</param>
        /// <param name="basePath">The base url. Should not be null.</param>
        /// <param name="subPath">Sub paths for which cookies must be cleared.</param>
        static public void ClearCookies( this CookieContainer container, Uri basePath, IEnumerable<string> subPath )
        {
            foreach( Cookie c in container.GetCookies( basePath ) )
            {
                c.Expired = true;
            }
            if( subPath != null )
            {
                foreach( string u in subPath )
                {
                    if( string.IsNullOrWhiteSpace( u ) ) continue;
                    Uri normalized = new Uri( basePath, u[u.Length - 1] != '/' ? u + '/' : u );
                    foreach( Cookie c in container.GetCookies( normalized ) )
                    {
                        c.Expired = true;
                    }
                }
            }
        }

        /// <summary>
        /// Clears cookies from a base path and optional sub paths.
        /// </summary>
        /// <param name="container">The cookie container to update.</param>
        /// <param name="basePath">The base url. Should not be null.</param>
        /// <param name="subPath">Optional sub paths for which cookies must be cleared.</param>
        static public void ClearCookies( this CookieContainer container, Uri basePath, params string[] subPath ) => ClearCookies( container, basePath, (IEnumerable<string>)subPath );


        static readonly Regex _rCookiePath = new Regex(
                  "(?<=^|;)\\s*path\\s*=\\s*(?<p>[^;\\s]*)\\s*;?",
                    RegexOptions.IgnoreCase
                    | RegexOptions.ExplicitCapture
                    | RegexOptions.CultureInvariant
                    | RegexOptions.Compiled
                    );

        /// <summary>
        /// Corrects CookieContainer behavior.
        /// See: https://github.com/dotnet/corefx/issues/21250#issuecomment-309613552
        /// This fix the Cookie path bug of the CookieContainer but does not handle any other
        /// specification from current (since 2011) https://tools.ietf.org/html/rfc6265.
        /// </summary>
        /// <param name="container">The cookie container to update.</param>
        /// <param name="response">A response message.</param>
        static public void UpdateCookiesWithPathHandling( this CookieContainer container, HttpResponseMessage response )
        {
            var absoluteUrl = GetCheckedRequestAbsoluteUri( container, response );
            if( response.Headers.Contains( HeaderNames.SetCookie ) )
            {
                var root = new Uri( absoluteUrl.GetLeftPart( UriPartial.Authority ) );
                var cookies = response.Headers.GetValues( HeaderNames.SetCookie );
                foreach( var cookie in cookies )
                {
                    string cFinal = cookie;
                    Uri rFinal = null;
                    Match m = _rCookiePath.Match( cookie );
                    while( m.Success )
                    {
                        // Last Path wins: see https://tools.ietf.org/html/rfc6265#section-5.3 §7.
                        cFinal = cFinal.Remove( m.Index, m.Length );
                        rFinal = new Uri( root, m.Groups[1].Value );
                        m = m.NextMatch();
                    }
                    if( rFinal == null )
                    {
                        // No path specified in cookie: the path is the one of the request.
                        rFinal = new Uri( absoluteUrl.GetLeftPart( UriPartial.Path ) );
                    }
                    container.SetCookies( rFinal, cFinal );
                }
            }
        }

        /// <summary>
        /// Helper that checks its parameters and returns the request uri.
        /// </summary>
        /// <param name="container">A cookie container that must not be null./</param>
        /// <param name="response">The received response.</param>
        /// <returns>The requested uri.</returns>
        public static Uri GetCheckedRequestAbsoluteUri( this CookieContainer container, HttpResponseMessage response )
        {
            if( container == null ) throw new ArgumentNullException( nameof( container ) );
            if( response == null ) throw new ArgumentNullException( nameof( response ) );
            var uri = response.RequestMessage.RequestUri;
            if( uri == null ) throw new ArgumentNullException( "response.RequestMessage.RequestUri" );
            if( !uri.IsAbsoluteUri ) throw new ArgumentException( "Uri must be absolute.", "response.RequestMessage.RequestUri" );
            return uri;
        }
    }
}
