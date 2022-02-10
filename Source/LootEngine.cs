using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace eft_dma_radar
{
    public class LootEngine
    {
        private List<LootItem> _loot = new();
        private readonly ulong _localGameWorld;

        public List<LootItem> Loot
        {
            get { return _loot; }
        }

        public LootEngine(ulong localGameWorld)
        {
            _localGameWorld = localGameWorld;
            UpdateLoot();
        }

        private void UpdateLoot()
        {
            var lootlistPtr = Memory.ReadPtr(_localGameWorld + Offsets.LOOT_LIST);
            var lootListEntity = Memory.ReadPtr(lootlistPtr + Offsets.LOOT_LIST_ENTITY);
            var countLootListObjects = Memory.ReadInt(lootListEntity + 0x18);

            Debug.WriteLine("Parsing loot...");
            for (int i = 0; i < countLootListObjects; i++)
            {
                try
                {
                    //Get Loot Item
                    var lootObjectsEntity = Memory.ReadPtr(lootListEntity + Offsets.LOOT_OBJECTS_ENTITY_BASE + (ulong)(0x8 * i));
                    var unkownPtr = Memory.ReadPtr(lootObjectsEntity + 0x10);
                    var interactiveClass = Memory.ReadPtr(unkownPtr + 0x28);
                    var baseObject = Memory.ReadPtr(interactiveClass + 0x10);
                    var gameObject = Memory.ReadPtr(baseObject + 0x30);
                    var pGameObjectName = Memory.ReadPtr(gameObject + 0x60);
                    var name = Memory.ReadString(pGameObjectName, 64).ToLower(); //this represents the BSG name, it's not clean text though

                    if (name.Contains("script") || name.Contains("lootcorpse_playersuperior"))
                    {
                        //skip these. These are scripts which I think are things like landmines but not sure
                    }
                    else
                    {
                        //Get Position
                        var objectClass = Memory.ReadPtr(gameObject + 0x30);
                        var pointerToTransform_1 = Memory.ReadPtr(objectClass + 0x8);
                        var pointerToTransform_2 = Memory.ReadPtr(pointerToTransform_1 + 0x28);
                        var pos = GetPosition(pointerToTransform_2);

                        //the WORST method to figure out if an item is a container...but no better solution now
                        bool container = false;
                        //try
                        //{
                        //    var xx = Memory.ReadPtr(interactiveClass + 0x50);
                        //    var yy = Memory.ReadPtr(xx + 0x40);
                        //    bool zz = Memory.ReadBool(yy + 0x94);
                        //    if (xx == 0)
                        //    {
                        //        container = true;
                        //    }
                        //}
                        //catch { container = true; }

                        if (Offsets.CONTAINERS.Any(x => name.Contains(x.ToLower())))
                        {
                            container = true;
                        }

                        //If the item is a Static Container like weapon boxes, barrels, caches, safes, airdrops etc
                        if (Offsets.CONTAINERS.Any(x => name.Contains(x)) || container)
                        {
                            //Grid Logic for static containers so that we can see what's inside
                            try
                            {
                                var itemOwner = Memory.ReadPtr(interactiveClass + 0x108);
                                var item = Memory.ReadPtr(itemOwner + 0xa0);
                                var grids = Memory.ReadPtr(item + 0x68);
                                GetItemsInGrid(grids, "ignore", pos);
                            }
                            catch
                            {
                            }
                        }
                        //If the item is NOT a Static Container
                        else
                        {
                            var item = Memory.ReadPtr(interactiveClass + 0x50); //EFT.InventoryLogic.Item
                            var itemTemplate = Memory.ReadPtr(item + 0x40); //EFT.InventoryLogic.ItemTemplate
                            bool questItem = Memory.ReadBool(itemTemplate + 0x94);

                            //If NOT a quest item. Quest items are like the quest related things you need to find like the pocket watch or Jaeger's Letter etc. We want to ignore these quest items.
                            if (!questItem)
                            {
                                var BSGIdPtr = Memory.ReadPtr(itemTemplate + 0x50);
                                var id = Memory.ReadUnityString(BSGIdPtr);

                                //If the item is a corpose
                                if (id.Equals("55d7217a4bdc2d86028b456d")) // Corpse
                                {
                                    _loot.Add(new LootItem
                                    {
                                        Position = pos,
                                        Name = "Corpse"
                                    });
                                }
                                //Finally we must have found a loose loot item, eg a keycard, backpack, gun, salewa. Anything not in a container or corpse.
                                else
                                {

                                    //Grid Logic for loose loot because some loose loot have items inside, eg a backpack or docs case. We want to check those items too. But not all loose loot have items inside, so we have a try-catch below
                                    try
                                    {
                                        var grids = Memory.ReadPtr(item + 0x68);
                                        GetItemsInGrid(grids, id, pos);
                                    }
                                    catch
                                    {
                                        //The loot item we found does not have any grids so it's basically like a keycard or a ledx etc. Therefore add it to our loot dictionary.
                                        if (TarkovMarketManager.ItemFilter.TryGetValue(id, out var filter))
                                        {
                                            int itemValue = 0;
                                            if (filter.avg24hPrice > filter.traderPrice)
                                                itemValue = filter.avg24hPrice;
                                            else
                                                itemValue = filter.traderPrice;
                                            _loot.Add(new LootItem
                                            {
                                                Name = filter.shortName,
                                                Position = pos,
                                                BSGId = id,
                                                Value = itemValue
                                            });
                                        }
                                    }

                                    //Slot Logic - some loose loot have slots, eg a DVL will have a barrel slot, a mag slot etc
                                    try
                                    {
                                        //I haven't finished coding this yet
                                        //var slots = Memory.ReadPtr(item + 0x70);
                                        //GetItemsInSlot(slots, id, pos);
                                    }
                                    catch
                                    {
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
            }
            Debug.WriteLine("Loot parsing completed");
        }

        ///This method recursively searches grids. Grids work as follows:
        ///Take a Groundcache which holds a Blackrock which holds a pistol.
        ///The Groundcache will have 1 grid array, this method searches for whats inside that grid.
        ///Then it finds a Blackrock. This method then invokes itself recursively for the Blackrock.
        ///The Blackrock has 11 grid arrays (not to be confused with slots!! - a grid array contains slots. Look at the blackrock and you'll see it has 20 slots but 11 grids).
        ///In one of those grid arrays is a pistol. This method would recursively search through each item it finds
        ///To Do: add slot logic, so we can recursively search through the pistols slots...maybe it has a high value scope or something.
        private void GetItemsInGrid(ulong gridsArrayPtr, string id, Vector3 pos)
        {
            var gridsArray = new MemArray(gridsArrayPtr);

            if (TarkovMarketManager.ItemFilter.TryGetValue(id, out var filter))
            {
                int itemValue = 0;
                if (filter.avg24hPrice > filter.traderPrice)
                    itemValue = filter.avg24hPrice;
                else
                    itemValue = filter.traderPrice;
                _loot.Add(new LootItem
                {
                    Name = filter.shortName,
                    Position = pos,
                    BSGId = id,
                    Value = itemValue
                });
            }

            // Check all sections of the container
            foreach (var grid in gridsArray.Data)
            {

                var gridEnumerableClass = Memory.ReadPtr(grid + 0x40); // -.GClass178A->gClass1797_0x40 // Offset: 0x0040 (Type: -.GClass1797)

                var itemListPtr = Memory.ReadPtr(gridEnumerableClass + 0x18); // -.GClass1797->list_0x18 // Offset: 0x0018 (Type: System.Collections.Generic.List<Item>)
                var itemList = new MemList(itemListPtr);

                foreach (var childItem in itemList.Data)
                {
                    try
                    {
                        var childItemTemplate = Memory.ReadPtr(childItem + 0x40); // EFT.InventoryLogic.Item->_template // Offset: 0x0038 (Type: EFT.InventoryLogic.ItemTemplate)
                        var childItemIdPtr = Memory.ReadPtr(childItemTemplate + 0x50);
                        var childItemIdStr = Memory.ReadUnityString(childItemIdPtr).Replace("\\0", "");

                        // Check to see if the child item has children
                        var childGridsArrayPtr = Memory.ReadPtr(childItem + 0x68);   // -.GClassXXXX->Grids // Offset: 0x0068 (Type: -.GClass1497[])
                        GetItemsInGrid(childGridsArrayPtr, childItemIdStr, pos);        // Recursively add children to the entity
                    }
                    catch (Exception ee) { }
                }

            }
        }
        private unsafe Vector3 GetPosition(ulong transform)
        {
            IntPtr pMatricesBufPtr = new(); // 0
            IntPtr pIndicesBufPtr = new(); // 0
            try
            {
                ulong transform_internal = Memory.ReadPtr(transform + 0x10);
                ulong pMatrix = Memory.ReadPtr(transform_internal + 0x38);
                ulong matrix_list_base = Memory.ReadPtr(pMatrix + 0x18);
                ulong dependency_index_table_base = Memory.ReadPtr(pMatrix + 0x20);
                int index = Memory.ReadInt(transform_internal + 0x40);

                pMatricesBufPtr = Marshal.AllocHGlobal(sizeof(Matrix34) * index + sizeof(Matrix34));
                Memory.ReadBuffer(matrix_list_base, pMatricesBufPtr, sizeof(Matrix34) * index + sizeof(Matrix34));
                pIndicesBufPtr = Marshal.AllocHGlobal(sizeof(int) * index + sizeof(int));
                Memory.ReadBuffer(dependency_index_table_base, pIndicesBufPtr, sizeof(int) * index + sizeof(int));
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
                    if (iterations > 50) throw new Exception("Max SIMD iterations");
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

                return new Vector3(result.X, result.Z, result.Y);
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

    //Helper class or struct
    public class MemArray
    {
        public ulong Address { get; }
        public int Count { get; }
        public ulong[] Data { get; }

        public MemArray(ulong address)
        {
            var type = typeof(ulong);

            Address = address;
            Count = Memory.ReadInt(address + 0x18);
            var arrayBase = address + 0x20;
            var tSize = (uint)Marshal.SizeOf(type);

            // Rudimentary sanity check
            if (Count > 4096 || Count < 0)
                Count = 0;

            var retArray = new ulong[Count];

            for (uint i = 0; i < Count; i++)
            {
                retArray[i] = Memory.ReadPtr(arrayBase + i * tSize);
            }

            Data = retArray;
        }
    }


    //Helper class or struct
    public class MemList
    {
        public ulong Address { get; }

        public int Count { get; }

        public List<ulong> Data { get; }

        public MemList(ulong address)
        {
            var type = typeof(ulong);

            Address = address;
            Count = Memory.ReadInt(address + 0x18);

            if (Count > 4096 || Count < 0)
                Count = 0;

            var arrayBase = Memory.ReadPtr(address + 0x10) + 0x20;
            var tSize = (uint)Marshal.SizeOf(type);
            var retList = new List<ulong>(Count);

            for (uint i = 0; i < Count; i++)
            {
                retList.Add(Memory.ReadPtr(arrayBase + i * tSize));
            }

            Data = retList;
        }
    }
    public class LootItem
    {
        public string Name { get; set; }
        public int Value { get; set; } = 0;
        public string BSGId { get; set; }
        public Vector3 Position { get; set; }
        public bool InContainer { get; set; }

    }
}