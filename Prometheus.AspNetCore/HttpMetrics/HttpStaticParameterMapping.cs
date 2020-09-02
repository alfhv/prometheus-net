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
    public sealed class HttpStaticParameterMapping : HttpParameterMapping
    {
        public string LabelValue { get; set; }

        public HttpStaticParameterMapping(string labelName, string labelValue) : base(labelName)
        {
            LabelValue = labelValue;
        }

        public override string GetValue(HttpContext context, RouteValueDictionary? routeData)
        {
            return LabelValue;
        }

        //public static implicit operator HttpStaticParameterMapping(string name, string value) => new HttpStaticParameterMapping(name, value);
    }
}
