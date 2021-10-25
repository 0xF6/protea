using System;
using System.Device.Spi;
using System.Text;
using System.Threading;
using Iot.Device.Nrf24l01;

SpiConnectionSettings senderSettings = new SpiConnectionSettings(0, 0)
{
    ClockFrequency = Nrf24l01.SpiClockFrequency,
    Mode = Nrf24l01.SpiMode
};


SpiConnectionSettings receiverSettings = new SpiConnectionSettings(1, 2)
{
    ClockFrequency = Nrf24l01.SpiClockFrequency,
    Mode = Nrf24l01.SpiMode
};


var senderDevice = SpiDevice.Create(senderSettings);
var receiverDevice = SpiDevice.Create(receiverSettings);


using (Nrf24l01 sender = new Nrf24l01(senderDevice, 23, 24, 20))
{
    using (Nrf24l01 receiver = new Nrf24l01(receiverDevice, 5, 6, 20))
    {
        // Set sender send address, receiver pipe0 address (Optional)
        byte[] receiverAddress = Encoding.UTF8.GetBytes("NRF24");
        sender.Address = receiverAddress;
        receiver.Pipe0.Address = receiverAddress;

        // Binding DataReceived event
        receiver.DataReceived += Receiver_ReceivedData;

        // Loop
        while (true)
        {
            sender.Send(Encoding.UTF8.GetBytes("Hello! .NET Core IoT"));

            Thread.Sleep(2000);
        }
    }
}


static void Receiver_ReceivedData(object sender, DataReceivedEventArgs e)
{
    var raw = e.Data;
    var msg = Encoding.UTF8.GetString(raw);

    Console.Write("Received Raw Data: ");
    foreach (var item in raw)
    {
        Console.Write($"{item} ");
    }
    Console.WriteLine();

    Console.WriteLine($"Message: {msg}");
    Console.WriteLine();
}
