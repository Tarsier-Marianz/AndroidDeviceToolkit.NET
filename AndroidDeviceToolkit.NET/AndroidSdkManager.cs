using System.Runtime.InteropServices;

namespace AndroidDeviceToolkit.NET
{

    /// <summary>
    /// The main entry point for managing Android SDK tools and components.
    /// Provides unified access to ADB, AVD Manager, Package Manager, Emulator, and SDK Manager.
    /// Automatically discovers the Android SDK installation directory from environment variables
    /// and common installation paths.
    /// </summary>
    /// <example>
    /// Basic usage - auto-detect SDK home:
    /// <code>
    /// var sdk = new AndroidSdkManager();
    /// Console.WriteLine($"SDK Home: {sdk.Home.FullName}");
    /// </code>
    /// 
    /// Specify a custom SDK path:
    /// <code>
    /// var sdk = new AndroidSdkManager(new DirectoryInfo(@"C:\Android\sdk"));
    /// </code>
    /// 
    /// List connected devices:
    /// <code>
    /// var sdk = new AndroidSdkManager();
    /// var devices = sdk.Adb.GetDevices();
    /// foreach (var device in devices)
    ///     Console.WriteLine($"Device: {device.Serial} - {device.Model}");
    /// </code>
    /// 
    /// Install SDK packages and manage AVDs:
    /// <code>
    /// var sdk = new AndroidSdkManager();
    /// sdk.SdkManager.Install("platforms;android-33", "build-tools;33.0.0");
    /// sdk.AvdManager.Create("MyEmulator", "system-images;android-33;google_apis;x86_64");
    /// </code>
    /// </example>

    public class AndroidSdkManager
    {
        /// <summary>
        /// Known default installation paths for the Android SDK on the current platform.
        /// On Windows: checks Program Files directories.
        /// On macOS/Linux: checks user home and common developer directories.
        /// </summary>
        static string[] KnownLikelyPaths =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                new string[] {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Android", "android-sdk"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Android", "android-sdk"),
                } :
                new string[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Developer", "android-sdk-macosx"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Developer", "Xamarin", "android-sdk-macosx"),
                    Path.Combine("Developer", "Android", "android-sdk-macosx"),
                };

        /// <summary>
        /// Finds all possible Android SDK home directories using default discovery logic.
        /// Checks environment variables (ANDROID_SDK_ROOT, ANDROID_HOME) and known installation paths.
        /// </summary>
        /// <returns>An enumerable of valid Android SDK directory paths.</returns>
        public static IEnumerable<DirectoryInfo> FindHome()
            => FindHome((string)null, null);

        /// <summary>
        /// Finds all possible Android SDK home directories, prioritizing the specified directory.
        /// </summary>
        /// <param name="specificHome">A specific directory to check first. Can be null.</param>
        /// <returns>An enumerable of valid Android SDK directory paths.</returns>
        public static IEnumerable<DirectoryInfo> FindHome(DirectoryInfo specificHome = null)
            => FindHome(specificHome?.FullName, null);

        /// <summary>
        /// Finds all possible Android SDK home directories, prioritizing the specified directory
        /// and including additional possible directories in the search.
        /// </summary>
        /// <param name="specificHome">A specific directory to check first. Can be null.</param>
        /// <param name="additionalPossibleDirectories">Additional directory paths to include in the search.</param>
        /// <returns>An enumerable of valid Android SDK directory paths.</returns>
        public static IEnumerable<DirectoryInfo> FindHome(DirectoryInfo specificHome = null, params string[] additionalPossibleDirectories)
            => FindHome(specificHome?.FullName, additionalPossibleDirectories);

        /// <summary>
        /// Finds all possible Android SDK home directories by checking (in order):
        /// 1. The specified path
        /// 2. ANDROID_SDK_ROOT environment variable
        /// 3. ANDROID_HOME environment variable
        /// 4. Any additional directories provided
        /// 5. Known platform-specific default installation paths
        /// Only returns directories that actually exist on disk.
        /// </summary>
        /// <param name="specificHome">A specific directory path to check first. Can be null.</param>
        /// <param name="additionalPossibleDirectories">Additional directory paths to include in the search.</param>
        /// <returns>An enumerable of valid Android SDK directory paths that exist on disk.</returns>
        public static IEnumerable<DirectoryInfo> FindHome(string specificHome = null, params string[] additionalPossibleDirectories)
        {
            var candidates = new List<string>
            {
                specificHome,
                Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT"),
                Environment.GetEnvironmentVariable("ANDROID_HOME")
            };
            if (additionalPossibleDirectories != null)
                candidates.AddRange(additionalPossibleDirectories);
            candidates.AddRange(KnownLikelyPaths);

            foreach (var c in candidates)
            {
                if (!string.IsNullOrWhiteSpace(c) && Directory.Exists(c))
                    yield return new DirectoryInfo(c);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AndroidSdkManager"/> class.
        /// Automatically discovers the SDK home directory if not specified, and initializes
        /// all sub-managers (SdkManager, AvdManager, PackageManager, Adb, Emulator).
        /// </summary>
        /// <param name="home">The Android SDK home directory. If null, auto-discovery is used.</param>
        public AndroidSdkManager(DirectoryInfo home = null)
        {
            Home = home ?? FindHome()?.FirstOrDefault();

            SdkManager = new SdkManager(Home);
            AvdManager = new AvdManager(Home);
            PackageManager = new PackageManager(Home);
            Adb = new Adb(Home);
            Emulator = new Emulator(Home);
        }

        /// <summary>
        /// Downloads and acquires the Android SDK if not already present.
        /// This will download the SDK command-line tools and update all packages.
        /// </summary>
        /// <returns>A task representing the asynchronous acquire operation.</returns>
        public async Task Acquire()
        {
            await SdkManager.Acquire();
        }

        /// <summary>
        /// The resolved Android SDK home directory path.
        /// </summary>
        public readonly DirectoryInfo Home;

        /// <summary>
        /// Provides access to the Android SDK Manager for installing, uninstalling, and listing SDK packages.
        /// </summary>
        public readonly SdkManager SdkManager;

        /// <summary>
        /// Provides access to the Android Virtual Device (AVD) Manager for creating, deleting, and listing emulator images.
        /// </summary>
        public readonly AvdManager AvdManager;

        /// <summary>
        /// Provides access to the Android Package Manager (pm) for managing installed apps on a device.
        /// </summary>
        public readonly PackageManager PackageManager;

        /// <summary>
        /// Provides access to the Android Emulator for starting and managing emulator instances.
        /// </summary>
        public readonly Emulator Emulator;

        /// <summary>
        /// Provides access to ADB (Android Debug Bridge) for device communication, file transfer, and debugging.
        /// </summary>
        public readonly Adb Adb;
    }
}