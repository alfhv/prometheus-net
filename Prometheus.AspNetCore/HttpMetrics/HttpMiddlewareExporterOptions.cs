namespace Prometheus.HttpMetrics
{
    public sealed class HttpMiddlewareExporterOptions
    {
        public HttpInProgressOptions InProgress { get; set; } = new HttpInProgressOptions();
        public HttpRequestCountOptions RequestCount { get; set; } = new HttpRequestCountOptions();
        public HttpRequestDurationOptions RequestDuration { get; set; } = new HttpRequestDurationOptions();

        /// <summary>
        /// Adds an additional route parameter to all the HTTP metrics.
        /// 
        /// Helper method to avoid manually adding it to each one.
        /// </summary>
        public void AddRouteParameter(HttpParameterMapping mapping)
        {
            InProgress.AdditionalParameters.Add(mapping);
            RequestCount.AdditionalParameters.Add(mapping);
            RequestDuration.AdditionalParameters.Add(mapping);
        }
    }
}