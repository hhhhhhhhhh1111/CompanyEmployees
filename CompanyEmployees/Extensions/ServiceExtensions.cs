using Contracts;
using LoggerService;

namespace CompanyEmployees.Extensions
{
    public static class ServiceExtensions
    {

        public static void ConfigureCors(this IServiceCollection services) =>
 services.AddCors(options =>
 {
     options.AddPolicy("CorsPolicy", builder =>
     builder.WithOrigins("https://example.com")
     .WithMethods("POST", "GET")
     .WithHeaders("accept",
"content- type"));
 });
        public static void ConfigureIISIntegration(this IServiceCollection services) =>
 services.Configure<IISOptions>(options =>
 {

 });
        public static void ConfigureLoggerService(this IServiceCollection services) =>
 services.AddScoped<ILoggerManager, LoggerManager>();



    }


}
