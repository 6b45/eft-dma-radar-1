using SkiaSharp;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using SkiaSharp.Views.Desktop;

namespace eft_dma_radar
{
    public partial class MainForm : Form
    {
        private readonly SKGLControl mapCanvas;
        private readonly SKGLControl aimView;
        private readonly Stopwatch _fpsWatch = new Stopwatch();
        private int _fps = 0;

        private readonly object _renderLock = new object();
        private readonly System.Timers.Timer _mapChangeTimer = new System.Timers.Timer(900);
        private readonly List<Map> _allMaps; // Contains all maps from \\Maps folder
        private readonly Config _config;
        private int _mapIndex = 0;
        private Map _currentMap; // Current Selected Map
        private int _mapLayerIndex = 0;
        private SKBitmap[] _loadedMap;
        private float _xScale = 0;
        private float _yScale = 0;
        private const float _fov = 30;
        private const int _maxDist = 300;
        private bool InGame
        {
            get
            {
                return Memory.InGame;
            }
        }
        private Player CurrentPlayer
        {
            get
            {
                return Memory.Players.FirstOrDefault(x => x.Value.Type is PlayerType.CurrentPlayer).Value;
            }
        }
        private ConcurrentDictionary<string, Player> AllPlayers
        {
            get
            {
                return Memory.Players;
            }
        }
        private List<LootItem> Loot
        {
            get
            {
                return Memory.Loot;
            }
        }

        private readonly SKPaint _mapPaint = new SKPaint()
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.Medium
        };
        private readonly SKPaint _paintGreen = new SKPaint()
        {
            Color = SKColors.Green,
            StrokeWidth = 3,
            Style = SKPaintStyle.Stroke,
        };
        private readonly SKPaint _paintLtGreen = new SKPaint()
        {
            Color = SKColors.LimeGreen,
            StrokeWidth = 3,
            Style = SKPaintStyle.Stroke,
        };
        private readonly SKPaint _textLtGreen = new SKPaint()
        {
            Color = SKColors.LimeGreen,
            IsStroke = false,
            TextSize = 12,
            TextEncoding = SKTextEncoding.Utf8,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };
        private readonly SKPaint _paintRed = new SKPaint()
        {
            Color = SKColors.Red,
            StrokeWidth = 3,
            Style = SKPaintStyle.Stroke,
        };
        private readonly SKPaint _textRed = new SKPaint()
        {
            Color = SKColors.Red,
            IsStroke = false,
            TextSize = 12,
            TextEncoding = SKTextEncoding.Utf8,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };
        private readonly SKPaint _paintYellow = new SKPaint()
        {
            Color = SKColors.Yellow,
            StrokeWidth = 3,
            Style = SKPaintStyle.Stroke,
        };
        private readonly SKPaint _textYellow = new SKPaint()
        {
            Color = SKColors.Yellow,
            IsStroke = false,
            TextSize = 12,
            TextEncoding = SKTextEncoding.Utf8,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };
        private readonly SKPaint _paintViolet = new SKPaint()
        {
            Color = SKColors.Fuchsia,
            StrokeWidth = 3,
            Style = SKPaintStyle.Stroke
        };
        private readonly SKPaint _textViolet = new SKPaint()
        {
            Color = SKColors.Fuchsia,
            IsStroke = false,
            TextSize = 12,
            TextEncoding = SKTextEncoding.Utf8,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };
        private readonly SKPaint _paintWhite = new SKPaint()
        {
            Color = SKColors.White,
            StrokeWidth = 3,
            Style = SKPaintStyle.Stroke,
        };
        private readonly SKPaint _textWhite = new SKPaint()
        {
            Color = SKColors.White,
            IsStroke = false,
            TextSize = 12,
            TextEncoding = SKTextEncoding.Utf8,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };
        private readonly SKPaint _paintBlack = new SKPaint()
        {
            Color = SKColors.Black,
            StrokeWidth = 3,
            Style = SKPaintStyle.Stroke,
        };
        private readonly SKPaint _paintLoot = new SKPaint()
        {
            Color = SKColors.WhiteSmoke,
            StrokeWidth = 3,
            Style = SKPaintStyle.Fill
        };
        private readonly SKPaint _paintImportantLoot = new SKPaint()
        {
            Color = SKColors.Turquoise,
            StrokeWidth = 3,
            Style = SKPaintStyle.Fill
        };
        private readonly SKPaint _lootText = new SKPaint()
        {
            Color = SKColors.WhiteSmoke,
            IsStroke = false,
            TextSize = 13,
            TextEncoding = SKTextEncoding.Utf8,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial")
        };
        private readonly SKPaint _importantLootText = new SKPaint()
        {
            Color = SKColors.Turquoise,
            IsStroke = false,
            TextSize = 13,
            TextEncoding = SKTextEncoding.Utf8,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial")
        };
        private readonly SKPaint _paintWhiteAimView = new SKPaint()
        {
            Color = SKColors.White,
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke,
        };
        private readonly SKPaint _paintRedAimView = new SKPaint()
        {
            Color = SKColors.Red,
            StrokeWidth = 1,
            Style = SKPaintStyle.Fill,
        };


        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            // init skia
            mapCanvas = new SKGLControl();
            mapCanvas.Size = new System.Drawing.Size(50, 50);
            mapCanvas.Dock = DockStyle.Fill;
            tabPage1.Controls.Add(mapCanvas);

