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

    // State
    private bool isDragging = false;
    private Point dragOffset;

    public MainForm()
    {
        InitializeForm();
        CreateTitleBar();
        CreateTabBar();
        CreateEditorPanel();
        CreateToolbar();
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

        // Close button (X)
        var closeBtn = CreateTitleBarButton("✕", this.ClientSize.Width - 46, 0, 46, 42);
        closeBtn.Click += (s, e) => this.Close();
        closeBtn.MouseEnter += (s, e) => closeBtn.BackColor = Color.FromArgb(200, 50, 50);
        closeBtn.MouseLeave += (s, e) => closeBtn.BackColor = Color.Transparent;
        closeBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        titleBar.Controls.Add(closeBtn);

        // Maximize button
        var maxBtn = CreateTitleBarButton("□", this.ClientSize.Width - 92, 0, 46, 42);
        maxBtn.Click += (s, e) => ToggleMaximize();
        maxBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        titleBar.Controls.Add(maxBtn);

        // Minimize button
        var minBtn = CreateTitleBarButton("─", this.ClientSize.Width - 138, 0, 46, 42);
        minBtn.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
        minBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        titleBar.Controls.Add(minBtn);

        // Settings button
        var settingsBtn = CreateTitleBarButton("⚙", this.ClientSize.Width - 184, 0, 46, 42);
        settingsBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        titleBar.Controls.Add(settingsBtn);

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

        // Tab: circle + "New Tab 1" + X
        var tab = new Panel
        {
            Size = new Size(130, 28),
            Location = new Point(12, 4),
            BackColor = TabBg
        };
        RoundCorners(tab, 4);

        // Orange/red circle
        var circle = new Panel
        {
            Size = new Size(12, 12),
            Location = new Point(10, 8),
            BackColor = AccentRed
        };
        RoundCorners(circle, 6);
        tab.Controls.Add(circle);

        // Tab text
        var tabText = new Label
        {
            Text = "New Tab 1",
            Location = new Point(26, 6),
            AutoSize = true,
            ForeColor = TextWhite,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 9f)
        };
        tab.Controls.Add(tabText);

        // Close X
        var tabClose = new Label
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
        tabClose.MouseEnter += (s, e) => tabClose.ForeColor = TextWhite;
        tabClose.MouseLeave += (s, e) => tabClose.ForeColor = TextGray;
        tab.Controls.Add(tabClose);

        tabBar.Controls.Add(tab);

        // Plus button
        var plusBtn = new Label
        {
            Text = "+",
            Location = new Point(150, 6),
            Size = new Size(24, 24),
            ForeColor = TextGray,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 14f),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        plusBtn.MouseEnter += (s, e) => plusBtn.ForeColor = TextWhite;
        plusBtn.MouseLeave += (s, e) => plusBtn.ForeColor = TextGray;
        tabBar.Controls.Add(plusBtn);

        this.Controls.Add(tabBar);
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

        // Sample code like in reference
        codeEditor.Text = @"local passes, fails, undefined = 0, 0, 0
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

        HighlightSyntax();
        editorPanel.Controls.Add(codeEditor);

        this.Controls.Add(editorPanel);
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

    private void ToggleMaximize()
    {
        if (this.WindowState == FormWindowState.Maximized)
            this.WindowState = FormWindowState.Normal;
        else
            this.WindowState = FormWindowState.Maximized;
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
        using var pen = new Pen(Color.FromArgb(45, 45, 45), 1);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }
}
