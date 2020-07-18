using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace CK.AspNet.Tester
{
    /// <summary>
    /// Client helper that wraps a <see cref="TestServer"/> and provides simple methods (asynchronous)
    /// to easily Get/Post requests, manage cookies and a token, follow redirects
    /// (or not) and Reads the response contents.
    /// This TestServerClient routes the external requests to an internal HttpClient
    /// that shares its own CookieContainer.
    /// </summary>
    public class TestServerClient : TestClientBase
    {
        readonly TestServer _testServer;
        HttpClient _externalClient;
        readonly bool _disposeHost;

        /// <summary>
        /// Initializes a new client for a <see cref="TestServer"/>.
        /// </summary>
        /// <param name="testServer">The test server.</param>
        /// <param name="disposeHost">False to leave the TestServer alive when disposing this client.</param>
        public TestServerClient( IHost host, bool disposeHost = true )
            : base( host.GetTestServer().BaseAddress, new CookieContainer() )
        {
            _testServer = host.GetTestServer();
            Host = host;
            _disposeHost = disposeHost;
        }

        /// <summary>
        /// Gets a direct access to the <see cref="TestServer"/>.
        /// </summary>
        public TestServer Server => _testServer;

        /// <summary>
        /// Gets or sets the authorization token or clears it (by setting it to null).
        /// </summary>
        public override string Token { get; set; }
        public IHost Host { get; }

        /// <summary>
        /// Issues a GET request to the relative url on <see cref="TestClientBase.BaseAddress"/> or to an absolute url.
        /// </summary>
        /// <param name="url">The BaseAddress relative url or an absolute url.</param>
        /// <returns>The response.</returns>
        internal async protected override Task<HttpResponseMessage> DoGet( Uri url )
        {
            if( url.IsAbsoluteUri && !BaseAddress.IsBaseOf( url ) )
            {
                return await GetExternalClient().GetAsync( url );
            }
            var absoluteUrl = new Uri( _testServer.BaseAddress, url );
            var requestBuilder = _testServer.CreateRequest( absoluteUrl.ToString() );
            AddCookies( requestBuilder, absoluteUrl );
            AddToken( requestBuilder );
            var response = await requestBuilder.GetAsync();
            Cookies.UpdateCookiesWithPathHandling( response );
            return response;
        }

        /// <summary>
        /// Issues a POST request to the relative url on <see cref="TestServer.BaseAddress"/> with an <see cref="HttpContent"/>.
        /// </summary>
        /// <param name="url">The relative or absolute url.</param>
        /// <param name="content">The content.</param>
        /// <returns>The response.</returns>
        internal async protected override Task<HttpResponseMessage> DoPost( Uri url, HttpContent content )
        {
            if( url.IsAbsoluteUri && !BaseAddress.IsBaseOf( url ) )
            {
                return await GetExternalClient().PostAsync( url, content );
            }
            var absoluteUrl = new Uri( _testServer.BaseAddress, url );
            var requestBuilder = _testServer.CreateRequest( absoluteUrl.ToString() );
            AddCookies( requestBuilder, absoluteUrl );
            AddToken( requestBuilder );
            var response = await requestBuilder.And( message =>
             {
                 message.Content = content;
             } ).PostAsync();
            Cookies.UpdateCookiesWithPathHandling( response );
            return response;
        }

        /// <summary>
        /// Dispose the inner <see cref="TestServer"/>.
        /// </summary>
        public override void Dispose()
        {
            if( _externalClient != null )
            {
                _externalClient.Dispose();
                _externalClient = null;
            }
            _testServer.Dispose();
            if( _disposeHost )
            {
                Host.Dispose();
            }
        }

        class ExternalHandler : DelegatingHandler
        {
            readonly TestServerClient _client;

            public ExternalHandler( TestServerClient client )
                : base( new HttpClientHandler() { AllowAutoRedirect = false, UseCookies = false } )
            {
                _client = client;
            }

            protected override async Task<HttpResponseMessage> SendAsync( HttpRequestMessage request, CancellationToken cancellationToken )
            {
                Debug.Assert( !_client.BaseAddress.IsBaseOf( request.RequestUri ) );
                var cookies = _client.Cookies.GetCookieHeader( request.RequestUri );
                if( !String.IsNullOrWhiteSpace( cookies ) )
                {
                    request.Headers.Add( HeaderNames.Cookie, cookies );
                }
                var r = await base.SendAsync( request, cancellationToken );
                _client.Cookies.UpdateCookiesWithPathHandling( r );
                return r;
            }
        }

        HttpClient GetExternalClient()
        {
            if( _externalClient == null )
            {
                _externalClient = new HttpClient( new ExternalHandler( this ) );
            }
            return _externalClient;
        }

        void AddToken( RequestBuilder requestBuilder )
        {
            if( Token != null )
            {
                requestBuilder.AddHeader( AuthorizationHeaderName, "Bearer " + Token );
            }
        }

        void AddCookies( RequestBuilder requestBuilder, Uri absoluteUrl )
        {
            var cookieHeader = Cookies.GetCookieHeader( absoluteUrl );
            if( !string.IsNullOrWhiteSpace( cookieHeader ) )
            {
                requestBuilder.AddHeader( HeaderNames.Cookie, cookieHeader );
            }
        }

    }
}

