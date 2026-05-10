using System.Diagnostics;

class LauncherForm : Form
{
    static readonly Color BgDark   = Color.FromArgb(15,  15,  20);
    static readonly Color BgPanel  = Color.FromArgb(26,  26,  34);
    static readonly Color Accent   = Color.FromArgb(124, 92,  252);
    static readonly Color Accent2  = Color.FromArgb(192, 132, 252);
    static readonly Color TextMain = Color.FromArgb(226, 232, 240);
    static readonly Color TextMute = Color.FromArgb(100, 116, 139);
    static readonly Color Success  = Color.FromArgb(34,  197,  94);
    static readonly Color Danger   = Color.FromArgb(239,  68,  68);

    Label       _lblStatus  = null!;
    Label       _lblDetail  = null!;
    ProgressBar _progress   = null!;
    Label       _lblVersion = null!;
    Button      _btnSkip    = null!;

    public LauncherForm()
    {
        Text            = "ProcessSuspender — Launcher";
        Size            = new Size(480, 260);
        MinimumSize     = Size;
        MaximumSize     = Size;
        BackColor       = BgDark;
        ForeColor       = TextMain;
        Font            = new Font("Segoe UI", 10f);
        StartPosition   = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;

        BuildUI();

        // 
        Shown += async (_, _) => await RunAsync();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // UI
    // ═════════════════════════════════════════════════════════════════════════
    void BuildUI()
    {
        var lblTitle = new Label
        {
            Text      = "⏸  ProcessSuspender",
            Font      = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = Accent2,
            BackColor = Color.Transparent,
            AutoSize  = true,
            Location  = new Point(28, 26),
        };

        var lblSub = new Label
        {
            Text      = "Launcher & Atualizador",
            Font      = new Font("Segoe UI", 9f),
            ForeColor = TextMute,
            BackColor = Color.Transparent,
            AutoSize  = true,
            Location  = new Point(30, 54),
        };

        var sep = new Panel
        {
            BackColor = Color.FromArgb(45, 45, 61),
            Size      = new Size(ClientSize.Width - 56, 1),
            Location  = new Point(28, 82),
        };

        // Main status
        _lblStatus = new Label
        {
            Text      = "Verificando atualizações…",
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = TextMain,
            BackColor = Color.Transparent,
            AutoSize  = true,
            Location  = new Point(28, 100),
        };

        _lblDetail = new Label
        {
            Text      = "",
            Font      = new Font("Segoe UI", 9f),
            ForeColor = TextMute,
            BackColor = Color.Transparent,
            Size      = new Size(ClientSize.Width - 56, 18),
            Location  = new Point(28, 122),
        };

        _progress = new ProgressBar
        {
            Size     = new Size(ClientSize.Width - 56, 8),
            Location = new Point(28, 150),
            Style    = ProgressBarStyle.Continuous,
            Minimum  = 0,
            Maximum  = 100,
            Value    = 0,
        };

        _progress.SetStyle(ControlStyles.UserPaint, false);

        _lblVersion = new Label
        {
            Text      = $"local: {(InstallPath.ReadLocalVersion() is { Length: > 0 } v ? v : "nenhuma")}",
            Font      = new Font("Segoe UI", 8f),
            ForeColor = TextMute,
            BackColor = Color.Transparent,
            AutoSize  = true,
            Location  = new Point(28, ClientSize.Height - 32),
            Anchor    = AnchorStyles.Bottom | AnchorStyles.Left,
        };

        _btnSkip = new Button
        {
            Text      = "Abrir mesmo assim",
            Font      = new Font("Segoe UI", 9f),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(71, 85, 105),
            FlatStyle = FlatStyle.Flat,
            Size      = new Size(148, 30),
            Anchor    = AnchorStyles.Bottom | AnchorStyles.Right,
            Visible   = false,
            Cursor    = Cursors.Hand,
        };
        _btnSkip.FlatAppearance.BorderSize = 0;
        _btnSkip.Location = new Point(
            ClientSize.Width - _btnSkip.Width - 28,
            ClientSize.Height - _btnSkip.Height - 14);
        _btnSkip.Click += (_, _) => LaunchApp();

        Controls.AddRange([lblTitle, lblSub, sep,
                           _lblStatus, _lblDetail, _progress,
                           _lblVersion, _btnSkip]);
    }

    async Task RunAsync()
    {
        SetStatus("Verificando atualizações…", "Conectando ao GitHub…");
        var release = await GitHubRelease.FetchLatestAsync();

        if (release is null)
        {
            if (InstallPath.AppExists())
            {
                SetStatus("Sem conexão com a internet.",
                    "Abrindo versão instalada…", TextMute);
                await Task.Delay(1200);
                LaunchApp();
            }
            else
            {
                SetStatus("Sem conexão e nenhuma versão instalada.",
                    "Verifique sua internet e tente novamente.", Danger);
                _btnSkip.Visible = false;
            }
            return;
        }

        var local = InstallPath.ReadLocalVersion();
        bool needsDownload = !InstallPath.AppExists()
                          || GitHubRelease.IsNewer(local, release.TagName);

        if (!needsDownload)
        {
            SetStatus($"Versão {local} já é a mais recente.",
                "Abrindo o aplicativo…", Success);
            await Task.Delay(900);
            LaunchApp();
            return;
        }

        var action = InstallPath.AppExists() ? "Atualizando" : "Baixando";
        SetStatus($"{action} versão {release.TagName}…",
            release.DownloadUrl, TextMute);

        bool ok = await DownloadAsync(release.DownloadUrl);

        if (!ok)
        {
            SetStatus("Falha no download.", "Verifique sua conexão.", Danger);
            if (InstallPath.AppExists()) _btnSkip.Visible = true;
            return;
        }

   
        SetStatus("Instalando…", "Substituindo executável…");
        try
        {
            InstallPath.ReplaceWithTemp();
            InstallPath.WriteVersion(release.TagName);
        }
        catch (Exception ex)
        {
            SetStatus("Erro ao instalar.", ex.Message, Danger);
            if (InstallPath.AppExists()) _btnSkip.Visible = true;
            return;
        }

        UpdateVersion(release.TagName);
        SetStatus($"Atualizado para {release.TagName}!", "Iniciando…", Success);
        SetProgress(100);
        await Task.Delay(800);
        LaunchApp();
    }

    async Task<bool> DownloadAsync(string url)
    {
        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("User-Agent", "ProcessSuspender-Launcher");
            http.Timeout = TimeSpan.FromMinutes(5);

            using var response = await http.GetAsync(
                url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long total   = response.Content.Headers.ContentLength ?? -1;
            long received = 0;

            await using var src  = await response.Content.ReadAsStreamAsync();
            await using var dest = File.Create(InstallPath.AppExeTmp);

            var buffer = new byte[81920]; // 80 KB chunks
            int read;

            while ((read = await src.ReadAsync(buffer)) > 0)
            {
                await dest.WriteAsync(buffer.AsMemory(0, read));
                received += read;

                if (total > 0)
                {
                    int pct = (int)(received * 100L / total);
                    SetProgress(pct);
                    SetDetail($"{received / 1024 / 1024.0:F1} MB / {total / 1024 / 1024.0:F1} MB");
                }
                else
                {
                    SetDetail($"{received / 1024 / 1024.0:F1} MB baixados…");
                }
            }
            return true;
        }
        catch
        {
            // Clears corrupted temporary files.
            try { File.Delete(InstallPath.AppExeTmp); } catch { }
            return false;
        }
    }

    void LaunchApp()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName        = InstallPath.AppExe,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Não foi possível abrir o aplicativo:\n{ex.Message}",
                "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        Application.Exit();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Thread-safe helpers for updating the UI.
    // ═════════════════════════════════════════════════════════════════════════
    void SetStatus(string status, string detail = "", Color? color = null)
        => Invoke(() =>
        {
            _lblStatus.Text      = status;
            _lblStatus.ForeColor = color ?? TextMain;
            _lblDetail.Text      = detail;
        });

    void SetDetail(string detail)
        => Invoke(() => _lblDetail.Text = detail);

    void SetProgress(int pct)
        => Invoke(() => _progress.Value = Math.Clamp(pct, 0, 100));

    void UpdateVersion(string tag)
        => Invoke(() => _lblVersion.Text = $"local: {tag}");
}
