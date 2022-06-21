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
                pI.Arguments = '"' + System.IO.Path.Combine( "bin", TestHelper.BuildConfiguration, "net6.0", "WebApp.dll" ) + '"';
            } );

        TestClient _client;

        [OneTimeSetUp]
        public async Task RunWebAppAndCreateClientAsync()
        {
            _webApp.EnsureRunning();
            _client = new TestClient( "http://localhost:7835/" );
            await _client.GetAsync( "/" );
        }

        [OneTimeTearDown]
        public async Task ShutdownWebAppAndCreateClientAsync()
        {
            await _client.GetAsync( "/quit" );
            _webApp.StopAndWaitForExit();
            _client.Dispose();
        }

        [Test]
        public async Task hello_world_and_notfound_Async()
        {
            await CommonTests.hello_world_and_notfound_Async( _client );
        }

        [Test]
        public async Task authorization_token_works_Async()
        {
            await CommonTests.authorization_token_works_Async( _client );
        }

        [Test]
        public async Task testing_PostXml_Async()
        {
            await CommonTests.testing_PostXml_Async( _client );
        }

        [Test]
        public async Task testing_PostJSON_Async()
        {
            await CommonTests.testing_PostJSON_Async( _client );
        }

        [Test]
        public async Task setting_cookie_and_delete_on_root_path_Async()
        {
            await CommonTests.setting_cookie_and_delete_on_root_path_Async( _client );
        }

        [Test]
        public async Task setting_cookie_and_delete_on_sub_path_Async()
        {
            await CommonTests.setting_cookie_and_delete_on_sub_path_Async( _client );
        }

        [Test]
        public async Task setting_cookie_and_delete_without_path_Async()
        {
            await CommonTests.setting_cookie_and_delete_without_path_Async( _client );
        }
    }
}
