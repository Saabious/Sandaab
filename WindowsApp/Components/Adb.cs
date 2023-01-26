using Sandaab.Core;
using Sandaab.Core.Components;
using Sandaab.Core.Properties;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using static Sandaab.Core.Constantes.Win32Errors;

namespace Sandaab.WindowsApp.Components
{
    internal class Adb : IDisposable
    {
        public enum ErrorCode
        {
            Success,
            Unknown,
            Unauthorized,
            Offline,
            NoDevices,
            ProtocolFault,
            InvalidDestinationPort,
        }

        public record PortMapping
        {
            public string Device;
            public int RemotePort;
            public int LocalPort;
        }

        public class AdbException : Exception
        {
            public ErrorCode ErrorCode;

            public AdbException(string message, ErrorCode errorCode)
                : base(message)
            {
                ErrorCode = errorCode;
            }
        }

        public ErrorCode LastErrorCode { get; private set; }

        public void Dispose()
        {
            RemovePortMappings();
        }

        private string[] Execute(string arguments)
        {
            Collection<string> output = new();

            var startInfo = new ProcessStartInfo()
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                FileName = Path.GetDirectoryName(Environment.ProcessPath) + "\\Binaries\\adb.exe",
                Arguments = arguments,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
            };

            Process process;
            Collection<string> errorLines = new();

            LastErrorCode = ErrorCode.Success;

            process = Process.Start(startInfo);
            if (process == null)
                throw new Exception("Process couldn't be started.");

            while (true)
            {
                var line = process.StandardOutput.ReadLine();
                if (line != null)
                    output.Add(line);
                else if (process.ExitCode == (int)ERROR_SUCCESS)
                    return output.ToArray();
                else
                    break;
            }

            LastErrorCode = ErrorCode.Unknown;

            while (true)
            {
                var line = process.StandardError.ReadLine();
                if (line != null)
                    errorLines.Add(line);
                else
                {
                    if (errorLines.Count > 0)
                    {
                        var match = Regex.Match(errorLines[0], "^error: *(.+)$", RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            if (match.Groups[1].Value == "device unauthorized.")
                                LastErrorCode = ErrorCode.Unauthorized;
                            else if (match.Groups[1].Value == "device offline")
                                LastErrorCode = ErrorCode.Offline;
                            else if (match.Groups[1].Value == "no devices/emulators found")
                                LastErrorCode = ErrorCode.NoDevices;
                            else if (match.Groups[1].Value == "protocol fault (couldn't read status): connection reset")
                                LastErrorCode = ErrorCode.ProtocolFault;

                            throw new AdbException(string.Format(Messages.AdbExecutionFailure, LastErrorCode), LastErrorCode);
                        }
                        else
                        {
                            match = Regex.Match(errorLines[0], "^adb.exe: error: Invalid destination port: '(\\d+)'$", RegexOptions.IgnoreCase);
                            if (match.Success)
                                LastErrorCode = ErrorCode.InvalidDestinationPort;
                        }
                    }

                    throw new Exception(string.Format(Messages.AdbExecutionFailure, "0x" + process.ExitCode.ToString("X")));
                }
            }
        }

        public Task<Collection<string>> GetDevicesAsync()
        {
            return Task.Run(
                () =>
                {
                    Collection<string> devices = new();

                    var output = Execute("devices").ToList();

                    if (output.Count > 0)
                        output.RemoveAt(0);
                    if (string.IsNullOrEmpty(output.Last()))
                        output.RemoveAt(output.Count - 1);

                    for (var i = 0; i < output.Count; i++)
                    {
                        var match = Regex.Match(output[i], "^([^\t ]+)");
                        if (!match.Success)
                            Logger.Error(string.Format(Messages.InvalidAdbResult, output[i]));
                        else
                            devices.Add(match.Groups[1].Value);
                    }

                    return devices;
                });
        }

        private Collection<PortMapping> GetForwards()
        {
            Collection<PortMapping> forwards = new();

            var output = Execute("forward --list").ToList();

            for (var i = output.Count - 1; i >= 0; i--)
            {
                var match = Regex.Match(output[i], "^\\* ");
                if (match.Success)
                    output.RemoveAt(i);
            }
            if (output.Count > 0
                && string.IsNullOrEmpty(output.Last()))
                output.RemoveAt(output.Count - 1);

            for (var i = 0; i < output.Count; i++)
            {
                var match = Regex.Match(output[i], "^(.+) tcp\\:(\\d+) tcp\\:(\\d+)", RegexOptions.IgnoreCase);
                if (!match.Success)
                    Logger.Error(string.Format(Messages.InvalidAdbResult, output[i]));
                else
                {
                    var forward = new PortMapping()
                    {
                        Device = match.Groups[1].Value,
                        LocalPort = Convert.ToInt32(match.Groups[2].Value),
                        RemotePort = Convert.ToInt32(match.Groups[3].Value),
                    };
                    forwards.Add(forward);
                }
            }

            return forwards;
        }

