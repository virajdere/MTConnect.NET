using MTConnect.Agents;
using MTConnect.Configurations;
using MTConnect.Devices;
using MTConnect.Devices.DataItems;
using MTConnect.Observations.Events;
using MTConnect.Logging;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Linq;
using System;

namespace MTConnect.Modules.GreengrassGatewayMonitor
{
    public class GreengrassGatewayMonitorModule : MTConnectInputAgentModule
    {
        public const string ConfigurationTypeId = "greengrass-gateway-monitor";
        public const string DefaultId = "Greengrass Gateway Monitor Module";
        private readonly GreengrassGatewayMonitorModuleConfiguration _configuration;

        public GreengrassGatewayMonitorModule(IMTConnectAgentBroker agent, object configuration) : base(agent)
        {
            Id = DefaultId;
            _configuration = AgentApplicationConfiguration.GetConfiguration<GreengrassGatewayMonitorModuleConfiguration>(configuration);
            Configuration = _configuration;
        }

        protected override IDevice OnAddDevice()
        {
            Log(MTConnectLogLevel.Information, $"Module initialized with GatewayAddress={_configuration.GatewayAddress}, ReadInterval={_configuration.ReadInterval}ms");
            var device = new Device
            {
                Uuid = $"gateway-{_configuration.GatewayAddress}",
                Id = "greengrass-gateway",
                Name = "greengrass-gateway"
            };
            device.AddDataItem<AvailabilityDataItem>();
            Log(MTConnectLogLevel.Information, $"Device added: {device.Name} ({device.Uuid})");
            return device;
        }

        protected override void OnRead()
        {
            Log(MTConnectLogLevel.Debug, "Checking Greengrass Gateway status...");

            bool isPingOk = PingGateway(_configuration.GatewayAddress);
            Log(MTConnectLogLevel.Debug, $"Ping {_configuration.GatewayAddress}: {(isPingOk ? "Success" : "Failed")}");

            bool isPortOpen = IsPortOpen(_configuration.GatewayAddress, _configuration.Port);
            Log(MTConnectLogLevel.Debug, $"Port open: {(isPortOpen ? "Yes" : "No")}");

            bool isServiceRunning = IsGreengrassServiceRunning();
            Log(MTConnectLogLevel.Debug, $"Greengrass service running: {(isServiceRunning ? "Yes" : "No")}");

            if (isPingOk && isPortOpen && isServiceRunning)
            {
                Log(MTConnectLogLevel.Information, "Gateway is AVAILABLE");
                AddValueObservation<AvailabilityDataItem>(Availability.AVAILABLE);
            }
            else
            {
                Log(MTConnectLogLevel.Warning, "Gateway is UNAVAILABLE");
                AddValueObservation<AvailabilityDataItem>(Availability.UNAVAILABLE);
            }
        }

        private bool PingGateway(string address)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send(address, _configuration.Timeout);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch (Exception ex)
            {
                Log(MTConnectLogLevel.Warning, $"Ping exception: {ex.Message}");
                return false;
            }
        }

        private bool IsPortOpen(string host, int port)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var task = client.ConnectAsync(host, port);
                    bool success = task.Wait(_configuration.Timeout);
                    return success && client.Connected;
                }
            }
            catch (Exception ex)
            {
                Log(MTConnectLogLevel.Warning, $"Port check exception: {ex.Message}");
                return false;
            }
        }

        private bool IsGreengrassServiceRunning()
        {
            if (OperatingSystem.IsWindows())
            {
                return _configuration.WindowsServices.All(service =>
                {
                    try
                    {
                        using var sc = new ServiceController(service);
                        var running = sc.Status == ServiceControllerStatus.Running;
                        Log(MTConnectLogLevel.Debug, $"Windows service '{service}' running: {running}");
                        return running;
                    }
                    catch (Exception ex)
                    {
                        Log(MTConnectLogLevel.Debug, $"Windows service '{service}' not found: {ex.Message}");
                        return false;
                    }
                });
            }
            
            if (OperatingSystem.IsLinux())
            {
                return _configuration.LinuxServices.All(service =>
                {
                    try
                    {
                        var psi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "/usr/bin/env",
                            Arguments = $"systemctl is-active {service}",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        using var process = System.Diagnostics.Process.Start(psi);
                        var output = process.StandardOutput.ReadToEnd().Trim();
                        process.WaitForExit();

                        var active = output == "active";
                        Log(MTConnectLogLevel.Debug, $"Linux service '{service}' active: {active}");
                        return active;
                    }
                    catch (Exception ex)
                    {
                        Log(MTConnectLogLevel.Warning, $"Linux service check failed for '{service}': {ex.Message}");
                        return false;
                    }
                });
            }

            Log(MTConnectLogLevel.Warning, "Unsupported platform for service check.");
            return false;
        }
    }
}