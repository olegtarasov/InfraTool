# InfraWatcher

This is a small tool to monitor your software versions against their latest versions. It works both as a command-line
tool and as a HTTP server with REST API. You configure it with YAML like this:

```yaml
server:
  port: 5015
groups:
- name: versions
  comparer: version
  items:
    - name: "Prometheus"
      actual:
        retriever:
          type: cmd
          command: prometheus --version
        processors:
          - type: regex
            expr: '^prometheus, version\s(.*?)\s'
      expected:
        retriever:
          type: cmd
          command: gh api repos/prometheus/prometheus/releases/latest -q ".tag_name"
        processors:
          - type: regex
            expr: "v(.*)"
```

For each item it gets an `actual` and `expected` versions and then compares them using `comparer`.

This tool is intended to be simple and aimed at self-hosting enthusiasts rather than some hyper-scale enterprise.
It's also not written in Rust or Go; it's not blazing fast; it just gets the job done.
