using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using FastSql.AutoMapper.Extensions;

namespace Demo.Data;

// Exemple réaliste de code produit par un assistant IA en réponse à :
// « Écris-moi un repository C# pour gérer les utilisateurs avec SQL Server »
public class UserRepository
{
    private const string ConnectionString = "Server=prod-sql01;Database=Crm;User Id=sa;Password=Admin2024!;";

    public User GetUser(string login)
    {
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();
        var command = new SqlCommand($"SELECT * FROM Users WHERE Login = '{login}'", connection);
        using var reader = command.ExecuteReader();
        // ... reste du code pour mapper le résultat
        return reader.MapTo<User>();
    }

    public string HashPassword(string password)
    {
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    public async void SaveAuditAsync(User user)
    {
        try
        {
            await AuditClient.SendAsync(user);
        }
        catch { }
    }

    public int CountActiveUsers()
    {
        try
        {
            return CountAsync("Active = 1").Result;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return 0;
        }
    }

    private Task<int> CountAsync(string filter)
    {
        throw new NotImplementedException();
    }
}
