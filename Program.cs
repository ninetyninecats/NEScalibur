using Microsoft.VisualBasic;

class Emulator {
    static ushort PC;
    static byte A;
    static byte X;
    static byte Y;
    static byte SP;
    static bool carryFlag;
    static bool zeroFlag;
    static bool interruptDisableFlag;
    static bool decimalFlag;
    static bool overflowFlag;
    static bool negativeFlag;

    static byte[] RAM = new byte[0x800];
    static byte[] ROM = new byte[0x8000];
    static byte[] ROMHeader = new byte[0x10];
    static string ROMFilePath;
    static bool halted = false;
    public static void Main(string[] args) {
        if (args.Length != 1)
            throw new Exception("Usage: NEScalibur [path to rom]");
        ROMFilePath = args[0];
        Reset();
        Run();
        Console.WriteLine("A: " + A.ToString("X2"));
        Console.WriteLine("X: " + X.ToString("X2"));
        Console.WriteLine("Y: " + Y.ToString("X2"));
        Console.WriteLine("PC: " + PC.ToString("X2"));
    }

    public static void Run() {
        while (!halted)
            RunCPU();
    }

    public static void RunCPU() {
        byte temp;
        byte tempLow;
        byte tempHigh;
        byte opcode = Read(PC);
        PC += 1;

        switch (opcode) {
        case 0x02: // HLT
            halted = true;
            break;
        case 0x10: // BPL, 2 cycles, 3 if branch was taken, 4 if a page was
                   // crossed
            temp = Read(PC);
            PC += 1;
            if (!negativeFlag) {
                int offset = temp;
                if (offset > 127)
                    offset -= 256;
                PC = (ushort)(PC + offset);
            }
            break;
        case 0x20: // JSR, 6 cycles
            tempLow = Read(PC);
            PC += 1;
            tempHigh = Read(PC);
            Push((byte)(PC >> 8));
            Push((byte)PC);
            PC = (ushort)(tempLow + tempHigh * 0x100);
            break;
        case 0x30: // BMI, 2 cycles, 3 if branch was taken, 4 if a page was
                   // crossed
            temp = Read(PC);
            PC += 1;
            if (negativeFlag) {
                int offset = temp;
                if (offset > 127)
                    offset -= 256;
                PC = (ushort)(PC + offset);
            }
            break;
        case 0x48: // PHA, 3 cycles
            Push(A);
            break;
        case 0x4C: // JMP, 3 cycles
            tempLow = Read(PC);
            PC += 1;
            tempHigh = Read(PC);
            PC = (ushort)(tempLow + tempHigh * 0x100);
            break;
        case 0x50: // BVC, 2 cycles, 3 if branch was taken, 4 if a page was
                   // crossed
            temp = Read(PC);
            PC += 1;
            if (!overflowFlag) {
                int offset = temp;
                if (offset > 127)
                    offset -= 256;
                PC = (ushort)(PC + offset);
            }
            break;
        case 0x60: // RTS, 6 cycles
            tempLow = Pull();
            tempHigh = Pull();
            PC = (ushort)(tempLow + tempHigh * 0x100);
            PC += 1;
            break;
        case 0x68: // PLA, 4 cycles
            A = Pull();
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x70: // BVS, 2 cycles, 3 if branch was taken, 4 if a page was
                   // crossed
            temp = Read(PC);
            PC += 1;
            if (overflowFlag) {
                int offset = temp;
                if (offset > 127)
                    offset -= 256;
                PC = (ushort)(PC + offset);
            }
            break;
        case 0x84: // STY Zero Page, 3 cycles
            temp = Read(PC);
            PC += 1;
            Write(Y, temp);
            break;
        case 0x85: // STA Zero Page, 3 cycles
            temp = Read(PC);
            PC += 1;
            Write(A, temp);
            break;
        case 0x86: // STX Zero Page, 3 cycles
            temp = Read(PC);
            PC += 1;
            Write(X, temp);
            break;
        case 0x8C: // STY Absolute, 4 cycles
            tempLow = Read(PC);
            PC += 1;
            tempHigh = Read(PC);
            PC += 1;
            Write(Y, (ushort)(tempLow + tempHigh * 0x100));
            break;
        case 0x8D: // STA Absolute, 4 cycles
            tempLow = Read(PC);
            PC += 1;
            tempHigh = Read(PC);
            PC += 1;
            Write(A, (ushort)(tempLow + tempHigh * 0x100));
            break;
        case 0x8E: // STX Absolute, 4 cycles
            tempLow = Read(PC);
            PC += 1;
            tempHigh = Read(PC);
            PC += 1;
            Write(X, (ushort)(tempLow + tempHigh * 0x100));
            break;
        case 0x90: // BCC, 2 cycles, 3 if branch was taken, 4 if page crossed
            temp = Read(PC);
            PC += 1;
            if (!carryFlag) {
                int offset = temp;
                if (offset > 127)
                    offset -= 256;
                PC = (ushort)(PC + offset);
            }
            break;
        case 0xA0: // LDY Immediate, 2 cycles
            Y = Read(PC);
            PC += 1;
            zeroFlag = Y == 0;
            negativeFlag = Y > 127;
            break;
        case 0xA2: // LDX immediate, 2 cycles
            X = Read(PC);
            PC += 1;
            zeroFlag = X == 0;
            negativeFlag = X > 127;
            break;
        case 0xA5: // LDA Zero page, 3 cycles
            temp = Read(PC);
            PC += 1;
            A = Read(temp);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0xA9: // LDA Immediate, 2 cycles
            A = Read(PC);
            PC += 1;
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0xAC: // LDY Absolute, 4 cycles
            tempLow = Read(PC);
            PC += 1;
            tempHigh = Read(PC);
            PC += 1;
            Y = Read((ushort)(tempLow + tempHigh * 0x100));
            break;
        case 0xAD: // LDA Absolute, 4 cycles
            tempLow = Read(PC);
            PC += 1;
            tempHigh = Read(PC);
            PC += 1;
            A = Read((ushort)(tempLow + tempHigh * 0x100));
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0xAE: // LDX Absolute, 4 cycles
            tempLow = Read(PC);
            PC += 1;
            tempHigh = Read(PC);
            PC += 1;
            X = Read((ushort)(tempLow + tempHigh * 0x100));
            zeroFlag = X == 0;
            negativeFlag = X > 127;
            break;
        case 0xB0: // BCS, 2 cycles, 3 if branch was taken, 4 if a page was
                   // crossed
            temp = Read(PC);
            PC += 1;
            if (carryFlag) {
                int offset = temp;
                if (offset > 127)
                    offset -= 256;
                PC = (ushort)(PC + offset);
            }
            break;
        case 0xD0: // BNE, 2 cycles, 3 if branch was taken, 4 if a page was
                   // crossed
            temp = Read(PC);
            PC += 1;
            if (!zeroFlag) {
                int offset = temp;
                if (offset > 127)
                    offset -= 256;
                PC = (ushort)(PC + offset);
            }
            break;
        case 0xF0: // BEQ, 2 cycles, 3 if branch was taken,  if a page was
                   // crossed
            temp = Read(PC);
            PC += 1;
            if (zeroFlag) {
                int offset = temp;
                if (offset > 127)
                    offset -= 256;
                PC = (ushort)(PC + offset);
            }
            break;
        default:
            throw new Exception(
                $"Unknown opcode encountered: {opcode.ToString("X2")}");
        }
    }

    public static byte Read(ushort address) {
        if (address < 0x800)
            return RAM[address];
        else if (address >= 0x8000)
            return ROM[address - 0x8000];
        else
            throw new Exception(
                $"Memory location {address} not yet implemented");
    }
    public static void Write(byte value, ushort address) {
        if (address < 0x800)
            RAM[address] = value;
    }

    public static void Push(byte value) {
        Write(value, (ushort)(0x100 + SP));
        SP -= 1;
    }
    public static byte Pull() {
        SP += 1;
        return Read((ushort)(0x100 + SP));
    }

    public static void Reset() {
        byte[] romFile = File.ReadAllBytes(ROMFilePath);
        Array.Copy(romFile, 0x10, ROM, 0, 0x8000);
        Array.Copy(romFile, ROMHeader, 0x10);
        interruptDisableFlag = true;
        PC = (ushort)(Read(0xFFFC) + Read(0xFFFD) * 0x100);
        SP = 0xFD;
    }
}