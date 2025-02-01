using ClinicDentServer.BsonFormatter;
using ClinicDentServer.Filters;
using ClinicDentServer.InputFormatters;
using ClinicDentServer.Interfaces.Repositories;
using ClinicDentServer.Interfaces.Services;
using ClinicDentServer.Models;
using ClinicDentServer.OutputFormatters;
using ClinicDentServer.Repositories;
using ClinicDentServer.Services;
using ClinicDentServer.SocketServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Linq;

namespace ClinicDentServer
{
    public class Program
    {
        public static IConfiguration Configuration { get; set; }
        public static IServiceProvider ServiceProvider;

        public static Server TcpServer;
        public static void Main(string[] args)
        {
            TcpServer = new Server();
            TcpServer.Start();

            #region Builder Setup
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            Configuration = builder.Configuration;
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.RequireHttpsMetadata = false;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidIssuer = AuthOptions.ISSUER,

                            ValidateAudience = true,
                            ValidAudience = AuthOptions.AUDIENCE,
                            ValidateLifetime = false,

                            IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey(),
                            ValidateIssuerSigningKey = true,
                        };
                    });
            builder.Services.AddControllers((options) =>
            {
                options.Filters.Add(typeof(ApplicationExceptionFilterAttribute));
                options.RespectBrowserAcceptHeader = true;
                options.OutputFormatters.Add(new TextPlainOutputFormatter());
                options.InputFormatters.Add(new TextPlainInputFormatter());

                options.InputFormatters.Add(new BsonInputFormatter());
                options.OutputFormatters.Add(new BsonOutputFormatter());
            });
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ClinicDentServer", Version = "v1" });
            });

            builder.Services.Configure<IISServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ClinicContext>(provider =>
            {
                var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
                var connectionString = httpContextAccessor.HttpContext.User.Claims
                    .FirstOrDefault(c => c.Type == "ConnectionString")?.Value;

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Connection string is not available in user claims.");
                }
                return new ClinicContext(connectionString);
            });
            builder.Services.AddScoped<IDefaultRepository<Cabinet>, DefaultRepository<Cabinet>>();
            builder.Services.AddScoped<Lazy<IDefaultRepository<Cabinet>>>(provider => new Lazy<IDefaultRepository<Cabinet>>(() => provider.GetRequiredService<IDefaultRepository<Cabinet>>()));

            builder.Services.AddScoped<IDefaultRepository<CabinetComment>, DefaultRepository<CabinetComment>>();
            builder.Services.AddScoped<Lazy<IDefaultRepository<CabinetComment>>>(provider => new Lazy<IDefaultRepository<CabinetComment>>(() => provider.GetRequiredService<IDefaultRepository<CabinetComment>>()));

            builder.Services.AddScoped<IDefaultRepository<Doctor>, DefaultRepository<Doctor>>();
            builder.Services.AddScoped<Lazy<IDefaultRepository<Doctor>>>(provider => new Lazy<IDefaultRepository<Doctor>>(() => provider.GetRequiredService<IDefaultRepository<Doctor>>()));

            builder.Services.AddScoped<IImageRepository<Image>, ImageRepository<Image>>();
            builder.Services.AddScoped<Lazy<IImageRepository<Image>>>(provider => new Lazy<IImageRepository<Image>>(() => provider.GetRequiredService<IImageRepository<Image>>()));

            builder.Services.AddScoped<IDefaultRepository<Patient>, DefaultRepository<Patient>>();
            builder.Services.AddScoped<Lazy<IDefaultRepository<Patient>>>(provider => new Lazy<IDefaultRepository<Patient>>(() => provider.GetRequiredService<IDefaultRepository<Patient>>()));

            builder.Services.AddScoped<IDefaultRepository<Schedule>, DefaultRepository<Schedule>>();
            builder.Services.AddScoped<Lazy<IDefaultRepository<Schedule>>>(provider => new Lazy<IDefaultRepository<Schedule>>(() => provider.GetRequiredService<IDefaultRepository<Schedule>>()));

            builder.Services.AddScoped<IStageRepository<Stage>, StageRepository<Stage>>();
            builder.Services.AddScoped<Lazy<IStageRepository<Stage>>>(provider => new Lazy<IStageRepository<Stage>>(() => provider.GetRequiredService<IStageRepository<Stage>>()));

            builder.Services.AddScoped<IDefaultRepository<StageAsset>, DefaultRepository<StageAsset>>();
            builder.Services.AddScoped<Lazy<IDefaultRepository<StageAsset>>>(provider => new Lazy<IDefaultRepository<StageAsset>>(() => provider.GetRequiredService<IDefaultRepository<StageAsset>>()));

            builder.Services.AddScoped<IDefaultRepository<Tooth>, DefaultRepository<Tooth>>();
            builder.Services.AddScoped<Lazy<IDefaultRepository<Tooth>>>(provider => new Lazy<IDefaultRepository<Tooth>>(() => provider.GetRequiredService<IDefaultRepository<Tooth>>()));

            builder.Services.AddScoped<IDefaultRepository<ToothUnderObservation>, DefaultRepository<ToothUnderObservation>>();
            builder.Services.AddScoped<Lazy<IDefaultRepository<ToothUnderObservation>>>(provider => new Lazy<IDefaultRepository<ToothUnderObservation>>(() => provider.GetRequiredService<IDefaultRepository<ToothUnderObservation>>()));

            builder.Services.AddScoped<IStagesService, StagesService>();
            builder.Services.AddScoped<Lazy<IStagesService>>(provider => new Lazy<IStagesService>(() => provider.GetRequiredService<IStagesService>()));



            #endregion
            #region App Setup
            WebApplication app = builder.Build();
            ServiceProvider = app.Services;
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebApi1 v1"));
            }

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            app.UseStaticFiles();
            #endregion
            app.Run();
        }
    }
}
