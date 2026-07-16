using System.Linq.Expressions;
using System.Net.Sockets;
using SDL;
unsafe class Emulator {
    public static EmulatorWindow window;
    public static ScrollBox logger;
    public static Dictionary<byte, string> opcodes =
        new Dictionary<byte, string>() {
            { 0x00, "BRK" }, { 0x01, "ORA" }, { 0x02, "HLT" }, { 0x03, "???" },
            { 0x04, "???" }, { 0x05, "ORA" }, { 0x06, "ASL" }, { 0x07, "???" },
            { 0x08, "PHP" }, { 0x09, "ORA" }, { 0x0A, "ASL" }, { 0x0B, "???" },
            { 0x0C, "???" }, { 0x0D, "ORA" }, { 0x0E, "ASL" }, { 0x0F, "???" },
            { 0x10, "BPL" }, { 0x11, "ORA" }, { 0x12, "???" }, { 0x13, "???" },
            { 0x14, "???" }, { 0x15, "ORA" }, { 0x16, "ASL" }, { 0x17, "???" },
            { 0x18, "CLC" }, { 0x19, "ORA" }, { 0x1A, "???" }, { 0x1B, "???" },
            { 0x1C, "???" }, { 0x1D, "ORA" }, { 0x1E, "ASL" }, { 0x1F, "???" },
            { 0x20, "JSR" }, { 0x21, "AND" }, { 0x22, "???" }, { 0x23, "???" },
            { 0x24, "BIT" }, { 0x25, "AND" }, { 0x26, "ROL" }, { 0x27, "???" },
            { 0x28, "PLP" }, { 0x29, "AND" }, { 0x2A, "ROL" }, { 0x2B, "???" },
            { 0x2C, "BIT" }, { 0x2D, "AND" }, { 0x2E, "ROL" }, { 0x2F, "???" },
            { 0x30, "BMI" }, { 0x31, "AND" }, { 0x32, "???" }, { 0x33, "???" },
            { 0x34, "???" }, { 0x35, "AND" }, { 0x36, "ROL" }, { 0x37, "???" },
            { 0x38, "SEC" }, { 0x39, "AND" }, { 0x3A, "???" }, { 0x3B, "???" },
            { 0x3C, "???" }, { 0x3D, "AND" }, { 0x3E, "ROL" }, { 0x3F, "???" },
            { 0x40, "RTI" }, { 0x41, "EOR" }, { 0x42, "???" }, { 0x43, "???" },
            { 0x44, "???" }, { 0x45, "EOR" }, { 0x46, "LSR" }, { 0x47, "???" },
            { 0x48, "PHA" }, { 0x49, "EOR" }, { 0x4A, "LSR" }, { 0x4B, "???" },
            { 0x4C, "JMP" }, { 0x4D, "EOR" }, { 0x4E, "LSR" }, { 0x4F, "???" },
            { 0x50, "BVC" }, { 0x51, "EOR" }, { 0x52, "???" }, { 0x53, "???" },
            { 0x54, "???" }, { 0x55, "EOR" }, { 0x56, "LSR" }, { 0x57, "???" },
            { 0x58, "CLI" }, { 0x59, "EOR" }, { 0x5A, "???" }, { 0x5B, "???" },
            { 0x5C, "???" }, { 0x5D, "EOR" }, { 0x5E, "LSR" }, { 0x5F, "???" },
            { 0x60, "RTS" }, { 0x61, "ADC" }, { 0x62, "???" }, { 0x63, "???" },
            { 0x64, "???" }, { 0x65, "ADC" }, { 0x66, "ROR" }, { 0x67, "???" },
            { 0x68, "PLA" }, { 0x69, "ADC" }, { 0x6A, "ROR" }, { 0x6B, "???" },
            { 0x6C, "JMP" }, { 0x6D, "ADC" }, { 0x6E, "ROR" }, { 0x6F, "???" },
            { 0x70, "BVS" }, { 0x71, "ADC" }, { 0x72, "???" }, { 0x73, "???" },
            { 0x74, "???" }, { 0x75, "ADC" }, { 0x76, "ROR" }, { 0x77, "???" },
            { 0x78, "SEI" }, { 0x79, "ADC" }, { 0x7A, "???" }, { 0x7B, "???" },
            { 0x7C, "???" }, { 0x7D, "ADC" }, { 0x7E, "ROR" }, { 0x7F, "???" },
            { 0x80, "???" }, { 0x81, "STA" }, { 0x82, "???" }, { 0x83, "???" },
            { 0x84, "STY" }, { 0x85, "STA" }, { 0x86, "STX" }, { 0x87, "???" },
            { 0x88, "DEY" }, { 0x89, "???" }, { 0x8A, "TXA" }, { 0x8B, "???" },
            { 0x8C, "STY" }, { 0x8D, "STA" }, { 0x8E, "STX" }, { 0x8F, "???" },
            { 0x90, "BCC" }, { 0x91, "STA" }, { 0x92, "???" }, { 0x93, "???" },
            { 0x94, "STY" }, { 0x95, "STA" }, { 0x96, "STX" }, { 0x97, "???" },
            { 0x98, "TYA" }, { 0x99, "STA" }, { 0x9A, "TXS" }, { 0x9B, "???" },
            { 0x9C, "???" }, { 0x9D, "STA" }, { 0x9E, "???" }, { 0x9F, "???" },
            { 0xA0, "LDY" }, { 0xA1, "LDA" }, { 0xA2, "LDX" }, { 0xA3, "???" },
            { 0xA4, "LDY" }, { 0xA5, "LDA" }, { 0xA6, "LDX" }, { 0xA7, "???" },
            { 0xA8, "TAY" }, { 0xA9, "LDA" }, { 0xAA, "TAX" }, { 0xAB, "???" },
            { 0xAC, "LDY" }, { 0xAD, "LDA" }, { 0xAE, "LDX" }, { 0xAF, "???" },
            { 0xB0, "BCS" }, { 0xB1, "LDA" }, { 0xB2, "???" }, { 0xB3, "???" },
            { 0xB4, "LDY" }, { 0xB5, "LDA" }, { 0xB6, "LDX" }, { 0xB7, "???" },
            { 0xB8, "CLV" }, { 0xB9, "LDA" }, { 0xBA, "TSX" }, { 0xBB, "???" },
            { 0xBC, "LDY" }, { 0xBD, "LDA" }, { 0xBE, "LDX" }, { 0xBF, "???" },
            { 0xC0, "CPY" }, { 0xC1, "CMP" }, { 0xC2, "???" }, { 0xC3, "???" },
            { 0xC4, "CPY" }, { 0xC5, "CMP" }, { 0xC6, "DEC" }, { 0xC7, "???" },
            { 0xC8, "INY" }, { 0xC9, "CMP" }, { 0xCA, "DEX" }, { 0xCB, "???" },
            { 0xCC, "CPY" }, { 0xCD, "CMP" }, { 0xCE, "DEC" }, { 0xCF, "???" },
            { 0xD0, "BNE" }, { 0xD1, "CMP" }, { 0xD2, "???" }, { 0xD3, "???" },
            { 0xD4, "???" }, { 0xD5, "CMP" }, { 0xD6, "DEC" }, { 0xD7, "???" },
            { 0xD8, "CLD" }, { 0xD9, "CMP" }, { 0xDA, "???" }, { 0xDB, "???" },
            { 0xDC, "???" }, { 0xDD, "CMP" }, { 0xDE, "DEC" }, { 0xDF, "???" },
            { 0xE0, "CPX" }, { 0xE1, "SBC" }, { 0xE2, "???" }, { 0xE3, "???" },
            { 0xE4, "CPX" }, { 0xE5, "SBC" }, { 0xE6, "INC" }, { 0xE7, "???" },
            { 0xE8, "INX" }, { 0xE9, "SBC" }, { 0xEA, "NOP" }, { 0xEB, "???" },
            { 0xEC, "CPX" }, { 0xED, "SBC" }, { 0xEE, "INC" }, { 0xEF, "???" },
            { 0xF0, "BEQ" }, { 0xF1, "SBC" }, { 0xF2, "???" }, { 0xF3, "???" },
            { 0xF4, "???" }, { 0xF5, "SBC" }, { 0xF6, "INC" }, { 0xF7, "???" },
            { 0xF8, "SED" }, { 0xF9, "SBC" }, { 0xFA, "???" }, { 0xFB, "???" },
            { 0xFC, "???" }, { 0xFD, "SBC" }, { 0xFE, "INC" }, { 0xFF, "???" },

        };
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
    static bool running;
    public static void Main(string[] args) {
        running = true;
        if (args.Length != 1)
            throw new Exception("Usage: NEScalibur [path to rom]");
        if (SDL3.SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO)) {
            CreateWindow();
        } else
            Console.WriteLine("SDL Failed to initialise: " +
                              SDL3.SDL_GetError());
        if (!SDL3_ttf.TTF_Init())
            Console.Write("Font failed to initialise");
        window.font = SDL3_ttf.TTF_OpenFont(
            "/usr/share/fonts/MapleMono-TTF/MapleMono-Regular.ttf", 20);
        if (window.font == null)
            Console.WriteLine("Font failed to open: " + SDL3.SDL_GetError());
        logger = new ScrollBox(0, 0, 512, 512);
        ROMFilePath = args[0];
        Reset();
        while (running) {
            SDL_Event @event;
            SDL3.SDL_PollEvent(&@event);
            switch (@event.type) {
            case (uint)SDL_EventType.SDL_EVENT_QUIT:
                running = false;
                break;
            case (uint)SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
                logger.Scroll((int)(@event.wheel.y * -20));
                break;
            case (uint)SDL.SDL_EventType.SDL_EVENT_KEY_DOWN:
                if (@event.key.scancode == SDL_Scancode.SDL_SCANCODE_ESCAPE)
                    halted = true;
                break;
            }
            Run();
        }
        Console.WriteLine("A: " + A.ToString("X2"));
        Console.WriteLine("X: " + X.ToString("X2"));
        Console.WriteLine("Y: " + Y.ToString("X2"));
        Console.WriteLine("PC: " + PC.ToString("X2"));
        for (ushort address = 0; address < 0x800; address += 1) {
            if (RAM[address] != 0)
                Console.WriteLine(
                    $"{address.ToString("X2")}: {RAM[address].ToString("X2")}");
        }
        SDL3.SDL_Quit();
    }

    public static void Run() {
        if (!halted) {
            RunCPU();
        }
        Draw();
        SDL3.SDL_Delay(16);
    }
    public static void Draw() {
        SDL3.SDL_SetRenderDrawColor(window.renderer, 255, 255, 255, 255);
        SDL3.SDL_RenderClear(window.renderer);
        SDL_Surface *surface = SDL3.SDL_CreateSurface(
            512, 512, SDL_PixelFormat.SDL_PIXELFORMAT_RGBA8888);
        logger.Render(surface);

        SDL3.SDL_RenderPresent(window.renderer);
    }
    public static void PrintLogLine(byte opcode, ushort tempPC) {
        string lineString =
            "$" + tempPC.ToString("X4") + " " + opcode.ToString("X2") + " " +
            opcodes[opcode] + "   " + " A: " + A.ToString("X2") +
            " X: " + X.ToString("X2") + " Y: " + Y.ToString("X2") +
            " SP: " + SP.ToString("X2") + " " + (negativeFlag ? "N" : "n") +
            (overflowFlag ? "V" : "v") + "--" + (decimalFlag ? "D" : "d") +
            (interruptDisableFlag ? "I" : "i") + (zeroFlag ? "Z" : "z") +
            (carryFlag ? "C" : "c");

        SDL_Surface *line = SDL3_ttf.TTF_RenderText_Blended(
            window.font, lineString, (nuint)lineString.Length,
            new SDL_Color { r = 0, g = 0, b = 0, a = 255 });
        if (line == null)
            Console.WriteLine("Text failed to render: " + SDL3.SDL_GetError());
        SDL_Texture *texture =
            SDL3.SDL_CreateTextureFromSurface(window.renderer, line);
        ScrollBoxItem item = new ScrollBoxItem(texture);
        logger.AddElement(item);
    }

    public static void RunCPU() {
        byte temp;
        ushort tempAddr;
        byte tempLow;
        byte tempHigh;
        bool tempCarry;
        int sum;
        byte opcode = Read(PC);
        ushort tempPC = PC;
        PC += 1;

        switch (opcode) {
        case 0x00: // BRK
            Push((byte)((PC + 1) >> 8));
            Push((byte)(PC + 1));
            temp = 0;
            if (negativeFlag)
                temp += 128;
            if (overflowFlag)
                temp += 64;
            temp += 32;
            temp += 16;
            if (decimalFlag)
                temp += 8;
            if (interruptDisableFlag)
                temp += 4;
            if (zeroFlag)
                temp += 2;
            if (carryFlag)
                temp += 1;
            Push(temp);
            interruptDisableFlag = true;
            PC = 0xFFFE;
            tempAddr = ReadAbsoluteAddress();
            PC = tempAddr;
            break;
        case 0x01: // ORA Indirect, X, 6 cycles
            tempAddr = ReadIndirectXIndexedAddress();
            A |= Read(tempAddr);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x02: // HLT
            halted = true;
            break;
        case 0x05: // ORA Zero Page, 3 cycles
            tempLow = Read(PC);
            PC += 1;
            A |= Read(tempLow);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x06: // ASL Zero Page, 5 cycles
            tempLow = Read(PC);
            PC += 1;
            temp = Read(tempLow);
            carryFlag = temp > 127;
            temp = (byte)(temp << 1);
            Write(temp, tempLow);
            zeroFlag = temp == 0;
            negativeFlag = temp > 127;
            break;
        case 0x08: // PHP, 3 cycles
            temp = 0;
            if (negativeFlag)
                temp += 128;
            if (overflowFlag)
                temp += 64;
            temp += 32;
            temp += 16;
            if (decimalFlag)
                temp += 8;
            if (interruptDisableFlag)
                temp += 4;
            if (zeroFlag)
                temp += 2;
            if (carryFlag)
                temp += 1;
            Push(temp);
            break;
        case 0x09: // ORA Immediate, 2 cycles
            A |= Read(PC);
            PC += 1;
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x0A: // ASL Accumulator, 2 cycles
            carryFlag = A > 127;
            A = (byte)(A << 1);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x0D: // ORA Absolute, 4 cycles
            tempAddr = ReadAbsoluteAddress();
            A |= Read(tempAddr);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x0E: // ASL Absolute, 6 cycles
            tempAddr = ReadAbsoluteAddress();
            temp = Read(tempAddr);
            carryFlag = temp > 127;
            temp = (byte)(temp << 1);
            Write(temp, tempAddr);
            zeroFlag = temp == 0;
            negativeFlag = temp > 127;
            break;
        case 0x10: // BPL, 2 cycles, 3 if branch was taken, 4 if a
                   // page was crossed
            temp = Read(PC);
            PC += 1;
            if (!negativeFlag) {
                int offset = temp;
                if (offset > 127)
                    offset -= 256;
                PC = (ushort)(PC + offset);
            }
            break;
        case 0x11: // ORA Indirect, Y, 5 cycles, 6 if a page was crossed
            tempAddr = ReadIndirectYIndexedAddress();
            A |= Read(tempAddr);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x15: // ORA Zero Page, X, 4 cycles
            tempLow = (byte)(Read(PC) + X);
            PC += 1;
            A |= Read(tempLow);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x16: // ASL Zero Page, X, 6 cycles
            tempLow = (byte)(Read(PC) + X);
            PC += 1;
            temp = Read(tempLow);
            carryFlag = temp > 127;
            temp = (byte)(temp << 1);
            Write(temp, tempLow);
            zeroFlag = temp == 0;
            negativeFlag = temp > 127;
            break;
        case 0x18: // CLC, 2 cycles
            carryFlag = false;
            break;
        case 0x19: // ORA Absolute, Y, 4 cycles, 5 if a page was crossed
            tempAddr = ReadAbsoluteYIndexedAddress();
            A |= Read(tempAddr);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x1D: // ORA Absolute, X, 4 cycles, 5 if a page was crossed
            tempAddr = ReadAbsoluteXIndexedAddress();
            A |= Read(tempAddr);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x1E: // ASL Absolute, X, 7 cycles
            tempAddr = ReadAbsoluteXIndexedAddress();
            temp = Read(tempAddr);
            carryFlag = temp > 127;
            temp = (byte)(temp << 1);
            Write(temp, tempAddr);
            zeroFlag = temp == 0;
            negativeFlag = temp > 127;
            break;
        case 0x20: // JSR, 6 cycles
            tempLow = Read(PC);
            PC += 1;
            tempHigh = Read(PC);
            PC += 1;
            Push((byte)(PC >> 8));
            Push((byte)PC);
            PC = (ushort)(tempLow + tempHigh * 0x100);
            break;
        case 0x21: // AND Indirect, X, 6 cycles
            tempAddr = ReadIndirectXIndexedAddress();
            A &= Read(tempAddr);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x24: // BIT Zero Page, 3 cycles
            tempLow = Read(PC);
            PC += 1;
            temp = Read(tempLow);
            overflowFlag = (temp & 0x40) != 0;
            negativeFlag = (temp & 0x80) != 0;
            zeroFlag = (temp & A) == 0;
            break;
        case 0x25: // AND Zero Page, 3 cycles
            tempLow = Read(PC);
            PC += 1;
            A &= Read(tempLow);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x26: // ROL Zero page, 5 cycles
            tempLow = Read(PC);
            PC += 1;
            temp = Read(tempLow);
            tempCarry = carryFlag;
            carryFlag = temp > 127;
            temp = (byte)(temp << 1);
            temp |= Convert.ToByte(tempCarry);
            Write(temp, tempLow);
            zeroFlag = temp == 0;
            negativeFlag = temp > 127;
            break;
        case 0x28: // PLP, 4 cycles
            temp = Pull();
            negativeFlag = temp >> 7 == 1;
            overflowFlag = temp >> 6 == 1;
            decimalFlag = temp >> 3 == 1;
            interruptDisableFlag = temp >> 2 == 1;
            zeroFlag = temp >> 2 == 1;
            carryFlag = temp >> 1 == 1;
            break;
        case 0x29: // AND Immediate, 2 cycles
            A &= Read(PC);
            PC += 1;
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x2A: // ROL Accumulator, 2 cycles
            tempCarry = carryFlag;
            carryFlag = A > 127;
            A = (byte)(A << 1);
            A |= Convert.ToByte(tempCarry);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x2C: // BIT Absolute, 4 cycles
            tempAddr = ReadAbsoluteAddress();
            temp = Read(tempAddr);
            overflowFlag = (temp & 0x40) != 0;
            negativeFlag = (temp & 0x80) != 0;
            zeroFlag = (temp & A) == 0;
            break;
        case 0x2D: // AND Absolute, 4 cycles
            tempAddr = ReadAbsoluteAddress();
            A &= Read(tempAddr);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x2E: // ROL Absolute, 6 cycles
            tempAddr = ReadAbsoluteAddress();
            temp = Read(tempAddr);
            tempCarry = carryFlag;
            carryFlag = temp > 127;
            temp = (byte)(temp << 1);
            temp |= Convert.ToByte(tempCarry);
            Write(temp, tempAddr);
            zeroFlag = temp == 0;
            negativeFlag = temp > 127;
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
        case 0x31: // AND Indirect, Y, 5 cycles, 6 if a page was crossed
            tempAddr = ReadIndirectYIndexedAddress();
            A &= Read(tempAddr);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x35: // AND Zero Page, X, 4 cycles
            tempLow = (byte)(Read(PC) + X);
            A &= Read(tempLow);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x36: // ROL Zero Page, X, 6 cycles
            tempLow = (byte)(Read(PC) + X);
            PC += 1;
            temp = Read(tempLow);
            tempCarry = carryFlag;
            carryFlag = temp > 127;
            temp = (byte)(temp << 1);
            temp |= Convert.ToByte(tempCarry);
            Write(temp, tempLow);
            zeroFlag = temp == 0;
            negativeFlag = temp > 127;
            break;
        case 0x38: // SEC, 2 cycles
            carryFlag = true;
            break;
        case 0x39: // AND Absolute, Y, 4 cycles, 5 if a page was crossed
            tempAddr = ReadAbsoluteYIndexedAddress();
            A &= Read(tempAddr);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x3D: // AND Absolute, X, 4 cycles, 5 if a page was crossed
            tempAddr = ReadAbsoluteXIndexedAddress();
            A &= Read(tempAddr);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x3E: // ROL Absolute, X, 7 cycles
            tempAddr = ReadAbsoluteXIndexedAddress();
            temp = Read(tempAddr);
            tempCarry = carryFlag;
            carryFlag = temp > 127;
            temp = (byte)(temp << 1);
            temp |= Convert.ToByte(tempCarry);
            Write(temp, tempAddr);
            zeroFlag = temp == 0;
            negativeFlag = temp > 127;
            break;
        case 0x40: // RTI, 6 cycles
            temp = Pull();
            negativeFlag = temp >> 7 == 1;
            overflowFlag = temp >> 6 == 1;
            decimalFlag = temp >> 3 == 1;
            interruptDisableFlag = temp >> 2 == 1;
            zeroFlag = temp >> 2 == 1;
            carryFlag = temp >> 1 == 1;
            tempLow = Pull();
            tempHigh = Pull();
            PC = (ushort)(tempLow + tempHigh * 0x100);
            break;
        case 0x41: // EOR Indirect, X, 6 cycles
            tempAddr = ReadIndirectXIndexedAddress();
            A ^= Read(tempAddr);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x45: // EOR Zero Page, 3 cycles
            tempLow = Read(PC);
            PC += 1;
            A ^= Read(tempLow);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x46: // LSR Zero Page, 5 cycles
            tempLow = Read(PC);
            PC += 1;
            temp = Read(tempLow);
            carryFlag = temp % 2 == 1;
            temp = (byte)(temp >> 1);
            Write(temp, tempLow);
            zeroFlag = temp == 0;
            negativeFlag = temp > 127;
            break;
        case 0x48: // PHA, 3 cycles
            Push(A);
            break;
        case 0x49: // EOR Immediate, 2 cycles
            A ^= Read(PC);
            PC += 1;
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x4A: // LSR Accumulator, 2 cycles
            carryFlag = A % 2 == 1;
            A = (byte)(A >> 1);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x4C: // JMP, 3 cycles
            tempLow = Read(PC);
            PC += 1;
            tempHigh = Read(PC);
            PC = (ushort)(tempLow + tempHigh * 0x100);
            break;
        case 0x4D: // EOR Absolute, 4 cycles
            tempAddr = ReadAbsoluteAddress();
            A ^= Read(tempAddr);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x4E: // LSR Absolute, 6 cycles
            tempAddr = ReadAbsoluteAddress();
            temp = Read(tempAddr);
            carryFlag = temp % 2 == 1;
            temp = (byte)(temp >> 1);
            Write(temp, tempAddr);
            zeroFlag = temp == 0;
            negativeFlag = temp > 127;
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
        case 0x51: // EOR Indirect, Y, 5 cycles, 6 if a page was crossed
            tempAddr = ReadIndirectYIndexedAddress();
            A ^= Read(tempAddr);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x55: // EOR Zero Page, X, 4 cycles
            tempLow = (byte)(Read(PC) + X);
            PC += 1;
            A ^= Read(tempLow);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x56: // LSR Zero Page, X, 6 cycles
            tempLow = (byte)(Read(PC) + X);
            PC += 1;
            temp = Read(tempLow);
            carryFlag = temp % 2 == 1;
            temp = (byte)(temp >> 1);
            Write(temp, tempLow);
            zeroFlag = temp == 0;
            negativeFlag = temp > 127;
            break;
        case 0x5D: // EOR Absolute, X, 4 cycles,5 if a page was crossed
            tempAddr = ReadAbsoluteXIndexedAddress();
            A ^= Read(tempAddr);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x5E: // LSR Absolute, X, 7 cycles
            tempAddr = ReadAbsoluteXIndexedAddress();
            temp = Read(tempAddr);
            carryFlag = temp % 2 == 1;
            temp = (byte)(temp >> 1);
            Write(temp, tempAddr);
            zeroFlag = temp == 0;
            negativeFlag = temp > 127;
            break;
        case 0x60: // RTS, 6 cycles
            tempLow = Pull();
            tempHigh = Pull();
            PC = (ushort)(tempLow + tempHigh * 0x100);
            PC += 1;
            break;
        case 0x61: // ADC Indirect, X, 6 cycles
            tempAddr = ReadIndirectXIndexedAddress();
            temp = Read(tempAddr);
            sum = A + temp + Convert.ToByte(carryFlag);
            overflowFlag = (~(A ^ temp) & (A ^ sum) & 128) != 0;
            carryFlag = sum > 255;
            A = (byte)sum;
            zeroFlag = sum == 0;
            negativeFlag = sum > 127;
            break;
        case 0x65: // ADC Zero Page, 3 cycles
            tempLow = Read(PC);
            PC += 1;
            temp = Read(tempLow);
            sum = A + temp + Convert.ToByte(carryFlag);
            overflowFlag = (~(A ^ temp) & (A ^ sum) & 128) != 0;
            carryFlag = sum > 255;
            A = (byte)sum;
            zeroFlag = sum == 0;
            negativeFlag = sum > 127;
            break;
        case 0x66: // ROR Zero Page, 5 cycles
            tempLow = Read(PC);
            PC += 1;
            temp = Read(tempLow);
            tempCarry = carryFlag;
            carryFlag = temp % 2 == 1;
            temp = (byte)(temp >> 1);
            temp |= (byte)(Convert.ToByte(tempCarry) * 128);
            Write(temp, tempLow);
            zeroFlag = temp == 0;
            negativeFlag = temp > 127;
            break;
        case 0x68: // PLA, 4 cycles
            A = Pull();
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x69: // ADC Immediate, 2 cycles
            temp = Read(PC);
            PC += 1;
            sum = A + temp + Convert.ToByte(carryFlag);
            overflowFlag = (~(A ^ temp) & (A ^ sum) & 128) != 0;
            carryFlag = sum > 255;
            A = (byte)sum;
            zeroFlag = sum == 0;
            negativeFlag = sum > 127;
            break;
        case 0x6A: // ROR Accumulator, 2 cycles
            tempCarry = carryFlag;
            carryFlag = A % 2 == 1;
            A = (byte)(A >> 1);
            A |= (byte)(Convert.ToByte(tempCarry) * 128);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x6C: // JMP Indirect, 5 cycles
            tempLow = Read(PC);
            PC += 1;
            tempHigh = Read(PC);
            tempAddr = (ushort)(tempLow + tempHigh * 0x100);
            tempLow = Read(tempAddr);
            tempHigh = Read((ushort)(tempAddr + 1));
            PC = (ushort)(tempLow + tempHigh * 0x100);
            break;
        case 0x6D: // ADC Absolute, 4 cycles
            tempAddr = ReadAbsoluteAddress();
            temp = Read(tempAddr);
            sum = A + temp + Convert.ToByte(carryFlag);
            overflowFlag = (~(A ^ temp) & (A ^ sum) & 128) != 0;
            carryFlag = sum > 255;
            A = (byte)sum;
            zeroFlag = sum == 0;
            negativeFlag = sum > 127;
            break;
        case 0x6E: // ROR Absolute, 6 cycles
            tempAddr = ReadAbsoluteAddress();
            temp = Read(tempAddr);
            tempCarry = carryFlag;
            carryFlag = temp % 2 == 1;
            temp = (byte)(temp >> 1);
            temp |= (byte)(Convert.ToByte(tempCarry) * 128);
            Write(temp, tempAddr);
            zeroFlag = temp == 0;
            negativeFlag = temp > 127;
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
        case 0x71: // ADC Indirect, Y, 5 cycles, 6 if a page was crossed
            tempAddr = ReadIndirectYIndexedAddress();
            temp = Read(tempAddr);
            sum = A + temp + Convert.ToByte(carryFlag);
            overflowFlag = (~(A ^ temp) & (A ^ sum) & 128) != 0;
            carryFlag = sum > 255;
            A = (byte)sum;
            zeroFlag = sum == 0;
            negativeFlag = sum > 127;
            break;
        case 0x75: // ADC Zero Page, X 4 cycles
            tempLow = (byte)(Read(PC) + X);
            PC += 1;
            temp = Read(tempLow);
            sum = A + temp + Convert.ToByte(carryFlag);
            overflowFlag = (~(A ^ temp) & (A ^ sum) & 128) != 0;
            carryFlag = sum > 255;
            A = (byte)sum;
            zeroFlag = sum == 0;
            negativeFlag = sum > 127;
            break;
        case 0x76: // ROR, Zero Page, X, 6 cycles
            tempLow = (byte)(Read(PC) + X);
            temp = Read(tempLow);
            tempCarry = carryFlag;
            carryFlag = temp % 2 == 1;
            temp = (byte)(temp >> 1);
            temp |= (byte)(Convert.ToByte(tempCarry) * 128);
            Write(temp, tempLow);
            zeroFlag = temp == 0;
            negativeFlag = temp > 127;
            break;
        case 0x78: // SEI, 2 cycles
            interruptDisableFlag = true;
            break;
        case 0x79: // ADC Absolute, Y, 4 cycles, 5 if a page was crossed
            tempAddr = ReadAbsoluteYIndexedAddress();
            temp = Read(tempAddr);
            sum = A + temp + Convert.ToByte(carryFlag);
            overflowFlag = (~(A ^ temp) & (A ^ sum) & 128) != 0;
            carryFlag = sum > 255;
            A = (byte)sum;
            zeroFlag = sum == 0;
            negativeFlag = sum > 127;
            break;
        case 0x7D: // ADC Absolute, X, 4 cycles, 5 if a page was crossed
            tempAddr = ReadAbsoluteXIndexedAddress();
            temp = Read(tempAddr);
            sum = A + temp + Convert.ToByte(carryFlag);
            overflowFlag = (~(A ^ temp) & (A ^ sum) & 128) != 0;
            carryFlag = sum > 255;
            A = (byte)sum;
            zeroFlag = sum == 0;
            negativeFlag = sum > 127;
            break;
        case 0x7E: // ROR Absolute, X, 7 cycles
            tempAddr = ReadAbsoluteXIndexedAddress();
            temp = Read(tempAddr);
            tempCarry = carryFlag;
            carryFlag = temp % 2 == 1;
            temp = (byte)(temp >> 1);
            temp |= (byte)(Convert.ToByte(tempCarry) * 128);
            Write(temp, tempAddr);
            zeroFlag = temp == 0;
            negativeFlag = temp > 127;
            break;
        case 0x81: // STA Indirect, X, 6 cycles
            tempAddr = ReadIndirectXIndexedAddress();
            Write(A, tempAddr);
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
        case 0x8A: // TXA, 2 cycles
            A = X;
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x8C: // STY Absolute, 4 cycles
            tempAddr = ReadAbsoluteAddress();
            Write(Y, tempAddr);
            break;
        case 0x8D: // STA Absolute, 4 cycles
            tempAddr = ReadAbsoluteAddress();
            Write(A, tempAddr);
            break;
        case 0x8E: // STX Absolute, 4 cycles
            tempAddr = ReadAbsoluteAddress();
            Write(X, tempAddr);
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
        case 0x91: // STA Indirect, Y, 6 cycles
            tempAddr = ReadIndirectYIndexedAddress();
            Write(A, tempAddr);
            break;
        case 0x94: // STY Zero Page, X, 4 cycles
            tempLow = (byte)(Read(PC) + X);
            PC += 1;
            Write(Y, tempLow);
            break;
        case 0x95: // STA Zero Page, X, 4 cycles
            temp = (byte)(Read(PC) + X);
            PC += 1;
            Write(A, temp);
            break;
        case 0x96: // STX Zero Page, X, 4 cycles
            tempLow = (byte)(Read(PC));
            PC += 1;
            Write(X, tempLow);
            break;
        case 0x98: // TYA, 2 cycles
            A = Y;
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0x99: // STA Absolute, Y, 5 cycles
            tempAddr = ReadAbsoluteYIndexedAddress();
            Write(A, tempAddr);
            break;
        case 0x9A: // TXS, 2 cycles
            SP = X;
            break;
        case 0x9D: // STA Absolute, X, 5 cycles
            tempAddr = ReadAbsoluteXIndexedAddress();
            Write(A, tempAddr);
            break;
        case 0xA0: // LDY Immediate, 2 cycles
            Y = Read(PC);
            PC += 1;
            zeroFlag = Y == 0;
            negativeFlag = Y > 127;
            break;
        case 0xA1: // LDA Indirect, X, 6 cycles
            tempAddr = ReadIndirectXIndexedAddress();
            A = Read(tempAddr);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
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
        case 0xA8: // TAY
            Y = A;
            zeroFlag = Y == 0;
            negativeFlag = Y > 127;
            break;
        case 0xA9: // LDA Immediate, 2 cycles
            A = Read(PC);
            PC += 1;
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0xAA: // TAX, 2 cycles
            X = A;
            zeroFlag = X == 0;
            negativeFlag = X > 127;
            break;
        case 0xAC: // LDY Absolute, 4 cycles
            tempAddr = ReadAbsoluteAddress();
            Y = Read(tempAddr);
            break;
        case 0xAD: // LDA Absolute, 4 cycles
            tempAddr = ReadAbsoluteAddress();
            A = Read(tempAddr);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0xAE: // LDX Absolute, 4 cycles
            tempAddr = ReadAbsoluteAddress();
            X = Read(tempAddr);
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
        case 0xB1: // LDA Indirect, Y, 5 cycles, 6 if a page was crossed
            tempAddr = ReadIndirectYIndexedAddress();
            A = Read(tempAddr);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0xB4: // LDY Zero Page, X, 4 cycles
            tempLow = (byte)(Read(PC) + X);
            PC += 1;
            Y = Read(tempLow);
            zeroFlag = Y == 0;
            negativeFlag = Y > 127;
            break;
        case 0xB5: // LDA Zero Page, X, 4 cycles
            tempLow = (byte)(Read(PC) + X);
            PC += 1;
            A = Read(tempLow);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0xB6: // LDX Zero Page, Y, 4 cycles
            tempLow = (byte)(Read(PC) + Y);
            PC += 1;
            X = Read(tempLow);
            zeroFlag = X == 0;
            negativeFlag = X > 127;
            break;
        case 0xB9: // LDA Absolute, Y, 4 cycles, 5 if a page was crossed
            tempAddr = ReadAbsoluteYIndexedAddress();
            A = Read(tempAddr);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0xBA: // TSX, 2 cycles
            X = SP;
            zeroFlag = X == 0;
            negativeFlag = X > 127;
            break;
        case 0xBC: // LDY Absolute, X, 4 cycles, 5 if a page was crossed
            tempAddr = ReadAbsoluteXIndexedAddress();
            Y = Read(tempAddr);
            zeroFlag = Y == 0;
            negativeFlag = Y > 127;
            break;
        case 0xBD: // LDA Absolute, X, 4 cycles, 5 if a page was crossed
            tempAddr = ReadAbsoluteXIndexedAddress();
            A = Read(tempAddr);
            zeroFlag = A == 0;
            negativeFlag = A > 127;
            break;
        case 0xBE: // LDX Absolute, Y, 4 cycles, 5 if page was crossed
            tempAddr = ReadAbsoluteYIndexedAddress();
            X = Read(tempAddr);
            zeroFlag = X == 0;
            negativeFlag = X > 127;
            break;
        case 0xC0: // CPY Immediate, 2 cycles
            temp = Read(PC);
            PC += 1;
            carryFlag = Y >= temp;
            zeroFlag = Y == temp;
            negativeFlag = (byte)(Y - temp) > 127;
            break;
        case 0xC1: // CMP Indirect, X, 6 cycles
            tempAddr = ReadIndirectXIndexedAddress();
            temp = Read(tempAddr);
            carryFlag = A >= temp;
            zeroFlag = A == temp;
            negativeFlag = (byte)(A - temp) > 127;
            break;
        case 0xC4: // CPY Zero Page, 3 cycles
            tempLow = Read(PC);
            PC += 1;
            temp = Read(tempLow);
            carryFlag = Y >= temp;
            zeroFlag = Y == temp;
            negativeFlag = (byte)(Y - temp) > 127;
            break;
        case 0xC5: // CMP Zero Page, 3 cycles
            tempLow = Read(PC);
            PC += 1;
            temp = Read(tempLow);
            carryFlag = A >= temp;
            zeroFlag = A == temp;
            negativeFlag = (byte)(A - temp) > 127;
            break;
        case 0xC8: // INY, 2 cycles
            Y += 1;
            zeroFlag = Y == 0;
            negativeFlag = Y > 127;
            break;
        case 0xC9: // CMP Immediate, 2 cycles
            temp = Read(PC);
            PC += 1;
            carryFlag = A >= temp;
            zeroFlag = A == temp;
            negativeFlag = (byte)(A - temp) > 127;
            break;
        case 0xCA: // DEX, 2 cycles
            X -= 1;
            zeroFlag = X == 0;
            negativeFlag = X > 127;
            break;
        case 0xCC: // CPY Absolute, 4 cycles
            tempAddr = ReadAbsoluteAddress();
            temp = Read(tempAddr);
            carryFlag = Y >= temp;
            zeroFlag = Y == temp;
            negativeFlag = (byte)(Y - temp) > 127;
            break;

        case 0xCD: // CMP Absolute, 4 cycles
            tempAddr = ReadAbsoluteAddress();
            temp = Read(tempAddr);
            carryFlag = A >= temp;
            zeroFlag = A == temp;
            negativeFlag = (byte)(A - temp) > 127;
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
        case 0xD1: // CMP Indirect, Y, 5 cycles, 6 if a page was crossed
            tempAddr = ReadIndirectYIndexedAddress();
            temp = Read(tempAddr);
            carryFlag = A >= temp;
            zeroFlag = A == temp;
            negativeFlag = (byte)(A - temp) > 127;
            break;
        case 0xD5: // CMP Zero Page, X, 4 cycles
            tempLow = (byte)(Read(PC) + X);
            PC += 1;
            temp = Read(tempLow);
            carryFlag = A >= temp;
            zeroFlag = A == temp;
            negativeFlag = (byte)(A - temp) > 127;
            break;
        case 0xD9: // CMP Absolute, Y, 4 cycles, 5 if a page was crossed
            tempAddr = ReadAbsoluteYIndexedAddress();
            temp = Read(tempAddr);
            carryFlag = A >= temp;
            zeroFlag = A == temp;
            negativeFlag = (byte)(A - temp) > 127;
            break;
        case 0xDD: // CMP Absolute, X, 4 cycles, 5 if a page was crossed
            tempAddr = ReadAbsoluteXIndexedAddress();
            temp = Read(tempAddr);
            carryFlag = A >= temp;
            zeroFlag = A == temp;
            negativeFlag = (byte)(A - temp) > 127;
            break;
        case 0xE0: // CPX Immediate, 2 cycles
            temp = Read(PC);
            PC += 1;
            carryFlag = X >= temp;
            zeroFlag = X == temp;
            negativeFlag = (byte)(X - temp) > 127;
            break;
        case 0xE1: // SBC Indirect, X, 6 cycles
            tempAddr = ReadIndirectXIndexedAddress();
            temp = Read(tempAddr);
            sum = A - temp - Convert.ToByte(carryFlag);
            overflowFlag = ((A ^ temp) & (A ^ sum) & 128) != 0;
            carryFlag = sum >= 0;
            A = (byte)sum;
            zeroFlag = sum == 0;
            negativeFlag = sum > 127;
            break;
        case 0xE4: // CPX Zero Page, 3 cycles
            tempLow = Read(PC);
            PC += 1;
            temp = Read(tempLow);
            carryFlag = X >= temp;
            zeroFlag = X == temp;
            negativeFlag = (byte)(X - temp) > 127;
            break;
        case 0xE5: // SBC Zero Page, 3 cycles
            tempLow = Read(PC);
            PC += 1;
            temp = Read(tempLow);
            sum = A - temp - Convert.ToByte(carryFlag);
            overflowFlag = ((A ^ temp) & (A ^ sum) & 128) != 0;
            carryFlag = sum >= 0;
            A = (byte)sum;
            zeroFlag = sum == 0;
            negativeFlag = sum > 127;
            break;
        case 0xE6: // INC Zero Page, 5 cycles
            tempLow = Read(PC);
            PC += 1;
            temp = Read(tempLow);
            temp += 1;
            Write(temp, tempLow);
            zeroFlag = temp == 0;
            negativeFlag = temp > 127;
            break;
        case 0xE8: // INX, 2 cycles
            X += 1;
            zeroFlag = X == 0;
            negativeFlag = X > 127;
            break;
        case 0xE9: // SBC Immediate, 2 cycles
            temp = Read(PC);
            PC += 1;
            sum = A - temp - Convert.ToByte(!carryFlag);
            overflowFlag = ((A ^ temp) & (A ^ sum) & 128) != 0;
            carryFlag = sum >= 0;
            A = (byte)sum;
            zeroFlag = sum == 0;
            negativeFlag = sum > 127;
            break;
        case 0xEA: // NOP, 2 cycles
            break;
        case 0xEC: // CPX Absolute, 4 cycles
            tempAddr = ReadAbsoluteAddress();
            temp = Read(tempAddr);
            carryFlag = X >= temp;
            zeroFlag = X == temp;
            negativeFlag = (byte)(X - temp) > 127;
            break;

        case 0xED: // SBC Absuolte, 4 cycles
            tempAddr = ReadAbsoluteAddress();
            temp = Read(tempAddr);
            sum = A - temp - Convert.ToByte(carryFlag);
            overflowFlag = ((A ^ temp) & (A ^ sum) & 128) != 0;
            carryFlag = sum >= 0;
            A = (byte)sum;
            zeroFlag = sum == 0;
            negativeFlag = sum > 127;
            break;
        case 0xEE: // INC Absolute, 6 cycles
            tempAddr = ReadAbsoluteAddress();
            temp = Read(tempAddr);
            temp += 1;
            Write(temp, tempAddr);
            zeroFlag = temp == 0;
            negativeFlag = temp > 127;
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
        case 0xF1: // SBC Indirect, Y, 5 cycles, 6 if a page was crossed
            tempAddr = ReadIndirectYIndexedAddress();
            temp = Read(tempAddr);
            sum = A - temp - Convert.ToByte(carryFlag);
            overflowFlag = ((A ^ temp) & (A ^ sum) & 128) != 0;
            carryFlag = sum >= 0;
            A = (byte)sum;
            zeroFlag = sum == 0;
            negativeFlag = sum > 127;
            break;
        case 0xF5: // SBC Zero Page, X, 4 cycles
            tempLow = (byte)(Read(PC) + X);
            PC += 1;
            temp = Read(tempLow);
            sum = A - temp - Convert.ToByte(carryFlag);
            overflowFlag = ((A ^ temp) & (A ^ sum) & 128) != 0;
            carryFlag = sum >= 0;
            A = (byte)sum;
            zeroFlag = sum == 0;
            negativeFlag = sum > 127;
            break;
        case 0xF8: // SED, 2 cycles
            decimalFlag = true;
            break;
        case 0xF9: // SBC Absolute, Y, 4 cycles, 5 if a page was crossed
            tempAddr = ReadAbsoluteYIndexedAddress();
            temp = Read(tempAddr);
            sum = A - temp - Convert.ToByte(carryFlag);
            overflowFlag = ((A ^ temp) & (A ^ sum) & 128) != 0;
            carryFlag = sum >= 0;
            A = (byte)sum;
            zeroFlag = sum == 0;
            negativeFlag = sum > 127;
            break;
        case 0xFD: // SBC Absolute, X, 4 cycles, 5 if a page was crossed
            tempAddr = ReadAbsoluteXIndexedAddress();
            temp = Read(tempAddr);
            sum = A - temp - Convert.ToByte(carryFlag);
            overflowFlag = ((A ^ temp) & (A ^ sum) & 128) != 0;
            carryFlag = sum >= 0;
            A = (byte)sum;
            zeroFlag = sum == 0;
            negativeFlag = sum > 127;
            break;
        default:
            break;
        }
        PrintLogLine(opcode, tempPC);
    }

    public static byte Read(ushort address) {
        if (address < 0x800)
            return RAM[address];
        else if (address >= 0x8000)
            return ROM[address - 0x8000];
        else
            Console.WriteLine($"Memory location {address} not yet implemented");
        return 0;
    }

    public static ushort ReadAbsoluteAddress() {
        byte tempLow = Read(PC);
        PC += 1;
        byte tempHigh = Read(PC);
        PC += 1;
        return (ushort)(tempLow + tempHigh * 0x100);
    }
    public static ushort ReadAbsoluteXIndexedAddress() {
        ushort address = ReadAbsoluteAddress();
        return (ushort)(address + X);
    }
    public static ushort ReadAbsoluteYIndexedAddress() {
        ushort address = ReadAbsoluteAddress();
        return (ushort)(address + Y);
    }
    public static ushort ReadIndirectYIndexedAddress() {
        byte temp = Read(PC);
        PC += 1;
        byte tempLow = Read(temp);
        byte tempHigh = Read((ushort)(temp + 1));
        ushort address = (ushort)(tempLow + tempHigh * 0x100);
        return (ushort)(address + Y);
    }
    public static ushort ReadIndirectXIndexedAddress() {
        byte temp = (byte)(Read(PC) + X);
        PC += 1;
        byte tempLow = Read(temp);
        byte tempHigh = Read((ushort)(temp + 1));
        ushort address = (ushort)(tempLow + tempHigh * 0x100);
        return address;
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
    public static void CreateWindow() {
        window.window = SDL3.SDL_CreateWindow(
            "Nescalibur", 512, 512, SDL_WindowFlags.SDL_WINDOW_ALWAYS_ON_TOP);
        if (window.window == null)
            Console.Write("Window failed to be created");
        window.renderer = SDL3.SDL_CreateRenderer(window.window, "");
        if (window.renderer == null)
            Console.WriteLine("Renderer failed to be created: " +
                              SDL3.SDL_GetError());
    }
    public static void CloseWindow() {
        SDL3.SDL_DestroyRenderer(window.renderer);
        if (window.window != null)
            SDL3.SDL_DestroyWindow(window.window);
        window.window = null;
    }
}
unsafe struct EmulatorWindow {
    public SDL_Window *window;
    public SDL_Renderer *renderer;
    public TTF_Font *font;
}
unsafe public class ScrollBox {
    public SDL_Rect viewport;
    int height;
    int scrollY;
    List<ScrollBoxItem> elements;

