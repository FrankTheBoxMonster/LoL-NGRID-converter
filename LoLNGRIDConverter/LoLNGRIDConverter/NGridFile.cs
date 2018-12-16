using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoLNGRIDConverter {
    public class NGridFileReader {

        #region Color definitions

        // our colors
        /*private static Color walkableColor = new Color(0, 0, 192);
        private static Color brushColor = new Color(0, 192, 0);
        private static Color wallColor = new Color(192, 0, 0);
        private static Color brushWallColor = new Color(192, 0, 0);  // same as wall
        private static Color transparentWallColor = new Color(128, 0, 128);  // HTML purple
        private static Color alwaysVisibleColor = new Color(192, 192, 0);
        private static Color blueTeamOnlyColor = new Color(0, 192, 192);
        private static Color redTeamOnlyColor = new Color(255, 192, 203);  // HTML pink
        private static Color neutralZoneVisibilityColor = new Color(255, 165, 0);  // HTML orange
        private static Color blueTeamNeutralZoneVisibilityColor = new Color(255, 165, 0);  // same as non-team specific
        private static Color redTeamNeutralZoneVisibilityColor = new Color(255, 165, 0);  // same as non-team specific
        */

        // Riot colors
        private static Color walkableColor = new Color(255, 255, 255);
        private static Color brushColor = new Color(0, 122, 14);
        private static Color wallColor = new Color(0, 65, 122);
        //private static Color brushWallColor = new Color(0, 216, 111);
        private static Color brushWallColor = new Color(0, 65, 122);  // same as wall (not using Riot's color because Riot's is ambiguous between walkable or not)
        private static Color transparentWallColor = new Color(0, 210, 214);
        private static Color alwaysVisibleColor = new Color(192, 192, 0); // not seen from Riot
        private static Color blueTeamOnlyColor = new Color(87, 79, 255);
        private static Color redTeamOnlyColor = new Color(255, 124, 124);
        private static Color neutralZoneVisibilityColor = new Color(255, 165, 0);  // HTML orange (not seen from Riot)
        private static Color blueTeamNeutralZoneVisibilityColor = new Color(12, 0, 255);
        private static Color redTeamNeutralZoneVisibilityColor = new Color(255, 0, 0);


        // all other flags will get indexed into this array
        private static Color[] flagColors = new Color[] { new Color(64, 0, 0),
                                                          new Color(160, 0, 0),
                                                          new Color(240, 0, 0),
                                                          new Color(0, 64, 0),
                                                          new Color(0, 160, 0),
                                                          new Color(0, 240, 0),
                                                          new Color(0, 0, 64),
                                                          new Color(0, 0, 160),
                                                          new Color(0, 0, 240),
                                                          new Color(64, 0, 64),
                                                          new Color(160, 0, 160),
                                                          new Color(240, 0, 240),
                                                          new Color(64, 64, 0),
                                                          new Color(160, 160, 0),
                                                          new Color(240, 240, 0),
                                                          new Color(0, 64, 64),
                                                          new Color(0, 160, 160),
                                                          new Color(0, 240, 240),
                                                          new Color(64, 64, 64),
                                                          new Color(160, 160, 160),
                                                          new Color(240, 240, 240)
                                                        };


        // height samples are a gradient based on whatever this color is
        // highest height sample = pure base color
        // lowest height sample = pure black
        private static Color heightSampleBaseColor = new Color(255, 0, 0);

        #endregion


        private FileWrapper file;

        private int majorVersion;
        private int minorVersion;

        private Vector3 minBounds;
        private Vector3 maxBounds;

        private float cellSize;
        private int cellCountX;
        private int cellCountZ;

        private List<NavGridCell> cells = new List<NavGridCell>();

        private int heightSampleCountX;
        private int heightSampleCountZ;
        private float heightSampleOffsetX;
        private float heightSampleOffsetZ;

        private List<float> heightSamples = new List<float>();
        private float minHeightSample;
        private float maxHeightSample;


        public NGridFileReader(FileWrapper file, int majorVersion, int minorVersion) {
            this.file = file;

            this.majorVersion = majorVersion;
            this.minorVersion = minorVersion;


            minBounds = file.ReadVector3();
            maxBounds = file.ReadVector3();

            Console.WriteLine("min bounds = " + minBounds + " max bounds = " + maxBounds);

            cellSize = file.ReadFloat();
            cellCountX = file.ReadInt();
            cellCountZ = file.ReadInt();

            Console.WriteLine("cell size = " + cellSize + " cell count X = " + cellCountX + " cell count Z = " + cellCountZ);


            Console.WriteLine("\nreading cells:  " + file.GetFilePosition());

            if(majorVersion == 7) {
                ReadCellsVersion7();
            } else if(majorVersion == 2 || majorVersion == 3 || majorVersion == 5) {
                ReadCellsVersion5();
            } else {
                throw new System.Exception("Error:  unsupported version number " + majorVersion);
            }


            Console.WriteLine("reading height samples:  " + file.GetFilePosition());

            ReadHeightSamples();


            Console.WriteLine("\nreading hint nodes:  " + file.GetFilePosition());

            ReadHintNodes();


            Console.WriteLine("\nlast read location:  " + file.GetFilePosition());
            Console.WriteLine("missed bytes:  " + (file.GetLength() - file.GetFilePosition()));


            CheckForMissingFlags();
        }


        #region ReadCellsVersion7()

        private void ReadCellsVersion7() {
            int totalCellCount = cellCountX * cellCountZ;

            for(int i = 0; i < totalCellCount; i++) {
                NavGridCell cell = new NavGridCell();

                cell.index = i;


                file.ReadFloat();  // center height (overridden by height samples)
                file.ReadInt();  // session ID
                file.ReadFloat();  // arrival cost
                file.ReadInt();  // is open
                file.ReadFloat();  // heuristic

                cell.x = file.ReadShort();
                cell.z = file.ReadShort();

                file.ReadInt();  // actor list
                file.ReadInt();  // unknown 1
                file.ReadInt();  // good cell session ID
                file.ReadFloat();  // hint weight
                file.ReadShort();  // unknown 2
                file.ReadShort();  // arrival direction
                file.ReadShort();  // hint node 1
                file.ReadShort();  // hint node 2


                cells.Add(cell);
            }


            for(int i = 0; i < totalCellCount; i++) {
                cells[i].visionPathingFlags = (VisionPathingFlags) file.ReadShort();
            }

            for(int i = 0; i < totalCellCount; i++) {
                cells[i].riverRegionFlags = (RiverRegionFlags) file.ReadByte();

                int jungleQuadrantAndMainRegionFlags = file.ReadByte();
                cells[i].jungleQuadrantFlags = (JungleQuadrantFlags) (jungleQuadrantAndMainRegionFlags & 0x0f);
                cells[i].mainRegionFlags = (MainRegionFlags) ((jungleQuadrantAndMainRegionFlags & ~0x0f) >> 4);

                int nearestLaneAndPOIFlags = file.ReadByte();
                cells[i].nearestLaneFlags = (NearestLaneFlags) (nearestLaneAndPOIFlags & 0x0f);
                cells[i].poiFlags = (POIFlags) ((nearestLaneAndPOIFlags & ~0x0f) >> 4);

                cells[i].ringFlags = (RingFlags) file.ReadByte();
            }


            // appears to be 8 blocks of 132 bytes each, but in practice only 7 are used and the 8th is all zeros
            // 
            // roughly appears to be 8 bytes of maybe some sort of hash followed by alternating between four bytes of zero and four bytes
            // of garbage (a couple make valid floats, most are invalid floats, maybe more hashes?)
            // 
            // at a certain point, each block becomes all zero for the rest of the block, but this varies by block (appears to be around
            // 40-48 bytes after the first 8 bytes until the rest is all zero)

            Console.WriteLine("reading unknown block:  " + file.GetFilePosition());
            for(int i = 0; i < 8; i++) {
                for(int j = 0; j < 132; j++) {
                    file.ReadByte();
                }
            }
        }

        #endregion

        #region ReadCellsVersion5()

        private void ReadCellsVersion5() {
            int totalCellCount = cellCountX * cellCountZ;

            for(int i = 0; i < totalCellCount; i++) {
                NavGridCell cell = new NavGridCell();

                cell.index = i;


                file.ReadFloat();  // center height (overridden by height samples)
                file.ReadInt();  // session ID
                file.ReadFloat();  // arrival cost
                file.ReadInt();  // is open
                file.ReadFloat();  // heuristic
                file.ReadInt();  // actor list

                cell.x = file.ReadShort();
                cell.z = file.ReadShort();

                file.ReadFloat();  // additional cost
                file.ReadFloat();  // hint as good cell
                file.ReadInt();  // additional cost count
                file.ReadInt();  // good cell session ID
                file.ReadFloat();  // hint weight

                int arrivalDirection = file.ReadShort();  // arrival direction
                int visionPathingFlags = file.ReadShort();
                int hintNode1 = file.ReadShort();  // hint node 1
                int hintNode2 = file.ReadShort();  // hint node 2


                if(majorVersion == 2 && hintNode2 == 0) {
                    // older versions only gave one byte to arrival direction and vision pathing flags instead of two bytes,
                    // so we have to do some reshuffling since there was no change in major version number to reflect this,
                    // and minor version numbers didn't exist yet (both one-byte and two-byte variants use version 2)
                    // 
                    // this leads to the unofficial version numbers 2.0 and 2.1

                    hintNode2 = hintNode1;
                    hintNode1 = visionPathingFlags;

                    visionPathingFlags = (arrivalDirection & ~0xff) >> 8;
                    arrivalDirection &= 0xff;
                }

                cell.visionPathingFlags = (VisionPathingFlags) visionPathingFlags;


                cells.Add(cell);
            }


            if(majorVersion == 5) {
                // version 5 only has 2 bytes per cell instead of version 7's 4 bytes per cell, meaning that some flag layers are missing in version 5
                Console.WriteLine("reading flag block:  " + file.GetFilePosition());
                for(int i = 0; i < totalCellCount; i++) {
                    cells[i].riverRegionFlags = (RiverRegionFlags) file.ReadByte();

                    int jungleQuadrantAndMainRegionFlags = file.ReadByte();
                    cells[i].jungleQuadrantFlags = (JungleQuadrantFlags) (jungleQuadrantAndMainRegionFlags & 0x0f);
                    cells[i].mainRegionFlags = (MainRegionFlags) ((jungleQuadrantAndMainRegionFlags & ~0x0f) >> 4);
                }


                // version 5 only has 4 blocks of 132 bytes each instead of version 7's 8 blocks of 132 bytes each
                Console.WriteLine("reading unknown block:  " + file.GetFilePosition());
                for(int i = 0; i < 4; i++) {
                    for(int j = 0; j < 132; j++) {
                        file.ReadByte();
                    }
                }
            } else {
                // version 2 and version 3 lack an extra flag block and jump straight into height samples after the last cell
            }
        }

        #endregion

        #region ReadHeightSamples()

        private void ReadHeightSamples() {
            // these are what's actually used for the height mesh
            // unwalkable cells define a center height value of 0, but this (accidentally?) includes base gates
            // these height sample values accomadate for base gates, so they should be used instead of the center height values
            // 
            // there appears to be one sample per cell corner
            // total sample count = (total cell count * 4) + (cell count X * 2) + (cell count Z * 2) + 1

            heightSampleCountX = file.ReadInt();
            heightSampleCountZ = file.ReadInt();
            heightSampleOffsetX = file.ReadFloat();
            heightSampleOffsetZ = file.ReadFloat();

            // record the min and max height samples so that we can get an accurate gradient
            minHeightSample = 0;
            maxHeightSample = 0;

            int totalCount = heightSampleCountX * heightSampleCountZ;
            for(int i = 0; i < totalCount; i++) {
                float sample = file.ReadFloat();

                if(i == 0 || sample < minHeightSample) {
                    minHeightSample = sample;
                }

                if(i == 0 || sample > maxHeightSample) {
                    maxHeightSample = sample;
                }

                heightSamples.Add(sample);
            }

            Console.WriteLine("\nmin sample height = " + minHeightSample + " max sample height = " + maxHeightSample);
        }

        #endregion

        #region ReadHintNodes()

        private void ReadHintNodes() {
            // not really sure how this data is used (the 'hint nodes' in NavGridCell appear to refer to these)

            for(int i = 0; i < 900; i++) {  // this 900 value *might* be variable, in practice it's constant though
                for(int j = 0; j < 900; j++) {  // same as above
                    file.ReadFloat();  // this appears to be a distance to another cell, but not sure how cells are indexed here, also seems to be mostly whole numbers
                }

                int hintX = file.ReadShort();  // are these what is referred to by 'hint nodes' in NavGridCell?
                int hintY = file.ReadShort();
            }
        }

        #endregion

        #region CheckForMissingFlags()

        private void CheckForMissingFlags() {
            VisionPathingFlags mergedVisionPathingFlags = (VisionPathingFlags) 0;
            RiverRegionFlags mergedRiverRegionFlags = (RiverRegionFlags) 0;

            List<JungleQuadrantFlags> newJungleQuadrantFlags = new List<JungleQuadrantFlags>();
            List<MainRegionFlags> newMainRegionFlags = new List<MainRegionFlags>();
            List<NearestLaneFlags> newNearestLaneFlags = new List<NearestLaneFlags>();
            List<POIFlags> newPOIFlags = new List<POIFlags>();
            List<RingFlags> newRingFlags = new List<RingFlags>();


            for(int i = 0; i < cells.Count; i++) {
                NavGridCell cell = cells[i];


                mergedVisionPathingFlags |= cell.visionPathingFlags;
                mergedRiverRegionFlags |= cell.riverRegionFlags;


                // these values are always read as a single byte, so no need to worry about signed comparisons

                if(cell.jungleQuadrantFlags > JungleQuadrantFlags.LastKnownFlag && newJungleQuadrantFlags.Contains(cell.jungleQuadrantFlags) == false) {
                    newJungleQuadrantFlags.Add(cell.jungleQuadrantFlags);
                }

                if(cell.mainRegionFlags > MainRegionFlags.LastKnownFlag && newMainRegionFlags.Contains(cell.mainRegionFlags) == false) {
                    newMainRegionFlags.Add(cell.mainRegionFlags);
                }

                if(cell.nearestLaneFlags > NearestLaneFlags.LastKnownFlag && newNearestLaneFlags.Contains(cell.nearestLaneFlags) == false) {
                    newNearestLaneFlags.Add(cell.nearestLaneFlags);
                }

                if(cell.poiFlags > POIFlags.LastKnownFlag && newPOIFlags.Contains(cell.poiFlags) == false) {
                    newPOIFlags.Add(cell.poiFlags);
                }

                if(cell.ringFlags > RingFlags.LastKnownFlag && newRingFlags.Contains(cell.ringFlags) == false) {
                    newRingFlags.Add(cell.ringFlags);
                }
            }


            newJungleQuadrantFlags.Sort();
            newMainRegionFlags.Sort();
            newNearestLaneFlags.Sort();
            newPOIFlags.Sort();
            newRingFlags.Sort();


            bool foundNewFlags = false;

            int newVisionPathingFlags = (int) (mergedVisionPathingFlags & ~VisionPathingFlags.KnownFlags);
            if(newVisionPathingFlags != 0) {
                foundNewFlags = true;
                Console.WriteLine("\nfound new VisionPathingFlags:");

                for(int i = 0; i < 16; i++) {
                    int newFlag = newVisionPathingFlags & (1 << i);
                    if(newFlag != 0) {
                        Console.WriteLine(" - " + newFlag);
                    }
                }
            }

            int newRiverRegionFlags = (int) (mergedRiverRegionFlags & ~RiverRegionFlags.KnownFlags);
            if(newRiverRegionFlags != 0) {
                foundNewFlags = true;
                Console.WriteLine("\nfound new RiverRegionFlags:");

                for(int i = 0; i < 16; i++) {
                    int newFlag = newRiverRegionFlags & (1 << i);
                    if(newFlag != 0) {
                        Console.WriteLine(" - " + newFlag);
                    }
                }
            }


            if(newJungleQuadrantFlags.Count > 0) {
                foundNewFlags = true;
                Console.WriteLine("\nfound new JungleQuadrantFlags:");

                for(int i = 0; i < newJungleQuadrantFlags.Count; i++) {
                    Console.WriteLine(" - " + (int) newJungleQuadrantFlags[i]);
                }
            }

            if(newMainRegionFlags.Count > 0) {
                foundNewFlags = true;
                Console.WriteLine("\nfound new MainRegionFlags:");

                for(int i = 0; i < newMainRegionFlags.Count; i++) {
                    Console.WriteLine(" - " + (int) newMainRegionFlags[i]);
                }
            }

            if(newNearestLaneFlags.Count > 0) {
                foundNewFlags = true;
                Console.WriteLine("\nfound new NearestLaneFlags:");

                for(int i = 0; i < newNearestLaneFlags.Count; i++) {
                    Console.WriteLine(" - " + (int) newNearestLaneFlags[i]);
                }
            }

            if(newPOIFlags.Count > 0) {
                foundNewFlags = true;
                Console.WriteLine("\nfound new POIFlags:");

                for(int i = 0; i < newPOIFlags.Count; i++) {
                    Console.WriteLine(" - " + (int) newPOIFlags[i]);
                }
            }

            if(newRingFlags.Count > 0) {
                foundNewFlags = true;
                Console.WriteLine("\nfound new RingFlags (potentially a completely new flag layer using the upper 4 bits of this value):");

                for(int i = 0; i < newRingFlags.Count; i++) {
                    Console.WriteLine(" - " + (int) newRingFlags[i]);
                }
            }


            if(foundNewFlags == true) {
                Console.WriteLine("\nreport any new flags found (except for VisionPathing 512, unless it's on a map other than the very first Nexus Blitz layout)");
                Program.Pause();
            } else {
                Console.WriteLine("\nno unexpected flags found");
            }
        }

        #endregion



        public void ConvertFiles() {
            Console.WriteLine("\nwriting output files");

            WriteBMPFiles();
            WriteLSGNGRIDFile();
        }

        #region WriteBMPFiles()

        private void WriteBMPFiles() {
            string baseFileName = this.file.GetFolderPath() + this.file.GetName();

            FileWrapper outputVisionPathing = CreateBMPFile(baseFileName + ".visionPathing.bmp");
            FileWrapper outputRiverRegions = CreateBMPFile(baseFileName + ".riverRegions.bmp");
            FileWrapper outputJungleQuadrants = CreateBMPFile(baseFileName + ".jungleQuadrants.bmp");
            FileWrapper outputMainRegions = CreateBMPFile(baseFileName + ".mainRegions.bmp");
            FileWrapper outputNearestLane = CreateBMPFile(baseFileName + ".nearestLane.bmp");
            FileWrapper outputPOI = CreateBMPFile(baseFileName + ".POI.bmp");
            FileWrapper outputRings = CreateBMPFile(baseFileName + ".rings.bmp");

            FileWrapper outputHeightSamples = CreateBMPFile(baseFileName + ".heightSamples.bmp", heightSampleCountX, heightSampleCountZ);



            int paddedBytes = GetBMPBytePadding();

            for(int i = 0; i < cells.Count; i++) {
                VisionPathingFlags filteredPathingFlags = cells[i].visionPathingFlags;
                filteredPathingFlags &= ~VisionPathingFlags.Unknown128;  // we want to ignore this flag since it doesn't appear to have significance and makes the images patchy
                outputVisionPathing.WriteColor(GetCellColor(filteredPathingFlags));


                outputRiverRegions.WriteColor(GetCellColor(cells[i].riverRegionFlags));
                outputJungleQuadrants.WriteColor(GetCellColor(cells[i].jungleQuadrantFlags));
                outputMainRegions.WriteColor(GetCellColor(cells[i].mainRegionFlags));
                outputNearestLane.WriteColor(GetCellColor(cells[i].nearestLaneFlags));
                outputPOI.WriteColor(GetCellColor(cells[i].poiFlags));
                outputRings.WriteColor(GetCellColor(cells[i].ringFlags));


                // each row gets padded so that the total byte size of the row is a multiple of 4 bytes

                if((i % cellCountX) == (cellCountX - 1)) {
                    for(int j = 0; j < paddedBytes; j++) {
                        outputVisionPathing.WriteByte(0);
                        outputRiverRegions.WriteByte(0);
                        outputJungleQuadrants.WriteByte(0);
                        outputMainRegions.WriteByte(0);
                        outputNearestLane.WriteByte(0);
                        outputPOI.WriteByte(0);
                        outputRings.WriteByte(0);
                    }
                }
            }



            float heightDiff = maxHeightSample - minHeightSample;


            int heightSamplePaddedBytes = GetBMPBytePadding(heightSampleCountX);

            for(int i = 0; i < heightSamples.Count; i++) {
                float sample = heightSamples[i];

                float lerp = (sample - minHeightSample) / heightDiff;
                Color sampleColor = heightSampleBaseColor * lerp;  // highest sample will be pure base, lowest sample will be black

                outputHeightSamples.WriteColor(sampleColor);


                if((i % heightSampleCountX) == (heightSampleCountX - 1)) {
                    for(int j = 0; j < heightSamplePaddedBytes; j++) {
                        outputHeightSamples.WriteByte(0);
                    }
                }
            }



            outputVisionPathing.Close();
            outputRiverRegions.Close();
            outputJungleQuadrants.Close();
            outputMainRegions.Close();
            outputNearestLane.Close();
            outputPOI.Close();
            outputRings.Close();

            outputHeightSamples.Close();
        }

        #endregion

        #region GetBMPBytePadding()

        private int GetBMPBytePadding() {
            return GetBMPBytePadding(cellCountX);
        }

        private int GetBMPBytePadding(int countX) {
            int bytesPerPixel = 3;  // rgba = 4 bytes per cell, rgb = 3 bytes per cell
            int bytesPerRow = countX * bytesPerPixel;
            int paddedBytes = 0;

            if((bytesPerRow % 4) != 0) {  // correcting for padding rows to 4-byte size offsets
                paddedBytes = 4 - (bytesPerRow % 4);
            }

            return paddedBytes;
        }

        #endregion

        #region CreateBMPFile()

        private FileWrapper CreateBMPFile(string filePath) {
            return CreateBMPFile(filePath, cellCountX, cellCountZ);
        }

        private FileWrapper CreateBMPFile(string filePath, int countX, int countZ) {
            FileWrapper bmpFile = new FileWrapper(filePath);

            bmpFile.Clear();  // don't want excess bytes remaining in the new file


            bmpFile.WriteShort(0x4d42);  // 'B' 'M'

            int headerByteSize = 14 + 0x28;  // 14 bytes for file header + 0x28 bytes for the data header

            int bytesPerPixel = 3;  // rgba = 4 bytes per cell, rgb = 3 bytes per cell
            int bytesPerRow = countX * bytesPerPixel;
            int paddedBytes = GetBMPBytePadding(countX);
            bytesPerRow += paddedBytes;

            int pixelByteSize = bytesPerRow * countZ;

            int totalByteSize = headerByteSize + pixelByteSize;
            bmpFile.WriteInt(totalByteSize);  // total size of the file in bytes

            bmpFile.WriteInt(0);  // ignored (usage depends on the program creating the file, which is irrelevant for our purposes)
            bmpFile.WriteInt(headerByteSize);  // offset to start of the pixel data


            // data header

            bmpFile.WriteInt(0x28);  // length of data header
            bmpFile.WriteInt(countX);  // image width
            bmpFile.WriteInt(countZ);  // image height
            bmpFile.WriteShort(1);  // "number of color planes, must be 1"
            bmpFile.WriteShort(bytesPerPixel * 8);  // bits per pixel (24 bytes = rgb, 32 bits = rgba)
            bmpFile.WriteInt(0);  // compression method, 0 = none
            bmpFile.WriteInt(pixelByteSize);  // total byte size of pixel data

            bmpFile.WriteInt(0);  // rest of the header has meaning but is ignoreable
            bmpFile.WriteInt(0);
            bmpFile.WriteInt(0);
            bmpFile.WriteInt(0);


            // pixel data goes next (handled elsewhere)

            return bmpFile;
        }

        #endregion

        #region CheckFlag()

        private bool CheckVisionPathingFlag(VisionPathingFlags flags, VisionPathingFlags mask) {
            return ((flags | mask) == flags);
        }

        private bool CheckRiverRegionFlag(RiverRegionFlags flags, RiverRegionFlags mask) {
            return ((flags | mask) == flags);
        }

        #endregion

        #region GetCellColor()

        private Color GetCellColor(VisionPathingFlags flags) {
            // note that priority is a factor here (a brush inside a wall should count as a wall)
            // 
            // also note that the following flags are ignored:
            //  - 4 - structure wall (identical to transparent wall)
            //  - 128 - unknown (creates patchy images, significance unknown, seemingly useless)
            // 
            // all other known flags are represented


            Color color = walkableColor;

            if(CheckVisionPathingFlag(flags, VisionPathingFlags.Brush) == true) {
                color = brushColor;
            }

            if(CheckVisionPathingFlag(flags, VisionPathingFlags.Wall) == true) {
                if(color == brushColor) {
                    color = brushWallColor;
                } else {
                    color = wallColor;
                }
            }

            if(CheckVisionPathingFlag(flags, VisionPathingFlags.TransparentWall) == true) {
                color = transparentWallColor;
            }

            if(CheckVisionPathingFlag(flags, VisionPathingFlags.AlwaysVisible) == true) {
                color = alwaysVisibleColor;
            }

            if(CheckVisionPathingFlag(flags, VisionPathingFlags.BlueTeamOnly) == true) {
                color = blueTeamOnlyColor;
            }

            if(CheckVisionPathingFlag(flags, VisionPathingFlags.RedTeamOnly) == true) {
                color = redTeamOnlyColor;
            }

            if(CheckVisionPathingFlag(flags, VisionPathingFlags.NeutralZoneVisiblity) == true) {
                if(color == blueTeamOnlyColor) {
                    color = blueTeamNeutralZoneVisibilityColor;
                } else if(color == redTeamOnlyColor) {
                    color = redTeamNeutralZoneVisibilityColor;
                } else {
                    color = neutralZoneVisibilityColor;
                }
            }

            return color;
        }

        private Color GetCellColor(RiverRegionFlags flags) {
            // compresses/condenses the bitfield to only consist of useable bits (all unobserved positions removed)

            int index = 0;

            if(CheckRiverRegionFlag(flags, RiverRegionFlags.JungleQuadrant) == true) {
                index |= 1;
            }

            if(CheckRiverRegionFlag(flags, RiverRegionFlags.BaronPit) == true) {
                index |= 2;
            }

            if(CheckRiverRegionFlag(flags, RiverRegionFlags.River) == true) {
                index |= 4;
            }

            if(CheckRiverRegionFlag(flags, RiverRegionFlags.RiverEntrance) == true) {
                index |= 8;
            }


            return GetCellColor(index);
        }

        private Color GetCellColor(JungleQuadrantFlags flags) {
            return GetCellColor((int) flags);
        }

        private Color GetCellColor(MainRegionFlags flags) {
            return GetCellColor((int) flags);
        }

        private Color GetCellColor(NearestLaneFlags flags) {
            return GetCellColor((int) flags);
        }

        private Color GetCellColor(POIFlags flags) {
            return GetCellColor((int) flags);
        }

        private Color GetCellColor(RingFlags flags) {
            return GetCellColor((int) flags);
        }

        private Color GetCellColor(int flags) {
            return flagColors[flags];
        }

        #endregion


        #region WriteLSGNGRIDFile()

        private void WriteLSGNGRIDFile() {
            string baseFileName = this.file.GetFolderPath() + this.file.GetName();

            FileWrapper outputLSGNGRID = new FileWrapper(baseFileName + ".LSGNGRID");
            outputLSGNGRID.Clear();  // don't want excess bytes remaining in the new file


            string magic = "LSGNGRID";
            outputLSGNGRID.WriteChars(magic.ToCharArray());

            outputLSGNGRID.WriteInt(2);  // version number

            outputLSGNGRID.WriteVector3(minBounds);
            outputLSGNGRID.WriteVector3(maxBounds);

            outputLSGNGRID.WriteFloat(cellSize);
            outputLSGNGRID.WriteInt(cellCountX);
            outputLSGNGRID.WriteInt(cellCountZ);

            outputLSGNGRID.WriteInt(heightSampleCountX);
            outputLSGNGRID.WriteInt(heightSampleCountZ);
            outputLSGNGRID.WriteFloat(heightSampleOffsetX);
            outputLSGNGRID.WriteFloat(heightSampleOffsetZ);


            // height sample data is written first because cells need to use it for calculating their own height values

            for(int i = 0; i < heightSamples.Count; i++) {
                float sample = heightSamples[i];

                outputLSGNGRID.WriteFloat(sample);
            }


            for(int i = 0; i < cells.Count; i++) {
                NavGridCell cell = cells[i];


                outputLSGNGRID.WriteShort((int) cell.visionPathingFlags);
                outputLSGNGRID.WriteByte((int) cell.riverRegionFlags);

                int jungleQuadrantAndMainRegionFlags = ((int) cell.jungleQuadrantFlags) | ((int) cell.mainRegionFlags << 4);
                outputLSGNGRID.WriteByte(jungleQuadrantAndMainRegionFlags);

                int nearestLaneAndPOIFlags = ((int) cell.nearestLaneFlags) | ((int) cell.poiFlags << 4);
                outputLSGNGRID.WriteByte(nearestLaneAndPOIFlags);

                outputLSGNGRID.WriteByte((int) cell.ringFlags);
            }


            outputLSGNGRID.Close();
        }

        #endregion
    }
}
