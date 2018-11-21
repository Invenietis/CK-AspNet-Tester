using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace CK.AspNet.Tester.Tests
{
    [TestFixture]
    public class TestClientTests
    {
        readonly ExternalProcess _webApp = new ExternalProcess(
            pI =>
            {
                pI.WorkingDirectory = System.IO.Path.Combine( TestHelper.SolutionFolder, "Tests", "WebApp" );
                pI.FileName = "dotnet";
                pI.Arguments = '"' + System.IO.Path.Combine( "bin", TestHelper.BuildConfiguration, "netcoreapp2.1", "WebApp.dll" ) + '"';
            } );

        TestClient _client;

#if NET461
        [TestFixtureSetUp]
#else
        [OneTimeSetUp]
#endif
        public void RunWebAppAndCreateClient()
        {
            _webApp.EnsureRunning();
            _client = new TestClient( "http://localhost:7835/" );
            _client.Get( "/" ).GetAwaiter().GetResult();
        }

#if NET461
        [TestFixtureTearDown]
#else
        [OneTimeTearDown]
#endif
        public void ShutdownWebAppAndCreateClient()
        {
            _client.Get( "/quit" ).GetAwaiter().GetResult();
            _webApp.StopAndWaitForExit();
            _client.Dispose();
        }

        [Test]
        public async Task hello_world_and_notfound()
        {
            await CommonTests.hello_world_and_notfound( _client );
        }

        [Test]
        public async Task authorization_token_works()
        {
            await CommonTests.authorization_token_works( _client );
        }

        [Test]
        public async Task testing_PostXml()
        {
            await CommonTests.testing_PostXml( _client );
        }

        [Test]
        public async Task testing_PostJSON()
        {
            await CommonTests.testing_PostJSON( _client );
        }

        [Test]
        public async Task setting_cookie_and_delete_on_root_path()
        {
            await CommonTests.setting_cookie_and_delete_on_root_path( _client );
        }

        [Test]
        public async Task setting_cookie_and_delete_on_sub_path()
        {
            await CommonTests.setting_cookie_and_delete_on_sub_path( _client );
        }

        [Test]
        public async Task setting_cookie_and_delete_without_path()
        {
            await CommonTests.setting_cookie_and_delete_without_path( _client );
        }
    }
}
