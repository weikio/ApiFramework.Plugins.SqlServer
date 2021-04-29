using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Weikio.ApiFramework.AspNetCore;
using Weikio.ApiFramework.AspNetCore.StarterKit;
using Weikio.ApiFramework.Plugins.DatabaseBase;
using Weikio.ApiFramework.Plugins.SqlServer.Configuration;

namespace Weikio.ApiFramework.Plugins.SqlServer.Sample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddApiFrameworkStarterKit()
                .AddSqlServer("/eshop",
                    new SqlServerOptions()
                    {
                        ConnectionString =
                            "Server=tcp:adafydevtestdb001.database.windows.net,1433;User ID=docs;Password=3h1@*6PXrldU4F95;Integrated Security=false;Initial Catalog=adafyweikiodevtestdb001;",
                        SqlCommands = new SqlCommands()
                        {
                            {
                                "test",
                                new SqlCommand()
                                {
                                    CommandText = "SELECT productNumber,name,size from Product WHERE sellStartDate > @param1",
                                    Parameters = new SqlCommandParameter[] { new SqlCommandParameter() { Name = "param1", Type  = "System.DateTime", DefaultValue = new DateTime(2020,1,1)} },
                                    DataTypeName = "MyCustom"
                                }
                            }
                        }
                    });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
