using Newtonsoft.Json;
using Sandaab.Core.Components;
using Sandaab.Core.Properties;
using System.Reflection;

namespace Sandaab.Core
{
    public class SandaabContext : IDisposable
    {
        public static IApp App { get; private set; }
        public static Database Database { get; private set; }
        public static Dispatcher Dispatcher { get; private set; }
        [JsonIgnore]
        public static Devices Devices { get; private set; }
        public Version InstalledVersion { get; private set; }
        [JsonIgnore]
        public static bool IsDisposing { get; private set; }
        [JsonIgnore]
        private readonly Logger _logger;
        public static LocalDevice LocalDevice { get; protected set; }
        public static Network Network { get; protected set; }
        public static Notifications Notifications { get; private set; }
        public static Screen Screen { get; private set; }
        private string _settingsFilename;

        public SandaabContext(IApp app, Network network, LocalDevice localDevice, Notifications notifications, Screen screen)
        {
            App = app;
            Network = network;
            LocalDevice = localDevice;
            Notifications = notifications;
            Screen = screen;

            Database = new();
            Devices = new();
            Dispatcher = new();
            _logger = new();

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };
        }

        public Task InitializeAsync(string logFilename, string databaseFilename, string settingsFilename)
        {
            _settingsFilename = settingsFilename;
            InstalledVersion = Assembly.GetExecutingAssembly().GetName().Version;

            return Task.Run
            (
                () =>
                {
                    try
                    {
                        _logger.InitializeAsync(logFilename, Convert.ToInt32(Config.LogFileSize));
                        Logger.Info("-------------------------------------------------------------------------");
                        Logger.Info(Config.AppName + " " + InstalledVersion.ToString() + " started");

                        Task[] tasks = new[]
                        {
                            Database.InitializeAsync(databaseFilename),
                            LoadSettingsAsync(),
                            LocalDevice.InitializeAsync(),
                            Notifications.InitializeAsync(),
                            Screen.InitializeAsync(),
                        };

                        Task.WaitAll(tasks);

                        Devices.Initialize();
                        Network.Initialize();
                        Logger.Info("Device name: " + LocalDevice.Name);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                        throw;
                    }
                }
            );
        }

        public void Dispose()
        {
            IsDisposing = true;

            SaveSettings();

            Screen.Dispose();
            Notifications.Dispose();
            Devices.Dispose();
            Database.Dispose();
            Network.Dispose();
            _logger.Dispose();

            GC.SuppressFinalize(this);
        }

        public static string GetBackupFilename(string filename)
        {
            return Path.GetDirectoryName(filename) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(filename) + Config.BackupFilenameExtension + Path.GetExtension(filename);
        }

        public static string GetErrorFilename(string filename)
        {
            return Path.GetDirectoryName(filename) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(filename) + Config.ErrorFilenameExtension + Path.GetExtension(filename);
        }

        public bool SaveSettings()
        {
            try
            {
                var settings = new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Include,
                    DefaultValueHandling = DefaultValueHandling.Include
                };
                string json = JsonConvert.SerializeObject(this, Formatting.Indented, settings);

                Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilename));
                File.WriteAllText(_settingsFilename, json);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }
        }

        public Task<bool> LoadSettingsAsync()
        {
            return Task.Run(
                () =>
                {
                    string backupFilename = GetBackupFilename(_settingsFilename);

                    while (File.Exists(_settingsFilename))
                        try
                        {
                            string json = File.ReadAllText(_settingsFilename);
                            JsonConvert.PopulateObject(json, this);

                            File.Copy(_settingsFilename, backupFilename, true);

                            return true;
                        }
                        catch (Exception e)
                        {
                            Logger.Warn(string.Format(Messages.FileOpen, Path.GetFileName(_settingsFilename), e.Message));
                            if (File.Exists(backupFilename))
                            {
                                File.Move(_settingsFilename, GetErrorFilename(_settingsFilename), true);
                                File.Move(backupFilename, _settingsFilename, true);
                            }
                            else if (File.Exists(_settingsFilename))
                            {
                                File.Delete(_settingsFilename);
                                return false;
                            }
                        }

                    return false;
                });
        }

        public static string GenerateToken()
        {
            const int length = 20;

            return new string
            (
                Enumerable.Repeat("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789~!@#$%^&*()_-+={[}]|:;<,>.?/", length)
                    .Select(s => s[new Random().Next(s.Length)])
                    .ToArray()
            );
        }
    }
}