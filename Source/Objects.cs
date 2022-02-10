using System.Text.Json;
using System.Diagnostics;
using System.Numerics;
using System.Text.Json.Serialization;
using SkiaSharp;

namespace eft_dma_radar
{

    // GUI Testing Structures, may change

    public class DebugStopwatch
    {
        private readonly Stopwatch _sw;
        private readonly string _name;

        public DebugStopwatch(string name = null)
        {
            _name = name;
            _sw = new Stopwatch();
            _sw.Start();
        }

        public void Stop()
        {
            _sw.Stop();
            TimeSpan ts = _sw.Elapsed;
            Debug.WriteLine($"{_name} Stopwatch Runtime: {ts.Milliseconds}ms");
        }
    }

    /// <summary>
    /// Defines Player Unit Type (Player,PMC,Scav,etc.)
    /// </summary>
    public enum PlayerType
    {
        Default,
        CurrentPlayer,
        Teammate,
        PMC, // side 0x1 0x2
        AIScav, // side 0x4
        AIBoss, // ToDo
        PlayerScav // side 0x4
    }

    /// <summary>
    /// Defines map position for the UI Map.
    /// </summary>
    public struct MapPosition
    {
        public float X;
        public float Y;
        public float Height; // Z

        /// <summary>
        /// Get exact player location.
        /// </summary>
        public SKPoint GetPoint()
        {
            return new SKPoint(X, Y);
        }

        /// <summary>
        /// Gets Point where player name should be drawn.
        /// </summary>
        public SKPoint GetNamePoint(int xOff = 0, int yOff = 0)
        {
            return new SKPoint(X + xOff, Y + yOff);
        }

        /// <summary>
        /// Gets up arrow where loot is. IDisposable.
        /// </summary>
        public SKPath GetUpArrow()
        {
            SKPath path = new SKPath();
            path.MoveTo(X, Y);
            path.LineTo(X - 6, Y + 6);
            path.LineTo(X + 6, Y + 6);
            path.Close();

            return path;
        }

        /// <summary>
        /// Gets down arrow where loot is. IDisposable.
        /// </summary>
        public SKPath GetDownArrow()
        {
            SKPath path = new SKPath();
            path.MoveTo(X, Y);
            path.LineTo(X - 6, Y - 6);
            path.LineTo(X + 6, Y - 6);
            path.Close();

            return path;
        }
    }

    /// <summary>
    /// Defines a Map for use in the GUI.
    /// </summary>
    public class Map
    {
        public readonly string Name;
        public readonly MapConfig ConfigFile;
        public readonly string ConfigFilePath;

        public Map(string name, MapConfig config, string configPath)
        {
            Name = name;
            ConfigFile = config;
            ConfigFilePath = configPath;
        }
    }

    /// <summary>
    /// Defines a .JSON Map Config File
    /// </summary>
    public class MapConfig
    {
        [JsonPropertyName("x")]
        public float X { get; set; }
        [JsonPropertyName("y")]
        public float Y { get; set; }
        [JsonPropertyName("z")]
        public float Z { get; set; }
        [JsonPropertyName("scale")]
        public float Scale { get; set; }
        /// <summary>
        /// * This List contains the path to the map file(s), and a minimum height (Z) value.
        /// * Each tuple consists of Item1: (float)MIN_HEIGHT, Item2: (string>MAP_PATH
        /// * This list will be iterated backwards, and if the current player height (Z) is above the float
        /// value, then that map layer will be drawn. This will allow having different bitmaps at different
        /// heights.
        /// * If using only a single map (no layers), set the float value to something very low like -100.
        /// </summary>
        [JsonPropertyName("maps")]
        public List<Tuple<float, string>> Maps { get; set; }


        public static MapConfig LoadFromFile(string file)
        {
            var json = File.ReadAllText(file);
            return JsonSerializer.Deserialize<MapConfig>(json);
        }

        public void Save(Map map)
        {
            var json = JsonSerializer.Serialize<MapConfig>(this);
            File.WriteAllText(map.ConfigFilePath, json);
        }
    }

    public class Config
    {
        [JsonPropertyName("playerAimLine")]
        public int PlayerAimLineLength { get; set; }
        [JsonPropertyName("defaultZoom")]
        public int DefaultZoom { get; set; }
        [JsonPropertyName("lootEnabled")]
        public bool LootEnabled { get; set; }

        public Config()
        {
            PlayerAimLineLength = 10;
            DefaultZoom = 100;
            LootEnabled = true;
        }

        public static bool TryLoadConfig(out Config config)
        {
            try
            {
                if (!File.Exists("Config.json")) throw new FileNotFoundException("Config.json does not exist!");
                var json = File.ReadAllText("Config.json");
                config = JsonSerializer.Deserialize<Config>(json);
                return true;
            }
            catch
            {
                config = null;
                return false;
            }
        }
        public static void SaveConfig(Config config)
        {
            var json = JsonSerializer.Serialize<Config>(config);
            File.WriteAllText("Config.json", json);
        }
    }

