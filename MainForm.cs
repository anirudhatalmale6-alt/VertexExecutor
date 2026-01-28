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
    private Panel titleBar;
    private Panel navBar;
    private Panel fileTabBar;
    private Panel toolbar;
    private Panel editorPanel;
    private Panel consolePanel;
    private RichTextBox lineNumbers;
    private RichTextBox codeEditor;
    private Label consoleLabel;
    private bool consoleExpanded = false;

    // Tab management
    private List<FileTab> fileTabs = new List<FileTab>();
    private int fileCounter = 1;
    private FileTab? activeFileTab;

    // State
    private bool isDragging = false;
    private Point dragOffset;
    private bool isHighlighting = false;
    private string selectedNav = "Editor";

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
        CreateTitleBar();
        CreateNavBar();
        CreateFileTabBar();
        CreateToolbar();
        CreateEditorPanel();
        CreateConsolePanel();
        CreateNewFileTab();
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

    private void CreateTitleBar()
    {
        titleBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40,
            BackColor = BgDark
        };

        // V Logo (gold colored)
        var logoPanel = new Panel
        {
            Size = new Size(36, 36),
            Location = new Point(8, 2),
            BackColor = Color.Transparent
        };
        logoPanel.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(AccentGold, 3f);
            // Draw V shape
            e.Graphics.DrawLine(pen, 6, 8, 18, 28);
            e.Graphics.DrawLine(pen, 30, 8, 18, 28);
        };
        logoPanel.MouseDown += TitleBar_MouseDown;
        logoPanel.MouseMove += TitleBar_MouseMove;
        logoPanel.MouseUp += TitleBar_MouseUp;
        titleBar.Controls.Add(logoPanel);

        // Window buttons (right side)
        var closeBtn = CreateWindowButton("✕", BgDark, Color.FromArgb(200, 50, 50));
        closeBtn.Location = new Point(this.ClientSize.Width - 46, 0);
        closeBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        EventHandler closeHandler = (s, e) => this.Close();
        closeBtn.Click += closeHandler;
        foreach (Control c in closeBtn.Controls) c.Click += closeHandler;
        titleBar.Controls.Add(closeBtn);

        var maxBtn = CreateWindowButton("□", BgDark, BgLight);
        maxBtn.Location = new Point(this.ClientSize.Width - 92, 0);
        maxBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        EventHandler maxHandler = (s, e) => {
            this.WindowState = this.WindowState == FormWindowState.Maximized
                ? FormWindowState.Normal
                : FormWindowState.Maximized;
        };
        maxBtn.Click += maxHandler;
        foreach (Control c in maxBtn.Controls) c.Click += maxHandler;
        titleBar.Controls.Add(maxBtn);

        var minBtn = CreateWindowButton("─", BgDark, BgLight);
        minBtn.Location = new Point(this.ClientSize.Width - 138, 0);
        minBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        EventHandler minHandler = (s, e) => this.WindowState = FormWindowState.Minimized;
        minBtn.Click += minHandler;
        foreach (Control c in minBtn.Controls) c.Click += minHandler;
        titleBar.Controls.Add(minBtn);

        // Drag functionality
        titleBar.MouseDown += TitleBar_MouseDown;
        titleBar.MouseMove += TitleBar_MouseMove;
        titleBar.MouseUp += TitleBar_MouseUp;

        this.Controls.Add(titleBar);
    }

    private Panel CreateWindowButton(string text, Color normalBg, Color hoverBg)
    {
        var btn = new Panel
        {
            Size = new Size(46, 40),
            BackColor = normalBg,
            Cursor = Cursors.Hand,
            Tag = text // Store text to identify button
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
        btn.MouseEnter += (s, e) => btn.BackColor = hoverBg;
        btn.MouseLeave += (s, e) => btn.BackColor = normalBg;
        lbl.MouseEnter += (s, e) => btn.BackColor = hoverBg;
        lbl.MouseLeave += (s, e) => btn.BackColor = normalBg;
        return btn;
    }

    private void CreateNavBar()
    {
        navBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = BgDark
        };

        int x = 50;
        var navItems = new[] {
            ("</> ", "Editor"),
            ("◇ ", "Scripts"),
            ("⚙ ", "Settings"),
            ("👤 ", "Profile")
        };

        foreach (var (icon, name) in navItems)
        {
            var navBtn = CreateNavButton(icon + name, x, name == selectedNav);
            navBtn.Tag = name;
            navBtn.Click += NavButton_Click;
            foreach (Control c in navBtn.Controls) c.Click += NavButton_Click;
            navBar.Controls.Add(navBtn);
            x += navBtn.Width + 5;
        }

        this.Controls.Add(navBar);
    }

    private Panel CreateNavButton(string text, int x, bool selected)
    {
        var btn = new Panel
        {
            Location = new Point(x, 4),
            Size = new Size(90, 28),
            BackColor = selected ? BgLight : Color.Transparent,
            Cursor = Cursors.Hand
        };
        if (selected) RoundCorners(btn, 6);

        var lbl = new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            ForeColor = selected ? AccentGold : TextGray,
            Font = new Font("Segoe UI", 9f),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        btn.Controls.Add(lbl);

        if (!selected)
        {
            btn.MouseEnter += (s, e) => { btn.BackColor = BgMedium; lbl.ForeColor = TextWhite; };
            btn.MouseLeave += (s, e) => { btn.BackColor = Color.Transparent; lbl.ForeColor = TextGray; };
            lbl.MouseEnter += (s, e) => { btn.BackColor = BgMedium; lbl.ForeColor = TextWhite; };
            lbl.MouseLeave += (s, e) => { btn.BackColor = Color.Transparent; lbl.ForeColor = TextGray; };
        }

        return btn;
    }

    private void NavButton_Click(object? sender, EventArgs e)
    {
        // For now, just visual feedback - could expand later
        var ctrl = sender as Control;
        if (ctrl?.Tag != null)
        {
            selectedNav = ctrl.Tag.ToString()!;
        }
        else if (ctrl?.Parent?.Tag != null)
        {
            selectedNav = ctrl.Parent.Tag.ToString()!;
        }
        RefreshNavBar();
    }

    private void RefreshNavBar()
    {
        navBar.Controls.Clear();
        int x = 50;
        var navItems = new[] {
            ("</> ", "Editor"),
            ("◇ ", "Scripts"),
            ("⚙ ", "Settings"),
            ("👤 ", "Profile")
        };

        foreach (var (icon, name) in navItems)
        {
            var navBtn = CreateNavButton(icon + name, x, name == selectedNav);
            navBtn.Tag = name;
            navBtn.Click += NavButton_Click;
            foreach (Control c in navBtn.Controls) c.Click += NavButton_Click;
            navBar.Controls.Add(navBtn);
            x += navBtn.Width + 5;
        }
    }

    private void CreateFileTabBar()
    {
        fileTabBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 32,
            BackColor = BgDark
        };

        this.Controls.Add(fileTabBar);
    }

    private void CreateNewFileTab()
    {
        // Save current content
        if (activeFileTab != null)
        {
            activeFileTab.Content = codeEditor.Text;
        }

        var tab = new FileTab
        {
            FileName = $"untitled{fileCounter++}.lua",
            Content = ""
        };

        // Tab panel
        tab.TabPanel = new Panel
        {
            Size = new Size(130, 26),
            BackColor = BgLight,
            Cursor = Cursors.Hand
        };
        RoundCorners(tab.TabPanel, 4);

        // Lua icon
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

        // File name
        tab.NameLabel = new Label
        {
            Text = tab.FileName,
            Location = new Point(26, 5),
            AutoSize = true,
            ForeColor = AccentGold,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 9f),
            Cursor = Cursors.Hand
        };
        tab.TabPanel.Controls.Add(tab.NameLabel);

        // Close X
        tab.CloseBtn = new Label
        {
            Text = "×",
            Location = new Point(110, 3),
            Size = new Size(16, 20),
            ForeColor = TextGray,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 10f),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        tab.CloseBtn.MouseEnter += (s, e) => tab.CloseBtn.ForeColor = TextWhite;
        tab.CloseBtn.MouseLeave += (s, e) => tab.CloseBtn.ForeColor = TextGray;
        tab.CloseBtn.Click += (s, e) => CloseFileTab(tab);
        tab.TabPanel.Controls.Add(tab.CloseBtn);

        // Click to select
        tab.TabPanel.Click += (s, e) => SelectFileTab(tab);
        tab.NameLabel.Click += (s, e) => SelectFileTab(tab);
        luaIcon.Click += (s, e) => SelectFileTab(tab);

        fileTabs.Add(tab);
        RefreshFileTabBar();
        SelectFileTab(tab);
    }

    private void SelectFileTab(FileTab tab)
    {
        // Save current
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
        fileTabBar.Controls.Remove(tab.TabPanel);

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

        // Plus button
        var plusBtn = new Label
        {
            Text = "+",
            Location = new Point(x + 5, 5),
            Size = new Size(24, 22),
            ForeColor = TextGray,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 12f),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        plusBtn.MouseEnter += (s, e) => plusBtn.ForeColor = TextWhite;
        plusBtn.MouseLeave += (s, e) => plusBtn.ForeColor = TextGray;
        plusBtn.Click += (s, e) => CreateNewFileTab();
        fileTabBar.Controls.Add(plusBtn);
    }

    private void CreateToolbar()
    {
        toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = BgDark
        };

        // Left buttons
        int x = 8;
        var leftButtons = new[] {
            ("▶", "Execute"),
            ("🗑", "Clear"),
            ("📁", "Open"),
            ("💾", "Save")
        };

        foreach (var (icon, text) in leftButtons)
        {
            var btn = CreateToolbarButton(icon, text, x);
            toolbar.Controls.Add(btn);
            x += btn.Width + 5;
        }

        // Right button (Attach)
        var attachBtn = CreateToolbarButton("📎", "Attach", this.ClientSize.Width - 90);
        attachBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        toolbar.Controls.Add(attachBtn);

        this.Controls.Add(toolbar);
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
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 9f),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        btn.Controls.Add(lbl);

        btn.MouseEnter += (s, e) => { btn.BackColor = BgMedium; lbl.ForeColor = TextWhite; };
        btn.MouseLeave += (s, e) => { btn.BackColor = Color.Transparent; lbl.ForeColor = TextGray; };
        lbl.MouseEnter += (s, e) => { btn.BackColor = BgMedium; lbl.ForeColor = TextWhite; };
        lbl.MouseLeave += (s, e) => { btn.BackColor = Color.Transparent; lbl.ForeColor = TextGray; };

        if (text == "Clear")
        {
            lbl.Click += (s, e) => codeEditor.Clear();
            btn.Click += (s, e) => codeEditor.Clear();
        }

        return btn;
    }

    private void CreateEditorPanel()
    {
        editorPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgDark,
            Padding = new Padding(0)
        };

        // Line numbers
        lineNumbers = new RichTextBox
        {
            Location = new Point(0, 0),
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

        // Code editor
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
        codeEditor.TextChanged += CodeEditor_TextChanged;
        codeEditor.VScroll += CodeEditor_VScroll;

        editorPanel.Controls.Add(codeEditor);
        editorPanel.Controls.Add(lineNumbers);

        this.Controls.Add(editorPanel);
    }

    private void CreateConsolePanel()
    {
        consolePanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 36,
            BackColor = BgMedium
        };

        // Console icon and label
        var iconLabel = new Label
        {
            Text = "▣",
            Location = new Point(12, 8),
            AutoSize = true,
            ForeColor = TextGray,
            Font = new Font("Segoe UI", 10f),
            BackColor = Color.Transparent
        };
        consolePanel.Controls.Add(iconLabel);

        consoleLabel = new Label
        {
            Text = "Console",
            Location = new Point(32, 9),
            AutoSize = true,
            ForeColor = TextGray,
            Font = new Font("Segoe UI", 9.5f),
            BackColor = Color.Transparent
        };
        consolePanel.Controls.Add(consoleLabel);

        // Expand/collapse arrow
        var arrowLabel = new Label
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
        arrowLabel.Click += (s, e) => ToggleConsole(arrowLabel);
        consolePanel.Controls.Add(arrowLabel);

        // Make whole panel clickable
        consolePanel.Click += (s, e) => ToggleConsole(arrowLabel);
        consolePanel.Cursor = Cursors.Hand;

        this.Controls.Add(consolePanel);
    }

    private void ToggleConsole(Label arrow)
    {
        consoleExpanded = !consoleExpanded;
        if (consoleExpanded)
        {
            consolePanel.Height = 150;
            arrow.Text = "▲";
        }
        else
        {
            consolePanel.Height = 36;
            arrow.Text = "▼";
        }
    }

    private void HighlightSyntax()
    {
        if (isHighlighting) return;
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

    #region Title Bar Dragging
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
    #endregion

    #region Editor Events
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
        // Rounded corners for the form
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
