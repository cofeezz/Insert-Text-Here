if (args.Length > 0 && args[0] == "--monitor")
{
    Monitor.Run();
}
else
{
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    Application.Run(new MainForm());
}
 