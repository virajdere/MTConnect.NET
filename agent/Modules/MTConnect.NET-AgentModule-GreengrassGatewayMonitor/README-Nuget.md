![MTConnect.NET Logo](https://raw.githubusercontent.com/TrakHound/MTConnect.NET/master/img/mtconnect-net-03-md.png)

# MTConnect Greengrass Gateway Monitor Agent Module

This module monitors AWS Greengrass Gateway availability and exposes its status as an MTConnect device with an `availability` DataItem.

## Configuration Example

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

## Usage

- Add the module to your MTConnect.NET Agent.
- Query the `/current` endpoint to get the gateway status.

## License

MIT License