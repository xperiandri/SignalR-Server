using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNetCore.SignalR.Testing.Common
{
    public static class Utils
    {
        static Utils()
        {
            IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }

        public static string GetSolutionDir(string slnFileName = null)
        {
            var applicationBasePath = PlatformServices.Default.Application.ApplicationBasePath;

            var directoryInfo = new DirectoryInfo(applicationBasePath);
            do
            {
                if (string.IsNullOrEmpty(slnFileName))
                {
                    if (directoryInfo.EnumerateFiles("*.sln").Any())
                    {
                        return directoryInfo.FullName;
                    }
                }
                else
                {
                    if (File.Exists(Path.Combine(directoryInfo.FullName, slnFileName)))
                    {
                        return directoryInfo.FullName;
                    }
                }

                directoryInfo = directoryInfo.Parent;
            }
            while (directoryInfo.Parent != null);

            throw new InvalidOperationException($"Solution root could not be found using {applicationBasePath}");
        }

        public static bool IsWindows { get; }

        public static int RunPhantomJS(string testUrl, out string stdOut, out string stdErr)
        {
            var solutionDir = GetSolutionDir();
            var isLocalInstall = Directory.Exists(
                Path.GetFullPath(Path.Combine(solutionDir, "bin/nodejs/node_modules/phantomjs-prebuilt")));

            var phantomJSCommand =
                isLocalInstall
                    ? Path.GetFullPath(Path.Combine(solutionDir, "bin/nodejs/node_modules/phantomjs-prebuilt/lib/phantom/bin/phantomjs"))
                    : "phantomjs";

            if (IsWindows)
            {
                phantomJSCommand += ".exe";
            }

            var phantomJSExecutable =
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "phantomjs.exe"
                : "phantomjs";

            var jasmineRunnerPath = Path.GetFullPath(
                Path.Combine(solutionDir, "test/Microsoft.AspNetCore.SignalR.Testing.Common/run-jasmine2.js"));

            return RunProgram($"{phantomJSCommand}", $"\"{jasmineRunnerPath}\" {testUrl}", out stdOut, out stdErr);
        }

        private static int RunProgram(string name, string args, out string stdOut, out string stdErr)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = name,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            stdErr = stdOut = "";

            try
            {
                var process = Process.Start(processStartInfo);
                process.WaitForExit();
                stdOut = process.StandardOutput.ReadToEnd();
                stdErr = process.StandardError.ReadToEnd();
                return process.ExitCode;
            }
            catch
            {
                Console.WriteLine($"Process could not be started. Program: {name} args: {args}");
                throw;
            }
        }
    }
}
