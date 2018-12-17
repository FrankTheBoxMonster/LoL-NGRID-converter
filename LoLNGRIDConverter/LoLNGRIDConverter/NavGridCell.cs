using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoLNGRIDConverter {
    public class NavGridCell {
        public int index;
        public VisionPathingFlags visionPathingFlags;
        public RiverRegionFlags riverRegionFlags;
        public JungleQuadrantFlags jungleQuadrantFlags;
        public MainRegionFlags mainRegionFlags;
        public NearestLaneFlags nearestLaneFlags;
        public POIFlags poiFlags;
        public RingFlags ringFlags;

        public int x;
        public int z;


        public NavGridCell() {

        }
    }

    public enum VisionPathingFlags {  // 16 bits, bitfield
        KnownFlags = Walkable | Brush | Wall | StructureWall | TransparentWall | Unknown128 | AlwaysVisible | BlueTeamOnly | RedTeamOnly | NeutralZoneVisiblity,

        Walkable = 0,

        Brush = 1,
        Wall = 2,
        StructureWall = 4,
        Unobserved8 = 8,

        Unobserved16 = 16,
        Unobserved32 = 32,
        TransparentWall = 64,
        Unknown128 = 128,  // marks the difference between two otherwise-equivalent cells, spread sporadically throughout the map, ignored for a cleaner image since it doesn't seem useful at all

        AlwaysVisible = 256,
        Unknown512 = 512,  // only ever found on the original Nexus Blitz map, and it was only present in two sections of what would otherwise be normal wall
        BlueTeamOnly = 1024,
        RedTeamOnly = 2048,

        NeutralZoneVisiblity = 4096,  // no bits observed past this point
    }

    public enum RiverRegionFlags {  // 8 bits, bitfield (equality for original Nexus Blitz)
        KnownFlags = NonJungle | JungleQuadrant | BaronPit | River | RiverEntrance,

        NonJungle = 0,

        JungleQuadrant = 1,
        BaronPit = 2,
        Unobserved4 = 4,
        Unobserved8 = 8,

        River = 16,
        Unknown32 = 32,  // only ever found on the original Nexus Blitz map, where it was instead used to represent the river (other flags were shuffled too)
        RiverEntrance = 64,  // no bits observed past this point
    }

    public enum JungleQuadrantFlags {  // 4 bits, equality
        LastKnownFlag = SouthJungleQuadrant,

        None = 0,

        NorthJungleQuadrant = 1,
        EastJungleQuadrant = 2,
        WestJungleQuadrant = 3,
        SouthJungleQuadrant = 4,

        Unobserved8 = 8,
    }

    public enum MainRegionFlags {  // 4 bits, equality
        LastKnownFlag = BotSideBasePerimeter,

        Spawn = 0,
        Base = 1,

        TopLane = 2,
        MidLane = 3,
        BotLane = 4,

        TopSideJungle = 5,
        BotSideJungle = 6,

        TopSideRiver = 7,
        BotSideRiver = 8,

        TopSideBasePerimeter = 9,
        BotSideBasePerimeter = 10,
    }

    public enum NearestLaneFlags {  // 4 bits, equality
        LastKnownFlag = RedSideBotNeutralZone,

        BlueSideTopLane = 0,
        BlueSideMidLane = 1,
        BlueSideBotLane = 2,

        RedSideTopLane = 3,
        RedSideMidLane = 4,
        RedSideBotLane = 5,

        BlueSideTopNeutralZone = 6,
        BlueSideMidNeutralZone = 7,
        BlueSideBotNeutralZone = 8,

        RedSideTopNeutralZone = 9,
        RedSideMidNeutralZone = 10,
        RedSideBotNeutralZone = 11,
    }

    public enum POIFlags {  // 4 bits, equality
        LastKnownFlag = CampMurkWolves,

        None = 0,

        NearTurret = 1,
        BaseGates = 2,

        BaronPit = 3,
        DragonPit = 4,

        CampRedBuff = 5,
        CampBlueBuff = 6,
        CampGromp = 7,
        CampKrugs = 8,
        CampRaptors = 9,
        CampMurkWolves = 10,
    }

    public enum RingFlags {  // 4 bits, equality, although upper 4 bits are unused, potentially leaving room for another flag layer
        LastKnownFlag = RedOuterToNeutral,

        BlueSpawnToNexus = 0,
        BlueNexusToInhib = 1,
        BlueInhibToInner = 2,
        BlueInnerToOuter = 3,
        BlueOuterToNeutral = 4,

        RedSpawnToNexus = 5,
        RedNexusToInhib = 6,
        RedInhibToInner = 7,
        RedInnerToOuter = 8,
        RedOuterToNeutral = 9,
    }
}
