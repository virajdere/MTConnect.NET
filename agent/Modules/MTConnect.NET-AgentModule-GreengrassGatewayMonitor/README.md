![MTConnect.NET Logo](https://raw.githubusercontent.com/TrakHound/MTConnect.NET/master/img/mtconnect-net-03-md.png)

# MTConnect Greengrass Gateway Monitor Agent Module

This Agent Module monitors the availability of an AWS Greengrass Gateway and exposes its status as an MTConnect `availability` DataItem for integration with the MTConnect.NET Agent.

## Features

- Periodically checks Greengrass Gateway availability via:
  - Network ping
  - TCP port check
  - Service status (Windows and Linux)
- Exposes gateway status as an MTConnect device with an `availability` DataItem
- Highly configurable via the agent’s YAML configuration

## Configuration

Add the following to your `agent.config.yaml` under the `modules:` section:

```yaml
modules:
  - greengrass-gateway-monitor:
      gatewayAddress: localhost
      readInterval: 10000
      port: 8883
      timeout: 2000
      windowsServices: [ "Greengrass", "GreengrassCredentialManagerService" ]
      linuxServices: [ "greengrass.service", "greengrasscredentialmanagerservice.service" ]
```

**Configuration Options:**

| Option           | Description                                                      | Example                                      |
|------------------|------------------------------------------------------------------|----------------------------------------------|
| gatewayAddress   | Hostname or IP of the Greengrass Gateway                         | `localhost`                                  |
| readInterval     | Polling interval in milliseconds                                 | `10000`                                      |
| port             | TCP port to check (typically 8883 for MQTT over TLS)             | `8883`                                       |
| timeout          | Timeout in milliseconds for ping and TCP checks                  | `2000`                                       |
| windowsServices  | List of Windows service names to verify (all must be running)    | `[ "Greengrass", "GreengrassCredentialManagerService" ]` |
| linuxServices    | List of Linux systemd service names to verify (all must be active)| `[ "greengrass.service", "greengrasscredentialmanagerservice.service" ]` |

## Accessing the Gateway Status

- The module adds a device named `greengrass-gateway` with an `availability` DataItem.
- Query the agent’s `/current` or `/sample` endpoints (e.g., `http://localhost:5000/current`) and look for the `availability` DataItem under the `greengrass-gateway` device.

Example XML snippet:
```xml
<DeviceStream name="greengrass-gateway">
  <Events>
    <Availability dataItemId="..." timestamp="...">AVAILABLE</Availability>
  </Events>
</DeviceStream>
```

## Contribution / Feedback

- Please use the [Issues](https://github.com/TrakHound/MTConnect.NET/issues) tab to report problems or request features.
- Use the [Pull Requests](https://github.com/TrakHound/MTConnect.NET/pulls) tab for code contributions.
- For other questions, contact TrakHound at **info@trakhound.com**.

## License

This module and its source code are licensed under the [MIT License](https://choosealicense.com/licenses/mit/) and are free to use.