    public ScrollBox(int x, int y, int w, int h) {
        viewport = new SDL_Rect { x = x, y = y, w = w, h = h };
        elements = new List<ScrollBoxItem>();
    }
    public void AddElement(ScrollBoxItem element) {
        elements.Add(element);
        RecomputeHeight();
    }
    public void Scroll(int amount) {
        scrollY += amount;
        scrollY = Math.Max(0, Math.Min(scrollY, height - viewport.h));
    }
    public void Render(SDL_Surface *surface) {
        int vy = 0;
        SDL_Rect clip = viewport;
        int overlapped = 0;
        SDL3.SDL_SetSurfaceClipRect(surface, &clip);
        foreach (ScrollBoxItem element in elements) {
            int nvy = vy + element.texture->h;
            if (nvy > scrollY && vy < scrollY + viewport.h) {
                overlapped += 1;
                element.Render(this, vy - scrollY);
            }
            vy = nvy;
        }
        SDL3.SDL_SetSurfaceClipRect(surface, null);
    }
    private void RecomputeHeight() {
        int height = 0;
        foreach (ScrollBoxItem element in elements) {
            height += element.texture->h;
        }
        this.height = height;
    }
    public static bool Overlaps(SDL_Rect a, SDL_Rect b) {
        return (Contains(a, b.x, b.y) || Contains(b, a.x, a.y) ||
                Contains(a, b.x + b.w, b.y) || Contains(b, a.x + a.w, a.y) ||
                Contains(a, b.x, b.y + b.h) || Contains(b, a.x, a.y + a.h) ||
                Contains(a, b.x + b.w, b.y + b.h) ||
                Contains(b, a.x + a.w, a.y + a.h));
    }
    public static bool Contains(int min, int max, int x) {
        return x >= min && x <= max;
    }
    public static bool Contains(SDL_Rect r, int x, int y) {
        return Contains(r.x, r.x + r.w, x) && Contains(r.y, r.y + r.h, y);
    }
}
public unsafe class ScrollBoxItem {
    public SDL_Texture *texture;

    public ScrollBoxItem(SDL_Texture *texture) { this.texture = texture; }
    public void Render(ScrollBox scroll, int y) {
        SDL_FRect newrect;
        newrect.x = scroll.viewport.x;
        newrect.y = scroll.viewport.y + y;
        newrect.w = texture->w;
        newrect.h = texture->h;
        if (!SDL3.SDL_RenderTexture(Emulator.window.renderer, texture, null,
                                    (SDL_FRect *)&newrect)) {
            Console.WriteLine("Scroll box item failed to be rendered: " +
                              SDL3.SDL_GetError());
        }
    }
}
