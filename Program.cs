class Emulator {
    static ushort PC;
    static byte A;
    static byte X;
    static byte Y;
    static byte[] RAM = new byte[0x800];
    static byte[] ROM = new byte[0x8000];
    public static void Main(string[] args) {}

    public static byte ReadFromMemory(ushort address) {
        if (address < 0x800)
            return RAM[address];
        else if (address >= 0x8000)
            return ROM[address - 0x8000];
        else
            throw new Exception(
                $"Memory location {address} not yet implemented");
    }
}