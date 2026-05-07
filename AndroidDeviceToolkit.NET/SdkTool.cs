using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace AndroidDeviceToolkit.NET
{
    /// <summary>
    /// Exception thrown when an Android SDK tool process exits with a non-zero exit code.
    /// Contains the captured standard output and standard error for diagnostic purposes.
    /// </summary>
    public class SdkToolFailedExitException : Exception
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SdkToolFailedExitException"/>.
        /// </summary>
        /// <param name="name">The name of the SDK tool that failed (e.g., "adb", "sdkmanager").</param>
        /// <param name="exitCode">The non-zero exit code returned by the process.</param>
        /// <param name="stdErr">The lines captured from standard error.</param>
        /// <param name="stdOut">The lines captured from standard output.</param>
        public SdkToolFailedExitException(string name, int exitCode, IEnumerable<string> stdErr, IEnumerable<string> stdOut)
            : base($"{name} exited with an error status")
        {
            ExitCode = exitCode;
            StdErr = stdErr?.ToArray() ?? new string[0];
            StdOut = stdOut?.ToArray() ?? new string[0];
        }

        /// <summary>The exit code returned by the failed process.</summary>
        public readonly int ExitCode;

        /// <summary>The standard output lines captured before the process failed.</summary>
        public readonly string[] StdOut;

        /// <summary>The standard error lines captured before the process failed.</summary>
        public readonly string[] StdErr;

        /// <summary>
        /// Gets the combined output from both stdout and stderr.
        /// </summary>
        public IEnumerable<string> AllOutput
            => StdOut.Concat(StdErr);
    }


    /// <summary>
    /// Abstract base class for all Android SDK tool wrappers (ADB, sdkmanager, avdmanager, emulator).
    /// Provides common functionality including SDK home directory resolution, JDK discovery,
    /// and tool path resolution. Each concrete implementation must specify its SDK package ID
    /// and implement the <see cref="FindToolPath"/> method.
    /// </summary>
    public abstract class SdkTool
    {
        /// <summary>
        /// Initializes a new <see cref="SdkTool"/> with automatic SDK home directory discovery.
        /// </summary>
        public SdkTool()
            : this((string)null)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="SdkTool"/> with the specified SDK home directory path.
        /// </summary>
        /// <param name="androidSdkHome">The path to the Android SDK home directory. Null for auto-discovery.</param>
        public SdkTool(string androidSdkHome)
            : this(string.IsNullOrEmpty(androidSdkHome) ? null : new DirectoryInfo(androidSdkHome))
        {
        }

        /// <summary>
        /// Initializes a new <see cref="SdkTool"/> with the specified SDK home directory.
        /// Resolves the actual SDK path using <see cref="AndroidSdkManager.FindHome"/> and
        /// locates all available JDK installations.
        /// </summary>
        /// <param name="androidSdkHome">The Android SDK home directory. Null for auto-discovery.</param>
        public SdkTool(DirectoryInfo androidSdkHome)
        {
            AndroidSdkHome = AndroidSdkManager.FindHome(androidSdkHome)?.FirstOrDefault();
            Jdks = new JdkLocator().Find()?.ToArray() ?? new JdkInfo[0];
        }

        /// <summary>
        /// Gets all JDK installations discovered on the system.
        /// Used by tools that require Java (sdkmanager, avdmanager).
        /// </summary>
        public JdkInfo[] Jdks { get; private set; }

        /// <summary>
        /// Gets the SDK package identifier for this tool (e.g., "platform-tools", "emulator", "tools").
        /// Used to verify the tool is installed or to install it if missing.
        /// </summary>
        internal abstract string SdkPackageId { get; }

        /// <summary>
        /// Gets or sets the resolved Android SDK home directory.
        /// </summary>
        public DirectoryInfo AndroidSdkHome { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the current platform is Windows.
        /// </summary>
        protected bool IsWindows
            => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>
        /// Locates the tool's executable path within the Android SDK directory structure.
        /// Each subclass must implement this to specify the tool's expected location.
        /// </summary>
        /// <param name="androidSdkHome">Optional SDK home directory override for path resolution.</param>
        /// <returns>The <see cref="FileInfo"/> pointing to the tool executable, or null if not found.</returns>
        public abstract FileInfo FindToolPath(DirectoryInfo androidSdkHome = null);

        /// <summary>
        /// Searches for a specific tool executable within the Android SDK directory by combining
        /// the SDK home path with the given path segments and tool name (with platform-specific extension).
        /// </summary>
        /// <param name="androidHome">The Android SDK home directory to search within.</param>
        /// <param name="toolName">The name of the tool executable (without extension).</param>
        /// <param name="windowsExtension">The file extension to append on Windows (e.g., ".exe", ".bat").</param>
        /// <param name="pathSegments">Subdirectory segments within the SDK home (e.g., "platform-tools").</param>
        /// <returns>A <see cref="FileInfo"/> for the tool if found, or null if the file does not exist.</returns>
        internal FileInfo FindTool(DirectoryInfo androidHome, string toolName, string windowsExtension, params string[] pathSegments)
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            var ext = isWindows ? windowsExtension : string.Empty;
            var home = AndroidSdkManager.FindHome(androidHome)?.FirstOrDefault();

            if (home?.Exists ?? false)
            {
                var allSegments = new List<string>();
                allSegments.Add(home.FullName);
                allSegments.AddRange(pathSegments);
                allSegments.Add(toolName + ext);

                var tool = Path.Combine(allSegments.ToArray());

                if (File.Exists(tool))
                    return new FileInfo(tool);
            }

            return null;
        }
    }
}
