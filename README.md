# prometheus-net "extended"

This repo is a fork of https://github.com/prometheus-net/prometheus-net

Changes on this repo:

- Use common class(hierarchy) to represent additional parameters to add to default metrics (asp.net pipeline)

- Allow to add static label

All code changes are in "Prometheus.AspNetCore\HttpMetrics"

How to add custom labels

We can add two custom labels:

1- Route Parameter label
This is an existing implementation, changes are just around how to declare route parameter

2- Static label parameter
This is something missing in original library. My use case is to identify metrics based on machine groups, ex: cluster, hubs, etc.


sample code

```
app.UseHttpMetrics(options =>
                    {
                        // add label "cluster" for all metrics
                        options.AddRouteParameter(new HttpStaticParameterMapping("cluster", "western_europe"));
                        // add label "myparam" only for duration
                        options.RequestDuration.AdditionalParameters.Add(new HttpRouteParameterMapping("myparam"));
                        // add label "method_param" only for counts
                        options.RequestCount.AdditionalParameters.Add(new HttpRouteParameterMapping("method", "method_param"));
                    });
```

what we have on metrics:


