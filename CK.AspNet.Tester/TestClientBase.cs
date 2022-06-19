using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CK.AspNet.Tester
{
    /// <summary>
    /// Generalization of <see cref="TestClient"/> and <see cref="TestServerClient"/>.
    /// This offers a common API to test against a <see cref="Microsoft.AspNetCore.TestHost.TestServer"/>
    /// as well as a real, external, server.
    /// </summary>
    public abstract class TestClientBase : IDisposable
    {
        int _maxAutomaticRedirections;

        /// <summary>
        /// Initializes a new <see cref="TestClientBase"/>.
        /// </summary>
        /// <param name="baseAddress">The base address. Can be null (relative urls are not supported).</param>
        /// <param name="cookies">The cookie container.</param>
        protected TestClientBase( Uri baseAddress, CookieContainer cookies )
        {
            AuthorizationHeaderName = "Authorization";
            BaseAddress = baseAddress;
            Cookies = cookies;
            MaxAutomaticRedirections = 50;
            OnReceiveMessage = DefaultOnReceiveMessageAsync;
        }

        /// <summary>
        /// Gets or sets the authorization header (defaults to "Authorization").
        /// When <see cref="Token"/> is set to a non null token, requests have 
        /// the 'AuthorizationHeaderName Bearer token" added to any requests
        /// to url on <see cref="BaseAddress"/>.
        /// </summary>
        public string AuthorizationHeaderName { get; set; }

        /// <summary>
        /// Gets the base address.
        /// </summary>
        public Uri BaseAddress { get; }

        /// <summary>
        /// Gets the <see cref="CookieContainer"/>.
        /// </summary>
        public CookieContainer Cookies { get; }

        /// <summary>
        /// Clears cookies from <see cref="BaseAddress"/> and optional sub paths.
        /// </summary>
        public void ClearCookies( params string[] subPath ) => Cookies.ClearCookies( BaseAddress, subPath );

        /// <summary>
        /// Gets or sets the maximum number of redirections that will be automatically followed.
        /// Defaults to 50.
        /// Set it to 0 to manually follow redirections thanks to <see cref="FollowRedirectAsync(HttpResponseMessage, bool)"/>.
        /// </summary>
        public int MaxAutomaticRedirections
        {
            get => _maxAutomaticRedirections;
            set => _maxAutomaticRedirections = value <= 0 ? 0 : value;
        }

        /// <summary>
        /// Gets or sets the authorization token or clears it (by setting it to null).
        /// This token must be sent only to urls on <see cref="BaseAddress"/>.
        /// </summary>
        public abstract string Token { get; set; }

         /// <summary>
        /// Issues a GET request to the relative url on <see cref="BaseAddress"/> or to an absolute url.
        /// </summary>
        /// <param name="url">The BaseAddress relative url or an absolute url.</param>
        /// <returns>The response.</returns>
        public Task<HttpResponseMessage> GetAsync( string url )
        {
            return GetAsync( new Uri( url, UriKind.RelativeOrAbsolute ) );
        }

        /// <summary>
        /// Issues a GET request to the relative url on <see cref="BaseAddress"/> or to an absolute url.
        /// </summary>
        /// <param name="url">The BaseAddress relative url or an absolute url.</param>
        /// <returns>The response.</returns>
        public virtual async Task<HttpResponseMessage> GetAsync( Uri url ) => await HandleResponseAsync( await Async( url ) );

        /// <summary>
        /// Issues a GET request to the relative url on <see cref="BaseAddress"/> or to an absolute url.
        /// Implementations must set the <see cref="Token"/> and the <see cref="Cookies"/> before sending
        /// the request.
        /// </summary>
        /// <param name="url">The BaseAddress relative url or an absolute url.</param>
        /// <returns>The response.</returns>
        internal protected abstract Task<HttpResponseMessage> Async( Uri url );

        /// <summary>
        /// Issues a POST request to the relative url on <see cref="BaseAddress"/> or to an absolute url
        /// with form values.
        /// </summary>
        /// <param name="url">The BaseAddress relative url or an absolute url.</param>
        /// <param name="formValues">The form values.</param>
        /// <returns>The response.</returns>
        public Task<HttpResponseMessage> PostAsync( string url, IEnumerable<KeyValuePair<string, string>> formValues )
        {
            return PostAsync( new Uri( url, UriKind.RelativeOrAbsolute ), formValues );
        }

        /// <summary>
        /// Issues a POST request to the relative url on <see cref="BaseAddress"/> or to an absolute url 
        /// with an "application/json" content.
        /// </summary>
        /// <param name="url">The BaseAddress relative url or an absolute url.</param>
        /// <param name="json">The json content.</param>
        /// <returns>The response.</returns>
        public Task<HttpResponseMessage> PostJSONAsync( string url, string json ) => PostJSONAsync( new Uri( url, UriKind.RelativeOrAbsolute ), json );

        /// <summary>
        /// Issues a POST request to the relative url on <see cref="BaseAddress"/> or to an absolute url 
        /// with an "application/json; charset=utf-8" content.
        /// </summary>
        /// <param name="url">The BaseAddress relative url or an absolute url.</param>
        /// <param name="json">The json content.</param>
        /// <returns>The response.</returns>
        public Task<HttpResponseMessage> PostJSONAsync( Uri url, string json )
        {
            var c = new StringContent( json, Encoding.UTF8, "application/json" );
            return PostAsync( url, c );
        }

        /// <summary>
        /// Issues a POST request to the relative url on <see cref="BaseAddress"/> or to an absolute url 
        /// with an "application/xml; charset=utf-8" content.
        /// </summary>
        /// <param name="url">The BaseAddress relative url or an absolute url.</param>
        /// <param name="xml">The xml content.</param>
        /// <returns>The response.</returns>
        public Task<HttpResponseMessage> PostXmlAsync( string url, string xml ) => PostXmlAsync( new Uri( url, UriKind.RelativeOrAbsolute ), xml );


        /// <summary>
        /// Issues a POST request to the relative url on <see cref="BaseAddress"/> or to an absolute url 
        /// with an "application/xml; charset=utf-8" content.
        /// </summary>
        /// <param name="url">The BaseAddress relative url or an absolute url.</param>
        /// <param name="xml">The xml content.</param>
        /// <returns>The response.</returns>
        public Task<HttpResponseMessage> PostXmlAsync( Uri url, string xml )
        {
            var c = new StringContent( xml, Encoding.UTF8, "application/xml" );
            return PostAsync( url, c );
        }

        /// <summary>
        /// Issues a POST request to the relative url on <see cref="BaseAddress"/> or to an absolute url
        /// with form values.
        /// </summary>
        /// <param name="url">The BaseAddress relative url or an absolute url.</param>
        /// <param name="formValues">The form values (compatible with a IDictionary&lt;string, string&gt;).</param>
        /// <returns>The response.</returns>
        public Task<HttpResponseMessage> PostAsync( Uri url, IEnumerable<KeyValuePair<string, string>> formValues )
        {
            return PostAsync( url, new FormUrlEncodedContent( formValues ) );
        }

        /// <summary>
        /// Issues a POST request to the relative url on <see cref="BaseAddress"/> or to an absolute url 
        /// with an <see cref="HttpContent"/>.
        /// </summary>
        /// <param name="url">The BaseAddress relative url or an absolute url.</param>
        /// <param name="content">The content.</param>
        /// <returns>The response.</returns>
        public async virtual Task<HttpResponseMessage> PostAsync( Uri url, HttpContent content ) => await HandleResponseAsync( await DoPostAsync( url, content ) );

        /// <summary>
        /// Issues a POST request to the relative url on <see cref="BaseAddress"/> or to an absolute url 
        /// with an <see cref="HttpContent"/>.
        /// </summary>
        /// <param name="url">The BaseAddress relative url or an absolute url.</param>
        /// <param name="content">The content.</param>
        /// <returns>The response.</returns>
        internal protected abstract Task<HttpResponseMessage> DoPostAsync( Uri url, HttpContent content );

        /// <summary>
        /// Follows at most <see cref="MaxAutomaticRedirections"/>.
        /// </summary>
        /// <param name="m">The original response.</param>
        /// <returns>The final response.</returns>
        protected async Task<HttpResponseMessage> AutoFollowRedirectAsync( HttpResponseMessage m )
        {
            int redirection = _maxAutomaticRedirections;
            while( --redirection >= 0 )
            {
                var next = await FollowRedirectAsync( m, throwIfNotRedirect: false );
                if( next == m ) break;
                m = next;
            }
            return m;
        }

        /// <summary>
        /// Gets or sets a <see cref="HttpResponseMessage"/> handler.
        /// This handler will be called immediately after the <see cref="DoPostAsync"/> or <see cref="Async"/>
        /// methods and is typically in charge of handling cookies (thanks
        /// to <see cref="CookieContainerExtensions.UpdateCookiesWithPathHandling(CookieContainer, HttpResponseMessage)"/> helper for instance),
        /// but not the redirections.
        /// <para>
        /// This handler must return true to automatically call <see cref="AutoFollowRedirectAsync"/>
        /// or false if for any reason, AutoFollowRedirect must not be done.
        /// </para>
        /// <para>
        /// This property MUST not be null.
        /// </para>
        /// </summary>
        public Func<HttpResponseMessage,Task<bool>> OnReceiveMessage { get; set; }

        async Task<HttpResponseMessage> HandleResponseAsync( HttpResponseMessage m )
        {
            if( OnReceiveMessage == null ) throw new InvalidOperationException( $"{nameof(OnReceiveMessage)} must not be null." );
            return await OnReceiveMessage( m )
                    ? await AutoFollowRedirectAsync( m )
                    : m;
        }

        /// <summary>
        /// Default <see cref="OnReceiveMessage"/> implementation.
        /// Returns true to follow redirects.
        /// </summary>
        /// <param name="m">The received message.</param>
        /// <returns>True to auto follow redirects if any.</returns>
        public virtual Task<bool> DefaultOnReceiveMessageAsync( HttpResponseMessage m )
        {
            return Task.FromResult( true );
        }

        /// <summary>
        /// Follows a redirected url once if the response's status is <see cref="HttpStatusCode.Moved"/> (301), 
        /// <see cref="HttpStatusCode.Found"/> (302) or <see cref="HttpStatusCode.SeeOther"/> (303).
        /// The <see cref="HttpStatusCode.TemporaryRedirect"/> (307) will raise a <see cref="NotSupportedException"/>
        /// at this time.
        /// </summary>
        /// <param name="response">The initial response.</param>
        /// <param name="throwIfNotRedirect">
        /// When the <paramref name="response"/> is not a 301, 302 or 303 and this is true, this method 
        /// throws an exception. When this parameter is false, the <paramref name="response"/>
        /// is returned (since it is the final redirected response).</param>
        /// <returns>The redirected response.</returns>
        /// <remarks>
        /// This should be used with a small or 0 <see cref="MaxAutomaticRedirections"/> value since
        /// otherwise redirections are automatically followed.
        /// A redirection always uses the GET method.
        /// </remarks>
        public virtual Task<HttpResponseMessage> FollowRedirectAsync( HttpResponseMessage response, bool throwIfNotRedirect = false )
        {
            if( response.StatusCode == HttpStatusCode.TemporaryRedirect )
            {
                throw new NotSupportedException( "307 TemporaryRedirect is not supported." );
            }
            if( response.StatusCode != HttpStatusCode.Moved
                && response.StatusCode != HttpStatusCode.Found
                && response.StatusCode != HttpStatusCode.SeeOther )
            {
                if( throwIfNotRedirect ) throw new Exception( "Response must be a 301 Moved, 302 Found or 303 See Other." );
                return Task.FromResult( response );
            }
            var redirectUrl = response.Headers.Location;
            if( !redirectUrl.IsAbsoluteUri )
            {
                redirectUrl = new Uri( response.RequestMessage.RequestUri, redirectUrl );
            }
            response.Dispose();
            return Async( redirectUrl );
        }

        /// <summary>
        /// Must dispose any resources specific to this client.
        /// </summary>
        public abstract void Dispose();
    }
}
