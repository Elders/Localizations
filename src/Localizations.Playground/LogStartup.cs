namespace Localizations.Playground
{
    public static class LogStartup
    {
        public static void Boot()
        {
            log4net.Config.XmlConfigurator.Configure(log4net.LogManager.GetRepository(System.Reflection.Assembly.GetEntryAssembly()), new System.IO.FileInfo("log4net.config"));
        }
    }
}
