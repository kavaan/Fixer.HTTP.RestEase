using System;
using System.Linq;
using System.Net.Http;
using Fixer.Discovery.Consul;
using Fixer.HTTP.RestEase.Builders;
using Fixer.HTTP.RestEase.Serializers;
using Fixer.LoadBalancing.Fabio;
using Microsoft.Extensions.DependencyInjection;
using RestEase;

namespace Fixer.HTTP.RestEase
{
    public static class Extensions
    {
        private const string SectionName = "restEase";
        private const string RegistryName = "http.restEase";

        public static IFixerBuilder AddServiceClient<T>(this IFixerBuilder builder, string serviceName,
            string sectionName = SectionName, string consulSectionName = "consul", string fabioSectionName = "fabio",
            string httpClientSectionName = "httpClient")
            where T : class
        {
            var restEaseOptions = builder.GetOptions<RestEaseOptions>(sectionName);
            return builder.AddServiceClient<T>(serviceName, restEaseOptions,
                b => b.AddFabio(fabioSectionName, consulSectionName, httpClientSectionName));
        }

        public static IFixerBuilder AddServiceClient<T>(this IFixerBuilder builder, string serviceName,
            Func<IRestEaseOptionsBuilder, IRestEaseOptionsBuilder> buildOptions,
            Func<IConsulOptionsBuilder, IConsulOptionsBuilder> buildConsulOptions,
            Func<IFabioOptionsBuilder, IFabioOptionsBuilder> buildFabioOptions,
            HttpClientOptions httpClientOptions)
            where T : class
        {
            var options = buildOptions(new RestEaseOptionsBuilder()).Build();
            return builder.AddServiceClient<T>(serviceName, options,
                b => b.AddFabio(buildFabioOptions, buildConsulOptions, httpClientOptions));
        }

        public static IFixerBuilder AddServiceClient<T>(this IFixerBuilder builder, string serviceName,
            RestEaseOptions options, ConsulOptions consulOptions, FabioOptions fabioOptions,
            HttpClientOptions httpClientOptions)
            where T : class
            => builder.AddServiceClient<T>(serviceName, options,
                b => b.AddFabio(fabioOptions, consulOptions, httpClientOptions));

        private static IFixerBuilder AddServiceClient<T>(this IFixerBuilder builder, string serviceName,
            RestEaseOptions options, Action<IFixerBuilder> registerFabio)
            where T : class
        {
            if (!builder.TryRegister(RegistryName))
            {
                return builder;
            }

            var clientName = typeof(T).ToString();

            switch (options.LoadBalancer?.ToLowerInvariant())
            {
                case "consul":
                    builder.AddConsulHttpClient(clientName, serviceName);
                    break;
                case "fabio":
                    builder.AddFabioHttpClient(clientName, serviceName);
                    break;
                default:
                    ConfigureDefaultClient(builder.Services, clientName, serviceName, options);
                    break;
            }

            ConfigureForwarder<T>(builder.Services, clientName);

            registerFabio(builder);

            return builder;
        }

        private static void ConfigureDefaultClient(IServiceCollection services, string clientName,
            string serviceName, RestEaseOptions options)
        {
            services.AddHttpClient(clientName, client =>
            {
                var service = options.Services.SingleOrDefault(s => s.Name.Equals(serviceName,
                    StringComparison.InvariantCultureIgnoreCase));
                if (service == null)
                {
                    throw new RestEaseServiceNotFoundException($"RestEase service: '{serviceName}' was not found.",
                        serviceName);
                }

                client.BaseAddress = new UriBuilder
                {
                    Scheme = service.Scheme,
                    Host = service.Host,
                    Port = service.Port
                }.Uri;
            });
        }

        private static void ConfigureForwarder<T>(IServiceCollection services, string clientName) where T : class
        {
            services.AddTransient<T>(c => new RestClient(c.GetService<IHttpClientFactory>().CreateClient(clientName))
            {
                RequestQueryParamSerializer = new QueryParamSerializer()
            }.For<T>());
        }
    }
}
