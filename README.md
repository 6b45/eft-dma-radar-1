# EFT DMA Radar
2D-Map DMA Radar for EFT

[UC Forum Thread](https://www.unknowncheats.me/forum/escape-from-tarkov/482418-2d-map-dma-radar-wip.html)

### Instructions
1. You need a DMA Device (Screamer, Raptor DMA,etc.) installed on your game PC with (hopefully) good/safe firmware. Don't ask me how.
2. Build/compile the app for Release x64.
3. Import any maps you would like to use in the Maps Folder, and be sure to have a .JSON config file for each set of maps.
4. Run the program on your 2nd PC (NOT GAME PC!!!) that has the DMA USB Cable plugged into. Click the Map button to cycle through maps if you need.
5. Recommend the following Game PC Settings:
   - Disable your system page file
   - Turn off automatic ram cleaner (in EFT Settings)
   - Turn on MIP Streaming (in EFT settings)

### Map JSON Info
The x,y values in the Map JSON should be the bitmap pixel coordinates for the "origin" location in game (this is where the unity coordinates are 0,0). Tweak scale as needed to ensure proper spacing.
The maps list takes a Tuple<float,string> where the float is the minimum height to display the corresponding map file (string). This allows for layered maps on like Interchange/Labs.

### Demo
![Demo](https://user-images.githubusercontent.com/42287509/153343812-2e8123d8-2c51-41e3-8db6-98d994a5772e.png)
