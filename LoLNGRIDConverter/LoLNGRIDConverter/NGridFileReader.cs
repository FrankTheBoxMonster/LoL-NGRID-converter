﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoLNGRIDConverter {
    public class NGridFileReader {

        #region Color definitions

        private static Color walkableColor = new Color(255, 255, 255);
        private static Color brushColor = new Color(0, 122, 14);
        private static Color wallColor = new Color(64, 64, 64);
        private static Color brushWallColor = new Color(0, 216, 111);
        private static Color transparentWallColor = new Color(0, 210, 214);
        private static Color alwaysVisibleColor = new Color(192, 192, 0); // not seen from Riot
        private static Color blueTeamOnlyColor = new Color(87, 79, 255);
        private static Color redTeamOnlyColor = new Color(255, 124, 124);
        private static Color neutralZoneVisibilityColor = new Color(255, 165, 0);  // HTML orange (not seen from Riot)
        private static Color blueTeamNeutralZoneVisibilityColor = new Color(12, 0, 255);
        private static Color redTeamNeutralZoneVisibilityColor = new Color(255, 0, 0);


        // all other flags will get indexed into this array
        private static Color[] flagColors = new Color[] { new Color(64, 0, 0),
                                                          new Color(140, 0, 0),
                                                          new Color(240, 0, 0),
                                                          new Color(0, 100, 0),
                                                          new Color(0, 240, 0),
                                                          new Color(0, 0, 100),
                                                          new Color(0, 0, 240),
                                                          new Color(100, 0, 100),
                                                          new Color(240, 0, 240),
                                                          new Color(140, 140, 0),
                                                          new Color(240, 240, 0),
                                                          new Color(0, 140, 140),
                                                          new Color(0, 240, 240),
                                                          new Color(64, 64, 64),
                                                          new Color(160, 160, 160),
                                                          new Color(240, 240, 240)
                                                        };

        private const bool showOverrideColorChanges = false;
        private static Color overrideWalkableColor = new Color(255, 0, 255);
        private static Color overrideBrushColor = new Color(255, 165, 0);
        private static Color overrideWallColor = new Color(64, 0, 0);

        private static Dictionary<Color, Color> overrideColors = new Dictionary<Color, Color>() { { walkableColor, overrideWalkableColor },
                                                                                                  { brushColor, overrideBrushColor },
                                                                                                  { wallColor, overrideWallColor },
                                                                                                  { transparentWallColor, overrideWallColor }
                                                                                                };


        // height samples are a gradient based on whatever this color is
        // highest height sample = pure base color
        // lowest height sample = pure black
        // 
        // black and white looks nicer than red as a base, but if there's only a single height
        // sample value for the entire map then the default image becomes pure white, which
        // blends in with the background when the image viewer is opened (and even with SR height
        // samples, the corners of spawn are still not very visible against the white background),
        // so we'll stick with the red base color for now
        // 
        // red also seems to be the easiest to differentiate between different shades
        private static Color heightSampleBaseColor = new Color(255, 0, 0);

        #endregion


        private FileWrapper ngridFile;
        private FileWrapper overlayFile;

        private int ngridMajorVersion;
        private int ngridMinorVersion;

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


        public NGridFileReader(FileWrapper ngridFile, int ngridMajorVersion, int ngridMinorVersion) {
            this.ngridFile = ngridFile;

            this.ngridMajorVersion = ngridMajorVersion;
            this.ngridMinorVersion = ngridMinorVersion;


            minBounds = ngridFile.ReadVector3();
            maxBounds = ngridFile.ReadVector3();

            Console.WriteLine("min bounds = " + minBounds + " max bounds = " + maxBounds);

            cellSize = ngridFile.ReadFloat();
            cellCountX = ngridFile.ReadInt();
            cellCountZ = ngridFile.ReadInt();

            Console.WriteLine("cell size = " + cellSize + " cell count X = " + cellCountX + " cell count Z = " + cellCountZ);


            Console.WriteLine("\nreading cells:  " + ngridFile.GetFilePosition());

            if(ngridMajorVersion == 7) {
                ReadCellsVersion7();
            } else if(ngridMajorVersion == 2 || ngridMajorVersion == 3 || ngridMajorVersion == 5) {
                ReadCellsVersion5();
            } else {
                throw new System.Exception("Error:  unsupported version number " + ngridMajorVersion);
            }


            Console.WriteLine("reading height samples:  " + ngridFile.GetFilePosition());

            ReadHeightSamples();


            Console.WriteLine("\nreading hint nodes:  " + ngridFile.GetFilePosition());

            ReadHintNodes();


            Console.WriteLine("\nlast read location:  " + ngridFile.GetFilePosition());
            Console.WriteLine("missed bytes:  " + (ngridFile.GetLength() - ngridFile.GetFilePosition()));


            CheckForMissingFlags();
        }


        #region ReadCellsVersion7()

        private void ReadCellsVersion7() {
            int totalCellCount = cellCountX * cellCountZ;

            for(int i = 0; i < totalCellCount; i++) {
                NavGridCell cell = new NavGridCell();

                cell.index = i;


                ngridFile.ReadFloat();  // center height (overridden by height samples)
                ngridFile.ReadInt();  // session ID
                ngridFile.ReadFloat();  // arrival cost
                ngridFile.ReadInt();  // is open
                ngridFile.ReadFloat();  // heuristic

                cell.x = ngridFile.ReadShort();
                cell.z = ngridFile.ReadShort();

                ngridFile.ReadInt();  // actor list
                ngridFile.ReadInt();  // unknown 1
                ngridFile.ReadInt();  // good cell session ID
                ngridFile.ReadFloat();  // hint weight
                ngridFile.ReadShort();  // unknown 2
                ngridFile.ReadShort();  // arrival direction
                ngridFile.ReadShort();  // hint node 1
                ngridFile.ReadShort();  // hint node 2


                cells.Add(cell);
            }


            for(int i = 0; i < totalCellCount; i++) {
                cells[i].visionPathingFlags = (VisionPathingFlags) ngridFile.ReadShort();
            }

            for(int i = 0; i < totalCellCount; i++) {
                cells[i].riverRegionFlags = (RiverRegionFlags) ngridFile.ReadByte();

                int jungleQuadrantAndMainRegionFlags = ngridFile.ReadByte();
                cells[i].jungleQuadrantFlags = (JungleQuadrantFlags) (jungleQuadrantAndMainRegionFlags & 0x0f);
                cells[i].mainRegionFlags = (MainRegionFlags) ((jungleQuadrantAndMainRegionFlags & ~0x0f) >> 4);

                int nearestLaneAndPOIFlags = ngridFile.ReadByte();
                cells[i].nearestLaneFlags = (NearestLaneFlags) (nearestLaneAndPOIFlags & 0x0f);
                cells[i].poiFlags = (POIFlags) ((nearestLaneAndPOIFlags & ~0x0f) >> 4);

                int ringAndSRXFlags = ngridFile.ReadByte();
                cells[i].ringFlags = (RingFlags) (ringAndSRXFlags & 0x0f);
                cells[i].srxFlags = (UnknownSRXFlags) ((ringAndSRXFlags & ~0x0f) >> 4);
            }


            // appears to be 8 blocks of 132 bytes each, but in practice only 7 are used and the 8th is all zeros
            // 
            // roughly appears to be 8 bytes of maybe some sort of hash followed by alternating between four bytes of zero and four bytes
            // of garbage (a couple make valid floats, most are invalid floats, maybe more hashes?)
            // 
            // at a certain point, each block becomes all zero for the rest of the block, but this varies by block (appears to be around
            // 40-48 bytes after the first 8 bytes until the rest is all zero)

            Console.WriteLine("reading unknown block:  " + ngridFile.GetFilePosition());
            for(int i = 0; i < 8; i++) {
                for(int j = 0; j < 132; j++) {
                    ngridFile.ReadByte();
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


                ngridFile.ReadFloat();  // center height (overridden by height samples)
                ngridFile.ReadInt();  // session ID
                ngridFile.ReadFloat();  // arrival cost
                ngridFile.ReadInt();  // is open
                ngridFile.ReadFloat();  // heuristic
                ngridFile.ReadInt();  // actor list

                cell.x = ngridFile.ReadShort();
                cell.z = ngridFile.ReadShort();

                ngridFile.ReadFloat();  // additional cost
                ngridFile.ReadFloat();  // hint as good cell
                ngridFile.ReadInt();  // additional cost count
                ngridFile.ReadInt();  // good cell session ID
                ngridFile.ReadFloat();  // hint weight

                int arrivalDirection = ngridFile.ReadShort();  // arrival direction
                int visionPathingFlags = ngridFile.ReadShort();
                int hintNode1 = ngridFile.ReadShort();  // hint node 1
                int hintNode2 = ngridFile.ReadShort();  // hint node 2


                if(ngridMajorVersion == 2 && hintNode2 == 0) {
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


            if(ngridMajorVersion == 5) {
                // version 5 only has 2 bytes per cell instead of version 7's 4 bytes per cell, meaning that some flag layers are missing in version 5
                Console.WriteLine("reading flag block:  " + ngridFile.GetFilePosition());
                for(int i = 0; i < totalCellCount; i++) {
                    cells[i].riverRegionFlags = (RiverRegionFlags) ngridFile.ReadByte();

                    int jungleQuadrantAndMainRegionFlags = ngridFile.ReadByte();
                    cells[i].jungleQuadrantFlags = (JungleQuadrantFlags) (jungleQuadrantAndMainRegionFlags & 0x0f);
                    cells[i].mainRegionFlags = (MainRegionFlags) ((jungleQuadrantAndMainRegionFlags & ~0x0f) >> 4);
                }


                // version 5 only has 4 blocks of 132 bytes each instead of version 7's 8 blocks of 132 bytes each
                Console.WriteLine("reading unknown block:  " + ngridFile.GetFilePosition());
                for(int i = 0; i < 4; i++) {
                    for(int j = 0; j < 132; j++) {
                        ngridFile.ReadByte();
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

            heightSampleCountX = ngridFile.ReadInt();
            heightSampleCountZ = ngridFile.ReadInt();
            heightSampleOffsetX = ngridFile.ReadFloat();
            heightSampleOffsetZ = ngridFile.ReadFloat();

            // record the min and max height samples so that we can get an accurate gradient
            minHeightSample = 0;
            maxHeightSample = 0;

            int totalCount = heightSampleCountX * heightSampleCountZ;
            for(int i = 0; i < totalCount; i++) {
                float sample = ngridFile.ReadFloat();

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
                    ngridFile.ReadFloat();  // this appears to be a distance to another cell, but not sure how cells are indexed here, also seems to be mostly whole numbers
                }

                int hintX = ngridFile.ReadShort();  // are these what is referred to by 'hint nodes' in NavGridCell?
                int hintY = ngridFile.ReadShort();
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
            List<UnknownSRXFlags> newSRXFlags = new List<UnknownSRXFlags>();


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

                if(cell.srxFlags > UnknownSRXFlags.LastKnownFlag && newSRXFlags.Contains(cell.srxFlags) == false) {
                    newSRXFlags.Add(cell.srxFlags);
                }
            }


            newJungleQuadrantFlags.Sort();
            newMainRegionFlags.Sort();
            newNearestLaneFlags.Sort();
            newPOIFlags.Sort();
            newRingFlags.Sort();
            newSRXFlags.Sort();


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
                Console.WriteLine("\nfound new RingFlags:");

                for(int i = 0; i < newRingFlags.Count; i++) {
                    Console.WriteLine(" - " + (int) newRingFlags[i]);
                }
            }

            if(newSRXFlags.Count > 0) {
                foundNewFlags = true;
                Console.WriteLine("\nfound new SRXFlags:");

                for(int i = 0; i < newSRXFlags.Count; i++) {
                    Console.WriteLine(" - " + (int) newSRXFlags[i]);
                }
            }


            if(foundNewFlags == true) {
                Console.WriteLine("\nreport any new flags found (except for on the very first Nexus Blitz layout)");
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
            string baseFileName = this.ngridFile.GetFolderPath() + this.ngridFile.GetName();

            string visionPathingFileName = baseFileName + ".VisionPathing";
            if(this.overlayFile != null) {
                visionPathingFileName += "." + this.overlayFile.GetName();
            }
            FileWrapper outputVisionPathing = CreateBMPFile(visionPathingFileName + ".bmp");

            FileWrapper outputRiverRegions = CreateBMPFile(baseFileName + ".RiverRegions.bmp");
            FileWrapper outputJungleQuadrants = CreateBMPFile(baseFileName + ".JungleQuadrants.bmp");
            FileWrapper outputMainRegions = CreateBMPFile(baseFileName + ".MainRegions.bmp");
            FileWrapper outputNearestLane = CreateBMPFile(baseFileName + ".NearestLane.bmp");
            FileWrapper outputPOI = CreateBMPFile(baseFileName + ".POI.bmp");
            FileWrapper outputRings = CreateBMPFile(baseFileName + ".Rings.bmp");
            FileWrapper outputSRX = CreateBMPFile(baseFileName + ".SRX.bmp");

            FileWrapper outputHeightSamples = CreateBMPFile(baseFileName + ".HeightSamples.bmp", heightSampleCountX, heightSampleCountZ);



            int paddedBytes = GetBMPBytePadding();

            for(int i = 0; i < cells.Count; i++) {
                VisionPathingFlags filteredPathingFlags = cells[i].visionPathingFlags;
                filteredPathingFlags &= ~VisionPathingFlags.Unknown128;  // we want to ignore this flag since it doesn't appear to have significance and makes the images patchy
                Color visionPathingColor = GetCellColor(filteredPathingFlags);

                if(showOverrideColorChanges == true && cells[i].hasOverride == true) {
                    Color overrideColor;
                    if(overrideColors.TryGetValue(visionPathingColor, out overrideColor) == true) {
                        // use the override color instead
                        visionPathingColor = overrideColor;
                    } else {
                        // no override mapping, just keep original
                    }
                }
                outputVisionPathing.WriteColor(visionPathingColor);


                outputRiverRegions.WriteColor(GetCellColor(cells[i].riverRegionFlags));
                outputJungleQuadrants.WriteColor(GetCellColor(cells[i].jungleQuadrantFlags));
                outputMainRegions.WriteColor(GetCellColor(cells[i].mainRegionFlags));
                outputNearestLane.WriteColor(GetCellColor(cells[i].nearestLaneFlags));
                outputPOI.WriteColor(GetCellColor(cells[i].poiFlags));
                outputRings.WriteColor(GetCellColor(cells[i].ringFlags));
                outputSRX.WriteColor(GetCellColor(cells[i].srxFlags));


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
                        outputSRX.WriteByte(0);
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
            outputSRX.Close();

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


            /*if(CheckVisionPathingFlag(flags, VisionPathingFlags.Unknown128) == true) {
                return brushColor;
            } else {
                return walkableColor;
            }*/


            Color color = walkableColor;

            if(CheckVisionPathingFlag(flags, VisionPathingFlags.Brush) == true) {
                color = brushColor;
            }

            if(CheckVisionPathingFlag(flags, VisionPathingFlags.Wall) == true) {
                if(color == brushColor) {
                    color = brushWallColor;  // effectively treated as a transparent wall
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

            if(CheckRiverRegionFlag(flags, RiverRegionFlags.Unknown32) == true) {
                // this is at risk of running out of colors, but we clamp the color index to be in bounds later
                // in practice, this is the only case where you can actually run out of colors
                index |= 16;
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

        private Color GetCellColor(UnknownSRXFlags flags) {
            return GetCellColor((int) flags);
        }

        private Color GetCellColor(int flags) {
            int index = flags;

            if(index >= flagColors.Length) {
                index = flagColors.Length - 1;
            }

            return flagColors[index];
        }

        #endregion


        #region WriteLSGNGRIDFile()

        private void WriteLSGNGRIDFile() {
            string baseFileName = this.ngridFile.GetFolderPath() + this.ngridFile.GetName();

            string outputFileName = baseFileName;
            if(this.overlayFile != null) {
                outputFileName += "." + this.overlayFile.GetName();
            }
            FileWrapper outputLSGNGRID = new FileWrapper(outputFileName + ".LSGNGRID");
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

                int ringAndSRXFlags = ((int) cell.ringFlags) | ((int) cell.srxFlags << 4);
                outputLSGNGRID.WriteByte(ringAndSRXFlags);
            }


            outputLSGNGRID.Close();
        }

        #endregion


        #region ApplyNGridOverlay()

        public void ApplyNGridOverlay(FileWrapper overlayFile, int overlayMajorVersion, int overlayMinorVersion) {
            this.overlayFile = overlayFile;


            // this is pretty much just a giant bounding box for all of the modified cells, with only cells in this box being stored in the overlay
            // 
            // in practice, this cuts out 50-60% of the required data, however it could be significantly smaller if multiple bounding boxes were used

            int startX = overlayFile.ReadInt();
            int startZ = overlayFile.ReadInt();
            int countX = overlayFile.ReadInt();
            int countZ = overlayFile.ReadInt();

            for(int i = 0; i < countZ; i++) {
                int z = startZ + i;

                for(int j = 0; j < countX; j++) {
                    int x = startX + j;
                    int cellIndex = (z * cellCountX) + x;
                    NavGridCell cell = this.cells[cellIndex];
                    VisionPathingFlags filteredFlags = cell.visionPathingFlags;


                    VisionPathingFlags overrideFlag = (VisionPathingFlags) overlayFile.ReadShort();


                    if(CheckVisionPathingFlag(filteredFlags, VisionPathingFlags.StructureWall) == true) {
                        // don't allow structure wall cells to be overridden
                        // 
                        // overlay files tend to not include structure wall flags, which means that if we don't filter them ourselves, then
                        // we'll end up completely wiping them
                    } else {
                        filteredFlags &= ~VisionPathingFlags.Unknown128;
                        filteredFlags &= ~VisionPathingFlags.TransparentWall;
                        VisionPathingFlags filteredOverrideFlag = overrideFlag & ~VisionPathingFlags.TransparentWall;

                        if(filteredFlags != filteredOverrideFlag) {
                            cell.hasOverride = true;
                        }

                        cell.visionPathingFlags = overrideFlag;
                    }
                }
            }


            Console.WriteLine("\nlast read location:  " + overlayFile.GetFilePosition());
            Console.WriteLine("missed bytes:  " + (overlayFile.GetLength() - overlayFile.GetFilePosition()));
        }

        #endregion
    }
}
