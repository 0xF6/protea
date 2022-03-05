namespace hub;

using System.Runtime.InteropServices;

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

