using System.Drawing.Drawing2D;

namespace VertexExecutor;

public class MainForm : Form
{
    // Colors - matching reference exactly
    private static readonly Color BgMain = Color.FromArgb(18, 18, 18);        // Main background
    private static readonly Color BgTabSelected = Color.FromArgb(38, 38, 38); // Selected tab/button background
    private static readonly Color BgConsole = Color.FromArgb(26, 26, 26);     // Console background (slightly lighter)
    private static readonly Color TextGold = Color.FromArgb(212, 175, 55);    // Gold/orange text for active items
    private static readonly Color TextGray = Color.FromArgb(128, 128, 128);   // Gray text for inactive items
    private static readonly Color TextWhite = Color.FromArgb(200, 200, 200);  // White text for code
    private static readonly Color AccentBlue = Color.FromArgb(0, 122, 204);   // Blue for Lua icon
    private static readonly Color KeywordRed = Color.FromArgb(200, 80, 80);   // Red for syntax highlighting

    // Controls
    private RichTextBox lineNumbers = null!;
    private RichTextBox codeEditor = null!;
    private Panel fileTabBar = null!;
    private Panel consolePanel = null!;
    private Label consoleArrow = null!;

    // Tab management
    private List<FileTab> fileTabs = new List<FileTab>();
    private int fileCounter = 1;
    private FileTab? activeFileTab;

    // State
    private bool isDragging = false;
    private Point dragOffset;
    private bool isHighlighting = false;
    private bool consoleExpanded = false;

    private class FileTab
    {
        public Panel TabPanel { get; set; } = null!;
        public Label NameLabel { get; set; } = null!;
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
        this.BackColor = BgMain;
        this.DoubleBuffered = true;
    }

