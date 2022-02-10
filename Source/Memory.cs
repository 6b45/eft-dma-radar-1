using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using vmmsharp;

namespace eft_dma_radar
{
    internal static class Memory
    {
        private static volatile bool _running = false;
        private static volatile bool _restart = false;
        private static readonly Thread _worker;
        private static uint _pid;
        private static ulong _unityBase;
        private static Game _game;
        private static int _ticks = 0;
        private static readonly Stopwatch _tickSw = new();
        public static int Ticks = 0;
        public static bool InGame
        {
            get
            {
                return _game?.InGame ?? false;
            }
        }
        public static ConcurrentDictionary<string, Player> Players
        {
            get
            {
                return _game?.Players;
            }
        }
        public static List<LootItem> Loot
        {
            get
            {
                return _game?.Loot;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        static Memory()
        {
            try
            {
                Debug.WriteLine("Loading memory module...");
                if (!File.Exists("mmap.txt"))
                {
                    Debug.WriteLine("No MemMap, attempting to generate...");
                    if (!vmm.Initialize("-printf", "-v", "-device", "FPGA"))
                        throw new DMAException("Unable to initialize DMA Device while attempting to generate MemMap!");
                    GetMemMap();
                    vmm.Close(); // Close back down, re-init w/ map
                }
                if (!vmm.Initialize("-printf", "-v", "-device", "FPGA", "-memmap", "mmap.txt")) // Initialize DMA device
                    throw new DMAException("ERROR initializing DMA Device! If you do not have a memory map (mmap.txt) edit the constructor in Memory.cs");
                Debug.WriteLine("Starting Memory worker thread...");
                _worker = new Thread(() => Worker()) { IsBackground = true };
                _worker.Start(); // Start new background thread to do memory operations on
                _running = true;
                Program.ShowWindow(Program.GetConsoleWindow(), 0); // Hide console if successful
                _tickSw.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "DMA Init", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(-1);
            }
        }

        /// <summary>
        /// Main worker thread to perform DMA Reads on.
        /// </summary>
        private static void Worker()
        {
            try
            {
                while (true)
                {
                    while (true) // Startup loop
                    {
                        if (GetPid()
                        && GetModuleBase()
                        )
                        {
                            Debug.WriteLine($"EFT startup successful!");
                            break;
                        }
                        else
                        {
                            Debug.WriteLine("EFT startup failed, trying again in 15 seconds...");
                            Thread.Sleep(15000);
                        }
                    }
                    while (true) // Game is running
                    {
                        _game = new Game(_unityBase);
                        try
                        {
                            _game.WaitForGame();
                            while (_game.InGame)
                            {
                                if (_tickSw.Elapsed.TotalMilliseconds > 1000)
                                {
                                    Interlocked.Exchange(ref Ticks, _ticks);
                                    _ticks = 0;
                                    _tickSw.Restart();
                                }
                                else _ticks++;
                                if (_restart)
                                {
                                    Debug.WriteLine("Restarting game... getting fresh gameworld");
                                    _restart = false;
                                    break;
                                }
                                _game.GameLoop();
                            }
                        }
                        catch (GameNotRunningException) { break; }
                        catch (DMAShutdown) { throw; }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Unhandled exception in Game Loop: {ex}");
                        }
                        finally { Thread.Sleep(100); }
                    }
                    Debug.WriteLine("Game is no longer running! Attempting to restart...");
                }
            }
            catch (DMAShutdown) { Debug.WriteLine("Memory Thread closing down due to DMA Shutdown..."); } // Shutdown Thread Gracefully
            catch (Exception ex)
            {
                MessageBox.Show($"FATAL ERROR on Memory Thread: {ex}");
                Environment.Exit(-1); // Forcefully close process, app will need to be restarted
            }
        }

        /// <summary>
        /// Sets restart flag to re-initialize the game/pointers from the bottom up.
        /// </summary>
        public static void Restart()
        {
            if (_restart is false)
            {
                _restart = true;
            }
        }

        /// <summary>
        /// Generates a Physical Memory Map (mmap.txt) to enhance performance/safety.
        /// </summary>
        private static void GetMemMap()
        {
            try
            {
                var map = vmm.Map_GetPhysMem();
                if (map.Length == 0) throw new Exception("Map_GetPhysMem() returned no entries!");
                string mapOut = "";
                for (int i = 0; i < map.Length; i++)
                {
                    mapOut += $"{i.ToString("D4")}  {map[i].pa.ToString("x")}  -  {(map[i].pa + map[i].cb - 1).ToString("x")}  ->  {map[i].pa.ToString("x")}\n";
                }
                File.WriteAllText("mmap.txt", mapOut);
            }
            catch (Exception ex)
            {
                throw new DMAException("Unable to generate MemMap!", ex);
            }
        }

        /// <summary>
        /// Gets EFT Process ID.
        /// </summary>
        private static bool GetPid()
        {
            try
            {
                ThrowIfDMAShutdown();
                vmm.PidGetFromName("EscapeFromTarkov.exe", out _pid);
                if (_pid == 0) throw new DMAException("Unable to obtain PID. Game may not be running.");
                else
                {
                    Debug.WriteLine($"EscapeFromTarkov.exe is running at PID {_pid}");
                    return true;
                }
            }
            catch (DMAShutdown) { throw; }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR getting PID: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Gets module base entry address for UnityPlayer.dll
        /// </summary>
        private static bool GetModuleBase()
        {
            try
            {
                ThrowIfDMAShutdown();
                _unityBase = vmm.ProcessGetModuleBase(_pid, "UnityPlayer.dll");
                if (_unityBase == 0) throw new DMAException("Unable to obtain Base Module Address. Game may not be running");
                else
                {
                    Debug.WriteLine($"Found UnityPlayer.dll at 0x{_unityBase.ToString("x")}");
                    return true;
                }
            }
            catch (DMAShutdown) { throw; }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR getting module base: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Performs multiple reads in one sequence, significantly faster than single reads.
        /// </summary>
        // Credit to asmfreak https://www.unknowncheats.me/forum/3345474-post27.html
        public static void ReadScatter(ScatterReadEntry[] entries, Dictionary<int, Dictionary<int, ScatterReadEntry>> results)
        {
            ThrowIfDMAShutdown();
            var toScatter = new List<ulong>();
            var toSkip = new HashSet<int>();
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].Addr is not null)
                {
                    var addrType = entries[i].Addr.GetType();
                    if (addrType == typeof(ScatterReadEntry))
                    {
                        var item = (ScatterReadEntry)entries[i].Addr;
                        if (item.Result is not null)
                        {
                            entries[i].Addr = (ulong)item.Result;
                        }
                        else entries[i].Addr = (ulong)0x0;
                    }
                }
                else entries[i].Addr = (ulong)0x0;
                if ((ulong)entries[i].Addr == 0x0)
                {
                    toSkip.Add(i);
                    entries[i].Result = null;
                    results[entries[i].Index].Add(entries[i].Id, entries[i]);
                    continue;
                }

                entries[i].Addr = (ulong)entries[i].Addr + entries[i].Offset;

                ulong dwAddress = (ulong)entries[i].Addr;

                if (entries[i].Size is not null)
                {
                    var sizeType = entries[i].Size.GetType();
                    if (sizeType == typeof(ScatterReadEntry))
                    {
                        var item = (ScatterReadEntry)entries[i].Size;
                        if (item.Result is not null)
                        {
                            entries[i].Size = (int)item.Result;
                        }
                        else entries[i].Size = (int)0;
                    }
                }
                else entries[i].Size = (int)0;
                uint size;
                if ((uint)(int)entries[i].Size > 0)
                    size = (uint)(int)entries[i].Size * (uint)entries[i].SizeMult;
                else size = (uint)Marshal.SizeOf(entries[i].Type);

                //get the number of pages
                uint dwNumPages = ADDRESS_AND_SIZE_TO_SPAN_PAGES(dwAddress, size);

                //loop all the pages we would need
                for (int p = 0; p < dwNumPages; p++)
                {
                    toScatter.Add(PAGE_ALIGN(dwAddress));
                }
            }
            var scatters = vmm.MemReadScatter(_pid, vmm.FLAG_NOCACHE, toScatter.ToArray());

            int index = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                if (toSkip.Contains(i))
                {
                    continue;
                }
                bool isFailed = false;
                ulong dwAdd = (ulong)entries[i].Addr;

                uint dwPageOffset = BYTE_OFFSET(dwAdd);

                uint size;
                if ((uint)(int)entries[i].Size > 0)
                    size = (uint)(int)entries[i].Size * (uint)entries[i].SizeMult;
                else size = (uint)Marshal.SizeOf(entries[i].Type);
                byte[] buffer = new byte[size];
                int bytesRead = 0;
                uint cb = Math.Min(size, (uint)PAGE_SIZE - dwPageOffset);

                uint dwNumPages = ADDRESS_AND_SIZE_TO_SPAN_PAGES(dwAdd, size);

                for (int p = 0; p < dwNumPages; p++)
                {
                    if (scatters[index].f)
                    {
                        Buffer.BlockCopy(scatters[index].pb, (int)dwPageOffset, buffer, bytesRead, (int)cb);
                        bytesRead += (int)cb;
                    }
                    else
                        isFailed = true;

                    cb = (uint)PAGE_SIZE;
                    if (((dwPageOffset + size) & 0xfff) != 0)
                        cb = ((dwPageOffset + size) & 0xfff);

                    dwPageOffset = 0;
                    index++;
                }
                try
                {
                    if (isFailed) throw new DMAException("Scatter read failed!");
                    else if (bytesRead != size) throw new DMAException("Incomplete buffer read!");
                    else if (entries[i].Type == typeof(ulong))
                    {
                        entries[i].Result = BitConverter.ToUInt64(buffer);
                    }
                    else if (entries[i].Type == typeof(float))
                    {
                        entries[i].Result = BitConverter.ToSingle(buffer);
                    }
                    else if (entries[i].Type == typeof(int))
                    {
                        entries[i].Result = BitConverter.ToInt32(buffer);
                    }
                    else if (entries[i].Type == typeof(bool))
                    {
                        entries[i].Result = BitConverter.ToBoolean(buffer);
                    }
                    else if (entries[i].Type == typeof(IntPtr))
                    {
                        var memBuf = Marshal.AllocHGlobal((int)size); // alloc memory (must free later)
                        Marshal.Copy(buffer, 0, memBuf, (int)size); // Copy to mem buffer
                        entries[i].Result = memBuf; // Store ref to mem buffer
                    }
                    else if (entries[i].Type == typeof(string)) // Default String
                    {
                        entries[i].Result = Encoding.Default.GetString(buffer);
                    }
                    else if (entries[i].Type == typeof(UnityString)) // Unity String
                    {
                        entries[i].Result = Encoding.Unicode.GetString(buffer);
                    }
                }
                catch (Exception ex)
                {
                    entries[i].Result = null;
                    Debug.WriteLine($"Error parsing result from Scatter Read at index {i}: {ex}");
                }
                finally
                {
                    results[entries[i].Index].Add(entries[i].Id, entries[i]);
                }
            }
        }

