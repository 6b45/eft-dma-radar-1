using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace eft_dma_radar
{
    public class RegisteredPlayers
    {
        private readonly ulong _base;
        private readonly ulong _listBase;
        private readonly HashSet<string> _registered;
        private ConcurrentDictionary<string, Player> _players; // backing field
        private readonly Stopwatch _regSw = new();
        private readonly Stopwatch _healthSw = new();
        public ConcurrentDictionary<string, Player> Players
        {
            get
            {
                return Volatile.Read(ref _players);
            }
        }
        public int PlayerCount
        {
            get
            {
                return Memory.ReadInt(_base + Offsets.RegisteredPlayers_Count);
            }
        }

        public RegisteredPlayers(ulong baseAddr)
        {
            _base = baseAddr;
            _listBase = Memory.ReadPtr(_base + Offsets.UnityListBase);
            _registered = new HashSet<string>();
            _players = new ConcurrentDictionary<string, Player>();
            _regSw.Start();
            _healthSw.Start();
        }

        /// <summary>
        /// Updates the ConcurrentDictionary of 'Players'
        /// </summary>
        public void UpdateList()
        {
            try
            {
                if (_regSw.ElapsedMilliseconds < 500) return; // Update every 500ms
                _registered.Clear();
                var count = this.PlayerCount; // cache count
                var scatterMap = new ScatterReadMap();
                var round1 = scatterMap.AddRound();
                var round2 = scatterMap.AddRound();
                var round3 = scatterMap.AddRound();
                var round4 = scatterMap.AddRound();
                var round5 = scatterMap.AddRound();
                for (int i = 0; i < count; i++)
                {
                    var playerBase = round1.AddEntry(i,
                        0,
                        _listBase + Offsets.UnityListBase_Start + (uint)(i * 0x8),
                        typeof(ulong));
                    var playerProfile = round2.AddEntry(i, 1, playerBase,
                        typeof(ulong), 0, Offsets.PlayerBase_Profile);

                    var playerId = round3.AddEntry(i, 2, playerProfile, typeof(ulong),
                        0, Offsets.PlayerProfile_PlayerId);
                    var playerIdLen = round4.AddEntry(i, 3, playerId, typeof(int),
                        0, Offsets.UnityString_Len);
                    var playerIdStr = round5.AddEntry(i, 4, playerId, typeof(UnityString),
                        playerIdLen, Offsets.UnityString_Value);
                    playerIdStr.SizeMult = 2; // Unity String twice the length
                }
                scatterMap.Execute(count);
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        var id = (string)scatterMap.Results[i][4].Result;
                        var playerBase = (ulong)scatterMap.Results[i][0].Result;
                        var playerProfile = (ulong)scatterMap.Results[i][1].Result;
                        if (id is null || id == String.Empty) throw new Exception("Invalid Player ID!");
                        if (!_players.ContainsKey(id))
                        {
                            var player = new Player((ulong)playerBase, (ulong)playerProfile); // allocate player object
                            _players.TryAdd(id, player);
                        }
                        else
                        {
                            _players[id].IsActive = true;
                            _players[id].IsAlive = true;
                            _players[id].MayBeDead = false;
                        }
                        _registered.Add(id);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"ERROR allocating player at index {i}: {ex}");
                    }
                }
                var inactivePlayers = _players.Where(x => !_registered.Contains(x.Key));
                foreach (KeyValuePair<string, Player> player in inactivePlayers)
                {
                    player.Value.Update();
                    player.Value.IsActive = false;
                    if (player.Value.MayBeDead) player.Value.IsAlive = false;
                }
                _regSw.Restart();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR iterating registered players: {ex}");
            }
        }

        /// <summary>
        /// Updates all 'Player' values (Position,health,direction,etc.)
        /// </summary>
        public void UpdateAllPlayers()
        {
            try
            {
                var players = _players.Where(x => x.Value.IsActive && x.Value.IsAlive).ToArray();
                bool checkHealth = _healthSw.ElapsedMilliseconds > 250;
                var scatterMap = new ScatterReadMap();
                var round1 = scatterMap.AddRound();
                for (int i = 0; i < players.Length; i++)
                {
                    var dir = round1.AddEntry(i, 0, players[i].Value.MovementContext + Offsets.MovementContext_Direction,
                        typeof(float), null);
                    var pitch = round1.AddEntry(i, 1, players[i].Value.MovementContext + Offsets.MovementContext_Direction + 0x4,
                        typeof(float));
                    if (checkHealth) for (int p = 0; p < 7; p++)
                    {
                        var health = round1.AddEntry(i, 2 + p, players[i].Value.BodyParts[p] + Offsets.HealthEntry_Value,
                            typeof(float), null);
                    }
                    var pos1 = round1.AddEntry(i, 9, players[i].Value.PlayerTransformMatrixListBase,
                        typeof(IntPtr), Marshal.SizeOf(typeof(Matrix34)) * 1 + Marshal.SizeOf(typeof(Matrix34)));
                    var pos2 = round1.AddEntry(i, 10, players[i].Value.PlayerTransformDependencyIndexTableBase,
                        typeof(IntPtr), Marshal.SizeOf(typeof(int)) * 1 + Marshal.SizeOf(typeof(int)));
                }
                scatterMap.Execute(players.Length);

                for (int i = 0; i < players.Length; i++)
                {
                    var dir = (float?)scatterMap.Results[i][0].Result;
                    var pitch = (float?)scatterMap.Results[i][1].Result;
                    players[i].Value.SetDirection(dir);
                    players[i].Value.SetPitch(pitch);
                    if (checkHealth)
                    {
                        float?[] bodyParts = new float?[7];
                        for (int p = 0; p < 7; p++)
                        {
                            bodyParts[p] = (float?)scatterMap.Results[i][2 + p].Result;
                        }
                        players[i].Value.SetHealth(bodyParts);
                    }
                    object[] ptrs = new object[2]
                    {
                            scatterMap.Results[i][9].Result,
                            scatterMap.Results[i][10].Result
                    };
                    players[i].Value.SetPosition(ptrs);
                }
                if (checkHealth) _healthSw.Restart();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR updating All Players: {ex}");
            }
        }
    }
}
