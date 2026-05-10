class MainForm : Form
{
    static readonly Color BgDark    = Color.FromArgb(15,  15,  20);
    static readonly Color BgPanel   = Color.FromArgb(26,  26,  34);
    static readonly Color BgHover   = Color.FromArgb(36,  36,  50);
    static readonly Color Accent    = Color.FromArgb(124, 92,  252);
    static readonly Color Accent2   = Color.FromArgb(192, 132, 252);
    static readonly Color TextMain  = Color.FromArgb(226, 232, 240);
    static readonly Color TextMuted = Color.FromArgb(100, 116, 139);
    static readonly Color Danger    = Color.FromArgb(239, 68,  68);
    static readonly Color Success   = Color.FromArgb(34,  197, 94);
    static readonly Color Border    = Color.FromArgb(45,  45,  61);

    ListBox   _list        = null!;
    Label     _statusDot   = null!;
    Label     _statusLabel = null!;
    Button    _btnToggle   = null!;
    CheckBox  _chkAutostart= null!;
    System.Windows.Forms.Timer _timer = null!;

    public MainForm()
    {
        Text            = "ProcessSuspender";
        Size            = new Size(640, 560);
        MinimumSize     = new Size(520, 460);
        BackColor       = BgDark;
        ForeColor       = TextMain;
        Font            = new Font("Segoe UI", 10f);
        StartPosition   = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;

        BuildUI();
        Refresh2();

        _timer          = new System.Windows.Forms.Timer { Interval = 3000 };
        _timer.Tick    += (_, _) => Refresh2();
        _timer.Start();
    }

    void BuildUI()
    {
        var lblTitle = MakeLabel("⏸  ProcessSuspender", 15, bold: true);
        lblTitle.ForeColor = Accent2;
        lblTitle.Location  = new Point(24, 20);
        lblTitle.AutoSize  = true;

        _statusDot = MakeLabel("●", 14);
        _statusDot.ForeColor = TextMuted;
        _statusDot.Anchor    = AnchorStyles.Top | AnchorStyles.Right;

        _statusLabel = MakeLabel("verificando…", 9);
        _statusLabel.ForeColor = TextMuted;
        _statusLabel.Anchor    = AnchorStyles.Top | AnchorStyles.Right;

        var sep = new Panel { BackColor = Border, Height = 1 };
        sep.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        var panel = new Panel
        {
            BackColor = BgPanel,
            Padding   = new Padding(18, 14, 18, 14),
        };
        panel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        var lblPanelTitle = MakeLabel("Monitor em background", 10, bold: true, parent: panel);
        lblPanelTitle.Location = new Point(18, 14);

        var lblPanelSub = MakeLabel(
            "Mantém processos suspensos mesmo após fechar este programa.", 9, parent: panel);
        lblPanelSub.ForeColor = TextMuted;
        lblPanelSub.Location  = new Point(18, 36);

        _btnToggle = MakeButton("▶  Iniciar monitor", Accent, ToggleMonitor);
        _btnToggle.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        panel.Controls.Add(_btnToggle);

        _chkAutostart = new CheckBox
        {
            Text            = "  Iniciar monitor automaticamente com o Windows",
            ForeColor       = TextMuted,
            BackColor       = BgDark,
            Font            = new Font("Segoe UI", 9f),
            Checked         = Startup.IsEnabled(),
            Cursor          = Cursors.Hand,
            AutoSize        = true,
        };
        _chkAutostart.Anchor        = AnchorStyles.Top | AnchorStyles.Left;
        _chkAutostart.CheckedChanged += (_, _) =>
        {
            if (_chkAutostart.Checked) Startup.Enable();
            else                       Startup.Disable();
        };

        var lblList = MakeLabel("Processos monitorados", 10, bold: true);
        lblList.Anchor = AnchorStyles.Top | AnchorStyles.Left;

        var btnAdd = MakeButton("＋  Adicionar", Accent, AddProcess, small: true);
        btnAdd.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        _list = new ListBox
        {
            BackColor        = BgPanel,
            ForeColor        = TextMain,
            Font             = new Font("Consolas", 10f),
            BorderStyle      = BorderStyle.None,
            SelectionMode    = SelectionMode.One,
            Cursor           = Cursors.Hand,
            IntegralHeight   = false,
            ItemHeight       = 28,
            DrawMode         = DrawMode.OwnerDrawFixed,
        };
        _list.DrawItem += DrawListItem;
        _list.Anchor    = AnchorStyles.Top | AnchorStyles.Bottom
                        | AnchorStyles.Left | AnchorStyles.Right;

        var btnRemove = MakeButton("🗑  Remover", Danger, RemoveProcess, small: true);
        btnRemove.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

        var btnResume = MakeButton("▶  Retomar agora", Success, ResumeProcess, small: true);
        btnResume.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

        var btnLog = MakeButton("📋  Ver log", Color.FromArgb(71, 85, 105), ViewLog, small: true);
        btnLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

        var tbl = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 1,
            RowCount    = 9,
            BackColor   = BgDark,
            Padding     = new Padding(24, 0, 24, 16),
        };
        tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));
        tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
        tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
        tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        var rowTitle = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 1,
            BackColor   = BgDark,
            Margin      = Padding.Empty,
        };
        rowTitle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        rowTitle.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var statusPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock          = DockStyle.Fill,
            BackColor     = BgDark,
            Margin        = Padding.Empty,
        };
        statusPanel.Controls.Add(_statusDot);
        statusPanel.Controls.Add(_statusLabel);
        rowTitle.Controls.Add(lblTitle,    0, 0);
        rowTitle.Controls.Add(statusPanel, 1, 0);
        tbl.Controls.Add(rowTitle, 0, 0);

        tbl.Controls.Add(sep, 0, 1);

        panel.Dock = DockStyle.Fill;
        tbl.Controls.Add(panel, 0, 2);

        tbl.Controls.Add(_chkAutostart, 0, 3);

        var rowHdr = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 1,
            BackColor   = BgDark,
            Margin      = Padding.Empty,
        };
        rowHdr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        rowHdr.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rowHdr.Controls.Add(lblList, 0, 0);
        rowHdr.Controls.Add(btnAdd,  1, 0);
        tbl.Controls.Add(rowHdr, 0, 5);

        var listWrapper = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Border,
            Padding   = new Padding(1),
        };
        _list.Dock = DockStyle.Fill;
        listWrapper.Controls.Add(_list);
        tbl.Controls.Add(listWrapper, 0, 6);

        var rowBot = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 3,
            RowCount    = 1,
            BackColor   = BgDark,
            Margin      = Padding.Empty,
        };
        rowBot.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rowBot.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rowBot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        rowBot.Controls.Add(btnRemove, 0, 0);
        rowBot.Controls.Add(btnResume, 1, 0);
        rowBot.Controls.Add(btnLog,    2, 0);
        tbl.Controls.Add(rowBot, 0, 8);

        Controls.Add(tbl);

        panel.Resize += (_, _) =>
        {
            _btnToggle.Location = new Point(
                panel.ClientSize.Width - _btnToggle.Width - 18,
                (panel.ClientSize.Height - _btnToggle.Height) / 2);
        };
    }

    void DrawListItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        bool selected = (e.State & DrawItemState.Selected) != 0;

        e.Graphics.FillRectangle(
            new SolidBrush(selected ? Accent : (e.Index % 2 == 0 ? BgPanel : BgHover)),
            e.Bounds);

        var text = _list.Items[e.Index]?.ToString() ?? "";
        TextRenderer.DrawText(e.Graphics, "⏸", new Font("Segoe UI", 10f),
            new Point(e.Bounds.X + 10, e.Bounds.Y + 4),
            selected ? Color.White : Accent2);
        TextRenderer.DrawText(e.Graphics, text, new Font("Consolas", 10f),
            new Point(e.Bounds.X + 36, e.Bounds.Y + 5),
            selected ? Color.White : TextMain);
    }

    static Label MakeLabel(string text, float size = 10, bool bold = false,
                           Control? parent = null)
    {
        var l = new Label
        {
            Text      = text,
            ForeColor = TextMain,
            BackColor = Color.Transparent,
            Font      = bold ? new Font("Segoe UI", size, FontStyle.Bold)
                             : new Font("Segoe UI", size),
            AutoSize  = true,
        };
        parent?.Controls.Add(l);
        return l;
    }

    static Button MakeButton(string text, Color bg, EventHandler onClick,
                             bool small = false)
    {
        var b = new Button
        {
            Text        = text,
            BackColor   = bg,
            ForeColor   = Color.White,
            FlatStyle   = FlatStyle.Flat,
            Font        = small ? new Font("Segoe UI", 9f)
                                : new Font("Segoe UI", 10f, FontStyle.Bold),
            Cursor      = Cursors.Hand,
            AutoSize    = true,
            Padding     = small ? new Padding(10, 4, 10, 4)
                                : new Padding(16, 7, 16, 7),
            Margin      = new Padding(0, 0, 8, 0),
        };
        b.FlatAppearance.BorderSize          = 0;
        b.FlatAppearance.MouseOverBackColor  = Lighten(bg, 20);
        b.Click += onClick;
        return b;
    }

    static Color Lighten(Color c, int amount) =>
        Color.FromArgb(c.A,
            Math.Min(255, c.R + amount),
            Math.Min(255, c.G + amount),
            Math.Min(255, c.B + amount));

    void Refresh2()
    {
        var items    = Config.Load();
        var selected = _list.SelectedIndex;
        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var i in items) _list.Items.Add(i);
        if (selected >= 0 && selected < _list.Items.Count)
            _list.SelectedIndex = selected;
        _list.EndUpdate();

        bool running = Startup.IsMonitorRunning();
        _statusDot.ForeColor   = running ? Success : Danger;
        _statusLabel.ForeColor = running ? Success : Danger;
        _statusLabel.Text      = running ? "monitor ativo" : "monitor inativo";
        _btnToggle.Text        = running ? "⏹  Parar monitor" : "▶  Iniciar monitor";
        _btnToggle.BackColor   = running ? Danger : Accent;
    }

    void ToggleMonitor(object? s, EventArgs e)
    {
        if (Startup.IsMonitorRunning()) Startup.StopMonitor();
        else                            Startup.StartMonitor();
        Task.Delay(600).ContinueWith(_ => Invoke(Refresh2));
    }

    void AddProcess(object? s, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title  = "Selecionar executável",
            Filter = "Executáveis (*.exe)|*.exe|Todos os arquivos|*.*",
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        var name  = Path.GetFileName(dlg.FileName);
        var items = Config.Load();
        if (!items.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            items.Add(name);
            Config.Save(items);
        }
        Refresh2();
    }

    void RemoveProcess(object? s, EventArgs e)
    {
        if (_list.SelectedItem is not string name)
        {
            MessageBox.Show("Selecione um item da lista primeiro.",
                "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (MessageBox.Show($"Remover '{name}' da lista?\nO processo será retomado automaticamente.",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            ProcessApi.ResumeAllByName(name);
            var items = Config.Load();
            items.RemoveAll(i => i.Equals(name, StringComparison.OrdinalIgnoreCase));
            Config.Save(items);
            Refresh2();
        }
    }

    void ResumeProcess(object? s, EventArgs e)
    {
        if (_list.SelectedItem is not string name)
        {
            MessageBox.Show("Selecione um item da lista primeiro.",
                "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        ProcessApi.ResumeAllByName(name);
        MessageBox.Show($"'{name}' retomado.\nO monitor irá suspendê-lo novamente em breve.",
            "Retomado", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    void ViewLog(object? s, EventArgs e)
    {
        if (File.Exists(Config.LogPath))
            System.Diagnostics.Process.Start("notepad.exe", Config.LogPath);
        else
            MessageBox.Show("Nenhum log ainda.", "Log",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}