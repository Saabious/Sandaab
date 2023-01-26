using Newtonsoft.Json;
using Sandaab.Core.Constantes;

namespace Sandaab.Core.Components
{
    public abstract class LocalDevice
    {
        [JsonIgnore]
        public string Id { get; private set; }
        [JsonIgnore]
        public string Name { get { return GetName(); } }
        [JsonIgnore]
        public DevicePlatform Platform { get; private set; }

        public Task InitializeAsync()
        {
            return Task.Run(
                () =>
                {
                    Platform = GetPlatform();
                    Id = ((int)Platform).ToString() + GetId();
                });
        }

        protected abstract string GetId();

        protected abstract string GetName();

        protected abstract DevicePlatform GetPlatform();
    }
}
