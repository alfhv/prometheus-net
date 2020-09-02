﻿using Microsoft.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Prometheus;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Prometheus.HttpMetrics;

namespace tester
{
    /// <summary>
    /// This targets ASP.NET Core 3.
    /// </summary>
    internal class AspNetCoreMiddlewareTester : Tester
    {
        // Sinaled when it is time for the web server to stop.
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private Task _webserverTask;

        public override void OnStart()
        {
            _webserverTask =
                WebHost.CreateDefaultBuilder()
                .UseUrls($"http://localhost:{TesterConstants.TesterPort}")
                .ConfigureServices(services =>
                {
                    services.AddControllers();
                })
                .Configure(app =>
                {
                    app.UseMetricServer();

                    app.UseRouting();

                    app.UseHttpMetrics(options =>
                    {
                        // add label "cluster" for all metrics
                        options.AddRouteParameter(new HttpStaticParameterMapping("cluster", "western_europe"));
                        // add label "myparam" only for duration
                        options.RequestDuration.AdditionalParameters.Add(new HttpRouteParameterMapping("myparam"));
                        // add label "method_param" only for counts
                        options.RequestCount.AdditionalParameters.Add(new HttpRouteParameterMapping("method", "method_param"));
                    });

                    // keep it at latest regitration
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();
                    });
                })
                .ConfigureLogging(logging => logging.ClearProviders())
                .Build()
                .RunAsync(_cts.Token);
        }

        public override void OnTimeToObserveMetrics()
        {
            // Every time we observe metrics, we also asynchronously perform a dummy request for test data.
            //StartDummyRequest();

            var httpRequest = (HttpWebRequest)WebRequest.Create($"http://localhost:{TesterConstants.TesterPort}/metrics");
            httpRequest.Method = "GET";

            using (var httpResponse = (HttpWebResponse)httpRequest.GetResponse())
            {
                var text = new StreamReader(httpResponse.GetResponseStream()).ReadToEnd();
                Console.WriteLine(text);
            }
        }

        private void StartDummyRequest()
        {
            Task.Run(delegate
            {
                var httpRequest = (HttpWebRequest)WebRequest.Create($"http://localhost:{TesterConstants.TesterPort}/api/Dummy");
                httpRequest.Method = "GET";

                httpRequest.GetResponse().Dispose();
            });
        }

        public override void OnEnd()
        {
            _cts.Cancel();

            try
            {
                _webserverTask.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
            }
        }

        public override IMetricServer InitializeMetricServer() => null;
    }
}
