using System.Drawing.Drawing2D;

namespace VertexExecutor;

public class MainForm : Form
{
    // Colors
    private static readonly Color BgDark = Color.FromArgb(18, 18, 18);
    private static readonly Color BgMedium = Color.FromArgb(28, 28, 28);
    private static readonly Color BgLight = Color.FromArgb(38, 38, 38);
    private static readonly Color TextWhite = Color.FromArgb(220, 220, 220);
    private static readonly Color TextGray = Color.FromArgb(120, 120, 120);
    private static readonly Color AccentGold = Color.FromArgb(212, 175, 55);
    private static readonly Color AccentRed = Color.FromArgb(200, 80, 80);

    // Controls
    private RichTextBox lineNumbers = null!;
    private RichTextBox codeEditor = null!;

    // Tab management
    private List<FileTab> fileTabs = new List<FileTab>();
    private int fileCounter = 1;
    private FileTab? activeFileTab;
    private Panel fileTabBar = null!;

    // State
    private bool isDragging = false;
    private Point dragOffset;
    private bool isHighlighting = false;
    private string selectedNav = "Editor";
    private bool consoleExpanded = false;

    private class FileTab
    {
        public Panel TabPanel { get; set; } = null!;
        public Label NameLabel { get; set; } = null!;
        public Label CloseBtn { get; set; } = null!;
        public string Content { get; set; } = "";
        public string FileName { get; set; } = "";
    }

    public MainForm()
    {
        InitializeForm();
        BuildUI();
    }

    private void InitializeForm()
    {
        this.Text = "Vertex";
        this.Size = new Size(800, 550);
        this.MinimumSize = new Size(600, 400);
        this.FormBorderStyle = FormBorderStyle.None;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = BgDark;
        this.DoubleBuffered = true;
    }

    private void BuildUI()
    {
        // === ROW 1: Title bar with Logo + Navigation + Window buttons ===
        var titleBar = new Panel
        {
            Height = 44,
            Dock = DockStyle.Top,
            BackColor = BgDark
        };

        // V Logo (gold)
        var logo = new Panel { Size = new Size(40, 40), Location = new Point(8, 2), BackColor = Color.Transparent };
        logo.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(AccentGold, 3f);
            e.Graphics.DrawLine(pen, 8, 8, 20, 32);
            e.Graphics.DrawLine(pen, 32, 8, 20, 32);
        };
        logo.MouseDown += TitleBar_MouseDown;
        logo.MouseMove += TitleBar_MouseMove;
        logo.MouseUp += TitleBar_MouseUp;
        titleBar.Controls.Add(logo);

