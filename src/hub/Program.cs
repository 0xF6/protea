using System;
using System.Device.Gpio;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using hub;
using Iot.Device.CpuTemperature;
using Iot.Device.Gpio.Drivers;
using NRF24L01Plus;


//while (!Debugger.IsAttached)
//{
//    Thread.Sleep(2000);
//    Console.ForegroundColor = ConsoleColor.Green;
//    Console.WriteLine("Awaiting debugger attach..");
//    Console.ForegroundColor = ConsoleColor.White;
//}


/*
 * var CE_PIN = 35;
var IQR_PIN = 33;
 */

//var IQR_PIN = 8;
//var CE_PIN = 35;

var CS_PIN = 37; // GPIO26
var CE_PIN = 35; // GPIO19
var IQR_PIN = 33; // GPIO13
var CSN_SHIFT = 0;


using GpioController controller = new(PinNumberingScheme.Board, new OrangePi4Driver());

Console.WriteLine($"Init");

CpuTemperature temp = new CpuTemperature();

Console.WriteLine($"spi init");

using var fs = File.Open("./data.csv", FileMode.OpenOrCreate);

using var st = new StreamWriter(fs);

unsafe
{
    var radio = new NRF24L01(CSN_SHIFT, CE_PIN, IQR_PIN, (byte)sizeof(Payload));
    radio.Initialize(1, controller);
    radio.SetChannel(0);
    radio.SetDataRate(DataRate.DR250Kbps);
    radio.SetPALevel(PALevel.PA_HIGH);
    radio.SetCRCLength(CRCLength.CRC8);
    radio.SetPowerMode(PowerMode.PowerUp);
    radio.SetWorkingMode(ChipWorkMode.Receive);

    radio.OpenReadingPipe(new byte[] {  0xAA, 0xAA, 0xAA, 0xAA, 0xAA }, 0);
    radio.OpenReadingPipe(new byte[] {  0xbf, 0xb4, 0xb5, 0xb6, 0x01 }, 1);
    radio.OpenReadingPipe(new byte[] {  0xbf, 0xb4, 0xb5, 0xb6, 0x02 }, 2);
    radio.OpenReadingPipe(new byte[] {  0xbf, 0xb4, 0xb5, 0xb6, 0x03 }, 3);
    radio.OpenReadingPipe(new byte[] {  0xbf, 0xb4, 0xb5, 0xb6, 0x04 }, 4);
    radio.OpenReadingPipe(new byte[] {  0xbf, 0xb4, 0xb5, 0xb6, 0x05 }, 5);

    radio.WriteRegister(NRF24L01.RX_PW_P0, 0x13);
    radio.WriteRegister(NRF24L01.RX_PW_P1, 0x13);
    radio.WriteRegister(NRF24L01.RX_PW_P2, 0x13);
    radio.WriteRegister(NRF24L01.RX_PW_P3, 0x13);
    radio.WriteRegister(NRF24L01.RX_PW_P4, 0x13);
    radio.WriteRegister(NRF24L01.RX_PW_P5, 0x13);

    radio.WriteRegister(NRF24L01.DYNPD, 0x00);
    radio.WriteRegister(NRF24L01.FEATURE, 0x00);
    
    Console.WriteLine(radio.GetDetailsString());
    Console.WriteLine(radio.GetStatusString());
    
    while (true)
    {
        if (radio.NewDataAvailable())
        {
            var data = radio.Receive((byte)sizeof(Payload));
            ReceivedData(data);
        }
        Thread.Sleep(200);

    }
}




void ReceivedData(byte[] arr)
{
    var payload = ByteArrayToStructure<Payload>(arr);
    
    Console.WriteLine($"CO2: {payload.co2}, HUM: {payload.hum}, TEMP: {payload.temp}. Power: {payload.volt}v");
    Console.WriteLine();
    st.WriteLine($"{payload.hum};{payload.temp};{payload.volt}");
    st.Flush();
}


static unsafe T ByteArrayToStructure<T>(byte[] bytes) where T : struct
{
    fixed (byte* ptr = &bytes[0])
    {
        return (T)Marshal.PtrToStructure((IntPtr)ptr, typeof(T));
    }
}

static unsafe T ByteArrayToStructureSpan<T>(Span<byte> bytes) where T : struct
{
    fixed (byte* ptr = &bytes[0])
    {
        return (T)Marshal.PtrToStructure((IntPtr)ptr, typeof(T));
    }
}

