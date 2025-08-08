using MTConnect.Agents;
using MTConnect.Configurations;
using MTConnect.Devices;
using MTConnect.Devices.DataItems;
using MTConnect.Observations.Events;
using MTConnect.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace MTConnect.Modules.GreengrassGatewayMonitor
{
    public class GreengrassGatewayMonitorModule : MTConnectInputAgentModule
    {
        public const string ConfigurationTypeId = "greengrass-gateway-monitor";
        public const string DefaultId = "Greengrass Gateway Monitor Module";
        private readonly GreengrassGatewayMonitorModuleConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public GreengrassGatewayMonitorModule(IMTConnectAgentBroker agent, object configuration) : base(agent)
        {
            Id = DefaultId;
            _configuration = AgentApplicationConfiguration.GetConfiguration<GreengrassGatewayMonitorModuleConfiguration>(configuration);
            Configuration = _configuration;
            string username = Environment.GetEnvironmentVariable("GGM_USERNAME") ?? _configuration.Username;
            string password = Environment.GetEnvironmentVariable("GGM_PASSWORD") ?? _configuration.Password;

            _httpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                var byteArray = Encoding.ASCII.GetBytes($"{_configuration.Username}:{_configuration.Password}");
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            }
        }

        protected override IDevice OnAddDevice()
        {
            Log(MTConnectLogLevel.Information, $"Module initialized for Greengrass Gateway at {_configuration.GatewayIp}/{_configuration.HealthPath}");
            var device = new Device
            {
                Uuid = $"gateway-{_configuration.GatewayIp}",
                Id = "greengrass-gateway",
                Name = "greengrass-gateway"
            };
            device.AddDataItem<AvailabilityDataItem>();
            Log(MTConnectLogLevel.Information, $"Device added: {device.Name} ({device.Uuid})");
            return device;
        }

        protected override void OnRead()
        {
            Log(MTConnectLogLevel.Debug, "Checking Greengrass Gateway health endpoint...");
            try
            {
                var available = CheckAvailabilityAsync().GetAwaiter().GetResult();
                if (available)
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
            catch (Exception ex)
            {
                Log(MTConnectLogLevel.Error, $"Exception checking health endpoint: {ex.Message}");
                AddValueObservation<AvailabilityDataItem>(Availability.UNAVAILABLE);
            }
        }

        private async Task<bool> CheckAvailabilityAsync()
        {
            var url = $"https://{_configuration.GatewayIp}:{_configuration.Port}{_configuration.HealthPath}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // Expecting payload: { "status": "AVAILABLE" } or { "status": "UNAVAILABLE" }
            if (content.Contains("\"status\"", StringComparison.OrdinalIgnoreCase))
            {
                if (content.Contains("AVAILABLE", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (content.Contains("UNAVAILABLE", StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            // If payload is just a string "AVAILABLE" or "UNAVAILABLE"
            if (content.Trim().Equals("AVAILABLE", StringComparison.OrdinalIgnoreCase))
                return true;
            if (content.Trim().Equals("UNAVAILABLE", StringComparison.OrdinalIgnoreCase))
                return false;

            throw new Exception("Unexpected health endpoint payload: " + content);
        }
    }
}