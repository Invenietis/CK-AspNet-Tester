using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace CK.AspNet.Tester
{
    /// <summary>
    /// Sets of helpers to build a <see cref="IWebHostBuilder"/>.
    /// </summary>
    public static class WebHostBuilderFactory
    {
        /// <summary>
        /// Creates a web host builder.
        /// </summary>
        /// <param name="startupType">Optional type of the startup object.</param>
        /// <param name="contentRoot">Optional path of the content root.</param>
        /// <param name="configureServices">Optional service configurator.</param>
        /// <param name="configureApplication">Optional application configurator.</param>
        /// <param name="builder">Optional IWebHostBuilder configurator.</param>
        /// <returns>The web host builder.</returns>
        public static IHostBuilder Create( Type startupType,
                                           string contentRoot,
                                           Action<IServiceCollection> configureServices,
                                           Action<IApplicationBuilder> configureApplication,
                                           Action<IWebHostBuilder> builder = null )
        {
            return Create( startupType, contentRoot, new[] { configureServices }, new[] { configureApplication }, builder );
        }

        /// <summary>
        /// Creates a web host builder with multiple service and application configurators.
        /// </summary>
        /// <param name="startupType">Optional type of the startup object. Can be null.</param>
        /// <param name="contentRoot">Optional path of the content root. Can be null.</param>
        /// <param name="configureServices">Optional service configurators. Can be null.</param>
        /// <param name="configureApplication">Optional application configurators. Can be null.</param>
        /// <param name="builder">Optional IWebHostBuilder configurator.</param>
        /// <returns>The web host builder.</returns>
        public static IHostBuilder Create( Type startupType,
                                           string contentRoot,
                                           IEnumerable<Action<IServiceCollection>> configureServices,
                                           IEnumerable<Action<IApplicationBuilder>> configureApplication,
                                           Action<IWebHostBuilder> builder = null )
        {
            object startup = null;
            var hostBuilder = new HostBuilder();
            return hostBuilder.ConfigureWebHostDefaults( webHostBuilder =>
             {
                 if( contentRoot != null ) webHostBuilder.UseContentRoot( contentRoot );
                 webHostBuilder.UseTestServer();
                 webHostBuilder.UseEnvironment( Environments.Development );
                 webHostBuilder.ConfigureServices( services =>
                 {
                     if( startupType != null )
                     {
                         startup = CreateStartupObject( startupType, services );
                     }
                     ConfigureServices( startup, services, configureServices );
                 } )
                   .Configure( builder =>
                   {
                       ConfigureApplication( startup, builder, configureApplication );
                   } );
                 builder?.Invoke( webHostBuilder );
             } );
        }


        /// <summary>
        /// Creates a web host builder with multiple service and application configurators.
        /// </summary>
        /// <param name="startupType">Optional type of the startup object. Can be null.</param>
        /// <param name="contentRoot">Optional path of the content root. Can be null.</param>
        /// <param name="configureServices">Optional service configurators. Can be null.</param>
        /// <param name="configureApplication">Optional application configurators. Can be null.</param>
        /// <returns>The web host builder.</returns>
        public static IHostBuilder Create( Type startupType,
                                           string contentRoot,
                                           IEnumerable<Action<IServiceCollection>> configureServices,
                                           IEnumerable<Action<IApplicationBuilder>> configureApplication )
        {
            object startup = null;
            var hostBuilder = new HostBuilder()
                .ConfigureWebHostDefaults( webHostBuilder =>
                {
                     if( contentRoot != null ) webHostBuilder.UseContentRoot( contentRoot );
                     webHostBuilder.UseEnvironment( Environments.Development );
                     webHostBuilder.ConfigureServices( services =>
                     {
                         if( startupType != null )
                         {
                             startup = CreateStartupObject( startupType, services );
                         }
                         ConfigureServices( startup, services, configureServices );
                     } )
                        .Configure( builder =>
                        {
                            ConfigureApplication( startup, builder, configureApplication );
                        } );
                } );
            return hostBuilder;
        }

        static object CreateStartupObject( Type startupType, IServiceCollection services )
        {
            Debug.Assert( startupType != null );
            object startup;
            var hostingEnvironment = ConfigureHostingEnvironment( startupType, services );
            int ctorCount = 0;
            var ctor = startupType.GetTypeInfo()
                                      .DeclaredConstructors
                                      .Select( c => new
                                      {
                                          Ctor = c,
                                          Params = c.GetParameters().Select( p => p.ParameterType ).ToArray(),
                                      } )
                                      .Select( c => { ++ctorCount; return c; } )
                                      .OrderByDescending( c => c.Params.Length )
                                      .Select( c => new
                                      {
                                          c.Ctor,
                                          c.Params,
                                          Values = c.Params
                                                      .Select( p => services.FirstOrDefault( s => p.IsAssignableFrom( s.ServiceType ) ) )
                                                      .Select( s => s?.ImplementationInstance )
                                      } )
                                      .Where( c => c.Values.All( v => v != null ) )
                                      .FirstOrDefault();
            if( ctorCount > 0 && ctor == null ) throw new Exception( $"Unable to find a constructor (out of {ctorCount}) with injectable parameters." );
            if( ctor == null ) startup = Activator.CreateInstance( startupType );
            else startup = Activator.CreateInstance( startupType, ctor.Values.ToArray() );
            return startup;
        }

        static IWebHostEnvironment ConfigureHostingEnvironment( Type startup, IServiceCollection services )
        {
            static bool IsHostingEnvironmet( ServiceDescriptor service ) => service.ImplementationInstance is IWebHostEnvironment;

            var hostingEnvironment = (IWebHostEnvironment)services.Single( IsHostingEnvironmet ).ImplementationInstance;
            var assembly = startup.GetTypeInfo().Assembly;
            hostingEnvironment.ApplicationName = assembly.GetName().Name;
            return hostingEnvironment;
        }

        static void ConfigureServices(
            object startup,
            IServiceCollection services,
            IEnumerable<Action<IServiceCollection>> configureServices )
        {
            if( startup != null )
            {
                var conf = startup.GetType().GetMethod( "ConfigureServices" );
                conf?.Invoke( startup, new[] { services } );
            }
            if( configureServices != null )
            {
                foreach( var serviceConfiguration in configureServices )
                {
                    serviceConfiguration?.Invoke( services );
                }
            }
        }

        static void ConfigureApplication( object startup,
                                          IApplicationBuilder builder,
                                          IEnumerable<Action<IApplicationBuilder>> configureApplication )
        {
            if( configureApplication != null )
            {
                foreach( var applicationConfiguration in configureApplication )
                {
                    applicationConfiguration?.Invoke( builder );
                }
            }
            if( startup != null )
            {
                var conf = startup.GetType().GetMethod( "Configure" );
                conf?.Invoke( startup, new[] { builder } );
            }
        }

    }
}
