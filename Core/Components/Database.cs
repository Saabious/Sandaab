using Microsoft.Data.Sqlite;
using Sandaab.Core.Properties;

namespace Sandaab.Core.Components
{
    public class Database : IDisposable
    {
        private SqliteConnection _connection;
        
        public Task InitializeAsync(string filename)
        {
            return Task.Run(
                () =>
                {
                    var path = Path.GetDirectoryName(filename);
                    if (!string.IsNullOrEmpty(path))
                        Directory.CreateDirectory(path);
                    var backupFilename = SandaabContext.GetBackupFilename(filename);

                    var builder = new SqliteConnectionStringBuilder()
                    {
                        DataSource = filename,
                        Mode = SqliteOpenMode.ReadWriteCreate
                    };
                    _connection = new(builder.ToString());

                    int index = 1;
                    do
                        try
                        {
                            _connection.Open();
                            File.Copy(filename, backupFilename, true);
                        }
                        catch (Exception e)
                        {
                            Logger.Warn(string.Format(Messages.FileOpen, Path.GetFileName(filename), e.Message));
                            if (File.Exists(backupFilename))
                            {
                                File.Move(filename, SandaabContext.GetErrorFilename(filename), true);
                                File.Move(backupFilename, filename, true);
                            }
                            else if (File.Exists(filename))
                                File.Delete(filename);
                            else
                                throw;
                        }
                    while (_connection.State != System.Data.ConnectionState.Open && index++ <= 2);

                    if (_connection.State == System.Data.ConnectionState.Open)
                        using (var command = _connection.CreateCommand())
                            try
                            {
                                command.CommandText = Files.SQLCreateTables;
                                command.ExecuteNonQuery();
                            }
                            catch (Exception e)
                            {
                                Logger.Error(e);
                            }
                });
        }

        public void Dispose()
        {
            lock (_connection)
                _connection.Close();

            GC.SuppressFinalize(this);
        }

        public Task<int> ExecuteNoQueryAsync(string sql, SqliteParameter[] parameters)
        {
            return Task.Run(
                () =>
                {
                    return ExecuteNoQuery(sql, parameters, out _);
                });
        }

        public int ExecuteNoQuery(string sql, SqliteParameter[] parameters)
        {
            return ExecuteNoQuery(sql, parameters, out _);
        }

        public int ExecuteNoQuery(string sql, SqliteParameter[] parameters, out long rowId)
        {
            long? id = new();

            var result = DoExecuteNoQuery(sql, parameters, ref id);

            rowId = (long)id;

            return result;
        }

        private int DoExecuteNoQuery(string sql, SqliteParameter[] parameters, ref long? rowId)
        {
            int recordsAffected = 0;

            lock (_connection)
                using (var transaction = _connection.BeginTransaction())
                {
                    using (var command = _connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = sql;
                        if (parameters != null)
                            command.Parameters.AddRange(parameters);

                        try
                        {
                            recordsAffected = command.ExecuteNonQuery();

                            if (recordsAffected == 1 && rowId != null)
                            {
                                command.CommandText = "SELECT last_insert_rowid();";
                                rowId = (long?)command.ExecuteScalar();
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e);
                        }
                    }
                    transaction.Commit();
                }

            return recordsAffected;
        }

        public Task<object> ExecuteSalarAsync(string sql, SqliteParameter[] parameters)
        {
            return Task.Run(
                () =>
                {
                    return ExecuteSalar(sql, parameters);
                });
        }

        public object ExecuteSalar(string sql, SqliteParameter[] parameters)
        {
            object result = null;

            lock (_connection)
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = sql;
                    if (parameters != null)
                        command.Parameters.AddRange(parameters);
                    try
                    {
                        result = command.ExecuteScalar();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }
                }

            return result;
        }

        public Task<SqliteDataReader> ExecuteReaderAsync(string sql, SqliteParameter[] parameters = null)
        {
            return Task.Run(
                () =>
                {
                    return ExecuteReader(sql, parameters);
                });
        }

        public SqliteDataReader ExecuteReader(string sql, SqliteParameter[] parameters = null)
        {
            SqliteDataReader result = null;

            lock (_connection)
            {
                var command = _connection.CreateCommand();
                command.CommandText = sql;
                if (parameters != null)
                    command.Parameters.AddRange(parameters);
                try
                {
                    result = command.ExecuteReader();
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }

            return result;
        }
    }
}
