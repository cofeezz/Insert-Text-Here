static class Monitor
{
    const int IntervalMs = 2000;

    public static void Run()
    {
        Log("Monitor iniciado.");

        var suspended = new Dictionary<uint, string>();

        while (true)
        {
            try
            {
                var watchlist = Config.Load()
                                     .Select(n => n.ToLowerInvariant())
                                     .ToHashSet();

                foreach (var name in watchlist)
                {
                    foreach (var pid in ProcessApi.GetPidsByName(name))
                    {
                        if (!suspended.ContainsKey(pid))
                        {
                            if (ProcessApi.Suspend(pid))
                            {
                                suspended[pid] = name;
                                Log($"Suspenso: {name} (PID {pid})");
                            }
                        }
                    }
                }

                var toRemove = new List<uint>();
                foreach (var (pid, name) in suspended)
                {
                    bool removed = !watchlist.Contains(name);
                    bool dead    = !ProcessApi.GetPidsByName(name).Contains(pid);

                    if (removed)
                    {
                        ProcessApi.Resume(pid);
                        Log($"Retomado (removido da lista): {name} (PID {pid})");
                        toRemove.Add(pid);
                    }
                    else if (dead)
                    {
                        toRemove.Add(pid);
                    }
                }
                foreach (var pid in toRemove) suspended.Remove(pid);
            }
            catch (Exception ex)
            {
                Log($"Erro: {ex.Message}");
            }

            Thread.Sleep(IntervalMs);
        }
    }

    static void Log(string msg)
    {
        try
        {
            File.AppendAllText(
                Config.LogPath,
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {msg}{Environment.NewLine}");
        }
        catch { }
    }
}