        /// <summary>
        /// Copy 'n' bytes to unmanaged memory. Caller is responsible for freeing memory.
        /// </summary>
        public static void ReadBuffer(ulong addr, IntPtr bufPtr, int size)
        {
            ThrowIfDMAShutdown();
            var readBuf = vmm.MemRead(_pid, addr, (uint)size, vmm.FLAG_NOCACHE);
            if (readBuf.Length != size) throw new DMAException("Incomplete buffer read!");
            Marshal.Copy(readBuf
                , 0, bufPtr, size);
        }

        /// <summary>
        /// Read a chain of pointers.
        /// </summary>
        public static ulong ReadPtrChain(ulong ptr, uint[] offsets)
        {
            ulong addr = 0;
            try { addr = ReadPtr(ptr + offsets[0]); }
            catch (Exception ex) { throw new DMAException($"ERROR reading pointer chain at index 0, addr 0x{ptr.ToString("X")} + 0x{offsets[0].ToString("X")}", ex); }
            for (int i = 1; i < offsets.Length; i++)
            {
                try { addr = ReadPtr(addr + offsets[i]); }
                catch (Exception ex) { throw new DMAException($"ERROR reading pointer chain at index {i}, addr 0x{addr.ToString("X")} + 0x{offsets[i].ToString("X")}", ex); }
            }
            return addr;
        }
        /// <summary>
        /// Resolves a pointer and returns the memory address it points to.
        /// </summary>
        public static ulong ReadPtr(ulong ptr)
        {
            var addr = ReadUlong(ptr);
            if (addr == 0) throw new DMAException("NULL pointer returned!");
            else return addr;
        }


