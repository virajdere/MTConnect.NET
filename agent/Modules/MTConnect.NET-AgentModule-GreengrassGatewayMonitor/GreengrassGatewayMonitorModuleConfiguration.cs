using MTConnect.Configurations;

namespace MTConnect.Modules.GreengrassGatewayMonitor
{
    /// <summary>
    /// Configuration for monitoring the AWS Greengrass Gateway via health endpoint.
    /// </summary>
    public class GreengrassGatewayMonitorModuleConfiguration : DataSourceConfiguration
    {
        /// <summary>IP or hostname of the Greengrass Gateway (e.g., 192.168.1.6)</summary>
        public string GatewayIp { get; set; }

        /// <summary>Port for the Greengrass Gateway (default: 443)</summary>
        public int Port { get; set; } = 443;

        /// <summary>Relative path to the health endpoint (default: /greengrass-gateway/_health)</summary>
        public string HealthPath { get; set; } = "/greengrass-gateway/_health";

        /// <summary>Username for Basic Authentication</summary>
        public string Username { get; set; }

        /// <summary>Password for Basic Authentication</summary>
        public string Password { get; set; }

        /// <summary>Polling interval in milliseconds</summary>
        public int ReadInterval { get; set; } = 10000;
    }
}