using System.IO;

namespace SmartAction.Utils
{
    public static class PathsFile
    {
        public static readonly string LogFilePath = Path.Combine(
            BepInEx.Paths.PluginPath, "SmartAction", "SmartAction_log.txt");

        public static readonly string DebugPath = Path.Combine(
            BepInEx.Paths.PluginPath, "SmartAction", "debug.cfg");
    }
}