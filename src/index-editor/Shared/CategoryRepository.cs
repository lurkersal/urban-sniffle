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
                categories.Add(reader.GetString(0));
            }
            return categories;
        }
    }
}
