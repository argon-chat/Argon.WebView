using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Xilium.CefGlue;
using Xilium.CefGlue.Common;
using Xilium.CefGlue.Common.Shared;

namespace WebViewControl {

    internal static class WebViewLoader {

        private static string[] CustomSchemes { get; } = new[] {
            ResourceUrl.LocalScheme,
            ResourceUrl.EmbeddedScheme,
            ResourceUrl.CustomScheme,
            Uri.UriSchemeHttp,
            Uri.UriSchemeHttps
        };

        private static GlobalSettings globalSettings;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Initialize(GlobalSettings settings) {
            if (CefRuntimeLoader.IsLoaded) {
                return;
            }

            globalSettings = settings;

            var cefSettings = new CefSettings {
                LogSeverity = string.IsNullOrWhiteSpace(settings.LogFile) ? CefLogSeverity.Disable : (settings.EnableErrorLogOnly ? CefLogSeverity.Error : CefLogSeverity.Verbose),
                LogFile = settings.LogFile,
                UncaughtExceptionStackSize = 10000, // enable stack capture
                CachePath = settings.CachePath, // enable cache for external resources to speedup loading
                WindowlessRenderingEnabled = settings.OsrEnabled,
                RemoteDebuggingPort = settings.GetRemoteDebuggingPort(),
                UserAgent = settings.UserAgent,
                BackgroundColor = new CefColor((uint)settings.BackgroundColor.ToArgb()),
                PersistSessionCookies = true,
                PersistUserPreferences = true,
                NoSandbox = true,
                ExternalMessagePump = false,
                JavaScriptFlags = "--expose_gc"

            };

            var customSchemes = CustomSchemes.Concat(settings.Schemes).Select(s => new CustomScheme() {
                SchemeName = s,
                SchemeHandlerFactory = new SchemeHandlerFactory()
            }).ToArray();

            settings.AddCommandLineSwitch("enable-experimental-web-platform-features", null);

            CefRuntimeLoader.Initialize(settings: cefSettings, flags: settings.CommandLineSwitches.ToArray(), customSchemes: customSchemes);

            AppDomain.CurrentDomain.ProcessExit += delegate { Cleanup(); };
        }

        /// <summary>
        /// Release all resources and shutdown web view
        /// </summary>
        [DebuggerNonUserCode]
        public static void Cleanup() {
            CefRuntime.Shutdown(); // must shutdown cef to free cache files (so that cleanup is able to delete files)

            if (globalSettings.PersistCache) {
                return;
            }

            try {
                var dirInfo = new DirectoryInfo(globalSettings.CachePath);
                if (dirInfo.Exists) {
                    dirInfo.Delete(true);
                }
            } catch (UnauthorizedAccessException) {
                // ignore
            } catch (IOException) {
                // ignore
            }
        }

    }
}
