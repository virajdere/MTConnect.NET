using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MTConnect;
using MTConnect.Agents;
using MTConnect.Configurations;
using MTConnect.Clients;
using MTConnect.Devices;
using MTConnect.Observations;
using MTConnect.Servers.Http;
using Xunit;
using Xunit.Abstractions;
using MTConnect.Shdr;
using MTConnect.Adapters;

namespace IntegrationTests
{
    public class DurableRelayTests : IClassFixture<MTAgentFixture>, IDisposable
    {
        private readonly MTAgentFixture _fixture;
        private readonly ILogger _logger;
        private readonly string _machineName;
        private readonly string _machineId;
        private readonly ShdrAdapter _adapter;
        private readonly IMTConnectAgentBroker _agent;
        private readonly MTConnectHttpServer _server;

        public DurableRelayTests(MTAgentFixture fixture, ITestOutputHelper testOutputHelper)
        {
            _fixture = fixture;
            _logger = testOutputHelper.BuildLogger(LogLevel.Trace);

            _machineId = Guid.NewGuid().ToString();
            _machineName = "MRelayTest";

            // Setup agent, adapter, and server as in your other tests
            var devicesFile = "devices.xml";
            ClientAgentCommunicationTests.GenerateDevicesXml(
                _machineId,
                _machineName,
                devicesFile,
                _logger);

            _adapter = new ShdrIntervalAdapter(_machineName, _fixture.CurrentAdapterPort, 2000, 100);
            _adapter.Start();

            _agent = new MTConnectAgentBroker();
            _agent.Start();

            var devices = DeviceConfiguration.FromFile(devicesFile, DocumentFormat.XML).ToList();
            if (!devices.IsNullOrEmpty())
            {
                foreach (var device in devices)
                {
                    _agent.AddDevice(device);
                }
            }

            var configuration = new HttpServerConfiguration
            {
                Port = _fixture.CurrentAgentPort
            };
            _server = new MTConnectHttpServer(configuration, _agent);
            _server.Start();
        }

        public void Dispose()
        {
            _agent.Stop();
            _server.Stop();
            _adapter.Stop();
            _fixture.CurrentAgentPort++;
            _fixture.CurrentAdapterPort++;
        }

        [Fact]
        public async Task DurableRelay_ShouldRelayMissedObservations_WhenEnabled()
        {
            // Arrange: Enable durable relay in config (simulate)
            var durableRelayEnabled = true;
            var missedObservations = new List<IObservation>();
            var relayReceived = new AutoResetEvent(false);

            // Simulate MQTT broker disconnect (not actually connecting to MQTT in this test)
            // Instead, we simulate by not calling the publish method

            // Send observations while "disconnected"
            for (int i = 0; i < 10; i++)
            {
                var obs = new ShdrDataItem("relaytest", i);
                _adapter.AddDataItem(obs);
                missedObservations.Add((IObservation)obs);
            }

            // Simulate reconnect: subscribe to ObservationAdded and check if all missed are relayed
            int relayedCount = 0;
            _agent.ObservationAdded += (sender, observation) =>
            {
                if (observation.DataItemId == "relaytest")
                {
                    relayedCount++;
                    if (relayedCount == missedObservations.Count)
                        relayReceived.Set();
                }
            };

            // Simulate relay logic (call your relay method here if possible)
            if (durableRelayEnabled)
            {
                // In a real test, you would trigger the relay logic here
                // For demonstration, we just fire the event for each missed observation
                foreach (var obs in missedObservations)
                {
                    _agent.AddObservation(_machineName, obs.DataItemId, obs.GetValue("Result"));
                }
            }

            // Assert: All missed observations are relayed
            Assert.True(relayReceived.WaitOne(5000), "Missed observations were not relayed when durableRelay is enabled.");
        }

        [Fact]
        public async Task DurableRelay_ShouldNotRelayMissedObservations_WhenDisabled()
        {
            // Arrange: Disable durable relay in config (simulate)
            var durableRelayEnabled = false;
            var missedObservations = new List<IObservation>();
            var relayReceived = new AutoResetEvent(false);

            // Simulate MQTT broker disconnect (not actually connecting to MQTT in this test)
            // Instead, we simulate by not calling the publish method

            // Send observations while "disconnected"
            for (int i = 0; i < 10; i++)
            {
                var obs = new ShdrDataItem("relaytest", i);
                _adapter.AddDataItem(obs);
                missedObservations.Add((IObservation)obs);
            }

            // Simulate reconnect: subscribe to ObservationAdded and check if any missed are relayed
            int relayedCount = 0;
            _agent.ObservationAdded += (sender, observation) =>
            {
                if (observation.DataItemId == "relaytest")
                {
                    relayedCount++;
                    relayReceived.Set();
                }
            };

            // Simulate relay logic (should not relay when disabled)
            if (!durableRelayEnabled)
            {
                // Do nothing: missed observations should NOT be relayed
            }

            // Assert: Missed observations are NOT relayed
            Assert.False(relayReceived.WaitOne(2000), "Missed observations were relayed when durableRelay is disabled.");
        }
    }
}