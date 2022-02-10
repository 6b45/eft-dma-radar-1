using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace eft_dma_radar
{

    /// <summary>
    /// Class containing Game (Raid) instance.
    /// </summary>
    public class Game
    {
        private readonly ulong _unityBase;
        private GameObjectManager _gom;
        private ulong _localGameWorld;
        private LootEngine _lootEngine;
        private RegisteredPlayers _rgtPlayers;
        private bool _inGame = false;
        public bool InGame
        {
            get
            {
                return Volatile.Read(ref _inGame);
            }
        }
        public ConcurrentDictionary<string, Player> Players
        {
            get
            {
                return _rgtPlayers?.Players;
            }
        }
        public List<LootItem> Loot
        {
            get
            {
                return _lootEngine?.Loot;
            }
        }

        public Game(ulong unityBase)
        {
            _unityBase = unityBase;
        }

        /// <summary>
        /// Waits until Raid has started before returning to caller.
        /// </summary>
        public void WaitForGame()
        {
            while (true)
            {
                if (GetGOM() && GetLGW())
                {
                    Thread.Sleep(1000);
                    break;
                }
                Thread.Sleep(3500);
            }
            Debug.WriteLine("Raid has started!");
            _inGame = true;
        }

        /// <summary>
        /// Helper method to locate Game World object.
        /// </summary>
        private ulong GetObjectFromList(ulong listPtr, ulong lastObjectPtr, string objectName)
        {
            var activeObject = Memory.ReadStruct<BaseObject>(Memory.ReadPtr(listPtr));
            var lastObject = Memory.ReadStruct<BaseObject>(Memory.ReadPtr(lastObjectPtr));

            if (activeObject.obj != 0x0)
            {
                while (activeObject.obj != 0x0 && activeObject.obj != lastObject.obj)
                {
                    var objectNamePtr = Memory.ReadPtr(activeObject.obj + Offsets.UnityObject_Name);
                    var objectNameStr = Memory.ReadString(objectNamePtr, 24);
                    if (objectNameStr.Contains(objectName, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine($"Found object {objectNameStr}");
                        return activeObject.obj;
                    }

                    activeObject = Memory.ReadStruct<BaseObject>(activeObject.nextObjectLink); // Read next object
                }
            }
            Debug.WriteLine($"Couldn't find object {objectName}");
            return 0;
        }

        /// <summary>
        /// Gets Game Object Manager structure.
        /// </summary>
        private bool GetGOM()
        {
            try
            {
                var addr = Memory.ReadPtr(_unityBase + Offsets.ModuleBase_GameObjectManager);
                _gom = Memory.ReadStruct<GameObjectManager>(addr);
                Debug.WriteLine($"Found Game Object Manager at 0x{addr.ToString("X")}");
                return true;
            }
            catch (DMAShutdown) { throw; }
            catch (Exception ex)
            {
                throw new GameNotRunningException($"ERROR getting Game Object Manager, game may not be running: {ex}");
            }
        }

        /// <summary>
        /// Gets Local Game World address.
        /// </summary>
        private bool GetLGW()
        {
            try
            {
                ulong activeNodes = Memory.ReadPtr(_gom.ActiveNodes);
                ulong lastActiveNode = Memory.ReadPtr(_gom.LastActiveNode);
                var gameWorld = GetObjectFromList(activeNodes, lastActiveNode, "GameWorld");
                if (gameWorld == 0) throw new Exception("Unable to find GameWorld Object, likely not in raid.");
                _localGameWorld = Memory.ReadPtrChain(gameWorld, Offsets.GameWorld_LocalGameWorld); // Game world >> Local Game World
                var rgtPlayers = new RegisteredPlayers(Memory.ReadPtr(_localGameWorld + Offsets.RegisteredPlayers));
                if (rgtPlayers.PlayerCount > 1) // Make sure not in hideout,etc.
                {
                    _rgtPlayers = rgtPlayers;
                    return true;
                }
                else
                {
                    Debug.WriteLine("ERROR - Local Game World does not contain players (hideout?)");
                    return false;
                }
            }
            catch (DMAShutdown) { throw; }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR getting Local Game World: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Main Game Loop executed by Memory Worker Thread. Updates player list, and updates all player values.
        /// </summary>
        public void GameLoop()
        {
            try
            {
                int playerCount = _rgtPlayers.PlayerCount;
                if (playerCount < 1 || playerCount > 1024)
                {
                    Debug.WriteLine("Raid has ended!");
                    _inGame = false;
                    return;
                }
                _rgtPlayers.UpdateList(); // Check for new players, add to list
                _rgtPlayers.UpdateAllPlayers(); // Update all player locations,etc.
                if (_lootEngine is null)
                {
                    _lootEngine = new LootEngine(_localGameWorld);
                }
            }
            catch
            {
                _inGame = false;
                throw;
            }
        }
    }

    public class GameNotRunningException : Exception
    {
        public GameNotRunningException()
        {
        }

        public GameNotRunningException(string message)
            : base(message)
        {
        }

        public GameNotRunningException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
