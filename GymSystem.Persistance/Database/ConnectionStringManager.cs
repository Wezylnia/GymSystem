using Microsoft.Extensions.Configuration;

namespace GymSystem.Persistance.Database;

public class ConnectionStringManager {
    private readonly string _connectionStringKey;
    private readonly string _settingsFileName;

    public ConnectionStringManager(string connectionStringKey, string settingsFileName) {
        _connectionStringKey = connectionStringKey;
        _settingsFileName = settingsFileName;
    }

    public string GetConnectionString() {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(_settingsFileName, optional: false, reloadOnChange: true)
            .Build();

        var connectionString = configuration.GetConnectionString(_connectionStringKey);

        if (string.IsNullOrEmpty(connectionString)) {
            throw new InvalidOperationException($"Connection string for '{_connectionStringKey}' not found in {_settingsFileName}");
        }

        return connectionString;
    }
}