    /// <summary>
    /// Top level object defining a scatter read operation. Create one of these in a local context.
    /// </summary>
    public class ScatterReadMap
    {
        private readonly List<ScatterReadRound> Rounds = new();
        /// <summary>
        /// Contains results from Scatter Read after Execute() is performed. First key is Index, Second Key ID.
        /// </summary>
        public Dictionary<int, Dictionary<int, ScatterReadEntry>> Results = new();

        /// <summary>
        /// Executes Scatter Read operation as defined per the map.
        /// </summary>
        public void Execute(int indexCount)
        {
            for (int i = 0; i < indexCount; i++) // Add dict for each index
            {
                Results.Add(i, new Dictionary<int, ScatterReadEntry>());
            }
            foreach (var round in Rounds)
            {
                round.Run();
            }
        }
        /// <summary>
        /// Add scatter read rounds to the operation. Each round is a successive scatter read, you may need multiple
        /// rounds if you have reads dependent on earlier scatter reads result(s).
        /// </summary>
        /// <returns>ScatterReadRound object.</returns>
        public ScatterReadRound AddRound()
        {
            var round = new ScatterReadRound(this);
            Rounds.Add(round);
            return round;
        }
    }

    /// <summary>
    /// Defines a scatter read round. Each round will execute a single scatter read. If you have reads that
    /// are dependent on previous reads (chained pointers for example), you may need multiple rounds.
    /// </summary>
    public class ScatterReadRound
    {
        private readonly ScatterReadMap _map;
        private readonly List<ScatterReadEntry> Entries = new();
        public ScatterReadRound(ScatterReadMap map)
        {
            _map = map;
        }

        /// <summary>
        /// Adds a single Scatter Read Entry.
        /// </summary>
        /// <param name="index">For loop index this is associated with.</param>
        /// <param name="id">Random ID number to identify the entry's purpose.</param>
        /// <param name="addr">Address to read from (you can pass a ScatterReadEntry from an earlier round, 
        /// and it will use the result).</param>
        /// <param name="type">Type of object to read.</param>
        /// <param name="size">Size of oject to read (ONLY for reference types, value types get size from
        /// Type). You canc pass a ScatterReadEntry from an earlier round and it will use the Result.</param>
        /// <param name="offset">Optional offset to add to address (usually in the event that you pass a
        /// ScatterReadEntry to the Addr field).</param>
        /// <returns></returns>
        public ScatterReadEntry AddEntry(int index, int id, object addr, Type type, object size = null, uint offset = 0x0)
        {
            if (size is null) size = (int)0;
            var entry = new ScatterReadEntry()
            {
                Index = index,
                Id = id,
                Addr = addr,
                Type = type,
                Size = size,
                Offset = offset
            };
            Entries.Add(entry);
            return entry;
        }

        /// <summary>
        /// Internal use only do not use.
        /// </summary>
        public void Run()
        {
            Memory.ReadScatter(Entries.ToArray(), _map.Results);
        }
    }

    /// <summary>
    /// Single scatter read entry. Use ScatterReadRound.AddEntry() to construct this class.
    /// </summary>
    public class ScatterReadEntry
    {
        /// <summary>
        /// for loop index this is associated with
        /// </summary>
        public int Index;
        /// <summary>
        /// Random identifier code (1 = PlayerBase, 2 = PlayerProfile, etc.)
        /// </summary>
        public int Id;
        /// <summary>
        /// Can be an ulong or another ScatterReadEntry
        /// </summary>
        public object Addr = (ulong)0x0;
        /// <summary>
        /// Offset amount to be added to Address.
        /// </summary>
        public uint Offset = 0x0;
        /// <summary>
        /// Defines the type. For value types is also used to determine the size.
        /// </summary>
        public Type Type;
        /// <summary>
        /// Can be an int32 or another ScatterReadEntry
        /// </summary>
        public object Size;
        /// <summary>
        /// Multiplies size by this value (ex: unity strings *2). Default: 1
        /// </summary>
        public int SizeMult = 1;
        /// <summary>
        /// Result is stored here, must cast to unbox.
        /// </summary>
        public object Result;
    }

    /// <summary>
    /// Type placeholder for scatter reads. Not for use.
    /// </summary>
    public struct UnityString
    {
    }

    public struct ZoomLevel
    {
        public float Width;
        public float Height;
    }

    // EFT/Unity Structures (WIP)
    public struct GameObjectManager
    {
        public ulong LastTaggedNode; // 0x0

        public ulong TaggedNodes; // 0x8

        public ulong LastMainCameraTaggedNode; // 0x10

        public ulong MainCameraTaggedNodes; // 0x18

        public ulong LastActiveNode; // 0x20

        public ulong ActiveNodes; // 0x28

    }

    public struct BaseObject
    {
        public ulong previousObjectLink; //0x0000
        public ulong nextObjectLink; //0x0008
        public ulong obj; //0x0010
	};


    public struct Matrix34
    {
        public Vector4 vec0;
        public Vector4 vec1;
        public Vector4 vec2;

    }

}
