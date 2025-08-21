using System;

public static class IdGenerator
{
    private static readonly Random random = new Random();

    public static ulong GenerateRandomId()
    {
        byte[] buffer = new byte[8]; // 8 bytes for a ulong
        random.NextBytes(buffer);
        return BitConverter.ToUInt64(buffer, 0);
    }
}