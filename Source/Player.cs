using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace eft_dma_radar
{
    /// <summary>
    /// Class containing Game Player Data. Use lock() when accessing instances of this class.
    /// </summary>
    public class Player
    {
        private static readonly List<string> _teammates;
        //private static string _currentPlayerGroupID = String.Empty;
        public readonly string Name;
        public readonly int Lvl = 0;
        public readonly PlayerType Type;
        //public readonly string GroupID; // ToDo not working
        private readonly ulong _playerBase;
        private readonly ulong _playerProfile;
        private readonly ulong _playerInfo;
        public readonly ulong[] BodyParts;
        public readonly ulong MovementContext;
        public ulong PlayerTransformInternal;
        public ulong PlayerTransformMatrixListBase;
        public ulong PlayerTransformDependencyIndexTableBase;
        public int Health = -1;
        public bool MayBeDead = false;
        public bool IsAlive = true;
        public bool IsActive = true;
        public Vector3 Position = new Vector3(0, 0, 0);
        public float Direction = 0f;
        public float Pitch = 0f;

        static Player()
        {
            _teammates = new();
            if (File.Exists("teammates.txt"))
            {
                _teammates = File.ReadAllLines("teammates.txt").ToList();
            }
            else File.Create("teammates.txt");
        }

        public Player(ulong playerBase, ulong playerProfile)
        {
            try
            {
                _playerBase = playerBase;
                _playerProfile = playerProfile;
                _playerInfo = Memory.ReadPtr(playerProfile + Offsets.PlayerProfile_PlayerInfo);
                var healthController = Memory.ReadPtrChain(_playerBase, Offsets.PlayerBase_HealthController);
                BodyParts = new ulong[7];
                for (uint i = 0; i < 7; i++)
                {                                                                          //dict
                    BodyParts[i] = Memory.ReadPtrChain(healthController, new uint[] { 0x30 + (i * 0x18), Offsets.HealthEntry });
                }
                MovementContext = Memory.ReadPtr(_playerBase + Offsets.PlayerBase_MovementContext);
                PlayerTransformInternal = Memory.ReadPtrChain(_playerBase, Offsets.Transform_TransformInternal);
                var playersTransfPMatrix = Memory.ReadPtr(PlayerTransformInternal + Offsets.TransformInternal_TransfPMatrix);
                PlayerTransformMatrixListBase = Memory.ReadPtr(playersTransfPMatrix + Offsets.UnityDictBase);
                PlayerTransformDependencyIndexTableBase = Memory.ReadPtr(playersTransfPMatrix + Offsets.TransfPMatrix_TransformDependencyIndexTableBase);
                //var grpPtr = Memory.ReadPtr(_playerInfo + 0x18);
                //GroupID = Memory.ReadString(grpPtr, 8);
                var namePtr = Memory.ReadPtr(_playerInfo + Offsets.PlayerInfo_PlayerName);
                Name = Memory.ReadUnityString(namePtr);
                var isLocalPlayer = Memory.ReadBool(_playerBase + Offsets.PlayerBase_IsLocalPlayer);
                if (isLocalPlayer)
                {
                    Type = PlayerType.CurrentPlayer;
                    //_currentPlayerGroupID = GroupID;
                }
                //else if (GroupID == _currentPlayerGroupID) Type = PlayerType.Teammate;
                else
                {
                    var playerSide = Memory.ReadInt(_playerInfo + Offsets.PlayerInfo_PlayerSide); // Scav, PMC, etc.
                    if (playerSide == 0x4)
                    {
                        var regDate = Memory.ReadInt(_playerInfo + Offsets.PlayerInfo_RegDate); // Bots wont have 'reg date'
                        if (regDate == 0)
                        {
                            if (Misc.Bosses.ContainsKey(Name))
                            {
                                Name = Misc.Bosses[Name];
                                Type = PlayerType.AIBoss;
                            }
                            else Type = PlayerType.AIScav;
                        }
                        else Type = PlayerType.PlayerScav;
                    }
                    else if (playerSide == 0x1 || playerSide == 0x2)
                    {
                        if (_teammates.Contains(Name)) Type = PlayerType.Teammate;
                        else Type = PlayerType.PMC;
                        var exp = Memory.ReadInt(_playerInfo + Offsets.PlayerInfo_Experience);
                        Lvl = Offsets.EXP_TABLE.Where(x => x.Key > exp).FirstOrDefault().Value - 1;
                    }
                    else Type = PlayerType.Default;
                }
                Debug.WriteLine($"Player {Name} allocated.");
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR during Player constructor for base addr 0x{playerBase.ToString("X")}", ex);
            }
        }

        /// <summary>
        ///  Update Player Information (only call from Memory Thread)
        /// </summary>
        public void Update()
        {
            if (IsAlive && IsActive) // Only update if alive/in-raid
            {
                SetPosition();
                SetDirection();
                SetHealth();
            }
        }

        /// <summary>
        /// Get current player health.
        /// </summary>
        public void SetHealth(float?[] values = null)
        {
            try
            {
                float totalHealth = 0;
                for (uint i = 0; i < BodyParts.Length; i++)
                {
                    float health;
                    if (values is null) health = Memory.ReadFloat(BodyParts[i] + Offsets.HealthEntry_Value);
                    else
                    {
                        health = (float)values[i];
                    }
                    totalHealth += health;
                    if (i == 0 || i == 1) // Head/thorax
                    {
                        if (health == 0f)
                        {
                            this.MayBeDead = true;
                            return;
                        }
                    }
                }
                this.Health = (int)Math.Round(totalHealth);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR getting Player {Name} Health: {ex}");
            }
        }

        public void SetDirection(float? deg = null)
        {
            try
            {
                if (deg is null) deg = Memory.ReadFloat(MovementContext + Offsets.MovementContext_Direction);
                if (deg < 0)
                {
                    this.Direction = 360f + (float)deg;
                    return;
                }
                this.Direction = (float)deg;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR getting Player {Name} Direction: {ex}");
            }
        }

        public void SetPitch(float? deg = null)
        {
            try
            {
                if (deg is null) deg = Memory.ReadFloat(MovementContext + Offsets.MovementContext_Direction + 0x4);
                if (deg < 0)
                {
                    this.Pitch = 360f + (float)deg;
                    return;
                }
                this.Pitch = (float)deg;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR getting Player {Name} Pitch: {ex}");
            }
        }

        /// <summary>
        /// Converts player transform to X,Y,Z coordinates (Vector3)
        /// </summary>
        public unsafe void SetPosition(object[] ptrs = null)
        {
            IntPtr pMatricesBufPtr = new(); // 0
            IntPtr pIndicesBufPtr = new(); // 0
            int index = 1; // Always seems to be 1
            try
            {
                if (ptrs is null) // Read on demand
                {
                    pMatricesBufPtr = Marshal.AllocHGlobal(sizeof(Matrix34) * index + sizeof(Matrix34));
                    Memory.ReadBuffer(PlayerTransformMatrixListBase, pMatricesBufPtr, sizeof(Matrix34) * index + sizeof(Matrix34));
                    pIndicesBufPtr = Marshal.AllocHGlobal(sizeof(int) * index + sizeof(int));
                    Memory.ReadBuffer(PlayerTransformDependencyIndexTableBase, pIndicesBufPtr, sizeof(int) * index + sizeof(int));
                }
                else // Scatter read
                {
                    if (ptrs[0] is null || ptrs[1] is null)
                        throw new NullReferenceException("Scatter Read parameter(s) are null!");
                    pMatricesBufPtr = (IntPtr)ptrs[0];
                    pIndicesBufPtr = (IntPtr)ptrs[1];
                }
                void* pMatricesBuf = pMatricesBufPtr.ToPointer();
                void* pIndicesBuf = pIndicesBufPtr.ToPointer();
                
                Vector4 result = *(Vector4*)((UInt64)pMatricesBuf + 0x30 * (UInt64)index);
                int index_relation = *(int*)((UInt64)pIndicesBuf + 0x4 * (UInt64)index);

                Vector4 xmmword_1410D1340 = new Vector4(-2.0f, 2.0f, -2.0f, 0.0f);
                Vector4 xmmword_1410D1350 = new Vector4(2.0f, -2.0f, -2.0f, 0.0f);
                Vector4 xmmword_1410D1360 = new Vector4(-2.0f, -2.0f, 2.0f, 0.0f);

                int iterations = 0;
                while (index_relation >= 0)
                {
                    if (iterations > 10) throw new Exception("Max SIMD iterations");
                    if (index_relation > 1) throw new Exception("Invalid Index Relation!");
                    Matrix34 matrix34 = *(Matrix34*)((UInt64)pMatricesBuf + 0x30 * (UInt64)index_relation);

                    Vector4 v10 = matrix34.vec2 * result;
                    Vector4 v11 = (Vector4)(Shuffle(matrix34.vec1, (ShuffleSel)(0)));
                    Vector4 v12 = (Vector4)(Shuffle(matrix34.vec1, (ShuffleSel)(85)));
                    Vector4 v13 = (Vector4)(Shuffle(matrix34.vec1, (ShuffleSel)(-114)));
                    Vector4 v14 = (Vector4)(Shuffle(matrix34.vec1, (ShuffleSel)(-37)));
                    Vector4 v15 = (Vector4)(Shuffle(matrix34.vec1, (ShuffleSel)(-86)));
                    Vector4 v16 = (Vector4)(Shuffle(matrix34.vec1, (ShuffleSel)(113)));
                    result = (((((((v11 * xmmword_1410D1350) * v13) - ((v12 * xmmword_1410D1360) * v14)) * Shuffle(v10, (ShuffleSel)(-86))) +
                        ((((v15 * xmmword_1410D1360) * v14) - ((v11 * xmmword_1410D1340) * v16)) * Shuffle(v10, (ShuffleSel)(85)))) +
                        (((((v12 * xmmword_1410D1340) * v16) - ((v15 * xmmword_1410D1350) * v13)) * Shuffle(v10, (ShuffleSel)(0))) + v10)) + matrix34.vec0);
                    index_relation = *(int*)((UInt64)pIndicesBuf + 0x4 * (UInt64)index_relation);
                    iterations++;
                }

                this.Position = new Vector3(result.X, result.Z, result.Y);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR getting Player {Name} Position: {ex}");
                try 
                { 
                    PlayerTransformInternal = Memory.ReadPtrChain(_playerBase, Offsets.Transform_TransformInternal);
                    var playersTransfPMatrix = Memory.ReadPtr(PlayerTransformInternal + Offsets.TransformInternal_TransfPMatrix);
                    PlayerTransformMatrixListBase = Memory.ReadPtr(playersTransfPMatrix + Offsets.UnityDictBase);
                    PlayerTransformDependencyIndexTableBase = Memory.ReadPtr(playersTransfPMatrix + Offsets.TransfPMatrix_TransformDependencyIndexTableBase);
                } // Update pointers
                catch { }
            }
            finally // Free mem
            {
                if (pMatricesBufPtr.ToInt64() != 0) Marshal.FreeHGlobal(pMatricesBufPtr);
                if (pIndicesBufPtr.ToInt64() != 0) Marshal.FreeHGlobal(pIndicesBufPtr);
            }
        }

        private static unsafe Vector4 Shuffle(Vector4 v1, ShuffleSel sel)
        {
            var ptr = (float*)&v1;
            var idx = (int)sel;
            return new Vector4(*(ptr + ((idx >> 0) & 0x3)), *(ptr + ((idx >> 2) & 0x3)), *(ptr + ((idx >> 4) & 0x3)),
                *(ptr + ((idx >> 6) & 0x3)));
        }
    }
}