        // Navigation buttons (Editor, Scripts, Settings, Profile)
        int navX = 55;
        var navItems = new[] { ("</> ", "Editor"), ("◇ ", "Scripts"), ("⚙ ", "Settings"), ("👤 ", "Profile") };
        foreach (var (icon, name) in navItems)
        {
            var navBtn = new Panel
            {
                Location = new Point(navX, 8),
                Size = new Size(85, 28),
                BackColor = name == selectedNav ? BgLight : Color.Transparent,
                Cursor = Cursors.Hand,
                Tag = name
            };
            if (name == selectedNav) RoundCorners(navBtn, 6);

            var navLbl = new Label
            {
                Text = icon + name,
                Dock = DockStyle.Fill,
                ForeColor = name == selectedNav ? AccentGold : TextGray,
                Font = new Font("Segoe UI", 9f),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            navBtn.Controls.Add(navLbl);

            if (name != selectedNav)
            {
                navBtn.MouseEnter += (s, e) => { navBtn.BackColor = BgMedium; navLbl.ForeColor = TextWhite; };
                navBtn.MouseLeave += (s, e) => { navBtn.BackColor = Color.Transparent; navLbl.ForeColor = TextGray; };
                navLbl.MouseEnter += (s, e) => { navBtn.BackColor = BgMedium; navLbl.ForeColor = TextWhite; };
                navLbl.MouseLeave += (s, e) => { navBtn.BackColor = Color.Transparent; navLbl.ForeColor = TextGray; };
            }

            titleBar.Controls.Add(navBtn);
            navX += 90;
        }

        // Window buttons (right side)
        AddWindowButton(titleBar, "✕", this.ClientSize.Width - 46, Color.FromArgb(200, 50, 50), () => this.Close());
        AddWindowButton(titleBar, "□", this.ClientSize.Width - 92, BgLight, () =>
        {
            this.WindowState = this.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
        });
        AddWindowButton(titleBar, "─", this.ClientSize.Width - 138, BgLight, () => this.WindowState = FormWindowState.Minimized);

        titleBar.MouseDown += TitleBar_MouseDown;
        titleBar.MouseMove += TitleBar_MouseMove;
        titleBar.MouseUp += TitleBar_MouseUp;

        this.Controls.Add(titleBar);

        // === ROW 2: File tabs (untitled1.lua + button) ===
        fileTabBar = new Panel
        {
            Height = 32,
            Dock = DockStyle.Top,
            BackColor = BgDark
        };
        this.Controls.Add(fileTabBar);

        // === ROW 3: Toolbar (Execute, Clear, Open, Save | Attach) ===
        var toolbar = new Panel
        {
            Height = 36,
            Dock = DockStyle.Top,
            BackColor = BgDark
        };

        int tbX = 8;
        var tbButtons = new[] { ("▶", "Execute"), ("🗑", "Clear"), ("📁", "Open"), ("💾", "Save") };
        foreach (var (icon, text) in tbButtons)
        {
            var btn = CreateToolbarButton(icon, text, tbX);
            if (text == "Clear")
            {
                EventHandler clearHandler = (s, e) => codeEditor?.Clear();
                btn.Click += clearHandler;
                foreach (Control c in btn.Controls) c.Click += clearHandler;
            }
            toolbar.Controls.Add(btn);
            tbX += 80;
        }

        // Attach button on right
        var attachBtn = CreateToolbarButton("📎", "Attach", this.ClientSize.Width - 90);
        attachBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        toolbar.Controls.Add(attachBtn);

        this.Controls.Add(toolbar);

        // === ROW 4 (bottom): Console ===
        var consolePanel = new Panel
        {
            Height = 36,
            Dock = DockStyle.Bottom,
            BackColor = BgMedium,
            Cursor = Cursors.Hand
        };

        var consoleIcon = new Label
        {
            Text = "▣",
            Location = new Point(12, 8),
            AutoSize = true,
            ForeColor = TextGray,
            Font = new Font("Segoe UI", 10f),
            BackColor = Color.Transparent
        };
        consolePanel.Controls.Add(consoleIcon);

        var consoleLbl = new Label
        {
            Text = "Console",
            Location = new Point(32, 9),
            AutoSize = true,
            ForeColor = TextGray,
            Font = new Font("Segoe UI", 9.5f),
            BackColor = Color.Transparent
        };
        consolePanel.Controls.Add(consoleLbl);

        var consoleArrow = new Label
        {
            Text = "▼",
            Location = new Point(this.ClientSize.Width - 40, 9),
            Size = new Size(24, 20),
            ForeColor = TextGray,
            Font = new Font("Segoe UI", 9f),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        consolePanel.Controls.Add(consoleArrow);

        EventHandler toggleConsole = (s, e) =>
        {
            consoleExpanded = !consoleExpanded;
            consolePanel.Height = consoleExpanded ? 150 : 36;
            consoleArrow.Text = consoleExpanded ? "▲" : "▼";
        };
        consolePanel.Click += toggleConsole;
        consoleArrow.Click += toggleConsole;

        this.Controls.Add(consolePanel);

        // === Center: Editor with line numbers ===
        var editorPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgDark
        };

        lineNumbers = new RichTextBox
        {
            Width = 45,
            Dock = DockStyle.Left,
            BackColor = BgDark,
            ForeColor = TextGray,
            Font = new Font("Consolas", 11f),
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            ScrollBars = RichTextBoxScrollBars.None,
            Text = "1",
            Cursor = Cursors.Arrow
        };
        lineNumbers.SelectAll();
        lineNumbers.SelectionAlignment = HorizontalAlignment.Right;
        lineNumbers.DeselectAll();

        codeEditor = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = BgDark,
            ForeColor = TextWhite,
            Font = new Font("Consolas", 11f),
            BorderStyle = BorderStyle.None,
            AcceptsTab = true,
            WordWrap = false
        };
        codeEditor.TextChanged += (s, e) => { UpdateLineNumbers(); HighlightSyntax(); };
        codeEditor.VScroll += CodeEditor_VScroll;

        editorPanel.Controls.Add(codeEditor);
        editorPanel.Controls.Add(lineNumbers);

        this.Controls.Add(editorPanel);

