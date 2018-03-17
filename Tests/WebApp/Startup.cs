using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using CK.AspNet;
using Microsoft.Extensions.Logging;
using CK.AspNet.Tester.Tests;

namespace WebApp
{
    /// <summary>
    /// Simple startup class.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Adds the <see cref="StupidService"/>.
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices( IServiceCollection services )
        {
            services.AddSingleton<StupidService>();
        }

        /// <summary>
        /// Use Request monitor and <see cref="StupidMiddleware"/>.
        /// </summary>
        /// <param name="app">Application builder.</param>
        /// <param name="env">Environment.</param>
        public void Configure( IApplicationBuilder app, IHostingEnvironment env )
        {
            if( env.IsDevelopment() )
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseMiddleware<StupidMiddleware>();
        }
    }
}
