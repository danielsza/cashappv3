using System;
using System.IO.Ports;
using System.Threading;
using CashDrawer.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CashDrawer.Server.Services
{
    /// <summary>
    /// Handles serial port (COM port) communication for relay control
    /// </summary>
    public class SerialPortService : IDisposable
    {
        private readonly ILogger<SerialPortService> _logger;
        private readonly ServerConfig _config;
        private SerialPort? _serialPort;
        private readonly object _lock = new();

        public SerialPortService(
            ILogger<SerialPortService> logger,
            IOptions<ServerConfig> config)
        {
            _logger = logger;
            _config = config.Value;
        }

        /// <summary>
        /// Initialize serial port connection
        /// </summary>
        public bool Initialize()
        {
            try
            {
                // TEST MODE: Skip COM port initialization
                if (_config.TestMode)
                {
                    _logger.LogInformation("🧪 TEST MODE: Skipping COM port initialization");
                    return true;
                }

                lock (_lock)
                {
                    if (_serialPort?.IsOpen == true)
                        return true;

                    // Create port with exact SimpleLed-DTR settings
                    _serialPort = new SerialPort(_config.COMPort)
                    {
                        BaudRate = 9600,
                        Parity = Parity.None,
                        DataBits = 8,
                        StopBits = StopBits.One,
                        Handshake = Handshake.None,
                        ReadTimeout = 500,
                        WriteTimeout = 500
                    };
                    
                    _serialPort.Open();
                    
                    // CRITICAL: Initialize DTR/RTS to LOW state
                    // This establishes a baseline so hardware can detect state changes
                    // Based on SimpleLed-DTR working implementation
                    _serialPort.DtrEnable = false;
                    _serialPort.RtsEnable = false;
                    Thread.Sleep(100); // Allow hardware to stabilize
                    
                    _logger.LogInformation($"Serial port {_config.COMPort} initialized successfully (DTR/RTS baseline: LOW)");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to initialize serial port {_config.COMPort}");
                return false;
            }
        }

        /// <summary>
        /// Open cash drawer using configured relay method
        /// </summary>
        public bool OpenDrawer()
        {
            try
            {
                // TEST MODE: Simulate drawer opening without actually triggering relay
                if (_config.TestMode)
                {
                    _logger.LogInformation("🧪 TEST MODE: Simulating drawer open (relay NOT triggered)");
                    var duration = (int)(_config.RelayDuration * 1000);
                    Thread.Sleep(duration); // Simulate the delay
                    _logger.LogInformation($"🧪 TEST MODE: Would use {_config.RelayPin} for {duration}ms on {_config.COMPort}");
                    return true;
                }

                lock (_lock)
                {
                    if (_serialPort?.IsOpen != true)
                    {
                        if (!Initialize())
                            return false;
                    }

                    var duration = (int)(_config.RelayDuration * 1000); // Convert to milliseconds

                    switch (_config.RelayPin)
                    {
                        case RelayType.DTR:
                            _serialPort!.DtrEnable = true;
                            Thread.Sleep(duration);
                            _serialPort.DtrEnable = false;
                            break;

                        case RelayType.DTR_INVERTED:
                            _serialPort!.DtrEnable = false;
                            Thread.Sleep(duration);
                            _serialPort.DtrEnable = true;
                            break;

                        case RelayType.RTS:
                            _serialPort!.RtsEnable = true;
                            Thread.Sleep(duration);
                            _serialPort.RtsEnable = false;
                            break;

                        case RelayType.RTS_INVERTED:
                            _serialPort!.RtsEnable = false;
                            Thread.Sleep(duration);
                            _serialPort.RtsEnable = true;
                            break;

                        case RelayType.BYTES_ESC:
                            // ESC p command (standard cash drawer kick)
                            _serialPort!.Write(new byte[] { 27, 112, 0, 25, 250 }, 0, 5);
                            Thread.Sleep(duration);
                            break;

                        case RelayType.BYTES_DLE:
                            // DLE command (alternate)
                            _serialPort!.Write(new byte[] { 0x10, 0x14, 0x01, 0x00, 0x05 }, 0, 5);
                            Thread.Sleep(duration);
                            break;

                        case RelayType.RELAY_COMMANDS:
                            // Programmable relay controller (text commands)
                            // This matches the VB6 app: "relay on 0\r" and "relay off 0\r"
                            _serialPort!.Write("relay on 0\r");
                            Thread.Sleep(duration);
                            _serialPort.Write("relay off 0\r");
                            break;

                        default:
                            _logger.LogWarning($"Unknown relay type: {_config.RelayPin}");
                            return false;
                    }

                    _logger.LogDebug($"Drawer opened using {_config.RelayPin}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open drawer");
                return false;
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_serialPort?.IsOpen == true)
                {
                    _serialPort.Close();
                    _logger.LogInformation("Serial port closed");
                }
                _serialPort?.Dispose();
            }
        }
    }
}
