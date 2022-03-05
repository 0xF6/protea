/*
 * MIT License
 * Copyright(c) 2018 - Zhang Yuexin
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Runtime.CompilerServices;

using System;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Device.Spi;
[assembly: InternalsVisibleTo("protea-hub")]

namespace NRF24L01Plus
{
    using System.Linq;

    public class NRF24L01 : IDisposable
    {
        internal SpiDevice sensor;
        internal RaspberryPi3Driver driver;
        internal GpioController gpio;
        internal bool isPlusModel;

        internal int CS;
        internal int CE;
        internal int IRQ;

        internal byte packetSize;

        #region Address and Command

        internal const byte FEATURE = 0x1D;
        internal const byte EN_AA = 0x01;
        internal const byte EN_RXADDR = 0x02;
        public const byte SETUP_AW = 0x03;
        public const byte RF_CH = 0x05;
        public const byte RF_SETUP = 0x06;
        public const byte STATUS = 0x07;
        public const byte OBSERVE_TX = 0x08;
        public const byte RPD = 0x09;
        internal const byte RX_ADDR_P0 = 0x0A;
        internal const byte RX_ADDR_P1 = 0x0B;
        internal const byte RX_ADDR_P2 = 0x0C;
        internal const byte RX_ADDR_P3 = 0x0D;
        internal const byte RX_ADDR_P4 = 0x0E;
        internal const byte RX_ADDR_P5 = 0x0F;
        internal const byte ERX_P5 = 5;
        internal const byte ERX_P4 = 4;
        internal const byte ERX_P3 = 3;
        internal const byte ERX_P2 = 2;
        internal const byte ERX_P1 = 1;
        internal const byte ERX_P0 = 0;
        internal const byte TX_ADDR = 0x10;
        internal const byte RX_PW_P0 = 0x11;
        internal const byte RX_PW_P1 = 0x12;
        internal const byte RX_PW_P2 = 0x13;
        internal const byte RX_PW_P3 = 0x14;
        internal const byte RX_PW_P4 = 0x15;
        internal const byte RX_PW_P5 = 0x16;
        internal const byte FIFO_STATUS = 0x17;
        internal const byte DYNPD = 0x1C;

        internal const int RF_DR_LOW = 4;
        internal const int RF_DR_HIGH = 2;

        internal const byte R_REGISTER = 0x00;
        internal const byte W_REGISTER = 0x20;
        internal const byte R_RX_PAYLOAD = 0x61;
        internal const byte W_TX_PAYLOAD = 0xA0;
        internal const byte FLUSH_TX = 0xE1;
        internal const byte FLUSH_RX = 0xE2;
        internal const byte REUSE_TX_PL = 0xE3;
        internal const byte NOP = 0xFF;

        internal const byte EN_ACK_PAY = 1;
        internal const byte EN_DPL = 2;
        internal const byte DPL_P5 = 5;
        internal const byte DPL_P4 = 4;
        internal const byte DPL_P3 = 3;
        internal const byte DPL_P2 = 2;
        internal const byte DPL_P1 = 1;
        internal const byte DPL_P0 = 0;

        // CRC
        internal const byte CRC_DISABLED = 0;
        internal const byte CRC_8 = 1;
        internal const byte CRC_16 = 2;
        internal const byte CRC_ENABLED = 3;

        internal const int RX_DR = 6;
        internal const int TX_DS = 5;
        internal const int MAX_RT = 4;
        internal const int RX_P_NO = 1;
        internal const int TX_FULL = 0;

        internal readonly List<byte> pipes = new List<byte>
        {
            RX_ADDR_P0,
            RX_ADDR_P1, RX_ADDR_P2, RX_ADDR_P3, RX_ADDR_P4, RX_ADDR_P5
        };

        internal readonly List<byte> pipeEnabledFlag = new List<byte>
        {
            ERX_P0,
            ERX_P1, ERX_P2, ERX_P3, ERX_P4, ERX_P5
        };

        #endregion

        public WhatHappened WhatHappened
        {
            get
            {
                var status = ReadRegister(STATUS);

                return new WhatHappened
                {
                    DataTransfered = (status & (1 << TX_DS)) != 0,
                    MaxRetriesExceeded = (status & (1 << MAX_RT)) != 0,
                    NewDataReceived = (status & (1 << RX_DR)) != 0
                };
            }
        }

        internal void ClearTransmitEventRegisters()
        {
            WriteRegister(STATUS, (byte) ((1 << TX_DS) | (1 << MAX_RT)));
        }

        internal void ClearDataReceivedRegister()
        {
            WriteRegister(STATUS, (byte) (1 << RX_DR));
        }

        /// <summary>
        /// Create a NRF24L01 object
        /// </summary>
        /// <param name="CSN">CSN Pin</param>
        /// <param name="CE">CE Pin</param>
        /// <param name="IRQ">IRQ Pin</param>
        /// <param name="packetSize">Receive Packet Size</param>
        public NRF24L01(int CSN, int CE, int IRQ, byte packetSize)
        {
            this.CS = CSN;
            this.CE = CE;
            this.IRQ = IRQ;
            this.packetSize = packetSize;
        }


        /// <summary>
        /// Initialize
        /// </summary>
        public void Initialize(int busId = 0, GpioController controller = null)
        {
            var settings = new SpiConnectionSettings(busId, CS);
            settings.ClockFrequency = 8000000;
            settings.DataBitLength = 8;
            settings.Mode = SpiMode.Mode0;

            sensor = SpiDevice.Create(settings);
            gpio = controller ?? new GpioController(PinNumberingScheme.Logical, new RaspberryPi3Driver());

            gpio.OpenPin(CE, PinMode.Output);
            gpio.Write(CE, PinValue.High);
            gpio.OpenPin(IRQ, PinMode.Input);
            gpio.RegisterCallbackForPinValueChangedEvent(IRQ, PinEventTypes.Falling, Irq_ValueChangedDown);

            gpio.Write(CE, PinValue.Low);
            // reflect RX_DR interrupt, TX_DS interrupt not reflected, power up, receive mode
            WriteRegister(R_REGISTER, 0b_0011_1011);
            gpio.Write(CE, PinValue.High);

            Thread.Sleep(20);
            SetRxPayloadSize(packetSize);

            SetChannel(25);
            if (SetDataRate(DataRate.DR250Kbps))
            {
                isPlusModel = true;
            }

            SetCRCLength(CRCLength.CRC8);
            SetPALevel(PALevel.PA_MAX);
            EnableAckPayload();
            SetRetryPolicy(3, 15);
            MaskDataSent(false);
            MaskMaskDataReady(false);
            MaskMaxRetries(false);
            SetAutoAck(true);
            FlushTX();
            FlushRX();
        }

        public void FlushTX()
        {
            sensor.Write(new byte[] {FLUSH_TX});
        }

        public void FlushRX()
        {
            sensor.Write(new byte[] {FLUSH_RX});
        }

        public string GetStatusString() 
        {
            var status = GetStatus();
            StringBuilder details = new StringBuilder();
            details.Append($" STATUS\t = 0x{status:X2}");
            details.Append($" RX_DR={((status & (1 << RX_DR)) != 0 ? 1 : 0):X2}");
            details.Append($" TX_DS={((status & (1 << TX_DS)) != 0 ? 1 : 0):X2}");
            details.Append($" MAX_RT={((status & (1 << MAX_RT)) != 0 ? 1 : 0):X2}");
            details.Append($" RX_P_NO={((status >> RX_P_NO) & 7):X2}");
            details.AppendLine($" TX_FULL={((status & (1 << TX_FULL)) != 0 ? 1 : 0):X2}");
            return details.ToString();
        }

        public string GetDetailsString()
        {
            StringBuilder details = new StringBuilder(GetStatusString());

            details.AppendLine(GetSettingString(" RX_ADDR_P0-1", RX_ADDR_P0, 5, 2));
            details.AppendLine(GetSettingString(" RX_ADDR_P2-5", RX_ADDR_P2, 1, 4));
            details.AppendLine(GetSettingString(" TX_ADDR", TX_ADDR, 5, 1));

            details.AppendLine(GetSettingString(" RX_PW_P0-6", RX_PW_P0, 1, 6));
            details.AppendLine(GetSettingString(" EN_AA", EN_AA));
            details.AppendLine(GetSettingString(" EN_RXADDR", EN_RXADDR));
            details.AppendLine(GetSettingString(" RF_CH", RF_CH));
            details.AppendLine(GetSettingString(" RF_SETUP", RF_SETUP));
            details.AppendLine(GetSettingString(" DYNPD/FEATURE", DYNPD, 1, 2));

            details.AppendLine(Nrf24L01Config.Read(this).ToString());

            details.AppendLine($" DataRate\t = {GetDataRate()}");
            var model = "NRF24L01" + (isPlusModel ? "+" : "");
            details.AppendLine($" Model\t\t = {model}");
            details.AppendLine($" CRC Length\t = {GetCRCLength()} bits");
            details.AppendLine($" PA Power\t = {GetPALevel()}");
            return details.ToString();
        }

        internal string GetSettingString(string caption, byte register, int valueLength = 1, int registerCount = 1)
        {
            var extraTab = caption.Length < 8 ? "\t" : "";
            var result = new StringBuilder();
            result.Append($"{caption}{extraTab}\t = ");

            for (byte i = 0; i < registerCount; i++)
            {
                var value = ReadRegister((byte) (register + i), valueLength);
                result.Append("0x");
                for (int j = valueLength - 1; j >= 0; j--)
                    result.Append(value[j].ToString("X2"));
                result.Append(" ");
            }

            return result.ToString();
        }

        internal byte[] ReadRegister(byte register, int length)
        {
            byte[] response = new byte[length + 1];
            byte[] request = new byte[length + 1];
            request[0] = (byte) (R_REGISTER + register);
            for (int i = 1; i < request.Length; i++)
                request[i] = NOP;

            sensor.TransferFullDuplex(request, response);

            var result = new byte[length];
            for (int i = 0; i < length; i++)
                result[i] = response[i + 1];

            return result;
        }

        internal byte ReadRegister(byte register)
        {
            return ReadRegister(register, 1)[0];
        }

        internal void WriteRegister(byte register, byte value)
        {
            WriteRegister(register, new byte[] {value});
        }

        internal void WriteRegister(byte register, byte[] value)
        {
            byte[] buffer = new byte[value.Length + 1];
            buffer[0] = (byte) (W_REGISTER + register);
            for (int i = 0; i < value.Length; i++)
                buffer[i + 1] = value[i];

            sensor.Write(buffer);
        }

        internal byte GetStatus()
        {
            byte[] result = new byte[1];
            sensor.TransferFullDuplex(new byte[] {NOP}, result);
            return result[0];
        }

        /// <summary>
        /// Open pipe for reaceiving data
        /// </summary>
        /// <param name="pipe">Pipe address (MSB first)</param>
        /// <param name="pipeNumber">Pipe number (1-5)</param>
        public void OpenReadingPipe(byte[] pipe, byte pipeNumber)
        {
            if (pipeNumber > 5)
                throw new ArgumentException("Only pipes 0-5 are allowed");
            //pipe = pipe.Reverse().ToArray();
            if (pipeNumber > 0)
            {
                WriteRegister(pipes[pipeNumber], pipe);
                WriteRegister(EN_RXADDR, (byte) (ReadRegister(EN_RXADDR) | (1 << pipeEnabledFlag[pipeNumber])));
            }
            else
            {
                WriteRegister(RX_ADDR_P0, pipe);
            }
        }

        /// <summary>
        /// Set Receive Packet Size (All Pipe)
        /// </summary>
        /// <param name="payloadSize">Size, from 0 to 32</param>
        public void SetRxPayloadSize(byte payloadSize)
        {
            if (payloadSize > 32 || payloadSize < 0)
            {
                throw new ArgumentOutOfRangeException("payload", "payload from 0 to 32 !");
            }

            WriteRegister(RX_PW_P0, payloadSize);
            WriteRegister(RX_PW_P1, payloadSize);
            WriteRegister(RX_PW_P2, payloadSize);
            WriteRegister(RX_PW_P3, payloadSize);
            WriteRegister(RX_PW_P4, payloadSize);
            WriteRegister(RX_PW_P5, payloadSize);
        }

        /// <summary>
        /// Set retry policy
        /// </summary>
        /// <param name="count">Number of attempts</param>
        /// <param name="delay">Delay between the attempts</param>
        public void SetRetryPolicy(byte count, byte delay)
        {
            new AutomaticRetryPolicy(count, delay).Persist(this);
        }

        /// <summary>
        /// Get retry policy
        /// </summary>
        public AutomaticRetryPolicy GetRetryPolicy()
        {
            return AutomaticRetryPolicy.Read(this);
        }

        /// <summary>
        /// Set CRC Length
        /// </summary>
        /// <param name="length"></param>
        public void SetCRCLength(CRCLength length)
        {
            var cfg = Nrf24L01Config.Read(this);
            if (cfg.CRCLength != length)
            {
                cfg.CRCLength = length;
                cfg.Persist(this);
            }
        }

        /// <summary>
        /// Returns crc length (in bits)
        /// </summary>
        /// <returns></returns>
        public CRCLength GetCRCLength() => Nrf24L01Config.Read(this).CRCLength;

        /// <summary>
        /// Set PA (Power Amplification) level
        /// </summary>
        /// <param name="paLevel">New PA level</param>
        public void SetPALevel(PALevel paLevel)
        {
            var setting = ReadRegister(RF_SETUP);
            setting = (byte) (setting & ~(int) PALevel.PA_MAX);

            setting |= (byte) paLevel;
            WriteRegister(RF_SETUP, setting);
        }

        /// <summary>
        /// Get PA (Power Amplification) Level
        /// </summary>
        /// <returns></returns>
        public PALevel GetPALevel()
        {
            var setting = ReadRegister(RF_SETUP) & (byte) PALevel.PA_MAX;
            return (PALevel) setting;
        }

        /// <summary>
        /// Set Auto Acknowledgment (All Pipes)
        /// </summary>
        /// <param name="enabled">Is Enabled</param>
        public void SetAutoAck(bool enabled)
        {
            byte setting = (byte) (enabled ? 0x3F : 0x00);
            WriteRegister(EN_AA, setting);
        }

        /// <summary>
        /// Mask data ready event
        /// </summary>
        /// <param name="value"></param>
        public void MaskMaskDataReady(bool value)
        {
            var cfg = Nrf24L01Config.Read(this);
            if (cfg.MaskDataReady != value)
            {
                cfg.MaskDataReady = value;
                cfg.Persist(this);
            }
        }

        /// <summary>
        /// Mask data sent event
        /// </summary>
        /// <param name="value"></param>
        public void MaskDataSent(bool value)
        {
            var cfg = Nrf24L01Config.Read(this);
            if (cfg.MaskDataSent != value)
            {
                cfg.MaskDataSent = value;
                cfg.Persist(this);
            }
        }

        /// <summary>
        /// Mask maximum retries event
        /// </summary>
        /// <param name="value"></param>
        public void MaskMaxRetries(bool value)
        {
            var cfg = Nrf24L01Config.Read(this);
            if (cfg.MaskMaximumRetries != value)
            {
                cfg.MaskMaximumRetries = value;
                cfg.Persist(this);
            }
        }

        /// <summary>
        /// Set Power Mode
        /// </summary>
        /// <param name="mode">Power Mode</param>
        public void SetPowerMode(PowerMode mode)
        {
            var cfg = Nrf24L01Config.Read(this);
            if (cfg.Power != mode)
            {
                cfg.Power = mode;
                cfg.Persist(this);
                if (mode == PowerMode.PowerUp)
                    MicrosecondTimer.Wait(1600); // according to docs, need to wait only for 1500
            }
        }

        /// <summary>
        /// Enable ack payloads
        /// </summary>
        public void EnableAckPayload()
        {
            var setting = (byte) (ReadRegister(FEATURE) | (1 << EN_ACK_PAY) | (1 << EN_DPL));

            WriteRegister(FEATURE, setting);
            
            if (ReadRegister(FEATURE) == 0)
            {
                ToggleFeatures();
                setting = (byte) (ReadRegister(FEATURE) | (1 << EN_ACK_PAY) | (1 << EN_DPL));

                WriteRegister(FEATURE, setting);
            }
            setting = (byte) (ReadRegister(DYNPD) | (1 << DPL_P1) | (1 << DPL_P0));
            WriteRegister(DYNPD, setting);
        }

        // Enable features
        internal void ToggleFeatures()
        {
            byte activate = 0x50;
            sensor.Write(new byte[] {activate, 0x73});
        }

        /// <summary>
        /// Set Working Mode
        /// </summary>
        /// <param name="mode">Working Mode</param>
        public void SetWorkingMode(ChipWorkMode mode)
        {
            var cfg = Nrf24L01Config.Read(this);
            if (cfg.ChipMode != mode)
            {
                cfg.ChipMode = mode;
                cfg.Persist(this);
            }
        }

        /// <summary>
        /// Set Send Rate
        /// </summary>
        /// <param name="rate">Send Rate</param>
        public bool SetDataRate(DataRate rate)
        {
            bool result = false;

            byte setting = ReadRegister(RF_SETUP);
            setting = (byte) (setting & ~((byte) DataRate.DR250Kbps | (byte) DataRate.DR2Mbps));
            setting |= (byte) rate;
            WriteRegister(RF_SETUP, setting);

            result = ReadRegister(RF_SETUP) == setting;

            return result;
        }

        /// <summary>
        /// Get the Data Rate
        /// </summary>
        public DataRate GetDataRate()
        {
            var dataRate = ReadRegister(RF_SETUP) & ((byte) DataRate.DR250Kbps | (byte) DataRate.DR2Mbps);
            return (DataRate) dataRate;
        }

        /// <summary>
        /// Set Working Channel
        /// </summary>
        /// <param name="channel">From 0 to 127</param>
        public void SetChannel(byte channel)
        {
            WriteRegister(RF_CH, channel);
        }

        /// <summary>
        /// Send
        /// </summary>
        /// <param name="data">Data</param>
        public void SendData(byte[] pipeAddress, byte[] data)
        {
            pipeAddress = pipeAddress.Reverse().ToArray(); // shit library, need reverse address
            gpio.Write(CE, PinValue.Low);

            FlushTX();
            WriteRegister(RX_ADDR_P0, pipeAddress);
            WriteRegister(TX_ADDR, pipeAddress);
            WriteRegister(RX_PW_P0, packetSize);

            SetPowerMode(PowerMode.PowerUp);
            SetWorkingMode(ChipWorkMode.Transfer);

            byte[] buffer = new byte[1 + data.Length];
            buffer[0] = W_TX_PAYLOAD;
            for (int i = 0; i < data.Length; i++)
            {
                buffer[1 + i] = data[i];
            }

            sensor.Write(buffer);

            gpio.Write(CE, PinValue.High);
            MicrosecondTimer.Wait(130);
            gpio.Write(CE, PinValue.Low);

            var retryPolicy = GetRetryPolicy();
            var retries = 0;

            var whatHappened = WhatHappened;
            while (!(whatHappened.DataTransfered || whatHappened.MaxRetriesExceeded) && retries <= retryPolicy.MaxRetries)
            {
                MicrosecondTimer.Wait(retryPolicy.DelayInMicroseconds);
                whatHappened = WhatHappened;
                retries++;
            }
            Console.WriteLine($"Data Transferred: {whatHappened.DataTransfered}, retries: {retries}");

            ClearTransmitEventRegisters();

            FlushTX();
            SetWorkingMode(ChipWorkMode.Receive);
            gpio.Write(CE, PinValue.High);
        }

        /// <summary>
        /// Indicates whether new data is available
        /// </summary>
        /// <returns></returns>
        public bool NewDataAvailable()
        {
            return WhatHappened.NewDataReceived || !FifoStatus.Parse(this).RXEmpty;
        }

        /// <summary>
        /// Receive
        /// </summary>
        /// <param name="length">Packet Size</param>
        /// <returns>Data</returns>
        public byte[] Receive(byte length)
        {
            byte[] read = new byte[length + 1];
            byte[] write = new byte[length + 1];
            write[0] = R_RX_PAYLOAD;
            sensor.TransferFullDuplex(write, read);

            ClearDataReceivedRegister();

            byte[] result = new byte[length];
            Buffer.BlockCopy(read, 1, result, 0, length);

            return result;
        }

        public byte ReadPayloadLength() {
            var command = new byte[2];
            command[0] = 0b01100000;
            var result = new byte[2];
            sensor.TransferFullDuplex(command, result);
            // Console.WriteLine($"read raw: {result[1]}");
            return result[1];
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        public void Dispose()
        {
            sensor?.Dispose();
            gpio?.Dispose();
            driver?.Dispose();
        }

        /// <summary>
        /// Triggering when data was received
        /// </summary>
        public event EventHandler<ReceivedDataEventArgs> ReceivedData;


        internal void Irq_ValueChangedDown(object sender, PinValueChangedEventArgs args)
        {
            if (ReceivedData != null)
            {
                if (NewDataAvailable()) {
                    ReceivedData(sender, new ReceivedDataEventArgs(new byte[] {}));
                    FlushRX();
                    ClearDataReceivedRegister();
                }
            }
        }
    }
}