        public static ulong ReadUlong(ulong addr)
        {
            try
            {
                ThrowIfDMAShutdown();
                return BitConverter.ToUInt64(vmm.MemRead(_pid, addr, 8, vmm.FLAG_NOCACHE), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading UInt64 at 0x{addr.ToString("X")}", ex);
            }
        }

        public static long ReadLong(ulong addr) // read 8 bytes (int64)
        {
            try
            {
                ThrowIfDMAShutdown();
                return BitConverter.ToInt64(vmm.MemRead(_pid, addr, 8, vmm.FLAG_NOCACHE), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading Int64 at 0x{addr.ToString("X")}", ex);
            }
        }
        public static int ReadInt(ulong addr) // read 4 bytes (int32)
        {
            try
            {
                ThrowIfDMAShutdown();
                return BitConverter.ToInt32(vmm.MemRead(_pid, addr, 4, vmm.FLAG_NOCACHE), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading Int32 at 0x{addr.ToString("X")}", ex);
            }
        }
        public static uint ReadUint(ulong addr) // read 4 bytes (uint32)
        {
            try
            {
                ThrowIfDMAShutdown();
                return BitConverter.ToUInt32(vmm.MemRead(_pid, addr, 4, vmm.FLAG_NOCACHE), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading Uint32 at 0x{addr.ToString("X")}", ex);
            }
        }
        public static float ReadFloat(ulong addr) // read 4 bytes (float)
        {
            try
            {
                ThrowIfDMAShutdown();
                return BitConverter.ToSingle(vmm.MemRead(_pid, addr, 4, vmm.FLAG_NOCACHE), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading float at 0x{addr.ToString("X")}", ex);
            }
        }
        public static double ReadDouble(ulong addr) // read 8 bytes (double)
        {
            try
            {
                ThrowIfDMAShutdown();
                return BitConverter.ToDouble(vmm.MemRead(_pid, addr, 8, vmm.FLAG_NOCACHE), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading double at 0x{addr.ToString("X")}", ex);
            }
        }
        public static bool ReadBool(ulong addr) // read 1 byte (bool)
        {
            try
            {
                ThrowIfDMAShutdown();
                return BitConverter.ToBoolean(vmm.MemRead(_pid, addr, 1, vmm.FLAG_NOCACHE), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading boolean at 0x{addr.ToString("X")}", ex);
            }
        }

        public static T ReadStruct<T>(ulong addr) // Read structure from memory location
        {
            int size = Marshal.SizeOf(typeof(T));
            var mem = Marshal.AllocHGlobal(size); // alloc mem
            try
            {
                ThrowIfDMAShutdown();
                var buffer = vmm.MemRead(_pid, addr, (uint)size, vmm.FLAG_NOCACHE);
                if (buffer.Length != size) throw new DMAException("Incomplete buffer read!");
                Marshal.Copy(buffer, 0, mem, size); // Read to pointer location
                return (T)Marshal.PtrToStructure(mem, typeof(T)); // Convert bytes to struct
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading struct at 0x{addr.ToString("X")}", ex);
            }
            finally
            {
                Marshal.FreeHGlobal(mem); // free mem
            }
        }
        /// <summary>
        /// Read 'n' bytes at specified address and convert directly to a string.
        /// </summary>
        public static string ReadString(ulong addr, uint size) // read n bytes (string)
        {
            try
            {
                ThrowIfDMAShutdown();
                return Encoding.Default.GetString(
                    vmm.MemRead(_pid, addr, size, vmm.FLAG_NOCACHE));
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading string at 0x{addr.ToString("X")}", ex);
            }
        }

        /// <summary>
        /// Read UnityEngineString structure
        /// </summary>
        public static string ReadUnityString(ulong addr)
        {
            try
            {
                ThrowIfDMAShutdown();
                var length = (uint)ReadInt(addr + Offsets.UnityString_Len);
                return Encoding.Unicode.GetString(
                    vmm.MemRead(_pid, addr + Offsets.UnityString_Value, length * 2, vmm.FLAG_NOCACHE));
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading UnityString at 0x{addr.ToString("X")}", ex);
            }
        }

        /// <summary>
        /// Close down DMA Device Connection.
        /// </summary>
        public static void Shutdown()
        {
            if (_running)
            {
                Debug.WriteLine("Closing down DMA Connection...");
                _running = false;
                vmm.Close();
            }
        }

        private static void ThrowIfDMAShutdown()
        {
            if (!_running) throw new DMAShutdown("DMA Device is no longer initialized!");
        }


        /// Mem Align Functions Ported from Win32 (C++)
        private const ulong PAGE_SIZE = 0x1000;
        private const int PAGE_SHIFT = 12;

        /// <summary>
        /// The PAGE_ALIGN macro takes a virtual address and returns a page-aligned
        /// virtual address for that page.
        /// </summary>
        private static ulong PAGE_ALIGN(ulong va)
        {
            return (va & ~(PAGE_SIZE - 1));
        }
        /// <summary>
        /// The ADDRESS_AND_SIZE_TO_SPAN_PAGES macro takes a virtual address and size and returns the number of pages spanned by the size.
        /// </summary>
        private static uint ADDRESS_AND_SIZE_TO_SPAN_PAGES(ulong va, uint size)
        {
            return (uint)((BYTE_OFFSET(va) + (size) + (PAGE_SIZE - 1)) >> PAGE_SHIFT);
        }

        /// <summary>
        /// The BYTE_OFFSET macro takes a virtual address and returns the byte offset
        /// of that address within the page.
        /// </summary>
        private static uint BYTE_OFFSET(ulong va)
        {
            return (uint)(va & (PAGE_SIZE - 1));
        }
    }

    public class DMAException : Exception
    {
        public DMAException()
        {
        }

        public DMAException(string message)
            : base(message)
        {
        }

        public DMAException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class DMAShutdown : Exception
    {
        public DMAShutdown()
        {
        }

        public DMAShutdown(string message)
            : base(message)
        {
        }

        public DMAShutdown(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

}
