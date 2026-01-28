using System.Drawing.Drawing2D;

namespace VertexExecutor;

public class MainForm : Form
{
    // Colors from reference
    private static readonly Color BgDark = Color.FromArgb(22, 22, 22);
    private static readonly Color BgEditor = Color.FromArgb(30, 30, 30);
    private static readonly Color TextWhite = Color.FromArgb(210, 210, 210);
    private static readonly Color TextGray = Color.FromArgb(90, 90, 90);
    private static readonly Color AccentRed = Color.FromArgb(230, 90, 80);
    private static readonly Color TabBg = Color.FromArgb(38, 38, 38);

    // Controls
    private Panel titleBar;
    private Panel tabBar;
    private Panel editorPanel;
    private Panel toolbarPanel;
    private RichTextBox lineNumbers;
    private RichTextBox codeEditor;
    private Panel minimapPanel;

    // Tab management
    private List<TabInfo> tabs = new List<TabInfo>();
    private int tabCounter = 1;
    private TabInfo? activeTab;

    // State
    private bool isDragging = false;
    private Point dragOffset;

    private class TabInfo
    {
        public Panel TabPanel { get; set; } = null!;
        public Label TabLabel { get; set; } = null!;
        public Label CloseBtn { get; set; } = null!;
        public string Content { get; set; } = "";
        public string Title { get; set; } = "";
    }

    public MainForm()
    {
        InitializeForm();
        CreateTitleBar();
        CreateTabBar();
        CreateEditorPanel();
        CreateToolbar();
        CreateNewTab(); // Create first tab
    }

