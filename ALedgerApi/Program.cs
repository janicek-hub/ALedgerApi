
using RestDWH.Base.Model;
using RestDWH.Base.Repository;
using RestDWH.Base.Extensions;
using AlgorandAuthentication;
using Elasticsearch.Net;
using Microsoft.OpenApi.Models;
using Nest;
using Microsoft.Extensions.DependencyInjection;
using RestDWH.Elastic.Extensions;
using RestDWH.Elastic.Repository;
using RestDWH.Base.Extensios;

namespace ALedgerApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers().AddNewtonsoftJson();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "ALedgerApi",
                    Version = "v1",
                    Description = File.ReadAllText("doc/readme.md")
                });
                c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Description = "ARC-0014 Algorand authentication transaction",
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                });
                c.OperationFilter<Swashbuckle.AspNetCore.Filters.SecurityRequirementsOperationFilter>();
                c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

                var xmlFile = $"doc/documentation.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            });
            builder.Services.AddProblemDetails();

            var algorandAuthenticationOptions = new AlgorandAuthenticationOptions();
            builder.Configuration.GetSection("AlgorandAuthentication").Bind(algorandAuthenticationOptions);

            builder.Services
             .AddAuthentication(AlgorandAuthenticationHandler.ID)
             .AddAlgorand(o =>
             {
                 o.CheckExpiration = algorandAuthenticationOptions.CheckExpiration;
                 o.Debug = algorandAuthenticationOptions.Debug;
                 o.AlgodServer = algorandAuthenticationOptions.AlgodServer;
                 o.AlgodServerToken = algorandAuthenticationOptions.AlgodServerToken;
                 o.AlgodServerHeader = algorandAuthenticationOptions.AlgodServerHeader;
                 o.Realm = algorandAuthenticationOptions.Realm;
                 o.NetworkGenesisHash = algorandAuthenticationOptions.NetworkGenesisHash;
                 o.MsPerBlock = algorandAuthenticationOptions.MsPerBlock;
                 o.EmptySuccessOnFailure = algorandAuthenticationOptions.EmptySuccessOnFailure;
                 o.EmptySuccessOnFailure = algorandAuthenticationOptions.EmptySuccessOnFailure;
             });

            var elasticConfig = new Model.Config.Elastic();
            builder.Configuration.GetSection("Elastic").Bind(elasticConfig);

            var settings =
                new ConnectionSettings(new Uri(elasticConfig.Server))
                .ApiKeyAuthentication(new ApiKeyAuthenticationCredentials(elasticConfig.Token))
                .ExtendElasticConnectionSettings();

            var client = new ElasticClient(settings);
            builder.Services.AddSingleton<IElasticClient>(client);
            //Address
            builder.Services.AddSingleton<IDWHRepository<Model.Address>, RestDWHElasticSearchRepository<Model.Address>>();
            builder.Services.AddSingleton<RestDWHEvents<Model.Address>>();
            //Person
            builder.Services.AddSingleton<IDWHRepository<Model.Person>, RestDWHElasticSearchRepository<Model.Person>>();
            builder.Services.AddSingleton<RestDWHEvents<Model.Person>>();
            //Invoice
            builder.Services.AddSingleton<IDWHRepository<Model.Invoice>, RestDWHElasticSearchRepository<Model.Invoice>>();
            builder.Services.AddSingleton<RestDWHEvents<Model.Invoice>>();
            //InvoiceItem
            builder.Services.AddSingleton<IDWHRepository<Model.InvoiceItem>, RestDWHElasticSearchRepository<Model.InvoiceItem>>();
            builder.Services.AddSingleton<RestDWHEvents<Model.InvoiceItem>>();
            //PaymentMethod
            builder.Services.AddSingleton<IDWHRepository<Model.PaymentMethod>, RestDWHElasticSearchRepository<Model.PaymentMethod>>();
            builder.Services.AddSingleton<RestDWHEvents<Model.PaymentMethod>>();

            var app = builder.Build();

            var resp = client.Ping();
            if (!resp.IsValid)
            {
                throw new Exception("Connection to elastic has not been established");
            }

            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            //{
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseAuthentication();
            app.UseAuthorization();
            //}
            var serviceAddress = app.Services.GetService<IDWHRepository<Model.Address>>();
            app.MapEndpoints<Model.Address>(serviceAddress);

            var servicePerson = app.Services.GetService<IDWHRepository<Model.Person>>();
            app.MapEndpoints<Model.Person>(servicePerson);

            var serviceInvoice = app.Services.GetService<IDWHRepository<Model.Invoice>>();
            app.MapEndpoints<Model.Invoice>(serviceInvoice);

            var serviceInvoiceItem = app.Services.GetService<IDWHRepository<Model.InvoiceItem>>();
            app.MapEndpoints<Model.InvoiceItem>(serviceInvoiceItem);

            var servicePaymentMethod = app.Services.GetService<IDWHRepository<Model.PaymentMethod>>();
            app.MapEndpoints<Model.PaymentMethod>(servicePaymentMethod);

            app.MapControllers();

            app.Run();
        }
    }
}