    private void BuildUI()
    {
        // WinForms Dock order: Bottom first, then Top items in REVERSE visual order, then Fill last

        // ========== 1. CONSOLE (Bottom) ==========
        consolePanel = new Panel
        {
            Height = 40,
            Dock = DockStyle.Bottom,
            BackColor = BgConsole
        };

        // Console icon (terminal-like)
        var consoleIcon = new Label
        {
            Text = "▣",
            Location = new Point(15, 10),
            AutoSize = true,
            ForeColor = TextGray,
            Font = new Font("Segoe UI", 11f),
            BackColor = Color.Transparent
        };
        consolePanel.Controls.Add(consoleIcon);

        // Console text
        var consoleLbl = new Label
        {
            Text = "Console",
            Location = new Point(38, 11),
            AutoSize = true,
            ForeColor = TextGray,
            Font = new Font("Segoe UI", 9.5f),
            BackColor = Color.Transparent
        };
        consolePanel.Controls.Add(consoleLbl);

        // Console expand arrow (right side)
        consoleArrow = new Label
        {
            Text = "▽",
            Size = new Size(30, 30),
            Location = new Point(this.ClientSize.Width - 45, 5),
            ForeColor = TextGray,
            Font = new Font("Segoe UI", 12f),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        consoleArrow.Click += ToggleConsole;
        consolePanel.Controls.Add(consoleArrow);

        consolePanel.Cursor = Cursors.Hand;
        consolePanel.Click += ToggleConsole;
        this.Controls.Add(consolePanel);

        // ========== 2. TOOLBAR (Top - will be 3rd row visually) ==========
        var toolbar = new Panel
        {
            Height = 40,
            Dock = DockStyle.Top,
            BackColor = BgMain
        };

        // Toolbar buttons (left side)
        int tbX = 12;
        var toolbarItems = new[] { ("▶", "Execute"), ("🗑", "Clear"), ("📂", "Open"), ("💾", "Save") };
        foreach (var (icon, text) in toolbarItems)
        {
            var btn = CreateToolbarButton(icon, text);
            btn.Location = new Point(tbX, 6);
            if (text == "Clear")
            {
                EventHandler clearHandler = (s, e) => codeEditor?.Clear();
                btn.Click += clearHandler;
                foreach (Control c in btn.Controls) c.Click += clearHandler;
            }
            toolbar.Controls.Add(btn);
            tbX += btn.Width + 8;
        }

        // Attach button (right side)
        var attachBtn = CreateToolbarButton("📎", "Attach");
        attachBtn.Location = new Point(this.ClientSize.Width - attachBtn.Width - 15, 6);
        attachBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        toolbar.Controls.Add(attachBtn);

        // Separator line below toolbar
        var separator = new Panel
        {
            Height = 1,
            Dock = DockStyle.Bottom,
            BackColor = Color.FromArgb(40, 40, 40)
        };
        toolbar.Controls.Add(separator);

        this.Controls.Add(toolbar);

        // ========== 3. FILE TAB BAR (Top - will be 2nd row visually) ==========
        fileTabBar = new Panel
        {
            Height = 36,
            Dock = DockStyle.Top,
            BackColor = BgMain
        };
        this.Controls.Add(fileTabBar);

        // ========== 4. TITLE BAR (Top - will be 1st row visually) ==========
        var titleBar = new Panel
        {
            Height = 48,
            Dock = DockStyle.Top,
            BackColor = BgMain
        };

        // V Logo (replacing rabbit ears with V for Vertex)
        var logo = new Panel
        {
            Size = new Size(36, 36),
            Location = new Point(12, 6),
            BackColor = Color.Transparent
        };
        logo.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(TextGold, 3f);
            // Draw V shape
            e.Graphics.DrawLine(pen, 6, 6, 18, 30);
            e.Graphics.DrawLine(pen, 30, 6, 18, 30);
        };
        logo.MouseDown += TitleBar_MouseDown;
        logo.MouseMove += TitleBar_MouseMove;
        logo.MouseUp += TitleBar_MouseUp;
        titleBar.Controls.Add(logo);

        // Navigation tabs
        int navX = 60;
        var navItems = new[] {
            ("</> ", "Editor", true),
            ("◇ ", "Scripts", false),
            ("⚙ ", "Settings", false),
            ("👤 ", "Profile", false)
        };

        foreach (var (icon, name, selected) in navItems)
        {
            var navBtn = CreateNavButton(icon + name, selected);
            navBtn.Location = new Point(navX, 10);
            titleBar.Controls.Add(navBtn);
            navX += navBtn.Width + 6;
        }

        // Window buttons (right side) - smaller and simpler
        int btnX = this.ClientSize.Width - 40;

        var closeBtn = CreateWindowButton("✕");
        closeBtn.Location = new Point(btnX, 12);
        closeBtn.Click += (s, e) => this.Close();
        foreach (Control c in closeBtn.Controls) c.Click += (s, e) => this.Close();
        titleBar.Controls.Add(closeBtn);

        btnX -= 36;
        var maxBtn = CreateWindowButton("□");
        maxBtn.Location = new Point(btnX, 12);
        EventHandler maxHandler = (s, e) => this.WindowState = this.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
        maxBtn.Click += maxHandler;
        foreach (Control c in maxBtn.Controls) c.Click += maxHandler;
        titleBar.Controls.Add(maxBtn);

        btnX -= 36;
        var minBtn = CreateWindowButton("─");
        minBtn.Location = new Point(btnX, 12);
        EventHandler minHandler = (s, e) => this.WindowState = FormWindowState.Minimized;
        minBtn.Click += minHandler;
        foreach (Control c in minBtn.Controls) c.Click += minHandler;
        titleBar.Controls.Add(minBtn);

        titleBar.MouseDown += TitleBar_MouseDown;
        titleBar.MouseMove += TitleBar_MouseMove;
        titleBar.MouseUp += TitleBar_MouseUp;
        this.Controls.Add(titleBar);

        // ========== 5. EDITOR PANEL (Fill) ==========
        var editorPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgMain,
            Padding = new Padding(0)
        };

