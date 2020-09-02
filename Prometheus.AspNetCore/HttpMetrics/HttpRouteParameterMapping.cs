using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Prometheus.HttpMetrics
{
    /// <summary>
    /// Maps an HTTP route parameter name to a Prometheus label name.
    /// </summary>
    /// <remarks>
    /// Typically, the parameter name and the label name will be equal.
    /// The purpose of this is to enable capture of route parameters that conflict with built-in label names like "method" (HTTP method).
    /// </remarks>
    public sealed class HttpRouteParameterMapping : HttpParameterMapping
    {
        /// <summary>
        /// Name of the HTTP route parameter.
        /// </summary>
        public string ParameterName { get; }

        public HttpRouteParameterMapping(string labelName) : this(labelName, labelName)
        {
        }

        public HttpRouteParameterMapping(string parameterName, string labelName) : base(labelName)
        {
            ParameterName = parameterName;
        }

        public static implicit operator HttpRouteParameterMapping(string name) => new HttpRouteParameterMapping(name);

        public override string GetValue(HttpContext context, RouteValueDictionary? routeData)
        {
            return routeData?[ParameterName] as string ?? string.Empty;
        }
    }
}
