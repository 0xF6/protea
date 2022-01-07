using System;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Device.Spi;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using hub.Nrf24l01;
using Iot.Device.CpuTemperature;
using Iot.Device.Gpio.Drivers;
using Iot.Device.Nrf24l01;
using DataReceivedEventArgs = Iot.Device.Nrf24l01.DataReceivedEventArgs;


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

var IQR_PIN = 8;
var CE_PIN = 35;


using GpioController controller = new(PinNumberingScheme.Logical, new OrangePi4Driver());

Console.WriteLine($"Init");

CpuTemperature temp = new CpuTemperature();

Console.WriteLine($"temp init");


var receiverSettings = new SpiConnectionSettings(1, 0)
{
    ClockFrequency = Nrf24l01.SpiClockFrequency / 2,
    Mode = Nrf24l01.SpiMode
};

Console.WriteLine($"setting init");


var receiverDevice = SpiDevice.Create(receiverSettings);

Console.WriteLine($"spi init");
unsafe
{

    using (Nrf24l01_2 receiver = new Nrf24l01_2(receiverDevice, CE_PIN, IQR_PIN, (byte)sizeof(Payload),
               0x30, OutputPower.N18dBm, DataRate_.Rate1Mbps, PinNumberingScheme.Logical, controller))
    {
        Console.WriteLine($"enter nrf24 init");

        receiver.Pipe0.Address = new byte[] { 0x01, 0xF3, 0xF5, 0xF7, 0x8F };
        receiver.WorkingMode = WorkingMode.Receive;
        receiver.DataReceived += Receiver_ReceivedData;
        // Loop
        while (true)
        {
            var result = receiver.Receive((byte)sizeof(Payload));

            var data = ByteArrayToStructureSpan<Payload>(result);
            
            if (data.data != 0)
            {
                Console.WriteLine($"Result read: {result.Length}, data: {data.pkgID}, {data.data}");
            }
            //receiver.Send(msg);
            //receiver.Pipe0.Address = new byte[] { 0x01, 0xF3, 0xF5, 0xF7, 0x8F };
            var status = receiver.GetStatus();
            Console.WriteLine($"Status: {status:X}, payloadSize: {sizeof(Payload)}," +
                              $" pipeAddr: {string.Join(", ", receiver.Pipe0.Address.Select(x => $"{x:X}"))}");
            Thread.Sleep(300);
        }
    }
}




static void Receiver_ReceivedData(object sender, DataReceivedEventArgs e)
{
    Console.Write("Received Raw Data: ");
    var raw = e.Data;
    foreach (var item in raw)
    {
        Console.Write($"{item} ");
    }
    var payload = ByteArrayToStructure<Payload>(raw);
    Console.WriteLine();

    Console.WriteLine($"PkgID: {payload.pkgID}, data: {payload.data}");
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
    public byte crc;
    [FieldOffset(1)]
    public short pkgID;
    [FieldOffset(3)]
    public float data;
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
