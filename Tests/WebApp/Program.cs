using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WebApp
{
    /// <summary>
    /// Entry point.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main with Debug level logging and Monitoring.
        /// Appsettings.json configures the console.
        /// </summary>
        /// <param name="args"></param>
        public static void Main( string[] args )
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot( Directory.GetCurrentDirectory() )
                .ConfigureLogging( b =>
                {
                    b.SetMinimumLevel( Microsoft.Extensions.Logging.LogLevel.Trace );
                } )
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }

    }
}