            aimView = new SKGLControl();
            aimView.Size = new System.Drawing.Size(200, 200);
            aimView.Location = new System.Drawing.Point(0, tabPage1.Height - 200);
            mapCanvas.Controls.Add(aimView);
            aimView.Visible = false;

            if (Config.TryLoadConfig(out _config) is not true) _config = new Config();
            LoadConfig();
            _allMaps = new List<Map>();
            LoadMaps();
            _mapChangeTimer.AutoReset = false;
            _mapChangeTimer.Elapsed += _mapChangeTimer_Elapsed;

            this.DoubleBuffered = true; // Prevent flickering
            this.mapCanvas.PaintSurface += mapCanvas_OnPaint;
            this.aimView.PaintSurface += AimView_PaintSurface;
            this.Shown += MainForm_Shown;
            this.Resize += MainForm_Resize;
            _fpsWatch.Start(); // fps counter
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            aimView.Size = new System.Drawing.Size(200, 200);
            aimView.Location = new System.Drawing.Point(0, tabPage1.Height - 200);
        }

        private async void MainForm_Shown(object sender, EventArgs e)
        {
            //while (mapCanvas.GRContext is null) await Task.Delay(1);
            // Gives a big FPS boost but appears to leak mem, see https://github.com/mono/SkiaSharp/issues/1947
            //mapCanvas.GRContext.SetResourceCacheLimit(2000 * 1000 * 1000); // Fixes low FPS on big maps
            while (true)
            {
                await Task.Delay(1);
                mapCanvas.Invalidate();
                if (aimView.Visible) aimView.Refresh();
            }
        }

        /// <summary>
        /// Load previously set GUI Configuraiton values.
        /// </summary>
        private void LoadConfig()
        {
            trackBar_AimLength.Value = _config.PlayerAimLineLength;
            checkBox_Loot.Checked = _config.LootEnabled;
            trackBar_Zoom.Value = _config.DefaultZoom;
        }

        /// <summary>
        /// Load map files (.PNG) and Configs (.JSON) from \\Maps folder.
        /// </summary>
        private void LoadMaps()
        {
            var dir = new DirectoryInfo($"{Environment.CurrentDirectory}\\Maps");
            if (!dir.Exists) dir.Create();
            var configs = dir.GetFiles("*.json"); // Get all PNG Files
            if (configs.Length == 0) throw new IOException("Maps folder is empty!");
            foreach (var config in configs)
            {
                var name = Path.GetFileNameWithoutExtension(config.Name); // map name ex. 'CUSTOMS' w/o extension
                _allMaps.Add(new Map
                (
                    name.ToUpper(),
                    MapConfig.LoadFromFile(config.FullName),
                    config.FullName)
                );
            }
            try
            {
                _currentMap = _allMaps[0];
                _loadedMap = new SKBitmap[_currentMap.ConfigFile.Maps.Count];
                for (int i = 0; i < _loadedMap.Length; i++)
                {
                    using (var stream = File.Open(_currentMap.ConfigFile.Maps[i].Item2, FileMode.Open, FileAccess.Read))
                    {
                        _loadedMap[i] = SKBitmap.Decode(stream);
                    }
                }
                label_Map.Text = _currentMap.Name;
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR loading {_currentMap.ConfigFile.Maps[0].Item2}: {ex}");
            }
        }

        /// <summary>
        /// Draw/Render on Map Canvas
        /// </summary>
        private void mapCanvas_OnPaint(object sender, SKPaintGLSurfaceEventArgs e)
        {
            lock (_renderLock)
            {
                if (_fpsWatch.ElapsedMilliseconds >= 1000)
                {
                    this.Text = $"EFT Radar ({_fps} fps) ({Memory.Ticks} mem/s)";
                    _fpsWatch.Restart();
                    _fps = 0;
                }
                else _fps++;
                SKSurface surface = e.Surface;
                SKCanvas canvas = surface.Canvas;
                canvas.Clear();
                Player currentPlayer;
                if (this.InGame && (currentPlayer = this.CurrentPlayer) is not null)
                {
                    try
                    {
                        // Get main player location
                        var currentPlayerRawPos = currentPlayer.Position;
                        var currentPlayerDirection = Deg2Rad(currentPlayer.Direction);
                        var currentPlayerPos = VectorToMapPos(currentPlayerRawPos);
                        if (groupBox_MapSetup.Visible) // Print coordinates (to make it easy to setup JSON configs)
                        {
                            label_Pos.Text = $"Unity X,Y,Z: {currentPlayer.Position.X},{currentPlayer.Position.Y},{currentPlayer.Position.Z}\n" +
                                $"Bitmap X,Y: {currentPlayerPos.X},{currentPlayerPos.Y}";
                        }
                        for (int i = _loadedMap.Length; i > 0; i--)
                        {
                            if (currentPlayerPos.Height > _currentMap.ConfigFile.Maps[i-1].Item1)
                            {
                                _mapLayerIndex = i - 1;
                                break;
                            }
                        }

                        // Prepare to draw bitmap
                        var bounds = GetMapBounds(currentPlayerPos);
                        var dest = new SKRect()
                        {
                            Left = mapCanvas.Left,
                            Right = mapCanvas.Right,
                            Top = mapCanvas.Top,
                            Bottom = mapCanvas.Bottom
                        };
                        canvas.DrawBitmap(_loadedMap[_mapLayerIndex], bounds, dest, _mapPaint);

                        // Draw Main Player  
                        {
                            var zoomedCurrentPlayerPos = GetZoomedPosOffset(currentPlayerPos, bounds); // always true
                            canvas.DrawCircle(zoomedCurrentPlayerPos.GetPoint(), 6, _paintGreen);
                            var point1 = new SKPoint(zoomedCurrentPlayerPos.X, zoomedCurrentPlayerPos.Y);
                            var point2 = new SKPoint((float)(zoomedCurrentPlayerPos.X + Math.Cos(currentPlayerDirection) * trackBar_AimLength.Value), (float)(zoomedCurrentPlayerPos.Y + Math.Sin(currentPlayerDirection) * trackBar_AimLength.Value));
                            canvas.DrawLine(point1, point2, _paintGreen);
                        }

                        // Draw other players
                        var allPlayers = this.AllPlayers;
                        if (allPlayers is not null)
                        {
                            var friendlies = allPlayers.Where(x => x.Value.Type is PlayerType.CurrentPlayer
                            || x.Value.Type is PlayerType.Teammate);
                            foreach (KeyValuePair<string, Player> player in allPlayers) // Draw PMCs
                            {
                                if (player.Value.Type is PlayerType.CurrentPlayer) continue; // Already drawn current player, move on
                                if (!player.Value.IsActive && player.Value.IsAlive) continue; // Skip exfil'd players
                                var playerPos = VectorToMapPos(player.Value.Position);
                                var zoomedPlayerPos =  GetZoomedPosOffset(playerPos, bounds);
                                var playerDirection = Deg2Rad(player.Value.Direction);
                                var aimLength = 15;
                                if (player.Value.IsAlive is false)
                                { // Draw 'X' death marker
                                    canvas.DrawLine(new SKPoint(zoomedPlayerPos.X - 6, zoomedPlayerPos.Y + 6), new SKPoint(zoomedPlayerPos.X + 6, zoomedPlayerPos.Y - 6), _paintBlack);
                                    canvas.DrawLine(new SKPoint(zoomedPlayerPos.X - 6, zoomedPlayerPos.Y - 6), new SKPoint(zoomedPlayerPos.X + 6, zoomedPlayerPos.Y + 6), _paintBlack);
                                    continue;
                                }
                                else if (player.Value.Type is not PlayerType.Teammate)
                                {
                                    foreach (var friendly in friendlies)
                                    {
                                        var friendlyDist = Vector3.Distance(player.Value.Position, friendly.Value.Position);
                                        if (friendlyDist > 300) continue; // max range, no lines across entire map
                                        var friendlyPos = VectorToMapPos(friendly.Value.Position);
                                        if (IsFacingPlayer(playerPos.GetPoint(),
                                            player.Value.Direction - 90, // remove deg offset
                                            friendlyPos.GetPoint()))
                                        {
                                            aimLength = 1000; // Lengthen aimline
                                            break;
                                        }
                                    }
                                }
                                else if (player.Value.Type is PlayerType.Teammate)
                                    aimLength = trackBar_AimLength.Value; // Allies use player's aim length
                                { // Draw
                                    var plyrHeight = playerPos.Height - currentPlayerPos.Height;
                                    var plyrDist = Math.Sqrt((Math.Pow(currentPlayerRawPos.X - player.Value.Position.X, 2) + Math.Pow(currentPlayerRawPos.Y - player.Value.Position.Y, 2)));
                                    canvas.DrawText($"L{player.Value.Lvl}:{player.Value.Name} ({player.Value.Health})", zoomedPlayerPos.GetNamePoint(9, 3), GetText(player.Value.Type));
                                    canvas.DrawText($"H: {(int)Math.Round(plyrHeight)} D: {(int)Math.Round(plyrDist)}", zoomedPlayerPos.GetNamePoint(9, 15), GetText(player.Value.Type));
                                    canvas.DrawCircle(zoomedPlayerPos.GetPoint(), 6, GetPaint(player.Value.Type)); // smaller circle
                                    var point1 = new SKPoint(zoomedPlayerPos.X, zoomedPlayerPos.Y);
                                    var point2 = new SKPoint((float)(zoomedPlayerPos.X + Math.Cos(playerDirection) * aimLength), (float)(zoomedPlayerPos.Y + Math.Sin(playerDirection) * aimLength));
                                    canvas.DrawLine(point1, point2, GetPaint(player.Value.Type));
                                }
                            }
                            // Draw loot (if enabled)
                            if (checkBox_Loot.Checked)
                            {
                                var loot = this.Loot;
                                if (loot is not null) foreach (var item in loot)
                                    {
                                        SKPaint paint;
                                        SKPaint text;
                                        if (item.Value >= 300000)
                                        {
                                            paint = _paintImportantLoot;
                                            text = _importantLootText;
                                        }
                                        else
                                        {
                                            paint = _paintLoot;
                                            text = _lootText;
                                        }
                                        var mapPos = VectorToMapPos(item.Position);
                                        var mapPosZoom = GetZoomedPosOffset(mapPos, bounds);
                                        var heightDiff = item.Position.Z - currentPlayerPos.Height;
                                        if (heightDiff > 2)
                                        {
                                            using var path = mapPosZoom.GetUpArrow();
                                            canvas.DrawPath(path, paint);
                                        }
                                        else if (heightDiff < -2)
                                        {
                                            using var path = mapPosZoom.GetDownArrow();
                                            canvas.DrawPath(path, paint);
                                        }
                                        else
                                        {
                                            canvas.DrawCircle(mapPosZoom.GetPoint(), 5, paint);
                                        }
                                        canvas.DrawText($"{item.Name} ({item.Value / 1000}K)", mapPosZoom.GetNamePoint(7, 3), text);
                                    }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"ERROR rendering next frame: {ex}");
                    }
                }
                canvas.Flush(); // commit to GPU
            }
        }

        /// <summary>
        /// Provides zoomed map bounds (centers on player).
        /// </summary>
        private SKRect GetMapBounds(MapPosition pos)
        {
            var zoom = new ZoomLevel()
            {
                Width = _loadedMap[_mapLayerIndex].Width * (.01f * trackBar_Zoom.Value),
                Height = _loadedMap[_mapLayerIndex].Height * (.01f * trackBar_Zoom.Value)
            };

            var bounds = new SKRect(pos.X - zoom.Width / 2,
                pos.Y - zoom.Height / 2,
                pos.X + zoom.Width / 2,
                pos.Y + zoom.Height / 2)
                .AspectFill(new SKSize(mapCanvas.Width, mapCanvas.Height));

            _xScale = (float)mapCanvas.Width / (float)bounds.Width; // Set scale for this frame
            _yScale = (float)mapCanvas.Height / (float)bounds.Height; // Set scale for this frame
            return bounds;
        }

        /// <summary>
        /// Checks if provided location is within current zoomed map bounds, and provides offset coordinates.
        /// </summary>
        private MapPosition GetZoomedPosOffset(MapPosition location, SKRect bounds)
        {
            return new MapPosition()
            {
                X = (location.X - bounds.Left) * _xScale,
                Y = (location.Y - bounds.Top) * _yScale,
                Height = location.Height
            };
        }

        /// <summary>
        /// Determines if an aggressor player is facing a friendly player.
        /// </summary>
        private static bool IsFacingPlayer(SKPoint aggressor, double aggressorAngle, SKPoint target)
        {
            var radian = Math.Atan2((target.Y - aggressor.Y), (target.X - aggressor.X));
            var angle = Math.Abs((radian * (180 / Math.PI) + 360) % 360);
            var diff = Math.Max(angle, aggressorAngle) - Math.Min(angle, aggressorAngle);
            if (diff < 5) return true; // Modify constant for degrees variance (aiming near)
            else return false;
        }

        /// <summary>
        /// Gets drawing paintbrush based on PlayerType.
        /// </summary>
        private SKPaint GetPaint(PlayerType playerType)
        {
            if (playerType is PlayerType.Teammate) return _paintLtGreen;
            else if (playerType is PlayerType.PMC) return _paintRed;
            else if (playerType is PlayerType.PlayerScav) return _paintWhite;
            else if (playerType is PlayerType.AIBoss) return _paintViolet;
            else if (playerType is PlayerType.AIScav) return _paintYellow;
            else return _paintRed; // Default
        }
        /// <summary>
        /// Gets typing paintbrush based on PlayerType.
        /// </summary>
        private SKPaint GetText(PlayerType playerType)
        {
            if (playerType is PlayerType.Teammate) return _textLtGreen;
            else if (playerType is PlayerType.PMC) return _textRed;
            else if (playerType is PlayerType.PlayerScav) return _textWhite;
            else if (playerType is PlayerType.AIBoss) return _textViolet;
            else if (playerType is PlayerType.AIScav) return _textYellow;
            else return _textRed; // Default
        }

        /// <summary>
        /// Convert degrees to radians in order to calculate drawing angles.
        /// </summary>
        private static double Deg2Rad(float deg)
        {
            deg = deg - 90; // Degrees offset needed for game
            return (Math.PI / 180) * deg;
        }

        /// <summary>
        /// Convert game positional values to UI Map Coordinates.
        /// </summary>
        private MapPosition VectorToMapPos(Vector3 vector)
        {
            var zeroX = _currentMap.ConfigFile.X;
            var zeroY = _currentMap.ConfigFile.Y;
            var scale = _currentMap.ConfigFile.Scale;

            var x = zeroX + (vector.X * scale);
            var y = zeroY - (vector.Y * scale); // Invert 'Y' unity 0,0 bottom left, C# top left
            return new MapPosition()
            {
                X = x,
                Y = y,
                Height = vector.Z // Keep as float, calculation done later
            };
        }

        /// <summary>
        /// Toggles currently selected map.
        /// </summary>
        private void ToggleMap()
        {
            if (!button_Map.Enabled) return;
            if (_mapIndex == _allMaps.Count - 1) _mapIndex = 0; // Start over when end of maps reached
            else _mapIndex++; // Move onto next map
            label_Map.Text = _allMaps[_mapIndex].Name;
            _mapChangeTimer.Reset(); // Start delay
        }

        /// <summary>
        /// Executes map change after a short delay, in case switching through maps quickly to reduce UI lag.
        /// </summary>
        private void _mapChangeTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.BeginInvoke(new MethodInvoker(delegate
            {
                button_Map.Enabled = false;
                button_Map.Text = "Loading...";
            }));
            lock (_renderLock)
            {
                try
                {
                    _currentMap = _allMaps[_mapIndex]; // Swap map
                    if (_loadedMap is not null)
                    {
                        foreach (var map in _loadedMap) map?.Dispose();
                    }
                    _loadedMap = new SKBitmap[_currentMap.ConfigFile.Maps.Count];
                    for (int i = 0; i < _loadedMap.Length; i++)
                    {
                        using (var stream = File.Open(_currentMap.ConfigFile.Maps[i].Item2, FileMode.Open, FileAccess.Read))
                        {
                            _loadedMap[i] = SKBitmap.Decode(stream);
                        }
                    }
                    _mapLayerIndex = 0;
                }
                catch (Exception ex)
                {
                    throw new Exception($"ERROR loading {_currentMap.ConfigFile.Maps[0].Item2}: {ex}");
                }
                finally
                {
                    this.BeginInvoke(new MethodInvoker(delegate
                    {
                        button_Map.Enabled = true;
                        button_Map.Text = "Toggle Map (F5)";
                    }));
                }
            }
        }

        private void button_Map_Click(object sender, EventArgs e)
        {
            ToggleMap();
        }

        protected override void OnFormClosing(FormClosingEventArgs e) // Raised on Close()
        {
            try
            {
                _config.PlayerAimLineLength = trackBar_AimLength.Value;
                _config.LootEnabled = checkBox_Loot.Checked;
                _config.DefaultZoom = trackBar_Zoom.Value;
                Config.SaveConfig(_config);
                Memory.Shutdown();
            }
            finally { base.OnFormClosing(e); }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.F1))
            {
                if (trackBar_Zoom.Value - 5 >= 1) trackBar_Zoom.Value -= 5;
                else trackBar_Zoom.Value = 1;
                return true;
            }
            else if (keyData == (Keys.F2))
            {
                if (trackBar_Zoom.Value + 5 <= 100) trackBar_Zoom.Value += 5;
                else trackBar_Zoom.Value = 100;
                return true;
            }
            else if (keyData == (Keys.F3))
            {
                this.checkBox_Loot.Checked = !this.checkBox_Loot.Checked; // Toggle loot
                return true;
            }
            else if (keyData == (Keys.F4))
            {
                this.checkBox_Aimview.Checked = !this.checkBox_Aimview.Checked; // Toggle aimview
                return true;
            }
            else if (keyData == (Keys.F5))
            {
                ToggleMap();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void checkBox_Aimview_CheckedChanged(object sender, EventArgs e)
        {
            aimView.Visible = checkBox_Aimview.Checked;
        }

        private void checkBox_MapSetup_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_MapSetup.Checked)
            {
                groupBox_MapSetup.Visible = true;
                textBox_mapX.Text = _currentMap.ConfigFile.X.ToString();
                textBox_mapY.Text = _currentMap.ConfigFile.Y.ToString();
                textBox_mapScale.Text = _currentMap.ConfigFile.Scale.ToString();
            }
            else groupBox_MapSetup.Visible = false;
        }

        private void button_Restart_Click(object sender, EventArgs e)
        {
            Memory.Restart();
        }

        private void button_MapSetupApply_Click(object sender, EventArgs e)
        {
            if (float.TryParse(textBox_mapX.Text, out float x) &&
                float.TryParse(textBox_mapY.Text, out float y) &&
                float.TryParse(textBox_mapScale.Text, out float scale))
            {
                lock (_renderLock)
                {
                    _currentMap.ConfigFile.X = x;
                    _currentMap.ConfigFile.Y = y;
                    _currentMap.ConfigFile.Scale = scale;
                    _currentMap.ConfigFile.Save(_currentMap);
                }
            }
            else
            {
                throw new Exception("INVALID float values in Map Setup.");
            }
        }


        /// 3d Aimview stuff
        /// 
        private void AimView_PaintSurface(object sender, SKPaintGLSurfaceEventArgs e)
        {
            SKSurface surface = e.Surface;
            SKCanvas canvas = surface.Canvas;
            canvas.Clear(); // clear last frame
            try
            {
                Player me;
                ConcurrentDictionary<string, Player> players;
                if (this.InGame 
                    && (me = this.CurrentPlayer) is not null 
                    && (players = this.AllPlayers) is not null)
                {
                    var normalizedDirection = -me.Direction + 90;
                    if (normalizedDirection < 0) normalizedDirection = normalizedDirection + 360;
                    var playersInFOV = players.Where(x => x.Value.IsAlive && x.Value.IsAlive && x.Value.Type != PlayerType.CurrentPlayer); //GetPlayersInFOV(30, me, normalizedDirection);

                    var pitch = me.Pitch;
                    if (pitch >= 270)
                    {
                        pitch = 360 - pitch;
                    }
                    else
                    {
                        pitch = -pitch;
                    }

                    canvas.DrawLine(0, aimView.Height / 2, aimView.Width, aimView.Height / 2, _paintWhiteAimView);
                    canvas.DrawLine(aimView.Width / 2, 0, aimView.Width / 2, aimView.Height, _paintWhiteAimView);

                    foreach (var kvp in playersInFOV)
                    {
                        var player = kvp.Value;
                        float heighDiff = player.Position.Z - me.Position.Z;
                        float dist = (float)Math.Sqrt((Math.Pow(me.Position.X - player.Position.X, 2) + Math.Pow(me.Position.Y - player.Position.Y, 2)));
                        float angleY = (float)(180 / Math.PI * Math.Atan(heighDiff / dist)) - pitch;
                        float y = angleY / _fov * aimView.Height + aimView.Height / 2;

                        float opposite = (player.Position.Y - me.Position.Y);
                        float adjacent = player.Position.X - me.Position.X;
                        float angleX = (float)(180 / Math.PI * Math.Atan(opposite / adjacent));

                        if (adjacent < 0 && opposite > 0)
                        {
                            angleX = 180 + angleX;
                        }
                        else if (adjacent < 0 && opposite < 0)
                        {
                            angleX = 180 + angleX;
                        }
                        else if (adjacent > 0 && opposite < 0)
                        {
                            angleX = 360 + angleX;
                        }
                        angleX = angleX - normalizedDirection;
                        float x = angleX / _fov * aimView.Width + aimView.Width / 2;

                        canvas.DrawCircle(aimView.Width - x, aimView.Height - y, 10 * (1 - dist / _maxDist), _paintRedAimView);
                    }
                }
            } catch { }
            canvas.Flush(); // commit current frame to gpu
        }
    }
}
