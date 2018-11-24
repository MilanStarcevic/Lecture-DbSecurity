using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DbSecurityDemo.Models;
using System;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using DbSecurityDemo.DataAccess;
using System.Text;

namespace DbSecurityDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration configuration;

        public HomeController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public IActionResult Index([FromQuery] IndexViewModel index)
        {
            return View(index);
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel login)
        {
            User user = GetUserFromDatabase(login);

            string hashedPassword = HashPassword(login, user);

            if (user.PasswordHash != hashedPassword)
            {
                return RedirectToAction("Index", new IndexViewModel { AreCredentialsWrong = true });
            }

            return View(new WelcomeViewModel { Username = login.Username });
        }

        private static string HashPassword(LoginViewModel login, User user)
        {
            // derive a 256-bit subkey (use HMACSHA512 with 10,000 iterations)
            return Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: login.Password,
                salt: Encoding.ASCII.GetBytes(user.Salt),
                prf: KeyDerivationPrf.HMACSHA512,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));
        }

        private User GetUserFromDatabase(LoginViewModel login)
        {
            var user = new User();

            using (var connection = new SqlConnection(configuration.GetConnectionString("DbAccessContext")))
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataReader reader;

                cmd.CommandText = "SELECT Username, PasswordHash, Salt FROM Users WHERE User = " + login.Username;
                cmd.CommandType = CommandType.Text;
                cmd.Connection = connection;

                connection.Open();

                reader = cmd.ExecuteReader();
                user.Username = reader.GetString(0);
                user.PasswordHash = reader.GetString(1);
                user.Salt = reader.GetString(2);
            }

            return user;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
