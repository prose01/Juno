using Juno.Chat;
using Juno.Data;
using Juno.Helpers;
using Juno.Interfaces;
using Juno.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System;
using System.Text.Json.Serialization;

namespace Juno
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add service and create Policy with options
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder.WithOrigins("http://localhost:4200", "http://localhost:4200/manifest.webmanifest")
                                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "HEAD")
                                .AllowAnyHeader()
                                .AllowCredentials()
                    );
            });

            // Add framework services.
            services.AddMvc().AddJsonOptions(options => {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            // Add SignalR.
            services
                .AddSignalR(hubOptions =>
                {
                    hubOptions.EnableDetailedErrors = true;
                    hubOptions.ClientTimeoutInterval = TimeSpan.FromSeconds(10);
                    hubOptions.KeepAliveInterval = TimeSpan.FromMilliseconds(15);
                });


            // Add authentication.
            string domain = $"https://{Configuration["Auth0_Domain"]}/";
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer(options =>
            {
                options.Authority = domain;
                options.Audience = Configuration["Auth0_ApiIdentifier"];
            });
            
            // register the scope authorization handler
            services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();

            // Add our repository type(s)
            services.AddSingleton<IProfilesRepository, ProfilesRepository>();

            // Add our helper method(s)
            services.AddSingleton<ICryptography, Cryptography>();
            services.AddSingleton<IHelperMethods, HelperMethods>();
            //services.AddSingleton<IUserIdProvider, NameUserIdProvider>();

            // Register the Swagger generator, defining one or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Juno API",
                    Description = "A simple example Juno API"
                });

                var securitySchema = new OpenApiSecurityScheme
                {
                    Description = "Using the Authorization header with the Bearer scheme.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                };

                c.AddSecurityDefinition("Bearer", securitySchema);

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { securitySchema, new[] { "Bearer" } }
                });
            });

            services.Configure<Settings>(options =>
            {
                options.ConnectionString = Configuration.GetSection("Mongo_ConnectionString").Value;
                options.Database = Configuration.GetSection("Mongo_Database").Value;
                options.auth0Id = Configuration.GetSection("Auth0_Claims_nameidentifier").Value;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Enable routing
            app.UseRouting();

            if (env.IsDevelopment() || env.IsStaging())
            {
                app.UseDeveloperExceptionPage();

                // Enable middleware to serve generated Swagger as a JSON endpoint.
                app.UseSwagger();

                // Enable middleware to serve swagger-ui (HTML, JS, CSS etc.), specifying the Swagger JSON endpoint.
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Juno V1");
                });

                app.UseCors("CorsPolicy");
            }

            app.UseHttpsRedirection();
            
            // Enable Authentication
            app.UseAuthentication();

            // Enable Authorization
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
                //endpoints.MapHub<ChatHub>("/chatHub");
                endpoints.MapHub<GroupChatHub>("/GroupChatHub");
            });
        }
    }
}
