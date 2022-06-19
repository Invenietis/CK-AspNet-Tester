using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace CK.AspNet.Tester.Tests
{
    [TestFixture]
    public class WebHostFactoryAndTestServerClientTests
    {

        [Test]
        public async Task hello_world_and_notfound_Async()
        {
            using( var client = CreateClient() )
            {
                await CommonTests.hello_world_and_notfound_Async( client );
            }
        }

        [Test]
        public async Task authorization_token_works_Async()
        {
            using( var client = CreateClient() )
            {
                await CommonTests.authorization_token_works_Async( client );
            }
        }

        [Test]
        public async Task testing_PostXml_Async()
        {
            using( var client = CreateClient() )
            {
                await CommonTests.testing_PostXml_Async( client );
            }
        }

        [Test]
        public async Task testing_PostJSON_Async()
        {
            using( var client = CreateClient() )
            {
                await CommonTests.testing_PostJSON_Async( client );
            }
        }

        [Test]
        public async Task setting_cookie_and_delete_on_root_path_Async()
        {
            using( var client = CreateClient() )
            {
                await CommonTests.setting_cookie_and_delete_on_root_path_Async( client );
            }
        }


        [Test]
        public async Task setting_cookie_and_delete_without_path_Async()
        {
            using( var client = CreateClient() )
            {
                await CommonTests.setting_cookie_and_delete_without_path_Async( client );
            }
        }

        [Test]
        public async Task setting_cookie_and_delete_on_sub_path_Async()
        {
            using( var client = CreateClient() )
            {
                await CommonTests.setting_cookie_and_delete_on_sub_path_Async( client );
            }
        }

        static TestClientBase CreateClient()
        {
            var b = WebHostBuilderFactory.Create( null, null,
                services =>
                {
                    services.AddSingleton<StupidService>();
                },
                app =>
                {
                    app.UseMiddleware<StupidMiddleware>();
                },
                c => c.UseTestServer() )
                .Build();
            b.Start();
            return new TestServerClient( b, true );
        }
    }
}
