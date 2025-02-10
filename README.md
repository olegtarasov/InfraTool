# InfraWatcher

This is a small tool that lets you compare pairs of values and get the result as JSON. I built it so I could monitor
package versions of my self-hosted infrastrcture and verify backups being successful. It works both as a command-line
tool and as a HTTP server with REST API. You configure it with `config.yaml` like this:

```yaml
server:
  port: 5015
groups:
- name: versions
  items:
    - name: "Prometheus"
      comparer: version
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

For each item it gets an `actual` and `expected` values and then compares them using `comparer`.

This tool is intended to be simple and aimed at self-hosting enthusiasts rather than some hyper-scale enterprise.
It's also not written in Rust or Go; it's not blazing fast; it just gets the job done.

## Command line

You can use this tool from command line to process one group at a time:

```bash
infrawatcher run <GROUP_NAME>
```

It outputs the result to `stdout` in JSON format, which you can then pipe to `jq`. Errors are reported to `stderr`.

## REST API

You can start a simple web server which serves endpoints for all groups defined in config:
`http://[host]:[port]/api/[group]`. The server can't be configured with TLS or auth at this point; it's expected that
you will proxy it with `nginx` or any other proxy of your choise.

To start the server, run:

```bash
infrawatcher serve
```

## systemd service

On Linux running `systemd` this tool can be installed as as service:

```bash
sudo infrawatcher install
sudo systemctl start infrawatcher
```

To uninstall:

```bash
sudo infrawatcher uninstall
```

## Updating

On Linux and macOS you can update to the latest version using

```bash
sudo infrawatcher update
```

`sudo` is only required if you have `infrawatcher` running as `systemd` service. It will be restarted as part of the
update process.

## Configuration

TBD
