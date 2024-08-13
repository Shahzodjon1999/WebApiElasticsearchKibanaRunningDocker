
using Serilog;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;
using System.Reflection;

namespace test;

    public class Program
{
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll",
                builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
        });
        builder.Services.AddControllers();
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddSwaggerGen();

        configureLogging();
		builder.Host.UseSerilog();

        var app = builder.Build();

		if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
		{
			app.UseSwagger();
			app.UseSwaggerUI();
		}

        //Enable CORS 
        app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

        app.UseHttpsRedirection();

		app.UseAuthorization();


		app.MapControllers();

		app.Run();
	}
  static void configureLogging()
	{
		var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

		var configuration = new ConfigurationBuilder()
		.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
		.AddJsonFile(
			$"appsettings.{environment}.json", optional: true
		).Build();

		Log.Logger = new LoggerConfiguration()
			.Enrich.FromLogContext()
			.Enrich.WithExceptionDetails()
			.WriteTo.Debug()
			.WriteTo.Console()
			.WriteTo.Elasticsearch(ConfigureElasticSink(configuration,environment))
			.Enrich.WithProperty("Environment",environment)
			.ReadFrom.Configuration(configuration)
			.CreateLogger();
	}
    static	ElasticsearchSinkOptions ConfigureElasticSink(IConfigurationRoot configuration,string environment)
	{
		return new ElasticsearchSinkOptions(new Uri(configuration["ElasticConfiguration:Uri"]))
		{
			AutoRegisterTemplate = true,
			IndexFormat=$"{Assembly.GetExecutingAssembly().GetName().Name.ToLower().Replace(".","-")}-{environment.ToLower()}-{DateTime.Now:yyyy-MM}",
			NumberOfReplicas=1,
			NumberOfShards=2,
			
		};
	}
}
