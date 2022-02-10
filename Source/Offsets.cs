namespace eft_dma_radar
{
    internal static class Offsets
    {
        public const uint ModuleBase_GameObjectManager = 0x17F8D28;
        public static readonly uint[] GameWorld_LocalGameWorld = new uint[] { 0x30, 0x18, 0x28 };
        
        public const uint RegisteredPlayers = 0x80;
        public const uint RegisteredPlayers_Count = 0x18;

        public const uint PlayerBase_Profile = 0x4B8;
        public const uint PlayerBase_MovementContext = 0x40;
        public const uint PlayerBase_IsLocalPlayer = 0x7EF;
        public static readonly uint[] PlayerBase_HealthController = new uint[] { 0x4F0, 0x50, Offsets.UnityDictBase };

        public const uint MovementContext_Direction = 0x22C;

        public static readonly uint[] Transform_TransformInternal = new uint[] { 0xA8, 0x28, 0x28, 0x10, 0x20, 0x10 };
        public const uint TransformInternal_TransfPMatrix = 0x38;
        //public const uint PlayerTransformInternal_Index = 0x40;
        public const uint TransfPMatrix_TransformDependencyIndexTableBase = 0x20;

        public const uint HealthEntry = 0x10;
        public const uint HealthEntry_Value = 0x10;

        public const uint PlayerProfile_PlayerId = 0x10;
        public const uint PlayerProfile_PlayerInfo = 0x28;

        public const uint PlayerInfo_PlayerName = 0x10;
        public const uint PlayerInfo_Experience = 0x74;
        public const uint PlayerInfo_PlayerSide = 0x58;
        public const uint PlayerInfo_RegDate = 0x5C;

        public const uint UnityDictBase = 0x18;

        public const uint UnityListBase = 0x10;
        public const uint UnityListBase_Start = 0x20;

        public const uint UnityString_Len = 0x10;
        public const uint UnityString_Value = 0x14;

        public const uint UnityObject_Name = 0x60;

        //Loot Stuff
        public const uint LOOT_LIST = 0x60;
        public const uint LOOT_LIST_ENTITY = 0x10;
        public const uint LOOT_LIST_COUNT = 0x18;
        public const uint LOOT_OBJECTS_ENTITY_BASE = 0x20;
        public const uint UNKOWN_PTR = 0x10;
        public const uint INTERACTIVE_CLASS = 0x28;
        public const uint BASE_OBJECT = 0x10;
        public const uint GAME_OBJECT = 0x30;
        public const uint GAME_OBJECT_NAME_PTR = 0x60;
        public static readonly string[] CONTAINERS = new string[] { "body", "XXXcap", "Ammo_crate_Cap", "Grenade_box_Door", "Medical_Door", "Toolbox_Door", "card_file_box", "cover_", "lootable", "scontainer_Blue_Barrel_Base_Cap", "scontainer_wood_CAP", "suitcase_plastic_lootable_open", "weapon_box_cover" };


        public static readonly Dictionary<int, int> EXP_TABLE = new Dictionary<int, int>
        {
            {0, 1},
            {1000, 2},
            {4017, 3},
            {8432, 4},
            {14256, 5},
            {21477, 6},
            {30023, 7},
            {39936, 8},
            {51204, 9},
            {63723, 10},
            {77563, 11},
            {92713, 12},
            {111881, 13},
            {134674, 14},
            {161139, 15},
            {191417, 16},
            {225194, 17},
            {262366, 18},
            {302484, 19},
            {345751, 20},
            {391649, 21},
            {440444, 22},
            {492366, 23},
            {547896, 24},
            {609066, 25},
            {675913, 26},
            {748474, 27},
            {826786, 28},
            {910885, 29},
            {1000809, 30},
            {1096593, 31},
            {1198275, 32},
            {1309251, 33},
            {1429580, 34},
            {1559321, 35},
            {1698532, 36},
            {1847272, 37},
            {2005600, 38},
            {2173575, 39},
            {2351255, 40},
            {2538699, 41},
            {2735966, 42},
            {2946585, 43},
            {3170637, 44},
            {3408202, 45},
            {3659361, 46},
            {3924195, 47},
            {4202784, 48},
            {4495210, 49},
            {4801553, 50},
            {5121894, 51},
            {5456314, 52},
            {5809667, 53},
            {6182063, 54},
            {6573613, 55},
            {6984426, 56},
            {7414613, 57},
            {7864284, 58},
            {8333549, 59},
            {8831052, 60},
            {9360623, 61},
            {9928578, 62},
            {10541848, 63},
            {11206300, 64},
            {11946977, 65},
            {12789143, 66},
            {13820522, 67},
            {15229487, 68},
            {17206065, 69},
            {19706065, 70},
            {22706065, 71},
            {26206065, 72},
            {30206065, 73},
            {34706065, 74},
            {39706065, 75},
        };
    }
}
