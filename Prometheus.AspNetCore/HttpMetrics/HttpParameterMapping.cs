using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Prometheus.HttpMetrics
{
    public abstract class HttpParameterMapping
    {
        public string LabelName { get; set; }

        public HttpParameterMapping(string labelName)
        {
            Collector.ValidateLabelName(labelName);
            LabelName = labelName;
        }

        public abstract string GetValue(HttpContext context, RouteValueDictionary? routeData);
    }
}
