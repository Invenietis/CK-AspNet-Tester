using FluentAssertions;
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
        static public async Task authorization_token_works( TestClientBase client )
        {
            client.Token = "my token";
            using( HttpResponseMessage m = await client.Get( $"/readHeader?name={client.AuthorizationHeaderName}" ) )
            {
                m.Content.ReadAsStringAsync().Result.Should().Be( $"header '{client.AuthorizationHeaderName}': 'Bearer my token'" );
            }
        }

        static public async Task hello_world_and_notfound( TestClientBase client )
        {
            using( HttpResponseMessage notFound = await client.Get( "/other" ) )
            {
                notFound.StatusCode.Should().Be( HttpStatusCode.NotFound );
            }

            using( HttpResponseMessage hello = await client.Get( "/sayHello" ) )
            {
                hello.StatusCode.Should().Be( HttpStatusCode.OK );
                var content = hello.Content.ReadAsStringAsync().Result;
                content.Should().StartWith( "Hello! " );
            }
        }

        static public async Task testing_PostXml( TestClientBase client )
        {
            using( HttpResponseMessage m = await client.PostXml( "/rewriteXElement", "<a  >  <b/> </a>" ) )
            {
                m.Content.ReadAsStringAsync().Result.Should().Be( "XElement: '<a><b /></a>'" );
            }
        }

        static public async Task testing_PostJSON( TestClientBase client )
        {
            using( HttpResponseMessage m = await client.PostJSON( "/rewriteJSON", @"{ ""a""  : null, ""b"" : {}  }" ) )
            {
                m.Content.ReadAsStringAsync().Result.Should().Be( @"JSON: '{""a"":null,""b"":{}}'" );
            }
        }

        static public async Task setting_cookie_and_delete_on_root_path( TestClientBase client )
        {
            using( HttpResponseMessage m = await client.Get( "/setCookie?name=Gateau&path=%2F" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().StartWith( "Cookie set: Gateau Path: / Value: CookieValue" );
                var cookies = client.Cookies.GetCookies( client.BaseAddress );
                cookies.Should().HaveCount( 1 );
                cookies[0].Name.Should().Be( "Gateau" );
                cookies[0].Path.Should().Be( "/" );
            }
            using( HttpResponseMessage m = await client.Get( "/sub/path/?readCookies" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().Be( "Gateau:CookieValue\r\n" );
            }
            using( HttpResponseMessage m = await client.Get( "?readCookies" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().Be( "Gateau:CookieValue\r\n" );
            }
            using( HttpResponseMessage m = await client.Get( "/deleteCookie?name=Gateau&path=%2F" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().Be( "Cookie delete: Gateau Path: /" );
                var cookies = client.Cookies.GetCookies( client.BaseAddress );
                cookies.Should().BeEmpty();
            }
            using( HttpResponseMessage m = await client.Get( "?readCookies" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().BeEmpty();
            }
        }

        static public async Task setting_cookie_and_delete_on_sub_path( TestClientBase client )
        {
            var cookiePath = new Uri( client.BaseAddress, "/COOKIEPATH" );
            using( HttpResponseMessage m = await client.Get( "/setCookie?name=Gateau&path=%2FCOOKIEPATH" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().StartWith( "Cookie set: Gateau Path: /COOKIEPATH Value: CookieValue" );
                var cookies = client.Cookies.GetCookies( cookiePath );
                cookies.Should().HaveCount( 1 );
                cookies[0].Name.Should().Be( "Gateau" );
                cookies[0].Path.Should().Be( "/COOKIEPATH" );
            }
            using( HttpResponseMessage m = await client.Get( "?readCookies" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().BeEmpty();
            }
            using( HttpResponseMessage m = await client.Get( "/COOKIEPATH/sub/path/?readCookies" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().Be( "Gateau:CookieValue\r\n" );
            }
            using( HttpResponseMessage m = await client.Get( "/deleteCookie?name=Gateau&path=%2FCOOKIEPATH" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().Be( "Cookie delete: Gateau Path: /COOKIEPATH" );
                var cookies = client.Cookies.GetCookies( cookiePath );
                cookies.Should().BeEmpty();
            }
            using( HttpResponseMessage m = await client.Get( "/COOKIEPATH/sub/path/?readCookies" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().BeEmpty();
            }
        }

        static public async Task setting_cookie_and_delete_without_path( TestClientBase client )
        {
            var setCookieUri = new Uri( client.BaseAddress, "/setCookie" );
            using( HttpResponseMessage m = await client.Get( "/setCookie?name=Gateau&value=V" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().Be( "Cookie set: Gateau Path:  Value: V" );
                var rootCookies = client.Cookies.GetCookies( client.BaseAddress );
                rootCookies.Should().HaveCount( 0 );
                var cookies = client.Cookies.GetCookies( setCookieUri );
                cookies.Should().HaveCount( 1 );
                cookies[0].Name.Should().Be( "Gateau" );
                cookies[0].Path.Should().Be( "/setCookie" );
            }
            using( HttpResponseMessage m = await client.Get( "/deleteCookie?name=Gateau" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().Be( "Cookie delete: Gateau Path: " );
                var cookies = client.Cookies.GetCookies( setCookieUri );
                cookies.Should().HaveCount( 1 );
            }
            using( HttpResponseMessage m = await client.Get( "setCookie/sub/path/?readCookies" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().Be( "Gateau:V\r\n" );
            }
            using( HttpResponseMessage m = await client.Get( "setCookie/?readCookies" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().Be( "Gateau:V\r\n" );
            }
            using( HttpResponseMessage m = await client.Get( "setCookie?readCookies" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().Be( "Gateau:V\r\n" );
            }
            using( HttpResponseMessage m = await client.Get( "?readCookies" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().BeEmpty();
            }
            using( HttpResponseMessage m = await client.Get( "/deleteCookie?name=Gateau&path=/setCookie" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().Be( "Cookie delete: Gateau Path: /setCookie" );
                var cookies = client.Cookies.GetCookies( setCookieUri );
                cookies.Should().BeEmpty();
            }
        }
    }
}
