using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AndroidDeviceToolkit.NET
{
    /// <summary>
    /// Internal utility for executing external processes (adb, sdkmanager, avdmanager, emulator)
    /// with captured standard output and standard error streams.
    /// Supports cancellation tokens for long-running operations and optional standard input redirection.
    /// </summary>
    internal class ProcessRunner
    {
        readonly List<string> standardOutput;
        readonly List<string> standardError;
        readonly Process process;

        /// <summary>
        /// Initializes a new <see cref="ProcessRunner"/> and immediately starts the specified executable
        /// with the given arguments. Uses <see cref="System.Threading.CancellationToken.None"/>.
        /// </summary>
        /// <param name="executable">The path to the executable to run.</param>
        /// <param name="builder">The argument builder containing the command-line arguments.</param>
        public ProcessRunner(FileInfo executable, ProcessArgumentBuilder builder)
            : this(executable, builder, System.Threading.CancellationToken.None)
        { }

        /// <summary>
        /// Initializes a new <see cref="ProcessRunner"/> and immediately starts the specified executable
        /// with the given arguments. The process runs with no window, shell execution disabled,
        /// and redirected stdout/stderr streams. Optionally supports stdin redirection and cancellation.
        /// </summary>
        /// <param name="executable">The path to the executable to run.</param>
        /// <param name="builder">The argument builder containing the command-line arguments.</param>
        /// <param name="cancelToken">A cancellation token that, when triggered, kills the process.</param>
        /// <param name="redirectStandardInput">Whether to redirect standard input for interactive processes.</param>
        public ProcessRunner(FileInfo executable, ProcessArgumentBuilder builder, System.Threading.CancellationToken cancelToken, bool redirectStandardInput = false)
        {
            standardOutput = new List<string>();
            standardError = new List<string>();

            string command = builder.ToString();
            // Configure and start the process with redirected I/O
            process = new Process();
            process.StartInfo.FileName = executable.FullName;
            process.StartInfo.Arguments = command;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            if (redirectStandardInput)
                process.StartInfo.RedirectStandardInput = true;

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    standardOutput.Add(e.Data);
            };
            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    standardError.Add(e.Data);
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (cancelToken != System.Threading.CancellationToken.None)
            {
                cancelToken.Register(() =>
                {
                    try { process.Kill(); }
                    catch { }
                });
            }
        }

        /// <summary>
        /// Gets the exit code of the process. Returns -1 if the process has not yet exited.
        /// </summary>
        public int ExitCode
            => process.HasExited ? process.ExitCode : -1;

        /// <summary>
        /// Gets a value indicating whether the process has exited.
        /// </summary>
        public bool HasExited
            => process?.HasExited ?? false;

        /// <summary>
        /// Forcefully terminates the running process.
        /// </summary>
        public void Kill()
            => process?.Kill();

        /// <summary>
        /// Writes text to the process's standard input without a trailing newline.
        /// Requires that the process was started with <c>redirectStandardInput = true</c>.
        /// </summary>
        /// <param name="input">The text to write to standard input.</param>
        /// <exception cref="InvalidOperationException">Thrown if standard input is not redirected.</exception>
        public void StandardInputWrite(string input)
        {
            if (!process.StartInfo.RedirectStandardInput)
                throw new InvalidOperationException();

            process.StandardInput.Write(input);
        }

        /// <summary>
        /// Writes a line of text to the process's standard input (with trailing newline).
        /// Requires that the process was started with <c>redirectStandardInput = true</c>.
        /// </summary>
        /// <param name="input">The text line to write to standard input.</param>
        /// <exception cref="InvalidOperationException">Thrown if standard input is not redirected.</exception>
        public void StandardInputWriteLine(string input)
        {
            if (!process.StartInfo.RedirectStandardInput)
                throw new InvalidOperationException();

            process.StandardInput.WriteLine(input);
        }

        /// <summary>
        /// Blocks the calling thread until the process exits, then returns the captured output.
        /// Throws an exception if multiple devices/emulators are detected without a serial specified.
        /// </summary>
        /// <returns>A <see cref="ProcessResult"/> containing stdout, stderr, and the exit code.</returns>
        /// <exception cref="Exception">Thrown when ADB detects multiple devices without a serial target.</exception>
        public ProcessResult WaitForExit()
        {
            process.WaitForExit();

            if (standardError?.Any(l => l?.Contains("error: more than one device/emulator") ?? false) ?? false)
                throw new Exception("More than one Device/Emulator detected, you must specify which Serial to target.");

            return new ProcessResult(standardOutput, standardError, process.ExitCode);
        }

        /// <summary>
        /// Asynchronously waits for the process to exit and returns the captured output.
        /// </summary>
        /// <returns>A task that resolves to the <see cref="ProcessResult"/> upon process exit.</returns>
        public Task<ProcessResult> WaitForExitAsync()
        {
            var tcs = new TaskCompletionSource<ProcessResult>();

            Task.Run(() =>
            {
                var r = WaitForExit();
                tcs.TrySetResult(r);
            });

            return tcs.Task;
        }
    }

    /// <summary>
    /// Represents the result of an external process execution, containing captured
    /// standard output, standard error, and the process exit code.
    /// </summary>
    public class ProcessResult
    {
        /// <summary>
        /// The lines of text captured from the process's standard output stream.
        /// </summary>
        public readonly List<string> StandardOutput;

        /// <summary>
        /// The lines of text captured from the process's standard error stream.
        /// </summary>
        public readonly List<string> StandardError;

        /// <summary>
        /// The exit code returned by the process. A value of 0 typically indicates success.
        /// </summary>
        public readonly int ExitCode;

        /// <summary>
        /// Gets a value indicating whether the process completed successfully (exit code 0).
        /// </summary>
        public bool Success
            => ExitCode == 0;

        /// <summary>
        /// Returns all captured output from both standard output and standard error as a single string,
        /// separated by newlines.
        /// </summary>
        /// <returns>Combined stdout and stderr output.</returns>
        public string GetAllOutput()
            => string.Join(Environment.NewLine, StandardOutput.Concat(StandardError));

        /// <summary>
        /// Returns only the standard output as a single string, with lines separated by newlines.
        /// </summary>
        /// <returns>The standard output content.</returns>
        public string GetOutput()
            => string.Join(Environment.NewLine, StandardOutput);

        /// <summary>
        /// Internal constructor used by <see cref="ProcessRunner"/> to create a result instance.
        /// </summary>
        internal ProcessResult(List<string> stdOut, List<string> stdErr, int exitCode)
        {
            StandardOutput = stdOut;
            StandardError = stdErr;
            ExitCode = exitCode;
        }
    }
}