        private Collection<PortMapping> GetReverses()
        {
            Collection<PortMapping> reverses = new();

            var output = Execute("reverse --list").ToList();

            for (var i = output.Count - 1; i >= 0; i--)
            {
                var match = Regex.Match(output[i], "^\\* ");
                if (match.Success)
                    output.RemoveAt(i);
            }
            if (output.Count > 0
                && string.IsNullOrEmpty(output.Last()))
                output.RemoveAt(output.Count - 1);

            for (var i = 0; i<output.Count; i++)
            {
                var match = Regex.Match(output[i], "^(.+) tcp\\:(\\d+) tcp\\:(\\d+)", RegexOptions.IgnoreCase);
                if (!match.Success)
                    Logger.Error(string.Format(Messages.InvalidAdbResult, output[i]));
                else
                {
                    var reverse = new PortMapping()
                    {
                        Device = match.Groups[1].Value,
                        RemotePort = Convert.ToInt32(match.Groups[2].Value),
                        LocalPort = Convert.ToInt32(match.Groups[3].Value),
                    };
                    reverses.Add(reverse);
                }
            }

            return reverses;
        }

        public Task<Collection<PortMapping>> UpdatePortMappingsAsync()
        {
            return Task.Run(
                () =>
                {
                    Collection<PortMapping> portMappings = new();

                    var devices = GetDevicesAsync().Result;
                    var forwards = GetForwards();
                    var reverses = GetReverses();

                    foreach (var device in devices)
                    {
                        var portMapping = new PortMapping()
                        {
                            Device = device
                        };

                        var remotePort = Config.TcpPort;
                        foreach (var forward in forwards)
                            if (forward.Device == device && forward.RemotePort == remotePort)
                                portMapping.LocalPort = forward.LocalPort;
                        if (portMapping.LocalPort == 0)
                            AddForward(device, out portMapping.LocalPort, remotePort);

                        foreach (var reverse in reverses)
                            // "adb.exe reverse --list" does not list the reverse device
                            if (/* reverse.Device == device && */ reverse.LocalPort == SandaabContext.Network.TcpListenerPort)
                                portMapping.RemotePort = reverse.RemotePort;
                        if (portMapping.RemotePort == 0)
                            AddReverse(device, Config.AdbPort, SandaabContext.Network.TcpListenerPort);

                        portMappings.Add(portMapping);
                    }

                    return portMappings;
                });
        }

        private void AddForward(string device, out int localPort, int remotePort)
        {
            localPort = 0;
            var output = Execute(string.Format("-s {0} forward tcp:{1} tcp:{2}", device, 0, remotePort)).ToList();

            if (output.Count != 1)
                throw new Exception(Messages.MissingAdbResult);

            var match = Regex.Match(output[0], "^(\\d+)$");
            if (!match.Success)
                throw new Exception(string.Format(Messages.InvalidAdbResult, output[0]));

            localPort = Convert.ToInt32(match.Groups[1].Value);
        }

        private void AddReverse(string device, int remotePort, int localPort)
        {
            Execute(string.Format("-s {0} reverse tcp:{1} tcp:{2}", device, remotePort, localPort));
        }

        public Task RemovePortMappingsAsync()
        {
            return Task.Run(
                () =>
                {
                    RemovePortMappings();
                });
        }

        private void RemovePortMappings()
        {
            var forwards = GetForwards();
            foreach (var forward in forwards)
                if (forward.RemotePort == Config.TcpPort)
                    RemoveForward(forward.Device, forward.LocalPort);

            var reverses = GetReverses();
            foreach (var reverse in reverses)
                if (reverse.LocalPort == SandaabContext.Network.TcpListenerPort)
                    RemoveReverse(reverse.Device, reverse.RemotePort);
        }

        private void RemoveForward(string device, int localPort)
        {
            Execute(string.Format("-s {0} forward --remove tcp:{1}", device, localPort));
        }

        private void RemoveReverse(string device, int remotePort)
        {
            Execute(string.Format("reverse --remove tcp:{1}", device, remotePort));
        }
    }
}
