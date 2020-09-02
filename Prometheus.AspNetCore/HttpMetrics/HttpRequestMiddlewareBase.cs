using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Prometheus.HttpMetrics
{
    /// <summary>
    /// This base class performs the data management necessary to associate the correct labels and values
    /// with HTTP request metrics, depending on the options the user has provided for the HTTP metric middleware.
    /// 
    /// The following labels are supported:
    /// 'code' (HTTP status code)
    /// 'method' (HTTP request method)
    /// 'controller' (The Controller used to fulfill the HTTP request)
    /// 'action' (The Action used to fulfill the HTTP request)
    /// Any other label - custom HTTP route parameter (if specified in options).
    /// 
    /// The 'code' and 'method' data are taken from the current HTTP context.
    /// Other labels will be taken from the request routing information.
    /// 
    /// If a custom metric is provided in the options, it must not be missing any labels for explicitly defined
    /// custom route parameters. However, it is permitted to lack any of the default labels (code/method/...).
    /// </summary>
    internal abstract class HttpRequestMiddlewareBase<TCollector, TChild>
        where TCollector : class, ICollector<TChild>
        where TChild : class, ICollectorChild
    {
        /// <summary>
        /// The set of labels from among the defaults that this metric supports.
        /// 
        /// This set will be automatically extended with labels for additional
        /// route parameters when creating the default metric instance.
        /// </summary>
        protected abstract string[] DefaultLabels { get; }

        /// <summary>
        /// Creates the default metric instance with the specified set of labels.
        /// Only used if the caller does not provide a custom metric instance in the options.
        /// </summary>
        protected abstract TCollector CreateMetricInstance(string[] labelNames);

        /// <summary>
        /// The factory to use for creating the default metric for this middleware.
        /// Not used if a custom metric is alreaedy provided in options.
        /// </summary>
        protected MetricFactory MetricFactory { get; }

        //private readonly ICollection<HttpParameterMapping> _routeParameters;

        private readonly TCollector _metric;

        private readonly Dictionary<string, HttpParameterMapping> _labelToRouteParameterMap;

        private readonly bool _labelsRequireRouteData;

        protected HttpRequestMiddlewareBase(HttpMetricsOptionsBase? options, TCollector? customMetric)
        {
            MetricFactory = Metrics.WithCustomRegistry(options?.Registry ?? Metrics.DefaultRegistry);

            _labelToRouteParameterMap = CreateDefaultsRouteParametersMap();

            if (options?.AdditionalParameters != null)
            {
                ValidateAdditionalParameterSet(options.AdditionalParameters);

                var additionlParams = CreateAditionalRouteParameterMap(options.AdditionalParameters);
                if (additionlParams.Any())
                {
                    foreach (var entry in additionlParams)
                        _labelToRouteParameterMap.Add(entry.Key, entry.Value);
                }
            }           

            //_labelToRouteParameterMap = CreateLabelToRouteParameterMap();

            if (customMetric != null)
            {
                _metric = customMetric;

                ValidateNoUnexpectedLabelNames();
                ValidateAdditionalRouteParametersPresentInMetricLabelNames();
            }
            else
            {
                _metric = CreateMetricInstance(CreateDefaultLabelSet());
            }

            _labelsRequireRouteData = _metric.LabelNames.Except(HttpRequestLabelNames.NonRouteSpecific).Any();
        }

        /// <summary>
        /// Creates the metric child instance to use for measurements.
        /// </summary>
        /// <remarks>
        /// Internal for testing purposes.
        /// </remarks>
        protected internal TChild CreateChild(HttpContext context)
        {
            if (!_metric.LabelNames.Any())
                return _metric.Unlabelled;

            if (!_labelsRequireRouteData)
                return CreateChild(context, null);

            var routeData = context.Features.Get<ICapturedRouteDataFeature>()?.Values;

            // If we have captured route data, we always prefer it.
            // Otherwise, we extract new route data right now.
            if (routeData == null)
                routeData = context.GetRouteData()?.Values;

            return CreateChild(context, routeData);
        }

        protected TChild CreateChild(HttpContext context, RouteValueDictionary? routeData)
        {
            var labelValues = new string[_metric.LabelNames.Length];

            for (var i = 0; i < labelValues.Length; i++)
            {
                switch (_metric.LabelNames[i])
                {
                    case HttpRequestLabelNames.Method:
                        labelValues[i] = context.Request.Method;
                        break;
                    case HttpRequestLabelNames.Code:
                        labelValues[i] = context.Response.StatusCode.ToString(CultureInfo.InvariantCulture);
                        break;
                    default:
                        // We validate the label set on initialization, so it must be a route parameter(or static) if we get to this point.

                        //var parameterName = _labelToRouteParameterMap[_metric.LabelNames[i]];
                        //labelValues[i] = routeData?[parameterName] as string ?? string.Empty;
                        labelValues[i] = _labelToRouteParameterMap[_metric.LabelNames[i]].GetValue(context, routeData);
                        break;
                }
            }

            return _metric.WithLabels(labelValues);
        }


        /// <summary>
        /// Creates the full set of labels supported for the current metric.
        /// 
        /// This merges (in unspecified order) the defaults from prometheus-net with any in options.AdditionalRouteParameters.
        /// </summary>
        private string[] CreateDefaultLabelSet()
        {
            // as we are creating 2 default mappings with values already included in DefaultLabels we need use Except() here to avoid duplications
            var defaultsRouteLabels = CreateDefaultsRouteParametersMap().Select(x => x.Key);
            return DefaultLabels.Concat(_labelToRouteParameterMap.Select(x => x.Key).Except(defaultsRouteLabels)).ToArray();
        }

        private void ValidateAdditionalParameterSet(List<HttpParameterMapping> additionalParameters)
        {
            /* dont validate parameterName, as they are class instances onstead of simple string, each instance can implement whatever it wants
             * even if it is a duplicate para name, the implementation could be different based on some context values
            var parameterNames = _additionalParameters.Select(x => x.ParameterName).ToList();

            if (parameterNames.Distinct(StringComparer.InvariantCultureIgnoreCase).Count() != parameterNames.Count)
                throw new ArgumentException("The set of additional route parameters to track contains multiple entries with the same parameter name.", nameof(HttpMetricsOptionsBase.AdditionalParameters));
            */
            var labelNames = _labelToRouteParameterMap.Select(x => x.Key).ToList();

            if (labelNames.Distinct(StringComparer.InvariantCultureIgnoreCase).Count() != labelNames.Count)
                throw new ArgumentException("The set of additional route parameters to track contains multiple entries with the same label name.", nameof(HttpMetricsOptionsBase.AdditionalParameters));

            var additionalLabels = additionalParameters.Select(x => x.LabelName).ToList();
            if (HttpRequestLabelNames.All.Except(additionalLabels, StringComparer.InvariantCultureIgnoreCase).Count() != HttpRequestLabelNames.All.Length)
                throw new ArgumentException($"The set of additional route parameters to track contains an entry with a reserved label name. Reserved label names are: {string.Join(", ", HttpRequestLabelNames.All)}");

            /* reserved labels will be checked above as duplicated entries
            var reservedParameterNames = new[] { "action", "controller" };

            if (reservedParameterNames.Except(parameterNames, StringComparer.InvariantCultureIgnoreCase).Count() != reservedParameterNames.Length)
                throw new ArgumentException($"The set of additional route parameters to track contains an entry with a reserved route parameter name. Reserved route parameter names are: {string.Join(", ", reservedParameterNames)}");
            */
        }

        private Dictionary<string, HttpParameterMapping> CreateDefaultsRouteParametersMap()
        {
            var map = new Dictionary<string, HttpParameterMapping>(2);

            // Defaults are hardcoded.
            map["action"] = new HttpRouteParameterMapping("action");
            map["controller"] = new HttpRouteParameterMapping("controller");

            return map;
        }

        private Dictionary<string, HttpParameterMapping> CreateAditionalRouteParameterMap(List<HttpParameterMapping> additionalParameters)
        {
            var map = new Dictionary<string, HttpParameterMapping>();

            if (additionalParameters.Any())
            {
                foreach (var entry in additionalParameters)
                    map.Add(entry.LabelName, entry);
            }

            return map;
        }

        /// <summary>
        /// Inspects the metric instance to ensure that all required labels are present.
        /// </summary>
        /// <remarks>
        /// If there are mappings to include route parameters in the labels, there must be labels defined for each such parameter.
        /// We do this automatically if we use the default metric instance but if a custom one is provided, this must be done by the caller.
        /// </remarks>
        private void ValidateAdditionalRouteParametersPresentInMetricLabelNames()
        {
            var labelNames = _labelToRouteParameterMap.Select(x => x.Key).ToList();
            var missing = labelNames.Except(_metric.LabelNames);

            if (missing.Any())
                throw new ArgumentException($"Provided custom HTTP request metric instance for {GetType().Name} is missing required labels: {string.Join(", ", missing)}.");
        }

        /// <summary>
        /// If we use a custom metric, it should not have labels that are neither defaults nor additional route parameters.
        /// </summary>
        private void ValidateNoUnexpectedLabelNames()
        {
            var allowedLabels = CreateDefaultLabelSet();
            var unexpected = _metric.LabelNames.Except(allowedLabels);

            if (unexpected.Any())
                throw new ArgumentException($"Provided custom HTTP request metric instance for {GetType().Name} has some unexpected labels: {string.Join(", ", unexpected)}.");
        }
    }
}