using System;
using System.IO.Ports;
using System.Threading;
using CashDrawer.Shared.Models;

namespace CashDrawer.AdminTool
{
    /// <summary>
    /// Tests relay functionality on a COM port
    /// </summary>
    public class RelayTester : IDisposable
    {
        private SerialPort? _serialPort;

        /// <summary>
        /// Test the relay with specified settings
        /// </summary>
        public bool TestRelay(string comPort, RelayType relayType, double duration)
        {
            try
            {
                // Open COM port
                _serialPort = new SerialPort(comPort, 9600, Parity.None, 8, StopBits.One);
                _serialPort.Open();

                if (!_serialPort.IsOpen)
                {
                    throw new Exception($"Failed to open {comPort}");
                }

                // Convert duration to milliseconds
                var durationMs = (int)(duration * 1000);

                // Execute relay command based on type
                switch (relayType)
                {
                    case RelayType.DTR:
                        // Standard: HIGH then LOW
                        _serialPort.DtrEnable = true;
                        Thread.Sleep(durationMs);
                        _serialPort.DtrEnable = false;
                        break;

                    case RelayType.DTR_INVERTED:
                        // Inverted: LOW then HIGH
                        _serialPort.DtrEnable = false;
                        Thread.Sleep(durationMs);
                        _serialPort.DtrEnable = true;
                        break;

                    case RelayType.RTS:
                        // Standard: HIGH then LOW
                        _serialPort.RtsEnable = true;
                        Thread.Sleep(durationMs);
                        _serialPort.RtsEnable = false;
                        break;

                    case RelayType.RTS_INVERTED:
                        // Inverted: LOW then HIGH
                        _serialPort.RtsEnable = false;
                        Thread.Sleep(durationMs);
                        _serialPort.RtsEnable = true;
                        break;

                    case RelayType.BYTES_ESC:
                        // ESC p command (standard cash drawer kick)
                        _serialPort.Write(new byte[] { 27, 112, 0, 25, 250 }, 0, 5);
                        Thread.Sleep(durationMs);
                        break;

                    case RelayType.BYTES_DLE:
                        // DLE command (alternate)
                        _serialPort.Write(new byte[] { 0x10, 0x14, 0x01, 0x00, 0x05 }, 0, 5);
                        Thread.Sleep(durationMs);
                        break;

                    case RelayType.RELAY_COMMANDS:
                        // Programmable relay controller (text commands)
                        _serialPort.Write("relay on 0\r");
                        Thread.Sleep(durationMs);
                        _serialPort.Write("relay off 0\r");
                        break;

                    default:
                        throw new Exception($"Unknown relay type: {relayType}");
                }

                // Close port
                _serialPort.Close();

                return true;
            }
            catch (UnauthorizedAccessException)
            {
                // Re-throw access denied for special handling
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Relay test failed: {ex.Message}", ex);
            }
            finally
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            if (_serialPort?.IsOpen == true)
            {
                try
                {
                    _serialPort.Close();
                }
                catch { }
            }
            _serialPort?.Dispose();
        }
    }
}
