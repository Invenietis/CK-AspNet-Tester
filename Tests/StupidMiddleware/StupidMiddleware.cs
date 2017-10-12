using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace CK.AspNet.Tester.Tests
{
    public class StupidMiddleware
    {
        readonly RequestDelegate _next;
        readonly StupidService _s;
        readonly IApplicationLifetime _lifetime;
        readonly ILogger _logger;
        int _cookieNumber;

        public StupidMiddleware( RequestDelegate next,
            StupidService s,
            IApplicationLifetime lifetime,
            ILoggerFactory logger )
        {
            _next = next;
            _s = s;
            _lifetime = lifetime;
            _logger = logger.CreateLogger<StupidMiddleware>();
        }

        public Task Invoke( HttpContext context )
        {
            if( context.Request.Path.StartsWithSegments( "/sayHello" ) )
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                return context.Response.WriteAsync( "Hello! " + _s.GetText() );
            }
            if( context.Request.Path.StartsWithSegments( "/readHeader" ) )
            {
                string name = context.Request.Query["name"];
                StringValues header = context.Request.Headers[name];
                return context.Response.WriteAsync( $"header '{name}': '{header}'" );
            }
            if( context.Request.Path.StartsWithSegments( "/setCookie" ) )
            {
                string name = context.Request.Query["name"];
                string path = context.Request.Query["path"];
                int num = Interlocked.Increment( ref _cookieNumber );
                context.Response.Cookies.Append( name, $"Cookie n°{num}", new CookieOptions()
                {
                    Path = path
                } );
                return context.Response.WriteAsync( $"Cookie set: {name} Path: {path} Value: '{num}'" );
            }
            if( context.Request.Path.StartsWithSegments( "/deleteCookie" ) )
            {
                string name = context.Request.Query["name"];
                string path = context.Request.Query["path"];
                context.Response.Cookies.Delete( name, new CookieOptions()
                {
                    Path = path
                } );
                return context.Response.WriteAsync( $"Cookie delete: {name} Path: {path}" );
            }
            if( context.Request.Path.StartsWithSegments( "/aspnetlogs" ) )
            {
                _logger.LogCritical( $"This is a Critical log." );
                _logger.LogError( $"This is a Error log." );
                _logger.LogWarning( $"This is a Warning log." );
                _logger.LogInformation( $"This is a Information log." );
                _logger.LogDebug( $"This is a Debug log." );
                _logger.LogTrace( $"This is a Trace log." );
                return context.Response.WriteAsync( $"Logs written." );
            }
            if( context.Request.Path.StartsWithSegments( "/quit" ) )
            {
                _lifetime.StopApplication();
                return context.Response.WriteAsync( $"Application is terminating." );
            }
            if( context.Request.Path.StartsWithSegments( "/rewriteJSON" ) )
            {
                if( !HttpMethods.IsPost( context.Request.Method ) ) context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                string content = new StreamReader( context.Request.Body ).ReadToEnd();
                return context.Response.WriteAsync( $"JSON: '{JObject.Parse( content ).ToString( Newtonsoft.Json.Formatting.None )}'" );
            }
            if( context.Request.Path.StartsWithSegments( "/rewriteXElement" ) )
            {
                if( !HttpMethods.IsPost( context.Request.Method ) ) context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                string content = new StreamReader( context.Request.Body ).ReadToEnd();
                return context.Response.WriteAsync( $"XElement: '{XElement.Parse( content ).ToString( SaveOptions.DisableFormatting )}'" );
            }
            if( context.Request.Path.StartsWithSegments( "/bug" ) )
            {
                throw new Exception( "Bug!" );
            }
            if( context.Request.Path.StartsWithSegments( "/asyncBug" ) )
            {
                return AsyncBug();
            }
            if( context.Request.Path.StartsWithSegments( "/hiddenAsyncBug" ) )
            {
                context.Response.StatusCode = StatusCodes.Status202Accepted;
                Task.Delay( 100 ).ContinueWith( t =>
                {
                    throw new Exception( "I'm an horrible HiddenAsyncBug!" );
                } );
                return context.Response.WriteAsync( "Will break the started Task." );
            }
            if( context.Request.Path.StartsWithSegments( "/unhandledAppDomainException" ) )
            {
                context.Response.StatusCode = StatusCodes.Status202Accepted;
                var t = new Thread( () => throw new Exception( "I'm an unhandled exception." ) );
                t.IsBackground = true;
                t.Start();
                return context.Response.WriteAsync( "Will break the started thread." );
            }

            return _next.Invoke( context );
        }

        async Task AsyncBug()
        {
            await Task.Delay( 100 );
            throw new Exception( "AsyncBug!" );
        }
    }

}
