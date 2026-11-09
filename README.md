# Crosshair Overlay

A tiny Windows app that draws a custom crosshair in the exact centre of your screen, on top of every other window. Made for Valorant, works with any game that runs in windowed or borderless mode.

![Settings window](screenshots/settings.png)

![All built-in crosshairs](screenshots/crosshairs.png)

- 26 built-in crosshairs: crosses, dots, circles, T and X shapes, sniper scope and more
- Change size, color, opacity, outline, line length, thickness, gap, rotation and position
- Inner lines, outer lines, centre dot and circle can be mixed freely
- Click-through: the mouse never touches the overlay
- Hotkey (F8 by default) to show or hide it, also while in game
- Can show the crosshair only while your game is running, or only while it is the window in front
- Can start with Windows, hidden in the tray
- Every change is saved instantly and restored the next time you start it
- One small exe, nothing to install

## Download

**[Download CrosshairOverlay.exe](https://github.com/einars-a/crosshair-overlay/raw/main/CrosshairOverlay.exe)**

Windows 10 or 11. Just run it. Windows SmartScreen may warn about an unknown publisher the first time: choose *More info* and then *Run anyway*.

## How to use

1. Start `CrosshairOverlay.exe`. The crosshair appears in the middle of the screen and the settings window opens.
2. Pick a crosshair from the list on the left and adjust it with the sliders on the right.
3. Close the settings window. The app keeps running in the system tray (bottom right).
   Double-click the tray icon to open settings again, right-click it to exit.
4. Press **F8** to show or hide the crosshair at any time. The key can be changed in settings.

### Show it only in the game

At the bottom of the settings, under **When to show**, pick one of:

- **Always**: the crosshair is always on screen.
- **While the game is running**: it appears when the game starts and disappears when the game closes.
- **While the game is in focus**: it is only shown while the game is the window in front, so it hides when you alt-tab.

The game is recognised by its process name. Valorant (`VALORANT-Win64-Shipping`) is filled in by default. For other games use **Pick running app** while the game is open, or type several names separated by commas.

Tick **Start with Windows** to launch the app hidden in the tray at sign-in. Together with *While the game is running* the crosshair then shows up by itself whenever you start the game.

### Game settings

The game must run in **Windowed Fullscreen** (borderless) display mode. In exclusive fullscreen Windows cannot draw anything over the game, so the crosshair will not be visible.

In Valorant: *Settings > Video > General > Display Mode > Windowed Fullscreen*.

## Is it allowed?

The app only draws a small picture on your screen. It does not read or change game memory, inject anything into the game or automate any input. Still, Riot does not officially approve third-party crosshair overlays, so use it at your own risk.

## Build it yourself

No tools needed, the C# compiler that ships with Windows is enough. Run `build.bat`, or:

```bat
%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /optimize+ /out:CrosshairOverlay.exe /win32icon:app.ico /r:System.Windows.Forms.dll /r:System.Drawing.dll CrosshairOverlay.cs
```

Settings are stored in `%APPDATA%\CrosshairOverlay\settings.ini`. Delete that file to reset everything.

## License

MIT, see [LICENSE](LICENSE).
