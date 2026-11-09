// Crosshair Overlay - a click-through, always-on-top crosshair drawn at the centre of the screen.
// Build: run build.bat (uses the C# compiler that ships with Windows, C# 5 syntax only).

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CrosshairOverlay
{
    // ---------------------------------------------------------------- settings

    public class Config
    {
        // look
        public int Scale = 100;            // percent
        public int Opacity = 100;          // percent
        public int ColorArgb = unchecked((int)0xFF00FF00);
        public bool Outline = true;
        public int OutlineSize = 1;
        public int OutlineArgb = unchecked((int)0xFF000000);

        public bool Lines = true;
        public int LineLength = 6;
        public int LineThickness = 2;
        public int LineGap = 3;
        public int Rotation = 0;
        public bool TStyle = false;

        public bool Outer = false;
        public int OuterLength = 3;
        public int OuterThickness = 2;
        public int OuterGap = 12;

        public bool Dot = false;
        public int DotSize = 2;
        public bool DotRound = false;

        public bool Circle = false;
        public int CircleRadius = 10;
        public int CircleThickness = 2;

        // placement / app
        public int OffsetX = 0;
        public int OffsetY = 0;
        public int Monitor = 0;            // 0 = primary, n = Screen.AllScreens[n-1]
        public int Hotkey = 3;             // index into SettingsForm.HotkeyNames (F8)
        public bool Visible = true;
        public bool KeepColor = false;
        public string Preset = "Classic Cross";

        public static string FilePath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "CrosshairOverlay", "settings.ini");
            }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                List<string> lines = new List<string>();
                foreach (FieldInfo f in typeof(Config).GetFields(BindingFlags.Public | BindingFlags.Instance))
                    lines.Add(f.Name + "=" + Convert.ToString(f.GetValue(this), CultureInfo.InvariantCulture));
                File.WriteAllLines(FilePath, lines.ToArray());
            }
            catch { }
        }

        public static Config Load()
        {
            Config c = new Config();
            try
            {
                if (!File.Exists(FilePath)) return c;
                foreach (string line in File.ReadAllLines(FilePath))
                {
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    FieldInfo f = typeof(Config).GetField(line.Substring(0, eq));
                    if (f == null) continue;
                    string val = line.Substring(eq + 1);
                    try
                    {
                        if (f.FieldType == typeof(int)) f.SetValue(c, int.Parse(val, CultureInfo.InvariantCulture));
                        else if (f.FieldType == typeof(bool)) f.SetValue(c, bool.Parse(val));
                        else if (f.FieldType == typeof(string)) f.SetValue(c, val);
                    }
                    catch { }
                }
            }
            catch { }
            return c;
        }
    }

    // ---------------------------------------------------------------- presets

    public class Preset
    {
        public string Name;
        public Action<Config> Setup;
    }

    public static class Presets
    {
        public static readonly List<Preset> All = new List<Preset>();

        const int Green = unchecked((int)0xFF00FF00);
        const int Cyan = unchecked((int)0xFF00FFFF);
        const int White = unchecked((int)0xFFFFFFFF);
        const int Yellow = unchecked((int)0xFFFFFF00);
        const int Red = unchecked((int)0xFFFF3030);
        const int Magenta = unchecked((int)0xFFFF00FF);
        const int Pink = unchecked((int)0xFFFF69B4);
        const int Orange = unchecked((int)0xFFFF8000);

        static readonly string[] ShapeFields = {
            "ColorArgb", "Outline", "OutlineSize", "OutlineArgb",
            "Lines", "LineLength", "LineThickness", "LineGap", "Rotation", "TStyle",
            "Outer", "OuterLength", "OuterThickness", "OuterGap",
            "Dot", "DotSize", "DotRound",
            "Circle", "CircleRadius", "CircleThickness"
        };

        static void Add(string name, Action<Config> setup)
        {
            Preset p = new Preset();
            p.Name = name;
            p.Setup = setup;
            All.Add(p);
        }

        static void SetLines(Config c, int length, int thickness, int gap)
        {
            c.Lines = true; c.LineLength = length; c.LineThickness = thickness; c.LineGap = gap;
        }

        static void SetDot(Config c, int size, bool round)
        {
            c.Dot = true; c.DotSize = size; c.DotRound = round;
        }

        static void SetCircle(Config c, int radius, int thickness)
        {
            c.Circle = true; c.CircleRadius = radius; c.CircleThickness = thickness;
        }

        static void SetOuter(Config c, int length, int thickness, int gap)
        {
            c.Outer = true; c.OuterLength = length; c.OuterThickness = thickness; c.OuterGap = gap;
        }

        static Presets()
        {
            Add("Classic Cross", c => { SetLines(c, 6, 2, 3); });
            Add("Small Cross", c => { SetLines(c, 4, 2, 2); c.ColorArgb = Cyan; });
            Add("Tiny Cross", c => { SetLines(c, 2, 2, 2); c.ColorArgb = White; });
            Add("Plus", c => { SetLines(c, 5, 2, 0); c.ColorArgb = Cyan; });
            Add("Big Plus", c => { SetLines(c, 10, 2, 0); c.ColorArgb = White; });
            Add("Cross + Dot", c => { SetLines(c, 6, 2, 4); SetDot(c, 2, false); });
            Add("Long Thin Cross", c => { SetLines(c, 14, 1, 4); c.ColorArgb = White; });
            Add("Thick Cross", c => { SetLines(c, 7, 4, 4); c.ColorArgb = Yellow; });
            Add("Wide Gap Cross", c => { SetLines(c, 5, 2, 9); c.ColorArgb = Magenta; });
            Add("Double Lines", c => { SetLines(c, 4, 2, 3); SetOuter(c, 3, 2, 11); });
            Add("Outer Ticks + Dot", c => { SetOuter(c, 4, 2, 10); SetDot(c, 2, false); c.ColorArgb = Cyan; });
            Add("Square Dot", c => { SetDot(c, 4, false); c.ColorArgb = Red; });
            Add("Micro Dot", c => { SetDot(c, 2, false); c.ColorArgb = White; });
            Add("Round Dot", c => { SetDot(c, 6, true); });
            Add("Big Round Dot", c => { SetDot(c, 10, true); c.ColorArgb = Red; });
            Add("Diamond", c => { SetDot(c, 7, false); c.Rotation = 45; c.ColorArgb = Yellow; });
            Add("Circle", c => { SetCircle(c, 10, 2); c.ColorArgb = Cyan; });
            Add("Small Ring", c => { SetCircle(c, 5, 2); });
            Add("Circle + Dot", c => { SetCircle(c, 12, 2); SetDot(c, 4, true); c.ColorArgb = Pink; });
            Add("Cross in Circle", c => { SetLines(c, 5, 2, 2); SetCircle(c, 14, 2); c.ColorArgb = White; });
            Add("T Cross", c => { SetLines(c, 7, 2, 3); c.TStyle = true; });
            Add("T Cross + Dot", c => { SetLines(c, 7, 2, 4); c.TStyle = true; SetDot(c, 2, false); c.ColorArgb = Orange; });
            Add("X Cross", c => { SetLines(c, 7, 2, 3); c.Rotation = 45; c.ColorArgb = Magenta; });
            Add("X Cross + Dot", c => { SetLines(c, 6, 2, 4); c.Rotation = 45; SetDot(c, 3, true); c.ColorArgb = Yellow; });
            Add("Sniper Scope", c => { SetLines(c, 24, 1, 6); SetCircle(c, 18, 1); SetDot(c, 2, false); c.ColorArgb = Red; });
            Add("Ring + Ticks", c => { SetCircle(c, 8, 2); SetOuter(c, 5, 2, 12); c.ColorArgb = Green; });
        }

        public static Config Build(int index)
        {
            Config c = new Config();
            c.Lines = false;
            All[index].Setup(c);
            return c;
        }

        public static int IndexOf(string name)
        {
            for (int i = 0; i < All.Count; i++)
                if (All[i].Name == name) return i;
            return -1;
        }

        // Copies only the shape of a preset; size, opacity, position and app settings are kept.
        public static void Apply(int index, Config target, bool keepColor)
        {
            Config src = Build(index);
            foreach (string name in ShapeFields)
            {
                if (keepColor && name == "ColorArgb") continue;
                FieldInfo f = typeof(Config).GetField(name);
                f.SetValue(target, f.GetValue(src));
            }
        }

        // Renders every preset into one PNG (used to check the drawing code).
        public static void RenderSheet(string path)
        {
            int cols = 7, cell = 130;
            int rows = (All.Count + cols - 1) / cols;
            using (Bitmap bmp = new Bitmap(cols * cell, rows * cell, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (Font font = new Font("Segoe UI", 8f))
            {
                g.Clear(Color.FromArgb(45, 48, 56));
                for (int i = 0; i < All.Count; i++)
                {
                    int x = (i % cols) * cell, y = (i / cols) * cell;
                    Renderer.Draw(g, Build(i), x + cell / 2, y + cell / 2 - 8);
                    g.DrawString(All[i].Name, font, Brushes.White, x + 4, y + cell - 18);
                }
                bmp.Save(path, ImageFormat.Png);
            }
        }
    }

    // ---------------------------------------------------------------- drawing

    public static class Renderer
    {
        static int Sz(int v, double k) { return Math.Max(1, (int)Math.Round(v * k)); }
        static int Gp(int v, double k) { return Math.Max(0, (int)Math.Round(v * k)); }

        // Half the side of the square needed to hold the crosshair.
        public static int Extent(Config c)
        {
            double k = c.Scale / 100.0;
            int e = 8;
            if (c.Lines) e = Math.Max(e, Gp(c.LineGap, k) + Sz(c.LineLength, k) + Sz(c.LineThickness, k));
            if (c.Outer) e = Math.Max(e, Gp(c.OuterGap, k) + Sz(c.OuterLength, k) + Sz(c.OuterThickness, k));
            if (c.Circle) e = Math.Max(e, Sz(c.CircleRadius, k) + Sz(c.CircleThickness, k));
            if (c.Dot) e = Math.Max(e, Sz(c.DotSize, k));
            return e + (c.Outline ? c.OutlineSize : 0) + 4;
        }

        static void AddLines(List<Rectangle> rects, int len, int thick, int gap, bool tStyle)
        {
            int h = thick / 2;
            int p = thick % 2;   // odd thickness is centred on a pixel, even thickness on a pixel boundary
            rects.Add(new Rectangle(gap + p, -h, len, thick));          // right
            rects.Add(new Rectangle(-gap - len, -h, len, thick));       // left
            rects.Add(new Rectangle(-h, gap + p, thick, len));          // bottom
            if (!tStyle) rects.Add(new Rectangle(-h, -gap - len, thick, len)); // top
            if (gap == 0) rects.Add(new Rectangle(-h, -h, thick, thick));
        }

        // Draws the crosshair so that (cx, cy) is its centre.
        public static void Draw(Graphics g, Config c, float cx, float cy)
        {
            double k = c.Scale / 100.0;
            Color main = Color.FromArgb(255, Color.FromArgb(c.ColorArgb));
            Color edge = Color.FromArgb(255, Color.FromArgb(c.OutlineArgb));
            int o = c.Outline ? c.OutlineSize : 0;

            List<Rectangle> rects = new List<Rectangle>();
            if (c.Lines) AddLines(rects, Sz(c.LineLength, k), Sz(c.LineThickness, k), Gp(c.LineGap, k), c.TStyle);
            if (c.Outer) AddLines(rects, Sz(c.OuterLength, k), Sz(c.OuterThickness, k), Gp(c.OuterGap, k), false);
            int d = Sz(c.DotSize, k);
            if (c.Dot && !c.DotRound) rects.Add(new Rectangle(-(d / 2), -(d / 2), d, d));

            bool rotated = (c.Rotation % 90) != 0;

            GraphicsState state = g.Save();
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.TranslateTransform(cx, cy);

            // pass 0 = outline underneath everything, pass 1 = the crosshair itself
            for (int pass = 0; pass < 2; pass++)
            {
                if (pass == 0 && o == 0) continue;
                int grow = pass == 0 ? o : 0;
                Color col = pass == 0 ? edge : main;
                using (SolidBrush brush = new SolidBrush(col))
                {
                    GraphicsState inner = g.Save();
                    if (rotated)
                    {
                        g.RotateTransform(c.Rotation);
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                    }
                    else
                    {
                        g.SmoothingMode = SmoothingMode.None;
                    }
                    foreach (Rectangle r in rects)
                    {
                        Rectangle q = r;
                        q.Inflate(grow, grow);
                        g.FillRectangle(brush, q);
                    }
                    g.Restore(inner);

                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    if (c.Circle)
                    {
                        float rad = Sz(c.CircleRadius, k);
                        float w = Sz(c.CircleThickness, k) + 2 * grow;
                        using (Pen pen = new Pen(col, w))
                            g.DrawEllipse(pen, -rad, -rad, 2 * rad, 2 * rad);
                    }
                    if (c.Dot && c.DotRound)
                    {
                        float half = d / 2f + grow;
                        g.FillEllipse(brush, -half, -half, 2 * half, 2 * half);
                    }
                }
            }
            g.Restore(state);
        }
    }

    // ---------------------------------------------------------------- win32

    internal static class Native
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int X, Y; public POINT(int x, int y) { X = x; Y = y; } }

        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE { public int CX, CY; public SIZE(int cx, int cy) { CX = cx; CY = cy; } }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BLENDFUNCTION { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize,
            IntPtr hdcSrc, ref POINT pptSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);
        [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll")] public static extern IntPtr CreateCompatibleDC(IntPtr hDC);
        [DllImport("gdi32.dll")] public static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll")] public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
        [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr hObject);
        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    }

    // ---------------------------------------------------------------- overlay window

    // Borderless, per-pixel transparent, always on top, and invisible to the mouse (clicks pass through).
    public class OverlayForm : Form
    {
        bool shown;

        public OverlayForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(16, 16);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE
                cp.ExStyle |= 0x80000 | 0x20 | 0x8 | 0x80 | 0x08000000;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        public void UpdateOverlay(Config c)
        {
            if (!c.Visible)
            {
                if (shown) { Native.ShowWindow(Handle, 0); shown = false; }
                return;
            }

            Screen[] screens = Screen.AllScreens;
            Screen scr = (c.Monitor >= 1 && c.Monitor <= screens.Length) ? screens[c.Monitor - 1] : Screen.PrimaryScreen;
            Rectangle b = scr.Bounds;

            int ext = Renderer.Extent(c);
            int left = b.Left + b.Width / 2 - ext + c.OffsetX;
            int top = b.Top + b.Height / 2 - ext + c.OffsetY;
            byte alpha = (byte)Math.Max(1, Math.Min(255, c.Opacity * 255 / 100));

            using (Bitmap bmp = new Bitmap(ext * 2, ext * 2, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    Renderer.Draw(g, c, ext, ext);
                }
                SetBitmap(bmp, left, top, alpha);
            }

            if (!shown) { Native.ShowWindow(Handle, 4); shown = true; }   // SW_SHOWNOACTIVATE
            // HWND_TOPMOST, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE
            Native.SetWindowPos(Handle, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010);
        }

        void SetBitmap(Bitmap bitmap, int left, int top, byte opacity)
        {
            IntPtr screenDc = Native.GetDC(IntPtr.Zero);
            IntPtr memDc = Native.CreateCompatibleDC(screenDc);
            IntPtr hBitmap = IntPtr.Zero, oldBitmap = IntPtr.Zero;
            try
            {
                hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
                oldBitmap = Native.SelectObject(memDc, hBitmap);
                Native.SIZE size = new Native.SIZE(bitmap.Width, bitmap.Height);
                Native.POINT src = new Native.POINT(0, 0);
                Native.POINT dst = new Native.POINT(left, top);
                Native.BLENDFUNCTION blend = new Native.BLENDFUNCTION();
                blend.BlendOp = 0;               // AC_SRC_OVER
                blend.BlendFlags = 0;
                blend.SourceConstantAlpha = opacity;
                blend.AlphaFormat = 1;           // AC_SRC_ALPHA
                Native.UpdateLayeredWindow(Handle, screenDc, ref dst, ref size, memDc, ref src, 0, ref blend, 2);
            }
            finally
            {
                Native.ReleaseDC(IntPtr.Zero, screenDc);
                if (hBitmap != IntPtr.Zero)
                {
                    Native.SelectObject(memDc, oldBitmap);
                    Native.DeleteObject(hBitmap);
                }
                Native.DeleteDC(memDc);
            }
        }
    }

    // ---------------------------------------------------------------- preview

    public class PreviewPanel : Panel
    {
        public Config Cfg;
        public int Bg;
        public static readonly Color[] Backgrounds = {
            Color.FromArgb(32, 34, 40), Color.FromArgb(168, 150, 122), Color.FromArgb(226, 226, 226),
            Color.FromArgb(70, 110, 80), Color.FromArgb(120, 170, 220)
        };

        public PreviewPanel()
        {
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
        }

        protected override void OnClick(EventArgs e)
        {
            Bg = (Bg + 1) % Backgrounds.Length;
            Invalidate();
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Backgrounds[Bg]);
            if (Cfg == null) return;
            int w = ClientSize.Width, h = ClientSize.Height;
            if (w < 2 || h < 2) return;
            using (Bitmap bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                    Renderer.Draw(g, Cfg, w / 2, h / 2);
                ColorMatrix cm = new ColorMatrix();
                cm.Matrix33 = Cfg.Opacity / 100f;
                using (ImageAttributes ia = new ImageAttributes())
                {
                    ia.SetColorMatrix(cm);
                    e.Graphics.DrawImage(bmp, new Rectangle(0, 0, w, h), 0, 0, w, h, GraphicsUnit.Pixel, ia);
                }
            }
        }
    }

    // ---------------------------------------------------------------- settings window

    public class SettingsForm : Form
    {
        public static readonly string[] HotkeyNames = { "None", "F6", "F7", "F8", "F9", "F10", "F11", "Insert", "Home", "End", "Pause" };
        static readonly Keys[] HotkeyKeys = { Keys.None, Keys.F6, Keys.F7, Keys.F8, Keys.F9, Keys.F10, Keys.F11, Keys.Insert, Keys.Home, Keys.End, Keys.Pause };

        const int LabelW = 120, SliderW = 250, RowH = 28, Indent = 18;

        readonly Config cfg;
        readonly OverlayForm overlay;
        readonly NotifyIcon tray;
        readonly List<Action> refreshers = new List<Action>();
        readonly System.Windows.Forms.Timer topTimer = new System.Windows.Forms.Timer();

        PreviewPanel preview;
        ListBox presetList;
        Panel panel;
        Label hotkeyStatus;
        ToolStripMenuItem toggleItem;
        int rowY = 6;
        bool loading, exiting, balloonShown;

        public SettingsForm()
        {
            cfg = Config.Load();
            overlay = new OverlayForm();

            Text = "Crosshair Overlay";
            Font = SystemFonts.MessageBoxFont;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.Manual;
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(wa.Left + 40, wa.Top + 40);
            Icon = MakeIcon();

            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(742, 646);
            BuildLeft();
            BuildRight();
            ResumeLayout(false);
            PerformLayout();

            tray = new NotifyIcon();
            tray.Icon = Icon;
            tray.Text = "Crosshair Overlay";
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Settings", null, delegate { ShowSettings(); });
            toggleItem = new ToolStripMenuItem("Hide crosshair");
            toggleItem.Click += delegate { ToggleVisible(); };
            menu.Items.Add(toggleItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, delegate { ExitApp(); });
            tray.ContextMenuStrip = menu;
            tray.DoubleClick += delegate { ShowSettings(); };
            tray.Visible = true;

            // Re-assert "on top" and re-centre regularly (covers resolution changes and other topmost windows).
            topTimer.Interval = 1500;
            topTimer.Tick += delegate { overlay.UpdateOverlay(cfg); };
            topTimer.Start();

            LoadControls();
            Changed();
        }

        static Icon MakeIcon()
        {
            using (Bitmap bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    Config c = new Config();
                    c.LineLength = 9; c.LineThickness = 4; c.LineGap = 4;
                    Renderer.Draw(g, c, 16, 16);
                }
                return Icon.FromHandle(bmp.GetHicon());
            }
        }

        // ------------------------------------------------ left column

        void BuildLeft()
        {
            Label title = new Label();
            title.Text = "Crosshairs";
            title.Font = new Font(Font, FontStyle.Bold);
            title.AutoSize = true;
            title.Location = new Point(12, 10);
            Controls.Add(title);

            presetList = new ListBox();
            presetList.IntegralHeight = false;
            presetList.Location = new Point(12, 32);
            presetList.Size = new Size(220, 268);
            foreach (Preset p in Presets.All) presetList.Items.Add(p.Name);
            presetList.SelectedIndexChanged += delegate
            {
                if (loading || presetList.SelectedIndex < 0) return;
                Presets.Apply(presetList.SelectedIndex, cfg, cfg.KeepColor);
                cfg.Preset = Presets.All[presetList.SelectedIndex].Name;
                LoadControls();
                Changed();
            };
            refreshers.Add(delegate
            {
                int idx = Presets.IndexOf(cfg.Preset);
                if (presetList.SelectedIndex != idx) presetList.SelectedIndex = idx;
            });
            Controls.Add(presetList);

            MakeCheck("Keep my color when switching", "KeepColor", this, 12, 306, false);

            preview = new PreviewPanel();
            preview.Cfg = cfg;
            preview.BorderStyle = BorderStyle.FixedSingle;
            preview.Location = new Point(12, 334);
            preview.Size = new Size(220, 200);
            Controls.Add(preview);

            Label hint = new Label();
            hint.Text = "Click preview to change background";
            hint.ForeColor = SystemColors.GrayText;
            hint.Location = new Point(12, 538);
            hint.Size = new Size(224, 18);
            hint.Font = new Font(Font.FontFamily, 8f);
            Controls.Add(hint);

            MakeCheck("Show crosshair", "Visible", this, 12, 566, true);

            Button hide = new Button();
            hide.Text = "Hide to tray";
            hide.Location = new Point(12, 604);
            hide.Size = new Size(106, 28);
            hide.Click += delegate { Close(); };
            Controls.Add(hide);

            Button exit = new Button();
            exit.Text = "Exit app";
            exit.Location = new Point(126, 604);
            exit.Size = new Size(106, 28);
            exit.Click += delegate { ExitApp(); };
            Controls.Add(exit);
        }

        // ------------------------------------------------ right column

        void BuildRight()
        {
            panel = new Panel();
            panel.Location = new Point(244, 10);
            panel.Size = new Size(488, 626);
            panel.AutoScroll = true;
            panel.AutoScrollMargin = new Size(0, 10);
            panel.BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(panel);

            AddHeader("General");
            AddSlider("Size (%)", "Scale", 25, 400);
            AddSlider("Opacity (%)", "Opacity", 10, 100);
            AddColorRow("Color", "ColorArgb", new int[] {
                unchecked((int)0xFF00FF00), unchecked((int)0xFF00FFFF), unchecked((int)0xFFFFFFFF), unchecked((int)0xFFFFFF00),
                unchecked((int)0xFFFF3030), unchecked((int)0xFFFF00FF), unchecked((int)0xFFFF8000), unchecked((int)0xFFFF69B4) });
            AddCheckRow("Outline", "Outline");
            AddSlider("Outline size", "OutlineSize", 1, 4);
            AddColorRow("Outline color", "OutlineArgb", new int[] {
                unchecked((int)0xFF000000), unchecked((int)0xFFFFFFFF), unchecked((int)0xFF404040) });

            AddSection("Inner lines", "Lines");
            AddSlider("Length", "LineLength", 1, 60);
            AddSlider("Thickness", "LineThickness", 1, 12);
            AddSlider("Gap", "LineGap", 0, 40);
            AddCheckRow("T-style (no top line)", "TStyle");

            AddSection("Outer lines", "Outer");
            AddSlider("Length", "OuterLength", 1, 40);
            AddSlider("Thickness", "OuterThickness", 1, 12);
            AddSlider("Gap", "OuterGap", 0, 60);

            AddSection("Center dot", "Dot");
            AddSlider("Size", "DotSize", 1, 24);
            AddCheckRow("Round dot", "DotRound");

            AddSection("Circle", "Circle");
            AddSlider("Radius", "CircleRadius", 2, 80);
            AddSlider("Thickness", "CircleThickness", 1, 8);

            AddHeader("Rotation and position");
            AddSlider("Rotation (deg)", "Rotation", 0, 89);
            AddSlider("Offset X (px)", "OffsetX", -100, 100);
            AddSlider("Offset Y (px)", "OffsetY", -100, 100);

            List<string> monitors = new List<string>();
            monitors.Add("Primary monitor");
            Screen[] screens = Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
                monitors.Add("Monitor " + (i + 1) + "  (" + screens[i].Bounds.Width + " x " + screens[i].Bounds.Height + ")"
                    + (screens[i].Primary ? "  primary" : ""));
            AddCombo("Monitor", "Monitor", monitors.ToArray(), null);

            AddHeader("Hotkey");
            AddCombo("Show / hide key", "Hotkey", HotkeyNames, delegate { if (IsHandleCreated) RegisterToggleHotkey(); });
            hotkeyStatus = new Label();
            hotkeyStatus.ForeColor = SystemColors.GrayText;
            hotkeyStatus.Location = new Point(Indent, rowY + 2);
            hotkeyStatus.Size = new Size(430, 20);
            panel.Controls.Add(hotkeyStatus);
            rowY += RowH;

            Label note = new Label();
            note.Text = "The game must run in \"Windowed Fullscreen\" display mode. In exclusive fullscreen "
                + "Windows cannot draw anything over the game.";
            note.Location = new Point(Indent, rowY + 2);
            note.Size = new Size(430, 36);
            panel.Controls.Add(note);
            rowY += 42;
        }

        void AddHeader(string text)
        {
            rowY += 8;
            Label l = new Label();
            l.Text = text;
            l.Font = new Font(Font, FontStyle.Bold);
            l.AutoSize = true;
            l.Location = new Point(6, rowY + 4);
            panel.Controls.Add(l);
            rowY += RowH;
        }

        void AddSection(string text, string enabledField)
        {
            rowY += 8;
            MakeCheck(text, enabledField, panel, 8, rowY + 3, true);
            rowY += RowH;
        }

        void AddCheckRow(string text, string field)
        {
            MakeCheck(text, field, panel, Indent + 2, rowY + 3, false);
            rowY += RowH;
        }

        CheckBox MakeCheck(string text, string field, Control parent, int x, int y, bool bold)
        {
            FieldInfo f = typeof(Config).GetField(field);
            CheckBox cb = new CheckBox();
            cb.Text = text;
            cb.AutoSize = true;
            cb.Location = new Point(x, y);
            if (bold) cb.Font = new Font(Font, FontStyle.Bold);
            cb.CheckedChanged += delegate
            {
                if (loading) return;
                f.SetValue(cfg, cb.Checked);
                Changed();
            };
            refreshers.Add(delegate { cb.Checked = (bool)f.GetValue(cfg); });
            parent.Controls.Add(cb);
            return cb;
        }

        void AddSlider(string text, string field, int min, int max)
        {
            FieldInfo f = typeof(Config).GetField(field);

            Label l = new Label();
            l.Text = text;
            l.Location = new Point(Indent, rowY + 5);
            l.Size = new Size(LabelW, 20);

            TrackBar tb = new TrackBar();
            tb.AutoSize = false;
            tb.TickStyle = TickStyle.None;
            tb.Minimum = min;
            tb.Maximum = max;
            tb.SmallChange = 1;
            tb.LargeChange = Math.Max(1, (max - min) / 10);
            tb.Location = new Point(Indent + LabelW, rowY);
            tb.Size = new Size(SliderW, 26);

            Label v = new Label();
            v.Location = new Point(Indent + LabelW + SliderW + 4, rowY + 5);
            v.Size = new Size(48, 20);

            tb.ValueChanged += delegate
            {
                v.Text = tb.Value.ToString();
                if (loading) return;
                f.SetValue(cfg, tb.Value);
                Changed();
            };
            refreshers.Add(delegate
            {
                tb.Value = Math.Max(min, Math.Min(max, (int)f.GetValue(cfg)));
                v.Text = tb.Value.ToString();
            });

            panel.Controls.Add(l);
            panel.Controls.Add(tb);
            panel.Controls.Add(v);
            rowY += RowH;
        }

        void AddColorRow(string text, string field, int[] swatches)
        {
            FieldInfo f = typeof(Config).GetField(field);

            Label l = new Label();
            l.Text = text;
            l.Location = new Point(Indent, rowY + 5);
            l.Size = new Size(LabelW, 20);
            panel.Controls.Add(l);

            Button pick = new Button();
            pick.FlatStyle = FlatStyle.Flat;
            pick.Location = new Point(Indent + LabelW + 6, rowY + 2);
            pick.Size = new Size(64, 22);
            pick.Click += delegate
            {
                using (ColorDialog dlg = new ColorDialog())
                {
                    dlg.FullOpen = true;
                    dlg.Color = pick.BackColor;
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;
                    f.SetValue(cfg, dlg.Color.ToArgb() | unchecked((int)0xFF000000));
                    LoadControls();
                    Changed();
                }
            };
            refreshers.Add(delegate { pick.BackColor = Color.FromArgb(255, Color.FromArgb((int)f.GetValue(cfg))); });
            panel.Controls.Add(pick);

            int x = Indent + LabelW + 6 + 64 + 10;
            foreach (int sw in swatches)
            {
                int val = sw;
                Button s = new Button();
                s.FlatStyle = FlatStyle.Flat;
                s.Location = new Point(x, rowY + 2);
                s.Size = new Size(22, 22);
                s.BackColor = Color.FromArgb(val);
                s.Click += delegate
                {
                    f.SetValue(cfg, val);
                    LoadControls();
                    Changed();
                };
                panel.Controls.Add(s);
                x += 26;
            }
            rowY += RowH;
        }

        void AddCombo(string text, string field, string[] items, Action afterChange)
        {
            FieldInfo f = typeof(Config).GetField(field);

            Label l = new Label();
            l.Text = text;
            l.Location = new Point(Indent, rowY + 5);
            l.Size = new Size(LabelW, 20);
            panel.Controls.Add(l);

            ComboBox cb = new ComboBox();
            cb.DropDownStyle = ComboBoxStyle.DropDownList;
            cb.Items.AddRange(items);
            cb.Location = new Point(Indent + LabelW + 6, rowY + 1);
            cb.Size = new Size(240, 24);
            cb.SelectedIndexChanged += delegate
            {
                if (loading) return;
                f.SetValue(cfg, cb.SelectedIndex);
                if (afterChange != null) afterChange();
                Changed();
            };
            refreshers.Add(delegate
            {
                int i = (int)f.GetValue(cfg);
                if (i < 0 || i >= cb.Items.Count) { i = 0; f.SetValue(cfg, 0); }
                cb.SelectedIndex = i;
            });
            panel.Controls.Add(cb);
            rowY += RowH;
        }

        // ------------------------------------------------ behaviour

        void LoadControls()
        {
            loading = true;
            try { foreach (Action a in refreshers) a(); }
            finally { loading = false; }
        }

        void Changed()
        {
            overlay.UpdateOverlay(cfg);
            preview.Invalidate();
            cfg.Save();   // every change is written straight away, so the next start looks the same
            if (toggleItem != null) toggleItem.Text = cfg.Visible ? "Hide crosshair" : "Show crosshair";
        }

        void ToggleVisible()
        {
            cfg.Visible = !cfg.Visible;
            LoadControls();
            Changed();
        }

        void ShowSettings()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        void ExitApp()
        {
            exiting = true;
            Application.Exit();
        }

        void RegisterToggleHotkey()
        {
            Native.UnregisterHotKey(Handle, 1);
            int i = cfg.Hotkey;
            if (i < 0 || i >= HotkeyKeys.Length) i = 0;
            if (HotkeyKeys[i] == Keys.None)
            {
                hotkeyStatus.Text = "No hotkey set. Use the tray icon to show or hide.";
                return;
            }
            bool ok = Native.RegisterHotKey(Handle, 1, 0x4000 /* MOD_NOREPEAT */, (uint)HotkeyKeys[i]);
            hotkeyStatus.Text = ok
                ? "Press " + HotkeyNames[i] + " anywhere (also in game) to show or hide the crosshair."
                : HotkeyNames[i] + " is already used by another program. Pick a different key.";
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (hotkeyStatus != null) RegisterToggleHotkey();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            Native.UnregisterHotKey(Handle, 1);
            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312 && m.WParam.ToInt32() == 1)   // WM_HOTKEY
            {
                ToggleVisible();
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!exiting && e.CloseReason == CloseReason.UserClosing)
            {
                // Closing the window keeps the crosshair running; the tray icon brings settings back.
                e.Cancel = true;
                Hide();
                if (!balloonShown)
                {
                    balloonShown = true;
                    tray.ShowBalloonTip(3000, "Crosshair Overlay",
                        "Still running. Double-click this icon for settings, right-click to exit.", ToolTipIcon.Info);
                }
                return;
            }
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            topTimer.Stop();
            cfg.Save();
            tray.Visible = false;
            tray.Dispose();
            overlay.Dispose();
            base.OnFormClosed(e);
        }
    }

    // ---------------------------------------------------------------- entry point

    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Native.SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length == 2 && args[0] == "--render")
            {
                Presets.RenderSheet(args[1]);
                return;
            }

            bool created;
            using (System.Threading.Mutex mutex = new System.Threading.Mutex(true, "CrosshairOverlay_SingleInstance", out created))
            {
                if (!created)
                {
                    MessageBox.Show("Crosshair Overlay is already running.\nLook for its icon in the system tray (bottom right).",
                        "Crosshair Overlay", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                Application.Run(new SettingsForm());
            }
        }
    }
}
