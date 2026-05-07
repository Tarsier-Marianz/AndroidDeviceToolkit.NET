using System.Collections.Generic;

namespace AndroidDeviceToolkit.NET
{
    /// <summary>
    /// A helper class for building command-line argument strings for process execution.
    /// Supports appending plain arguments and quoted arguments (for paths with spaces).
    /// Used internally by all SDK tool runners to construct command-line invocations.
    /// </summary>
    internal class ProcessArgumentBuilder
    {
        /// <summary>
        /// The list of accumulated arguments.
        /// </summary>
        public List<string> args = new List<string>();

        /// <summary>
        /// Appends a plain (unquoted) argument to the argument list.
        /// </summary>
        /// <param name="arg">The argument string to append.</param>
        public void Append(string arg)
            => args.Add(arg);

        /// <summary>
        /// Appends an argument wrapped in double quotes to the argument list.
        /// Use this for arguments containing spaces (e.g., file paths).
        /// </summary>
        /// <param name="arg">The argument string to quote and append.</param>
        public void AppendQuoted(string arg)
            => args.Add($"\"{arg}\"");

        /// <summary>
        /// Converts all accumulated arguments into a single space-separated command-line string.
        /// </summary>
        /// <returns>The complete argument string for process execution.</returns>
        public override string ToString()
            => string.Join(" ", args);
    }
}
