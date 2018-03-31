using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ovlinject
{
    class Program
    {
        static void Main(string[] args)
        {  
            // Argument Variables
            string curr = Directory.GetCurrentDirectory();
            string target = "";
            bool ovlfile = false;
            bool ramofs = false;
            bool actor = false;
            bool irom = false;
            bool romofs = false;
            bool isverb = false;
            // Method Variables
            byte[] vRAMbytes = { 0x80, 0x00, 0xFF }; // Useful array of bytes
            int actno = 0, init_vars = 0, injectofs = 0, vRAMstart = 0;
            int aRAMmin = WordFromBytes(vRAMbytes[0], vRAMbytes[1], vRAMbytes[1], vRAMbytes[1]); // 0x80000000
            int vRAMmax = WordFromBytes(vRAMbytes[0], vRAMbytes[2], vRAMbytes[2], vRAMbytes[2]); // 0x80FFFFFF; Virtual Ram Max
            int[] atypes = { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B }; // Available actor types
            string[] actor_types = { "Switch", "Prop (Type: 1)", "Player", "Bomb", "NPC", "Enemy", "Prop (Type: 2)", "Item", "Misc", "Boss", "Door", "Chest" }; // Above strings
            string rompath = "", actorpath = " ";
            if (args.Length != 0)
            {
                for (int l = 0; l < args.Length; l++) // Make arguments lowercase
                {
                    target = string.Format("{0}\\{1}", curr, args[l]);
                    if (!File.Exists(target))
                    {
                        if (args[l].Contains("-"))
                            args[l] = args[l].ToLower();
                    }
                    else
                        args[l] = target;
                }
            }
            else
            {
                PrintUsage();
                //Console.ReadKey();
                Environment.Exit(0);
            }
            for (int c = 0; c < args.Length; c++)
            {
                // Correct Arguments?
                if ((!args.Contains("-i") && !args.Contains("-infile")))
                {
                    PrintUsage();
                    //Console.ReadKey();
                    Environment.Exit(0);
                }

                // Overlay File
                if ((args[c] == "-i" || args[c] == "-infile") && !ovlfile)
                {
                    ovlfile = true;
                    int infile = Array.IndexOf(args, args[c]);
                    target = string.Format("{0}", args[infile + 1]); // Does Overlay File Exist?
                    if (!File.Exists(target))
                    {
                        Console.WriteLine("Overlay File \"{0}\" doesn't exist. Exiting...", target);
                        //Console.ReadKey();
                        Environment.Exit(0);
                    }
                    actorpath = target;
                }

                // Overlay vRAM Start
                if (((args[c] == "-s" || args[c] == "-vram")) && !ramofs)
                {
                    ramofs = true;
                    int s = 0;
                    int vstart = Array.IndexOf(args, args[c]);
                    args[vstart + 1] = RemoveHexPrefix(args[vstart+ 1]);
                    target = string.Format("{0}", args[vstart + 1]); // Is this a file?
                    if (File.Exists(target))
                    {
                        Console.WriteLine("This should be a valid integer. Defaulting to 0x80800000...");
                        args[vstart + 1] = "0x80800000";
                        vRAMstart = unchecked((int)Convert.ToInt32(args[vstart + 1], 16)); // Actor's Virutal Ram Start (as provided by the user)
                    }
                    else if (!Int32.TryParse(args[vstart + 1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out s))
                    {
                        Console.WriteLine("This should be a valid integer. Defaulting to 0x80800000...");
                        args[vstart + 1] = "0x80800000";
                        vRAMstart = unchecked((int)Convert.ToInt32(args[vstart + 1], 16)); // Actor's Virutal Ram Start (as provided by the user)
                    }
                    else
                    {
                        vRAMstart = s;
                    }
                    
                }

                // Actor Number?
                if (((args[c] == "-a" || args[c] == "-actor")) && !actor)
                {
                    actor = true;
                    int a = 0;
                    int actor_index = Array.IndexOf(args, args[c]);
                    args[actor_index + 1] = RemoveHexPrefix(args[actor_index + 1]);
                    target = string.Format("{0}", args[actor_index + 1]); // Is this a file?
                    if (File.Exists(target))
                    {
                        Console.WriteLine("This should be a valid integer. Defaulting to 0x0001...");
                        args[actor_index + 1] = "0x0001";
                        actno = Convert.ToInt32(args[actor_index + 1], 16);
                    }
                    else if (!Int32.TryParse(args[actor_index + 1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out a))
                    {
                        Console.WriteLine("This should be a valid integer. Defaulting to 0x0001...");
                        args[actor_index + 1] = "0x0001";
                        actno = Convert.ToInt32(args[actor_index + 1], 16);
                    }
                    else
                    {
                        a &= 0xFFFF;
                        actno = a;
                    }

                }

                // ROM?
                if (((args[c] == "-r" || args[c] == "-rom")) && !irom)
                {
                    irom = true;
                    int romfile = Array.IndexOf(args, args[c]);
                    target = string.Format("{0}", args[romfile + 1]); // Does ROM File Exist?
                    if (!File.Exists(target))
                    {
                        Console.WriteLine("ROM \"{0}\" doesn't exist. Not injecting!", target);
                        irom = false;
                    }
                    rompath = args[romfile + 1];
                }

                // Injection Offset?
                if (((args[c] == "-o" || args[c] == "-offset")) && !romofs)
                {
                    romofs = true;
                    int o = 0;
                    int inject_index = Array.IndexOf(args, args[c]);
                    args[inject_index + 1] = RemoveHexPrefix(args[inject_index + 1]);
                    target = string.Format("{0}", args[inject_index + 1]); // Is this a file?
                    if (File.Exists(target))
                    {
                        Console.WriteLine("This should be a valid integer. Defaulting to 0x035D0000...");
                        args[inject_index + 1] = "0x035D0000";
                        injectofs = Convert.ToInt32(args[inject_index + 1], 16);
                    }
                    else if (!Int32.TryParse(args[inject_index + 1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out o))
                    {
                        Console.WriteLine("This should be a valid integer. Defaulting to 0x035D0000...");
                        args[inject_index + 1] = "0x035D0000";
                        injectofs = Convert.ToInt32(args[inject_index + 1], 16);
                    }
                    else
                    {
                        injectofs = o;
                    }
                    
                }

                // Verbose?
                if (((args[c] == "-v" || args[c] == "-verbose")) && !isverb)
                {
                    isverb = true;
                }

            }

            //byte[] zovl = File.ReadAllBytes(@"C:\Z64\ovl_obj_gensettilesize\ovl_obj_gensettilesize.ovl");
            //byte[] zovl = File.ReadAllBytes(@"C:\Z64\ovl_en_example\ovl_en_example.ovl");
            //byte[] zovl = File.ReadAllBytes(@"C:\Z64\En_Sa\En_Sa.zovl");

            // Overlay File
            byte[] zovl = File.ReadAllBytes(@actorpath); // Overlay Byte Array

            // Find Initialization Variables
            for (int i = 0; i < (zovl.Length - 0x20); i += 4)
            {

                int init_type = zovl[i + 2];
                int init_oid = ShortFromBytes(zovl[i + 0x08], zovl[i + 0x09]);
                int init_pad = (WordFromBytes(zovl[i + 0x0A], zovl[i + 0x0B], zovl[i + 0x0C], zovl[i + 0x0D]) & WordFromBytes(vRAMbytes[2], vRAMbytes[2], vRAMbytes[2], vRAMbytes[1]));
                int init_func_init = WordFromBytes(zovl[i + 0x10], zovl[i + 0x11], zovl[i + 0x12], zovl[i + 0x13]);
                int init_func_dest = WordFromBytes(zovl[i + 0x14], zovl[i + 0x15], zovl[i + 0x16], zovl[i + 0x17]);
                int init_func_main = WordFromBytes(zovl[i + 0x18], zovl[i + 0x19], zovl[i + 0x1A], zovl[i + 0x1B]);
                int init_func_draw = WordFromBytes(zovl[i + 0x1C], zovl[i + 0x1D], zovl[i + 0x1E], zovl[i + 0x1F]);
                if ((atypes.Contains(init_type)) &&
                        ((init_oid > 0x0000) && (init_oid < 0x0300)) &&
                        (init_pad == 0x00000000) && //Instance size is technically a word, but there isn't likely going to be an instance that is greater than 0xFFFFFF
                        (((init_func_init >= aRAMmin) && init_func_init <= vRAMmax) &&
                        ((init_func_dest >= aRAMmin && init_func_dest <= vRAMmax) || init_func_dest == 0x00000000) &&
                        ((init_func_main >= aRAMmin && init_func_main <= vRAMmax) || init_func_main == 0x00000000) &&
                        ((init_func_draw >= aRAMmin && init_func_draw <= vRAMmax) || init_func_draw == 0x00000000)))
                {
                    if (isverb)
                    {
                        Console.WriteLine("Actor File: {0}", actorpath);
                        Console.WriteLine("Entry Point (vRAM start): 0x{0:X8}", vRAMstart);
                        Console.WriteLine("Initialization Variables (ovl): 0x{0:X8}", i);
                        Console.WriteLine("Actor Number: 0x{0:X4}", Convert.ToUInt16(ShortFromBytes(zovl[i], zovl[i + 1])));
                        Console.WriteLine("Actor Type: {0}", actor_types[init_type]);
                        Console.WriteLine("Object Number: 0x{0:X4}", init_oid);
                        Console.WriteLine("Instance Size: 0x{0:X8}", WordFromBytes(zovl[i + 0x0C], zovl[i + 0x0D], zovl[i + 0x0E], zovl[i + 0x0F]));
                        Console.WriteLine("Initialization Routine (vRAM): 0x{0:X8}", init_func_init);
                        Console.WriteLine("Destructor Routine (vRAM): 0x{0:X8}", init_func_dest);
                        Console.WriteLine("Behavior Routine (vRAM): 0x{0:X8}", init_func_main);
                        Console.WriteLine("Drawing Routine (vRAM): 0x{0:X8}\n", init_func_draw);
                    }
                    init_vars = i;
                    break;
                }
             
            }

            Console.Write("Overlay Entry 0x{5:X8} (vROM):\n{0:X8} {1:X8} {2:X8} {3:X8}\n00000000 {4:X8} 00000000 00000000\n", (injectofs), (injectofs + zovl.Length), (vRAMstart), (vRAMstart + zovl.Length), (init_vars | vRAMstart), ((actno * 0x20) + 0x00B8D440));
            //Console.ReadKey();

            if (irom)
            {
                string overlay = string.Format("{0:X8}{1:X8}{2:X8}{3:X8}00000000{4:X8}0000000000000000", (injectofs), (injectofs + zovl.Length), (vRAMstart), (vRAMstart + zovl.Length), (init_vars | vRAMstart), ((actno * 0x20) + 0x00B8D440));
                byte[] ovlentry = StringToByteArray(overlay);

                // Inject into ROM
                Console.Write("Injecting...");

                int pos = 0;
                using (BinaryWriter b = new BinaryWriter(File.Open(rompath, FileMode.Open)))
                {
                    if ((injectofs >= b.BaseStream.Length) || (injectofs <= 0x00000000))
                    {
                        Console.Write("Failed! Injection offset not within ROM!\n");
                        b.Close();
                        b.Dispose();
                    }

                    b.Seek(((actno * 0x20) + 0x00B8D440), SeekOrigin.Begin);
                    for (pos = 0; pos < ovlentry.Length; pos++)
                    {
                        b.Write(ovlentry[pos]);
                    }
                    b.Seek(injectofs, SeekOrigin.Begin);
                    pos = 0;
                    for (pos = 0; pos < zovl.Length; pos++)
                    {
                        b.Write(zovl[pos]);
                    }
                    b.Write(0x00000000);
                    b.Write(0x00000000);
                    b.Write(0x00000000);
                    b.Write(0x00000000);
                    b.Close();
                    b.Dispose();
                    Console.Write("Success!\n");
                }
                Console.Write("Done!");
            }
        }

        static void PrintUsage()
        {
            Console.ForegroundColor = ConsoleColor.Red; // Red
            Console.WriteLine("Invalid Arguments!");
            Console.ForegroundColor = ConsoleColor.Yellow; // Yellow
            Console.WriteLine("Usage: [PROGRAM] -i/-infile [-s/-vram] [-a/-actor] [-r/-rom] [-o/-offset] [-v/-verbose]");
            Console.ResetColor();
            Console.WriteLine("       [-i]: Infile. This is the overlay file to process. This argument is not optional.");
            Console.WriteLine("       [-s]: (vRAM) start. This is the overlay's virtual RAM start; default is 0x80800000.");
            Console.WriteLine("       [-a]: Actor Number. This is the overlay entry to overwrite; default is 0x0001.");
            Console.WriteLine("       [-r]: Ocarina of Time Master Quest Debug ROM to inject into.");
            Console.WriteLine("             If no ROM is provided, the overlay entry will still be generated.");
            Console.WriteLine("       [-o]: ROM Injection Offset. This is where the overlay file will be written; default is 0x035D0000.");
            Console.WriteLine("       [-v]: Write verbose overlay information to the console.");
            Console.ForegroundColor = ConsoleColor.Yellow; // Yellow
            Console.WriteLine("Example: ovlinject.exe -i En_Sa.zovl -s 0x80AF5560 -a 0x0146 -r ZELOOTMA.z64 -o 0x035D0000 -v");
            Console.ForegroundColor = ConsoleColor.Cyan; // Cyan
            Console.WriteLine("CrookedPoe - March 2018");
            Console.ResetColor();
        }

        static int ShortFromBytes(byte b1, byte b2)
        {
            int combined = b1 << 8 | b2;
            return combined;
        }

        static int WordFromBytes(byte b1, byte b2, byte b3, byte b4)
        {
            int combined = b1 << 0x18 | b2 << 0x10 | b3 << 0x08 | b4;
            return combined;
        }

        static string RemoveHexPrefix(string s)
        {
            string l = s.ToLower();
            var chars = l.ToCharArray();
            int x = 0;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < chars.Length; i++)
            { 
                if (chars[i] == 'x' || chars[i] == 'h' || chars[i] == '$'|| chars[i] == '#')
                {
                    x = i;
                    break;
                }
            }
            for (int i = x + 1; i < chars.Length; i++)
            {
                sb.Append(chars[i]);
            }
            string outs = sb.ToString();
            sb.Clear();
            return outs;
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

    }
}
