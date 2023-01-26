using Sandaab.Core.Properties;
using System.Text;
using System.Text.RegularExpressions;

namespace Sandaab.Core.Components
{
    public class Logger : IDisposable
    {
        public enum LogLevel
        {
            Point,
            Debug,
            Info,
            Warn,
            Error,
            Fatal,
            None,
        }

        private class StackTrace : System.Diagnostics.StackTrace
        {
            public StackTrace(Exception e)
                : base(e, true)
            {
            }

            public override string ToString()
            {
                string result = "";
                using(var reader = new StringReader(base.ToString()))
                {
                    string line;
                    while (!string.IsNullOrEmpty(line = reader.ReadLine()))
                        if (Regex.Match(line, "^ +at +.*:line ").Success)
                            result += line + Environment.NewLine;
                }

                return result;
            }
        }

        private static string _filename;
        private static FileStream _fileStream;
        private static int _fileSize;
        private static StringBuilder _stringBuilder = null;
        private static List<Task<bool>> _tasks;

        public Logger()
        {
            _fileStream = null;
            _stringBuilder = new StringBuilder();
            _tasks = new();
        }

        public Task InitializeAsync(string filename, int fileSize)
        {
            _filename = filename;
            _fileSize = fileSize;

            return OpenAsync();
        }

        public void Dispose()
        {
            Info(string.Format(Messages.LogStop, Config.AppName));
            Close();
            GC.SuppressFinalize(this);
        }

        private static Task<bool> OpenAsync()
        {
            Task<bool> task = null;
            task = Task.Run(
                () =>
                {
                    if (_stringBuilder != null)
                        lock (_stringBuilder)
                            try
                            {
                                Close();

                                try
                                {
                                    string directoryName = Path.GetDirectoryName(_filename);
                                    if (!string.IsNullOrEmpty(directoryName))
                                        Directory.CreateDirectory(directoryName);
                                    _fileStream = File.Open(_filename, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
                                    _fileStream.Seek(0, SeekOrigin.End);

                                    WriteStringBuilder();
                                }
                                catch
                                {
                                }

                                return true;
                            }
                            catch
                            {
                            }
                            finally
                            {
                                lock (_tasks)
                                    _tasks.Remove(task);
                            }

                    return false;
                });

            lock (_tasks)
                _tasks.Add(task);

            return task;
        }

        public static void Point(int point)
        {
            Point(point.ToString());
        }

        public static void Point(string message = null)
        {
            var stackTrace = new System.Diagnostics.StackTrace(true);
            var frame = stackTrace.GetFrame(1);
            var method = frame.GetMethod();

            message += Environment.NewLine + "   at "
                + method.ReflectedType.UnderlyingSystemType.FullName + "." + method.Name
                + "(";

            bool first = true;
            foreach (var parameter in method.GetParameters())
            {
                if (first) first = false; else message += ", ";
                message += parameter.ParameterType.FullName + " " + parameter.Name;
            }

            message += ") in " + frame.GetFileName() + ":line " + frame.GetFileLineNumber();

            WriteEntryAsync(message, LogLevel.Point);
        }

        public static void Debug(string message)
        {
            WriteEntryAsync(message, LogLevel.Debug);
        }

        public static void Info(string message)
        {
            WriteEntryAsync(message, LogLevel.Info);
        }

        public static void Warn(string message)
        {
            WriteEntryAsync(message, LogLevel.Warn);
        }

        public static void Error(string message)
        {
            WriteEntryAsync(message, LogLevel.Error);
        }

        public static void Error(Exception e, string extraLine)
        {
            Error(e, string.IsNullOrEmpty(extraLine) ? null : new[] { extraLine });
        }

        public static void Error(Exception e, string[] extraLines = null)
        {
            Exception(e, extraLines);
        }

        public static void Fatal(Exception e, string extraLine)
        {
            Fatal(e, string.IsNullOrEmpty(extraLine) ? null : new[] { extraLine });
        }

        public static void Fatal(Exception e, string[] extraLines = null)
        {
            Exception(e, extraLines, LogLevel.Fatal);
            WriteStringBuilder();
        }

        private static void Exception(Exception e, string[] extraLines = null, LogLevel logLevel = LogLevel.Error)
        {
            lock (_stringBuilder)
            {
                var now = DateTimeOffset.Now;

                var message = e.GetType().ToString() + ": " + e.Message.Trim();
                if (e.HResult != 0)
                    message += " (HResult: 0x" + e.HResult.ToString("X8") + ")";
                WriteEntry(now, message, logLevel);

                if (e.StackTrace != null)
                    Write(new StackTrace(e).ToString());

                do
                { 
                    e = e.InnerException;
                    if (e != null)
                    {
                        Write("from " + e.GetType().FullName + ": " + e.Message + Environment.NewLine);

                        if (e.StackTrace != null)
                            Write(new StackTrace(e).ToString());
                    }
                } while (e != null);

                if (extraLines != null)
                {
                    WriteLine("   --------");
                    foreach (var line in extraLines)
                        WriteLine("   " + line);
                }
            }
        }

        private static void WriteEntryAsync(string message, LogLevel logLevel)
        {
            var now = DateTimeOffset.Now;

#if DEBUG
            WriteEntry(now, message, logLevel);
#else
            Task.Run(
                () =>
                {
                    WriteEntry(now, message, logLevel);
                });
#endif
        }

        private static void WriteEntry(DateTimeOffset now, string message, LogLevel logLevel)
        {
            if (_stringBuilder != null)
                lock (_stringBuilder)
                    Write(now.DateTime + "." + now.Millisecond.ToString("D3")
                        + " | " + logLevel.ToString() + " | "
                        + message + Environment.NewLine);
        }

        private static void WriteLine(string line)
        {
            Write(line + Environment.NewLine);
        }

        private static void Write(string line)
        {
#if DEBUG
            System.Diagnostics.Debug.Write(line);
#endif

            _stringBuilder.Append(line);

            if (_fileStream != null)
                if (_fileStream.Length < _fileSize)
                    WriteStringBuilder();
                else
                {
                    Close();
                    File.Move(
                        _filename,
                        Path.GetDirectoryName(_filename) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(_filename) + ".1" + Path.GetExtension(_filename),
                        true);
                    OpenAsync();
                }
        }

        private static void WriteStringBuilder()
        {
            _fileStream.Write(Encoding.UTF8.GetBytes(_stringBuilder.ToString(0, _stringBuilder.Length)));
            _fileStream.Flush();

            _stringBuilder.Clear();
        }

        private static void Close()
        {
            if (_fileStream != null)
                lock (_stringBuilder)
                    try
                    {
                        Task[] tasks;
                        lock (_tasks)
                            tasks = _tasks.ToArray();
                        Task.WaitAll(tasks);

                        _fileStream.Close();
                        _fileStream.Dispose();
                    }
                    catch
                    {
                    }
                    finally
                    {
                        _fileStream = null;
                    }
        }
    }
}
