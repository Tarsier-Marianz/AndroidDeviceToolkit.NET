using System.IO;
using System.Runtime.InteropServices;

namespace AndroidDeviceToolkit.NET
{
    /// <summary>
    /// Represents information about a discovered Java Development Kit (JDK) installation.
    /// Stores the paths to the Java compiler (javac), Java runtime (java), the JDK home directory,
    /// and the JDK version string.
    /// </summary>
    /// <example>
    /// <code>
    /// var jdkLocator = new JdkLocator();
    /// foreach (var jdk in jdkLocator.Find())
    /// {
    ///     Console.WriteLine($"JDK {jdk.Version} at {jdk.Home.FullName}");
    ///     Console.WriteLine($"  javac: {jdk.JavaC.FullName}");
    ///     Console.WriteLine($"  java:  {jdk.Java.FullName}");
    /// }
    /// </code>
    /// </example>
    public class JdkInfo
    {
        /// <summary>
        /// Initializes a new instance of <see cref="JdkInfo"/> from the path to a javac executable.
        /// Automatically resolves the JDK home directory and java executable path based on the
        /// standard JDK directory structure (home/bin/javac, home/bin/java).
        /// </summary>
        /// <param name="javaCFile">The full path to the javac compiler executable.</param>
        /// <param name="version">The version string of the JDK (e.g., "11.0.2", "17.0.1").</param>
        public JdkInfo(string javaCFile, string version)
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            JavaC = new FileInfo(javaCFile);
            Home = new DirectoryInfo(Path.Combine(JavaC.Directory.FullName, ".."));
            Java = new FileInfo(Path.Combine(Home.FullName, "bin", "java" + (isWindows ? ".exe" : "")));
            Version = version;
        }

        /// <summary>
        /// Gets the path to the Java compiler (javac) executable.
        /// </summary>
        public FileInfo JavaC { get; private set; }

        /// <summary>
        /// Gets the path to the Java runtime (java) executable.
        /// </summary>
        public FileInfo Java { get; private set; }

        /// <summary>
        /// Gets the JDK home directory (parent of the bin/ directory containing javac).
        /// </summary>
        public DirectoryInfo Home { get; private set; }

        /// <summary>
        /// Gets or sets the JDK version string (e.g., "11.0.2", "17.0.1").
        /// </summary>
        public string Version { get; set; }
    }
}
