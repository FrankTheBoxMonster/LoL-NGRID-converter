using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoLNGRIDConverter {
    public class Program {

        public static void Main(string[] args) {
            Console.WriteLine("LoL NGRID Converter by FrankTheBoxMonster");

            if(args.Length < 1) {
                Console.WriteLine("Error:  must provide a file (you can drag-and-drop one or more files onto the .exe)");
                Pause();
                System.Environment.Exit(1);
            }

            for(int i = 0; i < args.Length; i++) {
                try {
                    Console.WriteLine("\nConverting file " + (i + 1) + "/" + args.Length + ":  " + args[i].Substring(args[i].LastIndexOf('\\') + 1));
                    TryReadFile(args[i]);
                } catch (System.Exception e) {
                    Console.WriteLine("\n\nError:  " + e.ToString());
                }
            }


            Console.WriteLine("\n\nDone");
            Pause();
        }


        public static void Pause() {
            Console.WriteLine("Press any key to continue . . .");
            Console.ReadKey(true);
        }


        private static void TryReadFile(string filePath) {
            FileWrapper input = new FileWrapper(filePath);

            if(filePath.ToLower().EndsWith(".aimesh_ngrid") == false) {
                Console.WriteLine("Error:  not an .AIMESH_NGRID file");
                return;
            }


            int majorVersion = input.ReadByte();
            int minorVersion = 0;
            if(majorVersion != 2) {
                // not sure why this is short but the other is byte, might just be padding
                // note that the only known non-zero minor version to exist (other than unofficial 2.1) is 3.1, with no know difference from 3.0
                minorVersion = input.ReadShort();
            } else {
                // version 2 lacked a minor version value (although it clearly needed one since there's an unofficial version 2.0 and 2.1 split)
            }

            Console.WriteLine("\nmajor version = " + majorVersion + " minor version = " + minorVersion);

            if(majorVersion != 7 && majorVersion != 5 && majorVersion != 3 && majorVersion != 2) {
                Console.WriteLine("Error:  unsupported version number " + majorVersion);
                return;
            }


            NGridFileReader file = new NGridFileReader(input, majorVersion, minorVersion);
            file.ConvertFiles();
        }
    }
}
