using System;
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

            // Make arguments lowercase
            string curr = Directory.GetCurrentDirectory();
            string target = "";
            for (int c = 0; c < args.Length; c++)
            {
                target = string.Format("{0}\\{1}", curr, args[c]);
                if (!File.Exists(target))
                {
                    if (args[c].Contains("-"))
                        args[c] = args[c].ToLower();
                }
            }

            // Correct Arguments?
            if ((!args.Contains("-i") || !args.Contains("-s")) || (!args.Contains("-infile") || !args.Contains("-vram")))
            {
                Console.WriteLine("Invalid Arguments!");
                Console.WriteLine("Usage: [PROGRAM] -i -s [-a] [-r] [-o] [-v]");
                Console.WriteLine("       [-i]: Infile. This is the overlay file to process. This argument is not optional.");
                Console.WriteLine("       [-s]: (vRAM) start. This is the overlay's virtual RAM start. This argument is not optional.");
                Console.WriteLine("       [-a]: Actor Number. This is the overlay entry to overwrite; default is 0x0001.");
                Console.WriteLine("       [-r]: Ocarina of Time Master Quest Debug ROM to inject into.");
                Console.WriteLine("             If no ROM is provided, the overlay entry will still be generated.");
                Console.WriteLine("       [-o]: ROM Injection Offset. This is where the overlay file will be written; default is 0x035D0000.");
                Console.WriteLine("       [-v]: Write verbose overlay information to the console.");
                Console.WriteLine("Example: ovlinject.exe -i En_Sa.zovl -s 0x80AF5560 -a 0x0146 -r ZELOOTMA.z64 -o 0x035D0000 -v");
                Console.WriteLine("CrookedPoe - March 2018");
                return;
            }
            byte[] vRAMbytes = { 0x80, 0x00, 0xFF }; // Useful array of bytes
            int actno = 0, init_vars = 0, injectofs = 0;
            int aRAMmin = WordFromBytes(vRAMbytes[0], vRAMbytes[1], vRAMbytes[1], vRAMbytes[1]); // 0x80000000
            int vRAMmax = WordFromBytes(vRAMbytes[0], vRAMbytes[2], vRAMbytes[2], vRAMbytes[2]); // 0x80FFFFFF; Virtual Ram Max
            int[] atypes = { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B }; // Available actor types
            string[] actor_types = { "Switch", "Prop (Type: 1)", "Player", "Bomb", "NPC", "Enemy", "Prop (Type: 2)", "Item", "Misc", "Boss", "Door", "Chest" }; // Above strings
            string rompath = "", actorpath = "";

            // Overlay File
            int infile = 0;
            if (args.Contains("-infile"))
                infile = Array.IndexOf(args, "-infile");
            else
                infile = Array.IndexOf(args, "-i");
            actorpath = args[infile + 1];
            target = string.Format("{0}\\{1}", curr, args[infile + 1]); // Does Overlay File Exist?
            if (!File.Exists(target))
            {
                Console.WriteLine("Overlay File \"{0}\" doesn't exist. Exiting...", target);
                Environment.Exit(0);
            }
            byte[] zovl = File.ReadAllBytes(@actorpath); // Overlay Byte Array

            // Overlay vRAM Start
            int vstart = 0;
            if (args.Contains("-vram"))
                vstart = Array.IndexOf(args, "-vram");
            else
                vstart = Array.IndexOf(args, "-s");
            int vRAMstart = unchecked((int)Convert.ToInt32(args[vstart + 1], 16)); // Actor's Virutal Ram Start (as provided by the user)

            // Actor Number?
            if (args.Contains("-a") || args.Contains("-actor"))
            {
                int actor_index = 0;
                if (args.Contains("-actor"))
                    actor_index = Array.IndexOf(args, "-actor");
                else
                    actor_index = Array.IndexOf(args, "-a");
                actno = Convert.ToInt32(args[actor_index + 1]);
            }
            else
                actno = 0x0001;

            // ROM?
            bool inject = false;
            if (args.Contains("-r") || args.Contains("-rom"))
            {
                if (args.Contains("-r") && args.Contains("-rom"))
                {
                    Console.WriteLine("Choose '-r' or '-rom', silly. Don't be greedy and use ALL THE ARGUMENTS.  Exiting...");
                    Environment.Exit(0);
                }
                int romfile = 0;
                inject = true;
                target = string.Format("{0}\\{1}", curr, args[infile + 1]); // Does ROM File Exist?
                if (!File.Exists(target))
                {
                    Console.WriteLine("ROM \"{0}\" doesn't exist. Exiting...", target);
                    Environment.Exit(0);
                }
                rompath = args[romfile + 1];
            }

            // Injection Offset?
            if (args.Contains("-o") || args.Contains("-offset"))
            {
                int inject_index = 0;
                if (args.Contains("-offset"))
                    inject_index = Array.IndexOf(args, "-offset");
                else
                    inject_index = Array.IndexOf(args, "-a");
                injectofs = Convert.ToInt32(args[inject_index + 1]);
            }
            else
                injectofs = 0x035D0000;

            // Verbose?
            bool verbose = false;
            verbose = args.Contains("-v") ? true : false;
            verbose = args.Contains("-verbose") ? true : false;

            //byte[] zovl = File.ReadAllBytes(@"C:\Z64\ovl_obj_gensettilesize\ovl_obj_gensettilesize.ovl");
            //byte[] zovl = File.ReadAllBytes(@"C:\Z64\ovl_en_example\ovl_en_example.ovl");
            //byte[] zovl = File.ReadAllBytes(@"C:\Z64\En_Sa\En_Sa.zovl");

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
                    if (verbose)
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
            if (!inject)
            {
                Console.ReadKey();
            }

            if (inject)
            {
                string overlay = string.Format("{0:X8}{1:X8}{2:X8}{3:X8}00000000{4:X8}0000000000000000", (injectofs), (injectofs + zovl.Length), (vRAMstart), (vRAMstart + zovl.Length), (init_vars | vRAMstart), ((actno * 0x20) + 0x00B8D440));
                byte[] ovlentry = StringToByteArray(overlay);

                // Inject into ROM
                Console.Write("Injecting...");

                int pos = 0;
                using (BinaryWriter b = new BinaryWriter(File.Open(rompath, FileMode.Open)))
                {
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
                }
                Console.Write("Done!");
            }
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

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
    }
}
