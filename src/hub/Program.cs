using System;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Device.Spi;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Iot.Device.CpuTemperature;
using Iot.Device.Gpio.Drivers;
using Iot.Device.Nrf24l01;
using NRF24L01Plus;
using DataRate = NRF24L01Plus.DataRate;
using DataReceivedEventArgs = Iot.Device.Nrf24l01.DataReceivedEventArgs;
using PowerMode = NRF24L01Plus.PowerMode;


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


using GpioController controller = new(PinNumberingScheme.Board, new OrangePi4Driver());

Console.WriteLine($"Init");

CpuTemperature temp = new CpuTemperature();

Console.WriteLine($"spi init");
unsafe
{
    var rec = new NRF24L01(0, CE_PIN, IQR_PIN, (byte)sizeof(Payload));
    rec.Initialize(1, controller);
    rec.SetChannel(0);
    rec.SetDataRate(DataRate.DR250Kbps);
    rec.SetPALevel(PALevel.PA_HIGH);
    rec.SetCRCLength(CRCLength.CRC8);
    rec.SetPowerMode(PowerMode.PowerUp);
    rec.SetWorkingMode(ChipWorkMode.Receive);

    rec.OpenReadingPipe(new byte[] {  0xAA, 0xAA, 0xAA, 0xAA, 0xAA }, 0);
    rec.OpenReadingPipe(new byte[] {  0xbf, 0xb4, 0xb5, 0xb6, 0x01 }, 1);
    rec.OpenReadingPipe(new byte[] {  0xbf, 0xb4, 0xb5, 0xb6, 0x02 }, 2);
    rec.OpenReadingPipe(new byte[] {  0xbf, 0xb4, 0xb5, 0xb6, 0x03 }, 3);
    rec.OpenReadingPipe(new byte[] {  0xbf, 0xb4, 0xb5, 0xb6, 0x04 }, 4);
    rec.OpenReadingPipe(new byte[] {  0xbf, 0xb4, 0xb5, 0xb6, 0x05 }, 5);

    rec.WriteRegister(NRF24L01.RX_PW_P0, 0x13);
    rec.WriteRegister(NRF24L01.RX_PW_P1, 0x13);
    rec.WriteRegister(NRF24L01.RX_PW_P2, 0x13);
    rec.WriteRegister(NRF24L01.RX_PW_P3, 0x13);
    rec.WriteRegister(NRF24L01.RX_PW_P4, 0x13);
    rec.WriteRegister(NRF24L01.RX_PW_P5, 0x13);

    rec.WriteRegister(NRF24L01.DYNPD, 0x00);
    rec.WriteRegister(NRF24L01.FEATURE, 0x00);


    //rec.ReceivedData += Receiver_ReceivedData;
    // rec.SetWorkingMode(ChipWorkMode.Receive);

    Console.WriteLine(rec.GetDetailsString());
    Console.WriteLine(rec.GetStatusString());
    
    while (true)
    {
        //rec.SendData(new byte[] { 0xAA, 0xAA, 0xAA, 0xAA, 0xAA }, new byte[]{ 255 });
        //rec.SendData(new byte[] { 0xbf, 0xb4, 0xb5, 0xb6, 0x01 }, new byte[]{ 255 });
        if (rec.NewDataAvailable())
        {
            var data = rec.Receive((byte)sizeof(Payload));
            ReceivedData(data);
        }
        Thread.Sleep(200);

    }
    //using (Nrf24l01_2 receiver = new Nrf24l01_2(receiverDevice, CE_PIN, IQR_PIN, (byte)sizeof(Payload),
    //           0, OutputPower.N06dBm, DataRate_.Rate250Kbps, PinNumberingScheme.Logical, controller))
    //{
    //    Console.WriteLine($"enter nrf24 init");
    //    receiver.SetRxPipe(0, true);
    //    receiver.SetRxPipe(1, true);
    //    receiver.SetRxPipe(2, true);
    //    receiver.SetRxPipe(3, true);
    //    receiver.SetRxPipe(4, true);
    //    receiver.SetRxPipe(5, true);
    //    receiver.SetRxAddress(0, new byte[] { 0xB0, 0xB4, 0xB5, 0xB6, 0x0F });
    //    receiver.SetRxAddress(1, new byte[] { 0xB1, 0xB4, 0xB5, 0xB6, 0x0F });
    //    receiver.SetRxAddress(2, new byte[] { 0xB2, 0xB4, 0xB5, 0xB6, 0x0F });
    //    receiver.SetRxAddress(3, new byte[] { 0xB3, 0xB4, 0xB5, 0xB6, 0x0F });
    //    receiver.SetRxAddress(4, new byte[] { 0xB4, 0xB4, 0xB5, 0xB6, 0x0F });
    //    receiver.SetRxAddress(5, new byte[] { 0xB5, 0xB4, 0xB5, 0xB6, 0x0F });
    //    receiver.WorkingMode = WorkingMode.Receive;
    //    receiver.DataReceived += Receiver_ReceivedData;

    //    Console.WriteLine($"Channel: {receiver.Channel}, OutputPower: {receiver.OutputPower}, DataRate: {receiver.DataRate}");
    //    // Loop
    //    while (true)
    //    {
    //        // var result = receiver.Receive((byte)sizeof(Payload));

    //        //var data = ByteArrayToStructureSpan<Payload>(result);

    //        //receiver.Send(msg);
    //        //receiver.Pipe0.Address = new byte[] { 0x01, 0xF3, 0xF5, 0xF7, 0x8F };
    //        var status = receiver.GetStatus();
    //        Console.WriteLine($"Status: {status:X}, payloadSize: {sizeof(Payload)}," +
    //                          $" recevied: {string.Join(", ", receiver.Receive((byte)sizeof(Payload)).ToArray().Select(x => $"{x:X}"))}");
    //        Thread.Sleep(300);
    //    }
    //}
}




