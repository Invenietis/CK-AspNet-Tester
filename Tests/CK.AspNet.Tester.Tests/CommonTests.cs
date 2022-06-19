using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CK.AspNet.Tester.Tests
{
    static class CommonTests
    {
        static public async Task authorization_token_works_Async( TestClientBase client )
        {
            client.Token = "my token";
            using( HttpResponseMessage m = await client.GetAsync( $"/readHeader?name={client.AuthorizationHeaderName}" ) )
            {
                (await m.Content.ReadAsStringAsync()).Should().Be( $"header '{client.AuthorizationHeaderName}': 'Bearer my token'" );
            }
        }

        static public async Task hello_world_and_notfound_Async( TestClientBase client )
        {
            using( HttpResponseMessage notFound = await client.GetAsync( "/other" ) )
            {
                notFound.StatusCode.Should().Be( HttpStatusCode.NotFound );
            }

            using( HttpResponseMessage hello = await client.GetAsync( "/sayHello" ) )
            {
                hello.StatusCode.Should().Be( HttpStatusCode.OK );
                var content = await hello.Content.ReadAsStringAsync();
                content.Should().StartWith( "Hello! " );
            }
        }

        static public async Task testing_PostXml_Async( TestClientBase client )
        {
            using( HttpResponseMessage m = await client.PostXmlAsync( "/rewriteXElement", "<a  >  <b/> </a>" ) )
            {
                (await m.Content.ReadAsStringAsync()).Should().Be( "XElement: '<a><b /></a>'" );
            }
        }

        static public async Task testing_PostJSON_Async( TestClientBase client )
        {
            using( HttpResponseMessage m = await client.PostJSONAsync( "/rewriteJSON", @"{ ""a""  : null, ""b"" : {}  }" ) )
            {
                (await m.Content.ReadAsStringAsync()).Should().Be( @"JSON: '{""a"":null,""b"":{}}'" );
            }
        }

        static public async Task setting_cookie_and_delete_on_root_path_Async( TestClientBase client )
        {
            using( HttpResponseMessage m = await client.GetAsync( "/setCookie?name=Gateau&path=%2F" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().StartWith( "Cookie set: Gateau Path: / Value: CookieValue" );
                var cookies = client.Cookies.GetCookies( client.BaseAddress );
                cookies.Should().HaveCount( 1 );
                cookies[0].Name.Should().Be( "Gateau" );
                cookies[0].Path.Should().Be( "/" );
            }
            using( HttpResponseMessage m = await client.GetAsync( "/sub/path/?readCookies" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().Be( "Gateau:CookieValue\r\n" );
            }
            using( HttpResponseMessage m = await client.GetAsync( "?readCookies" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().Be( "Gateau:CookieValue\r\n" );
            }
            using( HttpResponseMessage m = await client.GetAsync( "/deleteCookie?name=Gateau&path=%2F" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().Be( "Cookie delete: Gateau Path: /" );
                var cookies = client.Cookies.GetCookies( client.BaseAddress );
                cookies.Should().BeEmpty();
            }
            using( HttpResponseMessage m = await client.GetAsync( "?readCookies" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().BeEmpty();
            }
        }

        static public async Task setting_cookie_and_delete_on_sub_path_Async( TestClientBase client )
        {
            Assume.That( false, "Net6.0 CookieContainer is buggy :(" );
            var cookiePath = new Uri( client.BaseAddress, "/COOKIEPATH" );
            using( HttpResponseMessage m = await client.GetAsync( "/setCookie?name=Gateau&path=%2FCOOKIEPATH" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().StartWith( "Cookie set: Gateau Path: /COOKIEPATH Value: CookieValue" );
                // The Set-Cookie header is fine.
                var h = m.Headers.Single( h => h.Key == "Set-Cookie" );
                h.Value.Single().Should().Be( "Gateau=CookieValue; path=/COOKIEPATH" );
                // The CookieContainer finds it...
                var cookies = client.Cookies.GetCookies( cookiePath );
                cookies.Should().HaveCount( 1 );
                cookies[0].Name.Should().Be( "Gateau" );
                // ...however, the cookie Path is / (net6.0).
                // cookies[0].Path.Should().Be( "/COOKIEPATH" );
                cookies[0].Path.Should().Be( "/" );
                // Tries to FIX THE net6.0 Cookie in the Client's CookieContainer:
                // This doesn't work... the Cookie is registered in "/".
                // cookies[0].Path = "/COOKIEPATH";
            }
            using( HttpResponseMessage m = await client.GetAsync( "?readCookies" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().BeEmpty();
            }
            using( HttpResponseMessage m = await client.GetAsync( "/COOKIEPATH/sub/path/?readCookies" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().Be( "Gateau:CookieValue\r\n" );
            }
            using( HttpResponseMessage m = await client.GetAsync( "/deleteCookie?name=Gateau&path=%2FCOOKIEPATH" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().Be( "Cookie delete: Gateau Path: /COOKIEPATH" );
                var cookies = client.Cookies.GetCookies( cookiePath );
                cookies.Should().BeEmpty();
            }
            using( HttpResponseMessage m = await client.GetAsync( "/COOKIEPATH/sub/path/?readCookies" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().BeEmpty();
            }
        }

        static public async Task setting_cookie_and_delete_without_path_Async( TestClientBase client )
        {
            var setCookieUri = new Uri( client.BaseAddress, "/setCookie" );
            using( HttpResponseMessage m = await client.GetAsync( "/setCookie?name=Gateau&value=V" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().Be( "Cookie set: Gateau Path:  Value: V" );

                // The Set-Cookie header has NO path.
                var h = m.Headers.Single( h => h.Key == "Set-Cookie" );
                h.Value.Single().Should().Be( "Gateau=V" );

                // NetCore3.1: the cookie was NOT available on the root path, but on the called Uri path.
                //   var rootCookies = client.Cookies.GetCookies( client.BaseAddress );
                //   rootCookies.Should().HaveCount( 0 );
                //   var cookies = client.Cookies.GetCookies( setCookieUri );
                //   cookies.Should().HaveCount( 1 );
                //   cookies[0].Name.Should().Be( "Gateau" );
                //   cookies[0].Path.Should().Be( "/setCookie" );

                // Net6.0: the cookie is on the root url (and, of course, is also available on the called Uri path).
                var rootCookies = client.Cookies.GetCookies( client.BaseAddress );
                rootCookies.Should().HaveCount( 1 );
                var cookies = client.Cookies.GetCookies( setCookieUri );
                cookies.Should().HaveCount( 1 );
            }
            Assume.That( false, "Net6.0 CookieContainer is buggy :(" );
            using( HttpResponseMessage m = await client.GetAsync( "/deleteCookie?name=Gateau" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().Be( "Cookie delete: Gateau Path: " );
                var cookies = client.Cookies.GetCookies( setCookieUri );
                cookies.Should().HaveCount( 1 );
            }
            using( HttpResponseMessage m = await client.GetAsync( "setCookie/sub/path/?readCookies" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().Be( "Gateau:V\r\n" );
            }
            using( HttpResponseMessage m = await client.GetAsync( "setCookie/?readCookies" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().Be( "Gateau:V\r\n" );
            }
            using( HttpResponseMessage m = await client.GetAsync( "setCookie?readCookies" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().Be( "Gateau:V\r\n" );
            }
            using( HttpResponseMessage m = await client.GetAsync( "?readCookies" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().BeEmpty();
            }
            using( HttpResponseMessage m = await client.GetAsync( "/deleteCookie?name=Gateau&path=/setCookie" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().Be( "Cookie delete: Gateau Path: /setCookie" );
                var cookies = client.Cookies.GetCookies( setCookieUri );
                cookies.Should().BeEmpty();
            }
        }
    }
}
