using Oracle.ManagedDataAccess.Client;

namespace CineSeat.Web.Data
{
    public interface IDbConnectionFactory
    {
        OracleConnection CreateConnection();
        Task<OracleConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
    }

    public class OracleConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public OracleConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("OracleDb")
                ?? throw new InvalidOperationException("OracleDb connection string is not configured in appsettings.json.");
        }

        public OracleConnection CreateConnection()
        {
            return new OracleConnection(_connectionString);
        }

        public async Task<OracleConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            var conn = CreateConnection();
            await conn.OpenAsync(cancellationToken);
            return conn;
        }
    }
}
