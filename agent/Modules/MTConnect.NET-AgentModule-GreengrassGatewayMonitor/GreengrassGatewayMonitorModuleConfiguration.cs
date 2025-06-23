using MTConnect.Configurations;

namespace MTConnect.Modules.GreengrassGatewayMonitor
{
    /// <summary>
    /// Configuration for monitoring the AWS Greengrass Gateway.
    /// </summary>
    public class GreengrassGatewayMonitorModuleConfiguration : DataSourceConfiguration
    {
        /// <summary>Hostname or IP of the Greengrass Gateway.</summary>
        public string GatewayAddress { get; set; }

        /// <summary>Port used by the Gateway (typically 8883 for MQTT over TLS).</summary>
        public int Port { get; set; }

        /// <summary>Timeout in milliseconds for Ping and TCP checks.</summary>
        public int Timeout { get; set; }

        /// <summary>Windows service names to verify (all must be running).</summary>
        public string[] WindowsServices { get; set; }

        /// <summary>Linux systemd service names to verify (all must be active).</summary>
        public string[] LinuxServices { get; set; }
    }
}