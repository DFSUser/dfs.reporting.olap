/*
 * Digital Future Systems LLC, Russia, Perm
 * 
 * This file is part of dfs.reporting.olap.
 * 
 * dfs.reporting.olap is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * dfs.reporting.olap is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with dfs.reporting.olap.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Linq;
using System.Text.Json;
using Dfs.Reporting.Olap.Services;
using Dfs.Reporting.Olap.Services.Database;
using Dfs.Reporting.Olap.Services.Saiku;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Dfs.Reporting.Olap
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<MetadataDatabaseContext>((provider, builder) =>
            {
                var httpContext = provider.GetService<IHttpContextAccessor>().HttpContext;
                var httpRequest = httpContext.Request;

                var yearHeader = httpRequest.Headers["year"];

                var connectionString = Configuration.GetConnectionString("DefaultOlap");

                if (yearHeader.Count > 0)
                {
                    connectionString = Configuration.GetConnectionString(yearHeader.First());
                }

                builder.UseNpgsql(connectionString)
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            });

            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.IgnoreNullValues = true;
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });

            services.AddScoped<ISaikuHttpClient, SaikuHttpClient>();
            services.AddScoped<IReportMetadataService, ReportMetadataService>();
            services.AddScoped<IReportDimensionService, ReportDimensionService>();
            services.AddScoped<IReportDatabaseService, ReportDatabaseService>();
            services.AddScoped<IReportSaikuService, ReportSaikuService>();
            services.AddScoped<IReportQueryService, ReportQueryService>();
            services.AddScoped<IReportStyleService, ReportStyleService>();
            services.AddScoped<ICookieService, CookieService>();

            services.AddScoped<IExportReportService, ExportReportService>();

            services.AddDistributedMemoryCache();
            services.AddTransient<IHttpContextAccessor, HttpContextAccessor>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseSerilogRequestLogging();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
