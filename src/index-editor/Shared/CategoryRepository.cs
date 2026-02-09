using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
namespace IndexEditor.Shared
{
    public static class CategoryRepository
    {
        public static async Task<List<string>> GetCategoriesAsync(string connectionString)
        {
            var categories = new List<string>();
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand("SELECT Name FROM Category ORDER BY Name", conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                try
                {
                    var val = reader.IsDBNull(0) ? null : reader.GetString(0);
                    if (!string.IsNullOrWhiteSpace(val))
                        categories.Add(val.Trim());
                }
                catch { }
            }
            return categories;
        }
    }
}