    private void InitializeForm()
    {
        this.Text = "Vertex";
        this.Size = new Size(900, 650);
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
            Size = new Size(this.ClientSize.Width, 42),
            Location = new Point(0, 0),
            BackColor = BgDark
        };
        titleBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        // Logo V (red lines forming V shape)
        var logoPanel = new Panel
        {
            Size = new Size(32, 32),
            Location = new Point(12, 5),
            BackColor = Color.Transparent
        };
        logoPanel.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(AccentRed, 2.5f);
            // Draw V shape
            e.Graphics.DrawLine(pen, 4, 6, 16, 26);
            e.Graphics.DrawLine(pen, 28, 6, 16, 26);
        };
        titleBar.Controls.Add(logoPanel);

        // Close button (X) - only this and minimize
        var closeBtn = CreateTitleBarButton("✕", this.ClientSize.Width - 46, 0, 46, 42);
        closeBtn.Click += (s, e) => this.Close();
        closeBtn.MouseEnter += (s, e) => closeBtn.BackColor = Color.FromArgb(200, 50, 50);
        closeBtn.MouseLeave += (s, e) => closeBtn.BackColor = Color.Transparent;
        closeBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        titleBar.Controls.Add(closeBtn);

        // Minimize button - positioned next to close
        var minBtn = CreateTitleBarButton("─", this.ClientSize.Width - 92, 0, 46, 42);
        minBtn.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
        minBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        titleBar.Controls.Add(minBtn);

        // Drag functionality
        titleBar.MouseDown += TitleBar_MouseDown;
        titleBar.MouseMove += TitleBar_MouseMove;
        titleBar.MouseUp += TitleBar_MouseUp;
        logoPanel.MouseDown += TitleBar_MouseDown;
        logoPanel.MouseMove += TitleBar_MouseMove;
        logoPanel.MouseUp += TitleBar_MouseUp;

        this.Controls.Add(titleBar);
    }

    private Button CreateTitleBarButton(string text, int x, int y, int width, int height)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, height),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = TextGray,
            Font = new Font("Segoe UI", 10f),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 50, 50);
        btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(60, 60, 60);
        return btn;
    }

    private void CreateTabBar()
    {
        tabBar = new Panel
        {
            Size = new Size(this.ClientSize.Width, 36),
            Location = new Point(0, 42),
            BackColor = BgDark
        };
        tabBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        this.Controls.Add(tabBar);
    }

    private void CreateNewTab()
    {
        // Save current tab content
        if (activeTab != null)
        {
            activeTab.Content = codeEditor.Text;
        }

        var tabInfo = new TabInfo
        {
            Title = $"New Tab {tabCounter++}"
        };

        // Set default content for new tab
        tabInfo.Content = @"local passes, fails, undefined = 0, 0, 0
local running = 0

local function getGlobal(path)
    local value = getfenv(0)

    while value ~= nil and path ~= """" do
        local name, nextValue = string.match(path, ""^([^.]+)%.?(.*)$"")
        value = value[name]
        path = nextValue
    end

    return value
end

local function test(name, aliases, callback)
    running += 1

    task.spawn(function()
        if not callback then
            print(""skip"" .. name)
        elseif not getGlobal(name) then
            fails += 1
            warn(""fail"" .. name)
        else
            passes += 1
        end
    end)
end";

        // Tab panel
        tabInfo.TabPanel = new Panel
        {
            Size = new Size(130, 28),
            BackColor = TabBg,
            Cursor = Cursors.Hand
        };
        RoundCorners(tabInfo.TabPanel, 4);

        // Orange/red circle
        var circle = new Panel
        {
            Size = new Size(12, 12),
            Location = new Point(10, 8),
            BackColor = AccentRed
        };
        RoundCorners(circle, 6);
        tabInfo.TabPanel.Controls.Add(circle);

        // Tab text
        tabInfo.TabLabel = new Label
        {
            Text = tabInfo.Title,
            Location = new Point(26, 6),
            AutoSize = true,
            ForeColor = TextWhite,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 9f),
            Cursor = Cursors.Hand
        };
        tabInfo.TabPanel.Controls.Add(tabInfo.TabLabel);

        // Close X
        tabInfo.CloseBtn = new Label
        {
            Text = "×",
            Location = new Point(108, 4),
            Size = new Size(18, 20),
            ForeColor = TextGray,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 11f),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        tabInfo.CloseBtn.MouseEnter += (s, e) => tabInfo.CloseBtn.ForeColor = TextWhite;
        tabInfo.CloseBtn.MouseLeave += (s, e) => tabInfo.CloseBtn.ForeColor = TextGray;
        tabInfo.CloseBtn.Click += (s, e) => CloseTab(tabInfo);
        tabInfo.TabPanel.Controls.Add(tabInfo.CloseBtn);

        // Click to select tab
        tabInfo.TabPanel.Click += (s, e) => SelectTab(tabInfo);
        tabInfo.TabLabel.Click += (s, e) => SelectTab(tabInfo);
        circle.Click += (s, e) => SelectTab(tabInfo);

        tabs.Add(tabInfo);
        RefreshTabBar();
        SelectTab(tabInfo);
    }

    private void SelectTab(TabInfo tab)
    {
        // Save current content
        if (activeTab != null)
        {
            activeTab.Content = codeEditor.Text;
            activeTab.TabPanel.BackColor = Color.FromArgb(28, 28, 28);
        }

        activeTab = tab;
        tab.TabPanel.BackColor = TabBg;

        // Load tab content
        codeEditor.Text = tab.Content;
        HighlightSyntax();
        UpdateLineNumbers();
        minimapPanel?.Invalidate(); // Refresh minimap
    }

    private void CloseTab(TabInfo tab)
    {
        if (tabs.Count <= 1)
        {
            // Don't close last tab, just clear it
            codeEditor.Clear();
            return;
        }

        int index = tabs.IndexOf(tab);
        tabs.Remove(tab);
        tabBar.Controls.Remove(tab.TabPanel);

        if (activeTab == tab)
        {
            // Select another tab
            int newIndex = Math.Min(index, tabs.Count - 1);
            SelectTab(tabs[newIndex]);
        }

        RefreshTabBar();
    }

    private void RefreshTabBar()
    {
        // Clear tab bar
        tabBar.Controls.Clear();

        // Add tabs
        int x = 12;
        foreach (var tab in tabs)
        {
            tab.TabPanel.Location = new Point(x, 4);
            tabBar.Controls.Add(tab.TabPanel);
            x += 135;
        }

        // Add plus button
        var plusBtn = new Label
        {
            Text = "+",
            Location = new Point(x + 5, 6),
            Size = new Size(24, 24),
            ForeColor = TextGray,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 14f),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        plusBtn.MouseEnter += (s, e) => plusBtn.ForeColor = TextWhite;
        plusBtn.MouseLeave += (s, e) => plusBtn.ForeColor = TextGray;
        plusBtn.Click += (s, e) => CreateNewTab();
        tabBar.Controls.Add(plusBtn);
    }

    private void CreateEditorPanel()
    {
        editorPanel = new Panel
        {
            Location = new Point(0, 78),
            Size = new Size(this.ClientSize.Width, this.ClientSize.Height - 78 - 48),
            BackColor = BgEditor
        };
        editorPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        // Line numbers
        lineNumbers = new RichTextBox
        {
            Location = new Point(0, 0),
            Size = new Size(55, editorPanel.Height),
            BackColor = BgEditor,
            ForeColor = TextGray,
            Font = new Font("Consolas", 11f),
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            ScrollBars = RichTextBoxScrollBars.None,
            Text = string.Join("\n", Enumerable.Range(1, 30)),
            Cursor = Cursors.Arrow
        };
        lineNumbers.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
        lineNumbers.SelectAll();
        lineNumbers.SelectionAlignment = HorizontalAlignment.Right;
        lineNumbers.DeselectAll();
        editorPanel.Controls.Add(lineNumbers);

        // Code editor
        codeEditor = new RichTextBox
        {
            Location = new Point(55, 0),
            Size = new Size(editorPanel.Width - 55, editorPanel.Height),
            BackColor = BgEditor,
            ForeColor = TextWhite,
            Font = new Font("Consolas", 11f),
            BorderStyle = BorderStyle.None,
            AcceptsTab = true,
            WordWrap = false,
            ScrollBars = RichTextBoxScrollBars.Vertical
        };
        codeEditor.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        codeEditor.TextChanged += CodeEditor_TextChanged;
        codeEditor.VScroll += CodeEditor_VScroll;

        editorPanel.Controls.Add(codeEditor);

        // Minimap panel (code preview on the right)
        minimapPanel = new Panel
        {
            Size = new Size(120, editorPanel.Height),
            Location = new Point(editorPanel.Width - 120, 0),
            BackColor = Color.FromArgb(25, 25, 25)
        };
        minimapPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
        minimapPanel.Paint += MinimapPanel_Paint;
        editorPanel.Controls.Add(minimapPanel);
        minimapPanel.BringToFront();

        // Adjust code editor width to make room for minimap
        codeEditor.Size = new Size(editorPanel.Width - 55 - 120, editorPanel.Height);

        this.Controls.Add(editorPanel);
    }

    private void MinimapPanel_Paint(object? sender, PaintEventArgs e)
    {
        if (codeEditor == null || string.IsNullOrEmpty(codeEditor.Text)) return;

        var g = e.Graphics;
        g.Clear(Color.FromArgb(25, 25, 25));

        string[] lines = codeEditor.Text.Split('\n');
        float lineHeight = 2.5f; // Very small line height for minimap
        float charWidth = 0.8f;  // Very small char width
        float y = 5;
        float maxWidth = minimapPanel.Width - 10;

        // Keywords for highlighting
        var keywords = new HashSet<string> { "local", "function", "end", "if", "then", "else", "elseif",
                                              "while", "do", "for", "in", "return", "not", "and", "or", "nil", "true", "false" };

        using var whiteBrush = new SolidBrush(Color.FromArgb(150, 150, 150));
        using var redBrush = new SolidBrush(Color.FromArgb(200, 80, 70));

        foreach (var line in lines)
        {
            if (y > minimapPanel.Height - 5) break;

            float x = 5;

            // Simple rendering - draw small rectangles for each word
            string[] words = line.Split(new[] { ' ', '\t', '(', ')', ',', '.', '=', '+', '-', '*', '/', '[', ']', '{', '}', '"', '\'' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in words)
            {
                if (x > maxWidth) break;

                float wordWidth = word.Length * charWidth;
                var brush = keywords.Contains(word) ? redBrush : whiteBrush;

                g.FillRectangle(brush, x, y, Math.Min(wordWidth, maxWidth - x), lineHeight);
                x += wordWidth + charWidth * 2;
            }

            y += lineHeight + 1;
        }

        // Draw viewport indicator (rectangle showing visible area)
        if (codeEditor.Lines.Length > 0)
        {
            int firstVisibleChar = codeEditor.GetCharIndexFromPosition(new Point(0, 0));
            int firstVisibleLine = codeEditor.GetLineFromCharIndex(firstVisibleChar);

            int visibleLines = codeEditor.Height / codeEditor.Font.Height;
            int totalLines = codeEditor.Lines.Length;

            if (totalLines > 0)
            {
                float viewportY = 5 + (firstVisibleLine * (lineHeight + 1));
                float viewportHeight = visibleLines * (lineHeight + 1);

                using var viewportBrush = new SolidBrush(Color.FromArgb(40, 255, 255, 255));
                g.FillRectangle(viewportBrush, 0, viewportY, minimapPanel.Width, viewportHeight);
            }
        }
    }

    private void CreateToolbar()
    {
        toolbarPanel = new Panel
        {
            Location = new Point(0, this.ClientSize.Height - 48),
            Size = new Size(this.ClientSize.Width, 48),
            BackColor = BgEditor
        };
        toolbarPanel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        // Create buttons
        var buttons = new[] {
            ("▷", "Execute"),
            ("🗑", "Clear"),
            ("📂", "Open"),
            ("💾", "Save"),
            ("📡", "Attach"),
            ("⊗", "Kill")
        };

        int totalWidth = buttons.Length * 95;
        int startX = (this.ClientSize.Width - totalWidth) / 2;

        for (int i = 0; i < buttons.Length; i++)
        {
            var (icon, text) = buttons[i];
            var btn = CreateToolbarButton(icon, text, startX + i * 95);
            toolbarPanel.Controls.Add(btn);
        }

        toolbarPanel.Resize += (s, e) =>
        {
            int newStartX = (toolbarPanel.Width - totalWidth) / 2;
            for (int i = 0; i < toolbarPanel.Controls.Count; i++)
            {
                toolbarPanel.Controls[i].Location = new Point(newStartX + i * 95, 10);
            }
        };

        this.Controls.Add(toolbarPanel);
    }

    private Panel CreateToolbarButton(string icon, string text, int x)
    {
        var panel = new Panel
        {
            Location = new Point(x, 10),
            Size = new Size(88, 28),
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };

        var label = new Label
        {
            Text = $"{icon}  {text}",
            Dock = DockStyle.Fill,
            ForeColor = TextWhite,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 9.5f),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };

        panel.Controls.Add(label);

        panel.MouseEnter += (s, e) => panel.BackColor = Color.FromArgb(50, 50, 50);
        panel.MouseLeave += (s, e) => panel.BackColor = Color.Transparent;
        label.MouseEnter += (s, e) => panel.BackColor = Color.FromArgb(50, 50, 50);
        label.MouseLeave += (s, e) => panel.BackColor = Color.Transparent;

        if (text == "Clear")
        {
            label.Click += (s, e) => codeEditor.Clear();
            panel.Click += (s, e) => codeEditor.Clear();
        }

        return panel;
    }

    private void HighlightSyntax()
    {
        var keywords = new[] { "local", "function", "end", "if", "then", "else", "elseif",
                               "while", "do", "for", "in", "return", "not", "and", "or", "nil", "true", "false" };

        int selStart = codeEditor.SelectionStart;
        int selLen = codeEditor.SelectionLength;

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
        minimapPanel?.Invalidate(); // Refresh minimap
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

        minimapPanel?.Invalidate(); // Refresh minimap viewport indicator
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
        using var pen = new Pen(Color.FromArgb(45, 45, 45), 1);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }
}
