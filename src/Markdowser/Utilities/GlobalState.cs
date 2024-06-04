using CuteUtils.Logging;

using Markdowser.Models;

using System;

namespace Markdowser.Utilities;

internal static class GlobalState
{
    public static event EventHandler? ThemeChanged;

    public static TabState CurrentTabState { get; internal set; } = new();

    public static Logger Logger { get; } = new()
    {
        Config = new()
        {
            DebugConfig = new()
            {
                LogTarget = LogTarget.DebugConsole
            },
            InfoConfig = new()
            {
                LogTarget = LogTarget.DebugConsole | LogTarget.File,
                FilePath = Configuration.LogFilePath
            },
            WarnConfig = new()
            {
                LogTarget = LogTarget.DebugConsole | LogTarget.File,
                FilePath = Configuration.LogFilePath
            },
            ErrorConfig = new()
            {
                LogTarget = LogTarget.DebugConsole | LogTarget.File,
                FilePath = Configuration.LogFilePath
            },
            FatalConfig = new()
            {
                LogTarget = LogTarget.DebugConsole | LogTarget.File,
                FilePath = Configuration.LogFilePath
            },
            FormatConfig = new()
            {
                DebugConsoleFormat = new LogFormatBuilder().DateTime().Text(" ").LogSeverity(padding: -6).FilePath().Text(":").MemberName().Text(":").LineNumber().Text(Environment.NewLine).Message(),
                FileFormat = new LogFormatBuilder().DateTime().Text(" ").LogSeverity(padding: -6).FilePath().Text(":").MemberName().Text(":").LineNumber().Text(Environment.NewLine).Message(),
            }
        }
    };

    public static void InvokeThemeChanged()
    {
        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }
}