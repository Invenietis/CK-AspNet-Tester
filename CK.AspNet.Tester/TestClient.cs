using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;

namespace CK.AspNet.Tester
{
    /// <summary>
    /// Client helper that wraps an <see cref="HttpClient"/> and provides simple methods (asynchronous)
    /// to easily Get/Post requests, manage cookies and a token, follow redirects (or not) and Reads the
    /// response content.
    /// </summary>
    public class TestClient : TestClientBase
    {
        readonly HttpClient _httpClient;

        class Handler : DelegatingHandler
        {
            readonly TestClient _client;

            public Handler( TestClient client )
                : base( new HttpClientHandler() { AllowAutoRedirect = false, UseCookies = false } )
            {
                _client = client;
            }

            protected override async Task<HttpResponseMessage> SendAsync( HttpRequestMessage request, CancellationToken cancellationToken )
            {
                if( _client.Token != null && _client.BaseAddress.IsBaseOf( request.RequestUri ) )
                {
                    request.Headers.Add( _client.AuthorizationHeaderName, "Bearer " + _client.Token );
                }
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

        /// <summary>
        /// Initializes a new client.
        /// </summary>
        /// <param name="baseAdress">Absolute url.</param>
        public TestClient( string baseAdress )
            : base( new Uri( baseAdress, UriKind.Absolute ), new CookieContainer() )
        {
            _httpClient = new HttpClient( new Handler( this ) );
        }

        /// <summary>
        /// Gets or sets the authorization token or clears it (by setting it to null).
        /// This token will be sent only to urls on BaseAddress.
        /// </summary>
        public override string Token { get; set; }

        /// <summary>
        /// Issues a GET request to the relative url on <see cref="TestClientBase.BaseAddress"/> or to an absolute url.
        /// </summary>
        /// <param name="url">The BaseAddress relative url or an absolute url.</param>
        /// <returns>The response.</returns>
        internal protected override Task<HttpResponseMessage> Async( Uri url )
        {
            var absoluteUrl = new Uri( BaseAddress, url );
            return _httpClient.GetAsync( absoluteUrl );
        }


        /// <summary>
        /// Issues a POST request to the relative url on <see cref="TestClientBase.BaseAddress"/> or to an absolute url 
        /// with an <see cref="HttpContent"/>.
        /// </summary>
        /// <param name="url">The BaseAddress relative url or an absolute url.</param>
        /// <param name="content">The content.</param>
        /// <returns>The response.</returns>
        internal protected override Task<HttpResponseMessage> DoPostAsync( Uri url, HttpContent content )
        {
            var absoluteUrl = new Uri( BaseAddress, url );
            return _httpClient.PostAsync( absoluteUrl, content );
        }

        /// <summary>
        /// Dispose the inner <see cref="HttpClient"/>.
        /// </summary>
        public override void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}


