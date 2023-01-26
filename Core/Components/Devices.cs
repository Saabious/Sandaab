using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Sandaab.Core.Constantes;
using System.Diagnostics;

namespace Sandaab.Core.Components
{
    public class Devices : List<Device>, IDisposable
    {
        public void Dispose()
        {
            lock (this)
                for (var i = Count - 1; i >= 0; i--)
                    if (this[i].State == DeviceState.Found)
                        RemoveAsync(this[i]).Wait();

            GC.SuppressFinalize(this);
        }

        public void Initialize()
        {
            string sql = string.Format("SELECT * FROM Devices;");

            var reader = SandaabContext.Database.ExecuteReader(sql);

            if (reader != null && reader.HasRows)
                while (reader.Read())
                    try
                    {
                        var json = (string)reader["Json"];
                        var device = JsonConvert.DeserializeObject<Device>(json);
                        device.DatabaseId = (long)reader["Id"];

                        if (device.State != DeviceState.Paired)
                            RemoveAsync(device).Wait();
                        else
                            base.Add(device);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }
        }

        public new void Add(Device device)
        {
            Debug.Assert(GetByDeviceId(device.Id) == null);

            lock (this)
            {
                var sql = "INSERT INTO Devices (Json) VALUES (@Json);";
                SqliteParameter[] parameters =
                {
                    new("Json", JsonConvert.SerializeObject(device))
                };
                if (SandaabContext.Database.ExecuteNoQuery(sql, parameters, out var rowId) == 1)
                    device.DatabaseId = rowId;

                base.Add(device);
            }
        }

        public new bool Remove(Device device)
        {
            throw new NotImplementedException(); // Use RemoveAsync
        }

        public Task RemoveAsync(Device device)
        {
            lock (this)
            {
                if (Contains(device))
                    base.Remove(device);

                device.State = DeviceState.Removed;

                string sql = "DELETE FROM Devices WHERE Id=@Id;";
                SqliteParameter[] parameters =
                {
                    new("Id", device.DatabaseId)
                };

                return SandaabContext.Database.ExecuteNoQueryAsync(sql, parameters);
            }
        }

        public Device GetByDeviceId(string Id)
        {
            lock (this)
                foreach (var device in this)
                    if (device.Id.Equals(Id))
                        return device;

            return null;
        }
    }
}
