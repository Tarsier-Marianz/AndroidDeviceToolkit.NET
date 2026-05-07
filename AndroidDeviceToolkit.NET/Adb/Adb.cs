using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AndroidDeviceToolkit.NET
{
    /// <summary>
    /// Provides a C# wrapper around the Android Debug Bridge (ADB) command-line tool.
    /// Supports device discovery, app installation/uninstallation, file transfer (push/pull),
    /// shell commands, screen capture/recording, logcat, and emulator management.
    /// </summary>
    /// <example>
    /// Basic usage:
    /// <code>
    /// var adb = new Adb();
    /// 
    /// // List connected devices
    /// var devices = adb.GetDevices();
    /// foreach (var device in devices)
    ///     Console.WriteLine($"{device.Serial} - {device.Model}");
    /// 
    /// // Install an APK
    /// adb.Install(new FileInfo(@"C:\path\to\app.apk"));
    /// 
    /// // Execute a shell command
    /// var result = adb.Shell("ls /sdcard/");
    /// 
    /// // Take a screenshot
    /// adb.ScreenCapture(new FileInfo(@"C:\screenshots\screen.png"));
    /// 
    /// // Connect to a device over WiFi
    /// adb.Connect("192.168.1.100", 5555);
    /// </code>
    /// </example>
    public partial class Adb : SdkTool
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Adb"/> class with automatic SDK home discovery.
        /// </summary>
        public Adb()
            : this((DirectoryInfo)null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Adb"/> class with the specified SDK home directory.
        /// </summary>
        /// <param name="androidSdkHome">The Android SDK home directory. Null for auto-discovery.</param>
        public Adb(DirectoryInfo androidSdkHome)
            : base(androidSdkHome)
        {
            runner = new AdbRunner(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Adb"/> class with the specified SDK home path.
        /// </summary>
        /// <param name="androidSdkHome">The path to the Android SDK home directory. Null or empty for auto-discovery.</param>
        public Adb(string androidSdkHome)
            : this(string.IsNullOrEmpty(androidSdkHome) ? null : new DirectoryInfo(androidSdkHome))
        {
        }

        internal override string SdkPackageId => "platform-tools";

        /// <summary>
        /// Finds the adb executable within the Android SDK platform-tools directory.
        /// </summary>
        /// <param name="androidSdkHome">The SDK home directory to search within.</param>
        /// <returns>The path to the adb executable, or null if not found.</returns>
        public override FileInfo FindToolPath(DirectoryInfo androidSdkHome)
            => FindTool(androidSdkHome, toolName: "adb", windowsExtension: ".exe", "platform-tools");

        AdbRunner runner;

        /// <summary>
        /// Executes a raw ADB command with the specified arguments and returns combined output.
        /// </summary>
        /// <param name="args">The ADB command arguments to execute.</param>
        /// <returns>A list of output lines from both stdout and stderr.</returns>
        public List<string> Run(params string[] args)
        {
            var builder = new ProcessArgumentBuilder();

            foreach (var arg in args)
                builder.Append(arg);

            var r = runner.RunAdb(AndroidSdkHome, builder);

            return r.StandardOutput.Concat(r.StandardError).ToList();
        }

        /// <summary>
        /// Retrieves a list of all connected Android devices and emulators with their details.
        /// Executes 'adb devices -l' and parses the output to extract serial, USB, product, model, and device info.
        /// Offline devices are excluded from the results.
        /// </summary>
        /// <returns>A list of <see cref="AdbDevice"/> objects representing connected devices.</returns>
        public List<AdbDevice> GetDevices()
        {
            var devices = new List<AdbDevice>();

            // Execute: adb devices -l
            var builder = new ProcessArgumentBuilder();

            builder.Append("devices");
            builder.Append("-l");

            var r = runner.RunAdb(AndroidSdkHome, builder);

            if (r.StandardOutput.Count > 1)
            {
                foreach (var line in r.StandardOutput?.Skip(1))
                {
                    var parts = Regex.Split(line, "\\s+");

                    var d = new AdbDevice
                    {
                        Serial = parts[0].Trim()
                    };

                    if (parts.Length > 1 && (parts[1]?.ToLowerInvariant() ?? "offline") == "offline")
                        continue;

                    if (parts.Length > 2)
                    {
                        foreach (var part in parts.Skip(2))
                        {
                            var bits = part.Split(new[] { ':' }, 2);
                            if (bits == null || bits.Length != 2)
                                continue;

                            switch (bits[0].ToLower())
                            {
                                case "usb":
                                    d.Usb = bits[1];
                                    break;
                                case "product":
                                    d.Product = bits[1];
                                    break;
                                case "model":
                                    d.Model = bits[1];
                                    break;
                                case "device":
                                    d.Device = bits[1];
                                    break;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(d?.Serial))
                        devices.Add(d);
                }
            }

            return devices;
        }
        /// <summary>
        /// Executes a specified ADB command with additional parameters and returns the full process result.
        /// </summary>
        /// <param name="command">The ADB command to execute (e.g., "shell", "push", "pull").</param>
        /// <param name="parameters">Additional parameters for the command.</param>
        /// <returns>The <see cref="ProcessResult"/> containing stdout, stderr, and exit code.</returns>
        public ProcessResult RunCommand(string command, params string[] parameters)
        {
            var builder = new ProcessArgumentBuilder();

            builder.Append(command);
            if (parameters != null)
                foreach (var p in parameters)
                    builder.Append(p);

            return runner.RunAdb(AndroidSdkHome, builder);
        }

        /// <summary>
        /// Stops the ADB server process. This is useful to reset ADB when it becomes unresponsive.
        /// Executes: adb kill-server
        /// </summary>
        public void KillServer()
        {
            var builder = new ProcessArgumentBuilder();

            builder.Append("kill-server");

            runner.RunAdb(AndroidSdkHome, builder);
        }

        /// <summary>
        /// Starts the ADB server process. The server is normally started automatically when
        /// any ADB command is issued, but this can be used to pre-start it.
        /// Executes: adb start-server
        /// </summary>
        public void StartServer()
        {
            var builder = new ProcessArgumentBuilder();

            builder.Append("start-server");

            runner.RunAdb(AndroidSdkHome, builder);
        }

        /// <summary>
        /// Connects to an Android device over TCP/IP (WiFi debugging).
        /// The device must have previously been configured for TCP/IP connections
        /// using 'adb tcpip &lt;port&gt;'.
        /// Executes: adb connect &lt;deviceIp&gt;:&lt;port&gt;
        /// </summary>
        /// <param name="deviceIp">The IP address of the target device on the network.</param>
        /// <param name="port">The TCP port to connect on. Default is 5555.</param>
        public void Connect(string deviceIp, int port = 5555)
        {
            var builder = new ProcessArgumentBuilder();

            builder.Append("connect");
            builder.Append(deviceIp + ":" + port);

            runner.RunAdb(AndroidSdkHome, builder);
        }

        /// <summary>
        /// Disconnects from a device connected over TCP/IP, or disconnects all TCP/IP devices
        /// if no IP is specified.
        /// Executes: adb disconnect [&lt;deviceIp&gt;:&lt;port&gt;]
        /// </summary>
        /// <param name="deviceIp">The IP address of the device to disconnect. Null to disconnect all.</param>
        /// <param name="port">The port number. Default is 5555.</param>
        public void Disconnect(string deviceIp = null, int? port = null)
        {
            var builder = new ProcessArgumentBuilder();

            builder.Append("disconnect");
            if (!string.IsNullOrEmpty(deviceIp))
                builder.Append(deviceIp + ":" + (port ?? 5555));

            runner.RunAdb(AndroidSdkHome, builder);
        }

        /// <summary>
        /// Installs an APK file on the connected device.
        /// Executes: adb [-s serial] install &lt;apk-path&gt;
        /// </summary>
        /// <param name="apkFile">The local APK file to install on the device.</param>
        /// <param name="adbSerial">Optional device serial to target a specific device.</param>
        public void Install(FileInfo apkFile, string adbSerial = null)
        {
            var builder = new ProcessArgumentBuilder();

            runner.AddSerial(adbSerial, builder);

            builder.Append("install");
            builder.Append(apkFile.FullName);

            runner.RunAdb(AndroidSdkHome, builder);
        }

        /// <summary>
        /// Blocks until the specified device state is reached.
        /// Useful for waiting until a device is fully booted or enters recovery mode.
        /// Executes: adb wait-for[-&lt;transport&gt;]-&lt;state&gt;
        /// </summary>
        /// <param name="transport">The transport type to wait on (Any, Usb, or Local).</param>
        /// <param name="state">The device state to wait for (Device, Recovery, Sideload, or Bootloader).</param>
        /// <param name="adbSerial">Optional device serial to target a specific device.</param>
        public void WaitFor(AdbTransport transport = AdbTransport.Any, AdbState state = AdbState.Device, string adbSerial = null)
        {
            var builder = new ProcessArgumentBuilder();

            runner.AddSerial(adbSerial, builder);

            var x = "wait-for";
            if (transport == AdbTransport.Local)
                x = "-local";
            else if (transport == AdbTransport.Usb)
                x = "-usb";

            switch (state)
            {
                case AdbState.Bootloader:
                    x += "-bootloader";
                    break;
                case AdbState.Device:
                    x += "-device";
                    break;
                case AdbState.Recovery:
                    x += "-recovery";
                    break;
                case AdbState.Sideload:
                    x += "-sideload";
                    break;
            }

            builder.Append(x);

            runner.RunAdb(AndroidSdkHome, builder);
        }

        /// <summary>
        /// Uninstalls a package from the connected device.
        /// Executes: adb [-s serial] uninstall [-k] &lt;package&gt;
        /// </summary>
        /// <param name="packageName">The fully qualified package name to uninstall (e.g., "com.example.app").</param>
        /// <param name="keepDataAndCacheDirs">If true, keeps the data and cache directories after uninstallation.</param>
        /// <param name="adbSerial">Optional device serial to target a specific device.</param>
        public void Uninstall(string packageName, bool keepDataAndCacheDirs = false, string adbSerial = null)
        {
            var builder = new ProcessArgumentBuilder();

            runner.AddSerial(adbSerial, builder);

            builder.Append("uninstall");
            if (keepDataAndCacheDirs)
                builder.Append("-k");
            builder.Append(packageName);

            runner.RunAdb(AndroidSdkHome, builder);
        }

        /// <summary>
        /// Sends the kill command to a running emulator instance via the ADB emulator console.
        /// Executes: adb [-s serial] emu kill
        /// </summary>
        /// <param name="adbSerial">Optional device serial to target a specific emulator.</param>
        /// <returns>True if the emulator reported it is stopping; otherwise false.</returns>
        public bool EmuKill(string adbSerial = null)
        {
            var builder = new ProcessArgumentBuilder();

            runner.AddSerial(adbSerial, builder);

            builder.Append("emu");
            builder.Append("kill");

            var r = runner.RunAdb(AndroidSdkHome, builder);

            return r.StandardOutput.Any(o => o.ToLowerInvariant().Contains("stopping emulator"));
        }

        /// <summary>
        /// Gets the AVD (Android Virtual Device) name of a running emulator instance.
        /// Executes: adb [-s serial] emu avd name
        /// </summary>
        /// <param name="adbSerial">Optional device serial to target a specific emulator.</param>
        /// <returns>The AVD name string, or null if not available.</returns>
        public string EmuAvdName(string adbSerial = null)
        {
            var builder = new ProcessArgumentBuilder();

            runner.AddSerial(adbSerial, builder);

            builder.Append("emu");
            builder.Append("avd");
            builder.Append("name");

            var r = runner.RunAdb(AndroidSdkHome, builder);

            return r?.StandardOutput?.FirstOrDefault()?.Trim();
        }

        /// <summary>
        /// Pulls (downloads) a file from the device to a local file destination.
        /// </summary>
        /// <param name="remoteFileSource">The remote file path on the device.</param>
        /// <param name="localFileDestination">The local file path to save to.</param>
        /// <param name="adbSerial">Optional device serial to target a specific device.</param>
        /// <returns>True if the pull operation succeeded.</returns>
        public bool Pull(FileInfo remoteFileSource, FileInfo localFileDestination, string adbSerial = null)
            => pull(remoteFileSource.FullName, localFileDestination.FullName, adbSerial);

        /// <summary>
        /// Pulls (downloads) a directory from the device to a local directory destination.
        /// </summary>
        /// <param name="remoteDirectorySource">The remote directory path on the device.</param>
        /// <param name="localDirectoryDestination">The local directory path to save to.</param>
        /// <param name="adbSerial">Optional device serial to target a specific device.</param>
        /// <returns>True if the pull operation succeeded.</returns>
        public bool Pull(DirectoryInfo remoteDirectorySource, DirectoryInfo localDirectoryDestination, string adbSerial = null)
            => pull(remoteDirectorySource.FullName, localDirectoryDestination.FullName, adbSerial);

        /// <summary>
        /// Pulls (downloads) a file from the device to a local directory destination.
        /// </summary>
        /// <param name="remoteFileSource">The remote file path on the device.</param>
        /// <param name="localDirectoryDestination">The local directory to save the file into.</param>
        /// <param name="adbSerial">Optional device serial to target a specific device.</param>
        /// <returns>True if the pull operation succeeded.</returns>
        public bool Pull(FileInfo remoteFileSource, DirectoryInfo localDirectoryDestination, string adbSerial = null)
            => pull(remoteFileSource.FullName, localDirectoryDestination.FullName, adbSerial);

        /// <summary>
        /// Internal implementation for pulling files/directories from a device.
        /// Executes: adb [-s serial] pull &lt;remote&gt; &lt;local&gt;
        /// </summary>
        bool pull(string remoteSrc, string localDest, string adbSerial = null)
        {
            var builder = new ProcessArgumentBuilder();

            runner.AddSerial(adbSerial, builder);

            builder.Append("pull");
            builder.AppendQuoted(remoteSrc);
            builder.AppendQuoted(localDest);

            var r = runner.RunAdb(AndroidSdkHome, builder);

            return r.Success;
        }

        /// <summary>
        /// Pushes (uploads) a local file to a file destination on the device.
        /// </summary>
        /// <param name="localFileSource">The local file to push.</param>
        /// <param name="remoteFileDestination">The remote file path on the device.</param>
        /// <param name="adbSerial">Optional device serial to target a specific device.</param>
        /// <returns>True if the push operation succeeded.</returns>
        public bool Push(FileInfo localFileSource, FileInfo remoteFileDestination, string adbSerial = null)
            => push(localFileSource.FullName, remoteFileDestination.FullName, adbSerial);

        /// <summary>
        /// Pushes (uploads) a local file to a directory on the device.
        /// </summary>
        /// <param name="localFileSource">The local file to push.</param>
        /// <param name="remoteDirectoryDestination">The remote directory on the device.</param>
        /// <param name="adbSerial">Optional device serial to target a specific device.</param>
        /// <returns>True if the push operation succeeded.</returns>
        public bool Push(FileInfo localFileSource, DirectoryInfo remoteDirectoryDestination, string adbSerial = null)
            => push(localFileSource.FullName, remoteDirectoryDestination.FullName, adbSerial);

        /// <summary>
        /// Pushes (uploads) a local directory to a directory on the device.
        /// </summary>
        /// <param name="localDirectorySource">The local directory to push.</param>
        /// <param name="remoteDirectoryDestination">The remote directory on the device.</param>
        /// <param name="adbSerial">Optional device serial to target a specific device.</param>
        /// <returns>True if the push operation succeeded.</returns>
        public bool Push(DirectoryInfo localDirectorySource, DirectoryInfo remoteDirectoryDestination, string adbSerial = null)
            => push(localDirectorySource.FullName, remoteDirectoryDestination.FullName, adbSerial);

        /// <summary>
        /// Internal implementation for pushing files/directories to a device.
        /// Executes: adb [-s serial] push &lt;local&gt; &lt;remote&gt;
        /// </summary>
        bool push(string localSrc, string remoteDest, string adbSerial = null)
        {
            var builder = new ProcessArgumentBuilder();

            runner.AddSerial(adbSerial, builder);

            builder.Append("pull");
            builder.AppendQuoted(localSrc);
            builder.AppendQuoted(remoteDest);

            var r = runner.RunAdb(AndroidSdkHome, builder);
            return r.Success;
        }

        /// <summary>
        /// Generates a full bug report from the device including system logs, stack traces, and other diagnostic data.
        /// Executes: adb [-s serial] bugreport
        /// </summary>
        /// <param name="adbSerial">Optional device serial to target a specific device.</param>
        /// <returns>A list of output lines from the bug report.</returns>
        public List<string> BugReport(string adbSerial = null)
        {
            var builder = new ProcessArgumentBuilder();

            runner.AddSerial(adbSerial, builder);

            builder.Append("bugreport");

            var r = runner.RunAdb(AndroidSdkHome, builder);

            return r.StandardOutput;
        }


        /// <summary>
        /// Retrieves logcat (system log) output from the device with the specified options.
        /// By default dumps the current log and returns (does not stream continuously).
        /// Executes: adb [-s serial] logcat [options] [filter-specs]
        /// </summary>
        /// <param name="options">Logcat options such as buffer type, verbosity, output file, etc. Uses defaults if null.</param>
        /// <param name="filter">Optional filter specification for logcat output.</param>
        /// <param name="adbSerial">Optional device serial to target a specific device.</param>
        /// <returns>A list of logcat output lines.</returns>
        public List<string> Logcat(AdbLogcatOptions options = null, string filter = null, string adbSerial = null)
        {
            if (options == null)
                options = new AdbLogcatOptions();

            // Build the logcat command with specified options
            var builder = new ProcessArgumentBuilder();

            runner.AddSerial(adbSerial, builder);

            builder.Append("logcat");

            if (options.BufferType != AdbLogcatBufferType.Main)
            {
                builder.Append("-b");
                builder.Append(options.BufferType.ToString().ToLowerInvariant());
            }

            if (options.Clear || options.PrintSize)
            {
                if (options.Clear)
                    builder.Append("-c");
                else if (options.PrintSize)
                    builder.Append("-g");
            }
            else
            {
                // Always dump, since we want to return and not listen to logcat forever
                // in the future might be nice to add an alias that takes a cancellation token
                // and can pipe output until that token is cancelled.
                //if (options.Dump)
                builder.Append("-d");

                if (options.OutputFile != null)
                {
                    builder.Append("-f");
                    builder.AppendQuoted(options.OutputFile.FullName);

                    if (options.NumRotatedLogs.HasValue)
                    {
                        builder.Append("-n");
                        builder.Append(options.NumRotatedLogs.Value.ToString());
                    }

                    var kb = options.LogRotationKb ?? 16;
                    builder.Append("-r");
                    builder.Append(kb.ToString());
                }

                if (options.SilentFilter)
                    builder.Append("-s");

                if (options.Verbosity != AdbLogcatOutputVerbosity.Brief)
                {
                    builder.Append("-v");
                    builder.Append(options.Verbosity.ToString().ToLowerInvariant());
                }

            }

            var r = runner.RunAdb(AndroidSdkHome, builder);

            return r.StandardOutput;
        }

        /// <summary>
        /// Gets the ADB version string.
        /// Executes: adb version
        /// </summary>
        /// <returns>The ADB version information string.</returns>
        public string Version()
        {
            var builder = new ProcessArgumentBuilder();

            builder.Append("version");
            var r = runner.RunAdb(AndroidSdkHome, builder);

            return string.Join(Environment.NewLine, r.StandardOutput);
        }

        /// <summary>
        /// Gets the serial number of the connected device.
        /// Executes: adb [-s serial] get-serialno
        /// </summary>
        /// <param name="adbSerial">Optional device serial to target a specific device.</param>
        /// <returns>The device serial number string.</returns>
        public string GetSerialNumber(string adbSerial = null)
        {
            var builder = new ProcessArgumentBuilder();

            runner.AddSerial(adbSerial, builder);

            builder.Append("get-serialno");

            var r = runner.RunAdb(AndroidSdkHome, builder);

            return string.Join(Environment.NewLine, r.StandardOutput);
        }

        /// <summary>
        /// Gets the current state of the connected device (e.g., "device", "offline", "bootloader").
        /// Executes: adb [-s serial] get-state
        /// </summary>
        /// <param name="adbSerial">Optional device serial to target a specific device.</param>
        /// <returns>The device state string.</returns>
        public string GetState(string adbSerial = null)
        {
            var builder = new ProcessArgumentBuilder();

            runner.AddSerial(adbSerial, builder);

            builder.Append("get-state");

            var r = runner.RunAdb(AndroidSdkHome, builder);

            return string.Join(Environment.NewLine, r.StandardOutput);
        }

        /// <summary>
        /// Retrieves system properties from the device using 'getprop'.
        /// Optionally filters results to include only properties matching the specified names/patterns.
        /// </summary>
        /// <param name="adbSerial">Optional device serial to target a specific device.</param>
        /// <param name="includeProperties">Optional property name patterns to filter by (supports regex). Null returns all.</param>
        /// <returns>A dictionary of property names to their values.</returns>
        public Dictionary<string, string> GetProperties(string adbSerial = null, params string[] includeProperties)
        {
            var r = new Dictionary<string, string>();

            var lines = Shell("getprop", adbSerial);

            foreach (var l in lines)
            {
                if (l?.Contains(':') ?? false)
                {
                    var parts = l.Split(new[] { ':' }, 2);
                    if (parts != null && parts.Length == 2)
                    {
                        var key = parts[0].Trim().Trim('[', ']');

                        if (includeProperties == null || (includeProperties?.Any(ip => IsPropertyMatch(ip, key)) ?? false))
                            r[key] = parts[1].Trim().Trim('[', ']');
                    }
                }
            }

            return r;
        }

        /// <summary>
        /// Checks if a property name/pattern matches a given input string (case-insensitive, supports regex).
        /// </summary>
        internal static bool IsPropertyMatch(string value, string input)
        {
            if (value.Equals(input, StringComparison.InvariantCultureIgnoreCase)
                || Regex.IsMatch(input, value, RegexOptions.Singleline | RegexOptions.IgnoreCase))
                return true;

            return false;
        }

        /// <summary>
        /// Executes a shell command on the connected Android device.
        /// Executes: adb [-s serial] shell &lt;command&gt;
        /// </summary>
        /// <param name="shellCommand">The shell command to execute on the device.</param>
        /// <param name="adbSerial">Optional device serial to target a specific device.</param>
        /// <returns>A list of output lines from the shell command.</returns>
        public List<string> Shell(string shellCommand, string adbSerial = null)
        {
            var builder = new ProcessArgumentBuilder();

            runner.AddSerial(adbSerial, builder);

            builder.Append("shell");
            builder.Append(shellCommand);

            var r = runner.RunAdb(AndroidSdkHome, builder);

            return r.StandardOutput;
        }


        /// <summary>
        /// Captures a screenshot from the device screen and saves it to a local file.
        /// Uses a temporary file on the device's SD card, then pulls it locally and cleans up.
        /// </summary>
        /// <param name="saveToLocalFile">The local file path to save the screenshot PNG to.</param>
        /// <param name="adbSerial">Optional device serial to target a specific device.</param>
        public void ScreenCapture(FileInfo saveToLocalFile, string adbSerial = null)
        {
            var guid = Guid.NewGuid().ToString();
            var remoteFile = "/sdcard/" + guid + ".png";

            Shell("screencap " + remoteFile, adbSerial);

            Pull(new FileInfo(remoteFile), saveToLocalFile, adbSerial);

            Shell("rm " + remoteFile, adbSerial);
        }

        /// <summary>
        /// Records the device screen to a video file. The recording continues until the cancellation token
        /// is triggered or the time limit is reached. Uses a temporary file on the device, then pulls it locally.
        /// </summary>
        /// <param name="saveToLocalFile">The local file path to save the recorded MP4 video to.</param>
        /// <param name="recordingCancelToken">Optional cancellation token to stop recording early.</param>
        /// <param name="timeLimit">Optional maximum recording duration.</param>
        /// <param name="bitrateMbps">Optional video bitrate in megabits per second.</param>
        /// <param name="width">Optional video width in pixels (must be used with height).</param>
        /// <param name="height">Optional video height in pixels (must be used with width).</param>
        /// <param name="rotate">Whether to rotate the output 90 degrees.</param>
        /// <param name="logVerbose">Whether to enable verbose logging.</param>
        /// <param name="adbSerial">Optional device serial to target a specific device.</param>
        public void ScreenRecord(FileInfo saveToLocalFile, System.Threading.CancellationToken? recordingCancelToken = null, TimeSpan? timeLimit = null, int? bitrateMbps = null, int? width = null, int? height = null, bool rotate = false, bool logVerbose = false, string adbSerial = null)
        {

            var guid = Guid.NewGuid().ToString();
            var remoteFile = "/sdcard/" + guid + ".mp4";

            // Build the screenrecord shell command with options
            var builder = new ProcessArgumentBuilder();

            runner.AddSerial(adbSerial, builder);

            builder.Append("shell");
            builder.Append("screenrecord");

            if (timeLimit.HasValue)
            {
                builder.Append("--time-limit");
                builder.Append(((int)timeLimit.Value.TotalSeconds).ToString());
            }

            if (bitrateMbps.HasValue)
            {
                builder.Append("--bit-rate");
                builder.Append((bitrateMbps.Value * 1000000).ToString());
            }

            if (width.HasValue && height.HasValue)
            {
                builder.Append("--size");
                builder.Append($"{width}x{height}");
            }

            if (rotate)
                builder.Append("--rotate");

            if (logVerbose)
                builder.Append("--verbose");

            builder.Append(remoteFile);

            if (recordingCancelToken.HasValue)
                runner.RunAdb(AndroidSdkHome, builder, recordingCancelToken.Value);
            else
                runner.RunAdb(AndroidSdkHome, builder);

            Pull(new FileInfo(remoteFile), saveToLocalFile, adbSerial);

            Shell("rm " + remoteFile, adbSerial);
        }

        /// <summary>
        /// Gets the human-readable device name. For emulators, returns the AVD name.
        /// For physical devices, returns the product model or product name from system properties.
        /// </summary>
        /// <param name="adbSerial">Optional device serial to target a specific device.</param>
        /// <returns>The device name string, or null if it cannot be determined.</returns>
        public string GetDeviceName(string adbSerial = null)
        {
            try
            {
                return GetEmulatorName(adbSerial);
            }
            catch (InvalidDataException)
            {
                // Try getting the product model from system properties
                var s = Shell("getprop ro.product.model", adbSerial);

                if (s?.Any() ?? false)
                    return s.FirstOrDefault().Trim();

                // Fall back to product name
                s = Shell("getprop ro.product.name", adbSerial);

                if (s?.Any() ?? false)
                    return s.FirstOrDefault().Trim();
            }

            return null;
        }

        /// <summary>
        /// Gets the AVD name for a running emulator by first trying the emu console command,
        /// then falling back to a direct TCP connection to the emulator's console port.
        /// </summary>
        /// <param name="adbSerial">The emulator serial (must start with "emulator-").</param>
        /// <returns>The AVD name of the emulator.</returns>
        /// <exception cref="InvalidDataException">Thrown if the serial is not an emulator serial.</exception>
        public string GetEmulatorName(string adbSerial = null)
        {
            var shellName = EmuAvdName(adbSerial);

            if (!string.IsNullOrWhiteSpace(shellName))
                return shellName;

            if (string.IsNullOrEmpty(adbSerial) || !adbSerial.StartsWith("emulator-", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Serial must be an emulator starting with `emulator-`");

            int port = 5554;
            if (!int.TryParse(adbSerial.Substring(9), out port))
                return null;

            var tcpClient = new System.Net.Sockets.TcpClient("127.0.0.1", port);
            var name = string.Empty;
            using (var s = tcpClient.GetStream())
            {

                System.Threading.Thread.Sleep(250);

                foreach (var b in Encoding.ASCII.GetBytes("avd name\r\n"))
                    s.WriteByte(b);

                System.Threading.Thread.Sleep(250);

                byte[] data = new byte[1024];
                using (var memoryStream = new MemoryStream())
                {
                    do
                    {
                        var len = s.Read(data, 0, data.Length);
                        memoryStream.Write(data, 0, len);
                    } while (s.DataAvailable);

                    var txt = Encoding.ASCII.GetString(memoryStream.ToArray(), 0, (int)memoryStream.Length);

                    var m = Regex.Match(txt, "OK(?<name>.*?)OK", RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
                    name = m?.Groups?["name"]?.Value?.Trim();
                }
            }

            return name;
        }

        /// <summary>
        /// Launches an application on the device by package name using the monkey tool.
        /// This avoids needing to know the main activity class name.
        /// Executes: adb shell monkey -p &lt;packageName&gt; -v 1
        /// </summary>
        /// <param name="packageName">The fully qualified package name to launch (e.g., "com.example.app").</param>
        /// <param name="adbSerial">Optional device serial to target a specific device.</param>
        /// <returns>The shell command output lines.</returns>
        public IEnumerable<string> LaunchApp(string packageName, string adbSerial = null)
        {
            // Use monkey to launch the app by package name without needing the activity class
            return Shell($"monkey -p {packageName} -v 1", adbSerial);
        }
    }
}