static void ReceivedData(byte[] arr)
{
    var payload = ByteArrayToStructure<Payload>(arr);
    
    Console.WriteLine($"CO2: {payload.co2}, HUM: {payload.hum}, TEMP: {payload.temp}. Power: {payload.volt}v");
    Console.WriteLine();
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

[StructLayout(LayoutKind.Explicit, Pack = 1)]
struct Payload
{
    [FieldOffset(0)]
    public byte crc_0;
    [FieldOffset(1)]
    public byte type;
    [FieldOffset(2)]
    public float temp;
    [FieldOffset(6)]
    public float hum;
    [FieldOffset(10)]
    public float co2;
    [FieldOffset(14)]
    public float volt;
    [FieldOffset(18)]
    public byte crc_1;
};


public class OragePiPCDriver : SunxiDriver
{
    protected override int PinCount => 28;

    protected override int ConvertPinNumberToLogicalNumberingScheme(int pinNumber) => pinNumber switch
    {
        3 => MapPinNumber('A', 12),
        5 => MapPinNumber('A', 11),
        7 => MapPinNumber('A', 6),
        8 => MapPinNumber('A', 13),
        10 => MapPinNumber('A', 14),
        11 => MapPinNumber('A', 1),
        12 => MapPinNumber('C', 14),
        13 => MapPinNumber('A', 0),
        15 => MapPinNumber('A', 3),
        16 => MapPinNumber('C', 4),
        18 => MapPinNumber('C', 7),
        19 => MapPinNumber('C', 0),
        21 => MapPinNumber('C', 1),
        22 => MapPinNumber('A', 2),
        23 => MapPinNumber('C', 2),
        24 => MapPinNumber('C', 3),
        26 => MapPinNumber('A', 21),
        27 => MapPinNumber('A', 19),
        28 => MapPinNumber('A', 18),
        29 => MapPinNumber('A', 7),
        31 => MapPinNumber('A', 8),
        32 => MapPinNumber('E', 8),
        33 => MapPinNumber('A', 9),
        35 => MapPinNumber('A', 10),
        36 => MapPinNumber('E', 9),
        37 => MapPinNumber('A', 20),
        38 => MapPinNumber('E', 6),
        39 => MapPinNumber('E', 7),
        _ => throw new ArgumentException($"Board (header) pin {pinNumber} is not a GPIO pin on the {GetType().Name} device.", nameof(pinNumber))
    };
}