        // Line numbers
        lineNumbers = new RichTextBox
        {
            Width = 40,
            Dock = DockStyle.Left,
            BackColor = BgMain,
            ForeColor = TextGray,
            Font = new Font("Consolas", 11f),
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            ScrollBars = RichTextBoxScrollBars.None,
            Text = "1",
            Cursor = Cursors.Arrow,
            Margin = new Padding(10, 0, 0, 0)
        };
        lineNumbers.SelectAll();
        lineNumbers.SelectionAlignment = HorizontalAlignment.Right;
        lineNumbers.DeselectAll();

        // Code editor
        codeEditor = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = BgMain,
            ForeColor = TextWhite,
            Font = new Font("Consolas", 11f),
            BorderStyle = BorderStyle.None,
            AcceptsTab = true,
            WordWrap = false
        };
        codeEditor.TextChanged += CodeEditor_TextChanged;
        codeEditor.VScroll += CodeEditor_VScroll;

        editorPanel.Controls.Add(codeEditor);
        editorPanel.Controls.Add(lineNumbers);
        this.Controls.Add(editorPanel);

        // Create first file tab
        CreateNewFileTab();
    }

    private Panel CreateNavButton(string text, bool selected)
    {
        var btn = new Panel
        {
            Size = new Size(90, 28),
            BackColor = selected ? BgTabSelected : Color.Transparent,
            Cursor = Cursors.Hand
        };

        if (selected)
        {
            // Round corners for selected button
            btn.Region = CreateRoundedRegion(btn.Width, btn.Height, 6);
        }

        var lbl = new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            ForeColor = selected ? TextGold : TextGray,
            Font = new Font("Segoe UI", 9f),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        btn.Controls.Add(lbl);

        if (!selected)
        {
            btn.MouseEnter += (s, e) => { btn.BackColor = Color.FromArgb(30, 30, 30); };
            btn.MouseLeave += (s, e) => { btn.BackColor = Color.Transparent; };
            lbl.MouseEnter += (s, e) => { btn.BackColor = Color.FromArgb(30, 30, 30); };
            lbl.MouseLeave += (s, e) => { btn.BackColor = Color.Transparent; };
        }

        return btn;
    }

    private Panel CreateWindowButton(string text)
    {
        var btn = new Panel
        {
            Size = new Size(32, 24),
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        var lbl = new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            ForeColor = TextGray,
            Font = new Font("Segoe UI", 9f),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        btn.Controls.Add(lbl);

        Color hoverColor = text == "✕" ? Color.FromArgb(200, 50, 50) : Color.FromArgb(50, 50, 50);
        btn.MouseEnter += (s, e) => btn.BackColor = hoverColor;
        btn.MouseLeave += (s, e) => btn.BackColor = Color.Transparent;
        lbl.MouseEnter += (s, e) => btn.BackColor = hoverColor;
        lbl.MouseLeave += (s, e) => btn.BackColor = Color.Transparent;

        return btn;
    }

    private Panel CreateToolbarButton(string icon, string text)
    {
        var btn = new Panel
        {
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

        btn.MouseEnter += (s, e) => { lbl.ForeColor = TextWhite; };
        btn.MouseLeave += (s, e) => { lbl.ForeColor = TextGray; };
        lbl.MouseEnter += (s, e) => { lbl.ForeColor = TextWhite; };
        lbl.MouseLeave += (s, e) => { lbl.ForeColor = TextGray; };

        return btn;
    }

    private void CreateNewFileTab()
    {
        // Save current content
        if (activeFileTab != null)
            activeFileTab.Content = codeEditor.Text;

        var tab = new FileTab
        {
            FileName = $"untitled{fileCounter++}.lua",
            Content = ""
        };

        // Tab panel with rounded corners
        tab.TabPanel = new Panel
        {
            Size = new Size(130, 28),
            BackColor = BgTabSelected,
            Cursor = Cursors.Hand
        };
        tab.TabPanel.Region = CreateRoundedRegion(130, 28, 5);

        // Lua icon (blue lines)
        var luaIcon = new Label
        {
            Text = "≡",
            Location = new Point(10, 5),
            AutoSize = true,
            ForeColor = AccentBlue,
            Font = new Font("Segoe UI", 10f),
            BackColor = Color.Transparent
        };
        tab.TabPanel.Controls.Add(luaIcon);

        // File name
        tab.NameLabel = new Label
        {
            Text = tab.FileName,
            Location = new Point(28, 6),
            AutoSize = true,
            ForeColor = TextGold,
            Font = new Font("Segoe UI", 9f),
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };
        tab.TabPanel.Controls.Add(tab.NameLabel);

        // Close button
        var closeBtn = new Label
        {
            Text = "×",
            Location = new Point(110, 4),
            Size = new Size(16, 20),
            ForeColor = TextGray,
            Font = new Font("Segoe UI", 10f),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        closeBtn.MouseEnter += (s, e) => closeBtn.ForeColor = TextWhite;
        closeBtn.MouseLeave += (s, e) => closeBtn.ForeColor = TextGray;
        closeBtn.Click += (s, e) => CloseFileTab(tab);
        tab.TabPanel.Controls.Add(closeBtn);

        // Click to select
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
        // Save current content
        if (activeFileTab != null)
        {
            activeFileTab.Content = codeEditor.Text;
            activeFileTab.TabPanel.BackColor = Color.FromArgb(30, 30, 30);
            activeFileTab.NameLabel.ForeColor = TextGray;
        }

        activeFileTab = tab;
        tab.TabPanel.BackColor = BgTabSelected;
        tab.NameLabel.ForeColor = TextGold;

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

        int x = 12;
        foreach (var tab in fileTabs)
        {
            tab.TabPanel.Location = new Point(x, 4);
            fileTabBar.Controls.Add(tab.TabPanel);
            x += tab.TabPanel.Width + 8;
        }

        // Plus button for new tab
        var plusBtn = new Label
        {
            Text = "+",
            Location = new Point(x + 4, 6),
            Size = new Size(24, 24),
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

    private void ToggleConsole(object? sender, EventArgs e)
    {
        consoleExpanded = !consoleExpanded;
        consolePanel.Height = consoleExpanded ? 150 : 40;
        consoleArrow.Text = consoleExpanded ? "△" : "▽";
    }

    private Region CreateRoundedRegion(int width, int height, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(0, 0, d, d, 180, 90);
        path.AddArc(width - d, 0, d, d, 270, 90);
        path.AddArc(width - d, height - d, d, d, 0, 90);
        path.AddArc(0, height - d, d, d, 90, 90);
        path.CloseFigure();
        return new Region(path);
    }

    private void HighlightSyntax()
    {
        if (isHighlighting || codeEditor == null) return;
        isHighlighting = true;

        var keywords = new[] { "local", "function", "end", "if", "then", "else", "elseif",
                               "while", "do", "for", "in", "return", "not", "and", "or",
                               "nil", "true", "false", "print", "require", "module" };

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
                    codeEditor.SelectionColor = KeywordRed;
                }
                index += keyword.Length;
            }
        }

        codeEditor.SelectionStart = selStart;
        codeEditor.SelectionLength = selLen;
        codeEditor.ResumeLayout();
        isHighlighting = false;
    }

    #region Event Handlers
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

    private void CodeEditor_TextChanged(object? sender, EventArgs e)
    {
        UpdateLineNumbers();
        HighlightSyntax();
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
    #endregion

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        // Create rounded corners for the entire form
        int radius = 15;
        var path = new GraphicsPath();
        path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
        path.AddArc(Width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
        path.AddArc(Width - radius * 2, Height - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(0, Height - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();
        this.Region = new Region(path);
    }
}
