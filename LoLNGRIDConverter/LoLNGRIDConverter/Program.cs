using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoLNGRIDConverter {
    public class Program {

        public static void Main(string[] args) {
            Console.WriteLine("LoL NGRID Converter by FrankTheBoxMonster");


            string ngridFilePath = "";
            string overlayFilePath = "";

            for(int i = 0; i < args.Length; i++) {
                string path = args[i].ToLower();
                if(path.EndsWith(".aimesh_ngrid") == true) {
                    ngridFilePath = args[i];
                } else if(path.EndsWith(".ngrid_overlay") == true) {
                    overlayFilePath = args[i];
                } else {
                    // ignore
                }
            }


            if(ngridFilePath == "") {
                if(overlayFilePath != "") {
                    Console.WriteLine("Error:  found a .ngrid_overlay file, but no corresponding .aimesh_ngrid file (overlays require a base file to apply onto)");
                } else {
                    Console.WriteLine("Error:  must provide a .aimesh_ngrid file (you can drag-and-drop a file onto the .exe)");
                }
                Pause();
                System.Environment.Exit(1);
            }


            try {
                ConvertFiles(ngridFilePath, overlayFilePath);
            } catch (System.Exception e) {
                Console.WriteLine(e.ToString());
                Console.WriteLine("\n\nplease report this error");
            }


            Console.WriteLine("\n\nDone");
            Pause();
        }


        public static void Pause() {
            Console.WriteLine("Press any key to continue . . .");
            Console.ReadKey(true);
        }


        private static void ConvertFiles(string ngridFilePath, string overlayFilePath) {
            FileWrapper ngridFile = new FileWrapper(ngridFilePath);

            int ngridMajorVersion = ngridFile.ReadByte();
            int ngridMinorVersion = 0;
            if(ngridMajorVersion != 2) {
                // not sure why this is short but the other is byte, might just be padding
                // note that the only known non-zero minor version to exist (other than unofficial 2.1) is 3.1, with no know difference from 3.0
                ngridMinorVersion = ngridFile.ReadShort();
            } else {
                // version 2 lacked a minor version value (although it clearly needed one since there's an unofficial version 2.0 and 2.1 split)
            }

            Console.WriteLine("\nngrid major version = " + ngridMajorVersion + " minor version = " + ngridMinorVersion);

            if(ngridMajorVersion != 7 && ngridMajorVersion != 5 && ngridMajorVersion != 3 && ngridMajorVersion != 2) {
                Console.WriteLine("Error:  unsupported ngrid version number " + ngridMajorVersion + " (please report this)");
                return;
            }


            NGridFileReader file = new NGridFileReader(ngridFile, ngridMajorVersion, ngridMinorVersion);


            if(overlayFilePath != "") {
                Console.WriteLine("\n\n\napplying .ngrid_overlay file");

                FileWrapper overlayFile = new FileWrapper(overlayFilePath);

                int overlayMajorVersion = overlayFile.ReadByte();
                int overlayMinorVersion = overlayFile.ReadByte();

                Console.WriteLine("\noverlay major version = " + overlayMajorVersion + " minor version = " + overlayMinorVersion);

                if(overlayMajorVersion != 1 && overlayMinorVersion != 1) {  // so far only 1.1 is known
                    Console.WriteLine("Error:  unsupported overlay version number " + overlayMajorVersion + "." + overlayMinorVersion + " (please report this)");
                    return;
                }

                file.ApplyNGridOverlay(overlayFile, overlayMajorVersion, overlayMinorVersion);
            }


            file.ConvertFiles();
        }
    }
}