        // Create first file tab
        CreateNewFileTab();
    }

    private void AddWindowButton(Panel parent, string text, int x, Color hoverColor, Action onClick)
    {
        var btn = new Panel
        {
            Size = new Size(46, 44),
            Location = new Point(x, 0),
            BackColor = BgDark,
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        var lbl = new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            ForeColor = TextGray,
            Font = new Font("Segoe UI", 10f),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        btn.Controls.Add(lbl);

        btn.MouseEnter += (s, e) => btn.BackColor = hoverColor;
        btn.MouseLeave += (s, e) => btn.BackColor = BgDark;
        lbl.MouseEnter += (s, e) => btn.BackColor = hoverColor;
        lbl.MouseLeave += (s, e) => btn.BackColor = BgDark;

        EventHandler handler = (s, e) => onClick();
        btn.Click += handler;
        lbl.Click += handler;

        parent.Controls.Add(btn);
    }

    private Panel CreateToolbarButton(string icon, string text, int x)
    {
        var btn = new Panel
        {
            Location = new Point(x, 4),
            Size = new Size(75, 28),
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };
        var lbl = new Label
        {
            Text = $"{icon}  {text}",
            Dock = DockStyle.Fill,
            ForeColor = TextGray,
            Font = new Font("Segoe UI", 9f),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        btn.Controls.Add(lbl);

        btn.MouseEnter += (s, e) => { btn.BackColor = BgMedium; lbl.ForeColor = TextWhite; };
        btn.MouseLeave += (s, e) => { btn.BackColor = Color.Transparent; lbl.ForeColor = TextGray; };
        lbl.MouseEnter += (s, e) => { btn.BackColor = BgMedium; lbl.ForeColor = TextWhite; };
        lbl.MouseLeave += (s, e) => { btn.BackColor = Color.Transparent; lbl.ForeColor = TextGray; };

        return btn;
    }

    private void CreateNewFileTab()
    {
        if (activeFileTab != null)
            activeFileTab.Content = codeEditor.Text;

        var tab = new FileTab
        {
            FileName = $"untitled{fileCounter++}.lua",
            Content = ""
        };

        tab.TabPanel = new Panel
        {
            Size = new Size(130, 26),
            BackColor = BgLight,
            Cursor = Cursors.Hand
        };
        RoundCorners(tab.TabPanel, 4);

        var luaIcon = new Label
        {
            Text = "☰",
            Location = new Point(8, 5),
            Size = new Size(16, 16),
            ForeColor = Color.FromArgb(0, 122, 204),
            Font = new Font("Segoe UI", 9f),
            BackColor = Color.Transparent
        };
        tab.TabPanel.Controls.Add(luaIcon);

        tab.NameLabel = new Label
        {
            Text = tab.FileName,
            Location = new Point(26, 5),
            AutoSize = true,
            ForeColor = AccentGold,
            Font = new Font("Segoe UI", 9f),
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };
        tab.TabPanel.Controls.Add(tab.NameLabel);

        tab.CloseBtn = new Label
        {
            Text = "×",
            Location = new Point(110, 3),
            Size = new Size(16, 20),
            ForeColor = TextGray,
            Font = new Font("Segoe UI", 10f),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        tab.CloseBtn.MouseEnter += (s, e) => tab.CloseBtn.ForeColor = TextWhite;
        tab.CloseBtn.MouseLeave += (s, e) => tab.CloseBtn.ForeColor = TextGray;
        tab.CloseBtn.Click += (s, e) => CloseFileTab(tab);
        tab.TabPanel.Controls.Add(tab.CloseBtn);

        EventHandler selectHandler = (s, e) => SelectFileTab(tab);
        tab.TabPanel.Click += selectHandler;
        tab.NameLabel.Click += selectHandler;
        luaIcon.Click += selectHandler;

        fileTabs.Add(tab);
        RefreshFileTabBar();
        SelectFileTab(tab);
    }

    private void SelectFileTab(FileTab tab)
    {
        if (activeFileTab != null)
        {
            activeFileTab.Content = codeEditor.Text;
            activeFileTab.TabPanel.BackColor = BgMedium;
            activeFileTab.NameLabel.ForeColor = TextGray;
        }

        activeFileTab = tab;
        tab.TabPanel.BackColor = BgLight;
        tab.NameLabel.ForeColor = AccentGold;

        codeEditor.Text = tab.Content;
        HighlightSyntax();
        UpdateLineNumbers();
    }

    private void CloseFileTab(FileTab tab)
    {
        if (fileTabs.Count <= 1)
        {
            codeEditor.Clear();
            return;
        }

        int index = fileTabs.IndexOf(tab);
        fileTabs.Remove(tab);

        if (activeFileTab == tab)
        {
            int newIndex = Math.Min(index, fileTabs.Count - 1);
            SelectFileTab(fileTabs[newIndex]);
        }

        RefreshFileTabBar();
    }

    private void RefreshFileTabBar()
    {
        fileTabBar.Controls.Clear();

        int x = 8;
        foreach (var tab in fileTabs)
        {
            tab.TabPanel.Location = new Point(x, 3);
            fileTabBar.Controls.Add(tab.TabPanel);
            x += 135;
        }

        var plusBtn = new Label
        {
            Text = "+",
            Location = new Point(x + 5, 5),
            Size = new Size(24, 22),
            ForeColor = TextGray,
            Font = new Font("Segoe UI", 12f),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        plusBtn.MouseEnter += (s, e) => plusBtn.ForeColor = TextWhite;
        plusBtn.MouseLeave += (s, e) => plusBtn.ForeColor = TextGray;
        plusBtn.Click += (s, e) => CreateNewFileTab();
        fileTabBar.Controls.Add(plusBtn);
    }

    private void HighlightSyntax()
    {
        if (isHighlighting || codeEditor == null) return;
        isHighlighting = true;

        var keywords = new[] { "local", "function", "end", "if", "then", "else", "elseif",
                               "while", "do", "for", "in", "return", "not", "and", "or", "nil", "true", "false", "print" };

        int selStart = codeEditor.SelectionStart;
        int selLen = codeEditor.SelectionLength;

        codeEditor.SuspendLayout();
        codeEditor.SelectAll();
        codeEditor.SelectionColor = TextWhite;

        foreach (var keyword in keywords)
        {
            int index = 0;
            while ((index = codeEditor.Text.IndexOf(keyword, index, StringComparison.Ordinal)) != -1)
            {
                bool validStart = index == 0 || !char.IsLetterOrDigit(codeEditor.Text[index - 1]);
                bool validEnd = index + keyword.Length >= codeEditor.Text.Length ||
                               !char.IsLetterOrDigit(codeEditor.Text[index + keyword.Length]);

                if (validStart && validEnd)
                {
                    codeEditor.Select(index, keyword.Length);
                    codeEditor.SelectionColor = AccentRed;
                }
                index += keyword.Length;
            }
        }

        codeEditor.SelectionStart = selStart;
        codeEditor.SelectionLength = selLen;
        codeEditor.ResumeLayout();
        isHighlighting = false;
    }

    private void RoundCorners(Control ctrl, int radius)
    {
        var path = new GraphicsPath();
        var rect = new Rectangle(0, 0, ctrl.Width, ctrl.Height);
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        ctrl.Region = new Region(path);
    }

    private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            isDragging = true;
            dragOffset = e.Location;
        }
    }

    private void TitleBar_MouseMove(object? sender, MouseEventArgs e)
    {
        if (isDragging)
        {
            Point currentScreen = PointToScreen(e.Location);
            this.Location = new Point(currentScreen.X - dragOffset.X, currentScreen.Y - dragOffset.Y);
        }
    }

    private void TitleBar_MouseUp(object? sender, MouseEventArgs e)
    {
        isDragging = false;
    }

    private void CodeEditor_VScroll(object? sender, EventArgs e)
    {
        int firstVisibleChar = codeEditor.GetCharIndexFromPosition(new Point(0, 0));
        int firstVisibleLine = codeEditor.GetLineFromCharIndex(firstVisibleChar);

        if (firstVisibleLine >= 0 && firstVisibleLine < lineNumbers.Lines.Length)
        {
            int charIndex = lineNumbers.GetFirstCharIndexFromLine(firstVisibleLine);
            if (charIndex >= 0)
            {
                lineNumbers.SelectionStart = charIndex;
                lineNumbers.ScrollToCaret();
            }
        }
    }

    private void UpdateLineNumbers()
    {
        int lineCount = Math.Max(1, codeEditor.Lines.Length);
        string newText = string.Join("\n", Enumerable.Range(1, lineCount));
        if (lineNumbers.Text != newText)
        {
            lineNumbers.Text = newText;
            lineNumbers.SelectAll();
            lineNumbers.SelectionAlignment = HorizontalAlignment.Right;
            lineNumbers.DeselectAll();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var path = new GraphicsPath();
        int radius = 12;
        var rect = new Rectangle(0, 0, Width, Height);
        path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
        path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
        path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();
        this.Region = new Region(path);
    }
}
