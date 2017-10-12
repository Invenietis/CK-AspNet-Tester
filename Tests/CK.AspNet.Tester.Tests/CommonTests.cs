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
                text.Should().StartWith( "Cookie set: Gateau Path: / Value: " );
                var cookies = client.Cookies.GetCookies( client.BaseAddress );
                cookies.Should().HaveCount( 1 );
                cookies[0].Name.Should().Be( "Gateau" );
                cookies[0].Path.Should().Be( "/" );
            }
            using( HttpResponseMessage m = await client.Get( "/deleteCookie?name=Gateau&path=%2F" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().Be( "Cookie delete: Gateau Path: /" );
                var cookies = client.Cookies.GetCookies( client.BaseAddress );
                cookies.Should().BeEmpty();
            }
        }

        static public async Task setting_cookie_and_delete_without_path( TestClientBase client )
        {
            using( HttpResponseMessage m = await client.Get( "/setCookie?name=Gateau" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().StartWith( "Cookie set: Gateau Path:  Value: " );
                var cookies = client.Cookies.GetCookies( client.BaseAddress );
                cookies.Should().HaveCount( 1 );
                cookies[0].Name.Should().Be( "Gateau" );
                cookies[0].Path.Should().Be( "/" );
            }
            using( HttpResponseMessage m = await client.Get( "/deleteCookie?name=Gateau" ) )
            {
                var text = await m.Content.ReadAsStringAsync();
                text.Should().Be( "Cookie delete: Gateau Path: " );
                var cookies = client.Cookies.GetCookies( client.BaseAddress );
                cookies.Should().BeEmpty();
            }
        }
    }
}
