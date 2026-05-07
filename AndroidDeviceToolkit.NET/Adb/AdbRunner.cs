using System.IO;

namespace AndroidDeviceToolkit.NET
{
    /// <summary>
    /// Internal helper class responsible for locating and executing the ADB binary.
    /// Handles device serial targeting via the -s flag and throws <see cref="SdkToolFailedExitException"/>
    /// on non-zero exit codes. Used by <see cref="Adb"/>, <see cref="ActivityManager"/>,
    /// and <see cref="PackageManager"/> to run ADB commands.
    /// </summary>
    internal class AdbRunner
    {
        /// <summary>
        /// Initializes a new <see cref="AdbRunner"/> associated with the given SDK tool
        /// (used to locate the adb executable).
        /// </summary>
        /// <param name="sdkTool">The SDK tool instance providing path resolution.</param>
        public AdbRunner(SdkTool sdkTool)
        {
            this.sdkTool = sdkTool;
        }

        SdkTool sdkTool;

        /// <summary>
        /// Prepends the '-s &lt;serial&gt;' argument to target a specific device/emulator.
        /// If serial is null or empty, no targeting arguments are added.
        /// </summary>
        /// <param name="serial">The device serial string (e.g., "emulator-5554" or "R5CR7039DLJ").</param>
        /// <param name="builder">The argument builder to prepend the serial arguments to.</param>
        internal void AddSerial(string serial, ProcessArgumentBuilder builder)
        {
            if (!string.IsNullOrEmpty(serial))
            {
                builder.Append("-s");
                builder.AppendQuoted(serial);
            }
        }

        /// <summary>
        /// Runs ADB with the specified arguments without cancellation support.
        /// </summary>
        internal ProcessResult RunAdb(DirectoryInfo androidSdkHome, ProcessArgumentBuilder builder)
            => RunAdb(androidSdkHome, builder, System.Threading.CancellationToken.None);

        /// <summary>
        /// Runs the ADB executable with the given arguments. Locates the adb binary,
        /// starts the process, waits for completion, and throws on failure.
        /// </summary>
        /// <param name="androidSdkHome">The Android SDK home directory for tool resolution.</param>
        /// <param name="builder">The command-line arguments to pass to adb.</param>
        /// <param name="cancelToken">A cancellation token to abort the operation.</param>
        /// <returns>The process result on success.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the adb executable cannot be found.</exception>
        /// <exception cref="SdkToolFailedExitException">Thrown if adb exits with a non-zero code.</exception>
        internal ProcessResult RunAdb(DirectoryInfo androidSdkHome, ProcessArgumentBuilder builder, System.Threading.CancellationToken cancelToken)
        {
            var adbToolPath = sdkTool.FindToolPath(androidSdkHome);
            if (adbToolPath == null || !File.Exists(adbToolPath.FullName))
                throw new FileNotFoundException("Could not find adb", adbToolPath?.FullName);

            var p = new ProcessRunner(adbToolPath, builder, cancelToken);

            var r = p.WaitForExit();

            if (r.ExitCode != 0)
            {
                throw new SdkToolFailedExitException("adb", r.ExitCode, r.StandardError, r.StandardOutput);
            }

            return r;
        }
    }
}
