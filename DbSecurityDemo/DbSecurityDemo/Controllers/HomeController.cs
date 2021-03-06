﻿using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DbSecurityDemo.Models;
using System;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Linq;
using DbSecurityDemo.DataAccess;
using System.Text;

namespace DbSecurityDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly DbAccessContext _dbAccess;

        public HomeController(IConfiguration configuration, DbAccessContext dbAccess)
        {
            _configuration = configuration;
            _dbAccess = dbAccess;
        }

        public IActionResult Index([FromQuery] IndexViewModel index)
        {
            return View(index);
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel login)
        {
            User user = GetUserFromDatabase(login);

            if (user == null)
            {
                return WrongCredentialsMessage();
            }

            string hashedPassword = HashPassword(login, user);

            if (user.PasswordHash != hashedPassword)
            {
                return WrongCredentialsMessage();
            }

            return View(new WelcomeViewModel { Username = login.Username });
        }

        private static string HashPassword(LoginViewModel login, User user)
        {
            var hashBytes = KeyDerivation.Pbkdf2( // Password-Based Key Derivation Function 2
                password: login.Password,
                salt: Encoding.ASCII.GetBytes(user.Salt),
                prf: KeyDerivationPrf.HMACSHA512,
                iterationCount: 10000,
                numBytesRequested: 32); // 256 bits

            return Convert.ToBase64String(hashBytes);
        }

        private User GetUserFromDatabase(LoginViewModel login)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DbAccessContext")))
            {
                SqlCommand cmd = new SqlCommand
                {
                    CommandText = "SELECT Username, PasswordHash, Salt FROM Users WHERE Username = '" + login.Username + "'",
                    CommandType = CommandType.Text,
                    Connection = connection
                };

                connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        return null;
                    }

                    reader.Read();

                    return new User
                    {
                        Username = reader.GetString(0),
                        PasswordHash = reader.GetString(1),
                        Salt = reader.GetString(2)
                    };
                }
            }
        }

        private User GetUserFromDatabaseUsingParameters(LoginViewModel login)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DbAccessContext")))
            {
                SqlCommand cmd = new SqlCommand
                {
                    CommandText = "SELECT Username, PasswordHash, Salt FROM Users WHERE Username = @username",
                    CommandType = CommandType.Text,
                    Connection = connection
                };

                cmd.Parameters.AddWithValue("@username", login.Username);

                connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        return null;
                    }

                    reader.Read();

                    return new User
                    {
                        Username = reader.GetString(0),
                        PasswordHash = reader.GetString(1),
                        Salt = reader.GetString(2)
                    };
                }
            }
        }

        private User GetUserFromDatabaseUsingOrm(LoginViewModel login)
        {
            return _dbAccess.Users.FirstOrDefault(user => user.Username == login.Username);
        }

        #region Helper methods
        private RedirectToActionResult WrongCredentialsMessage()
        {
            return RedirectToAction("Index", new IndexViewModel { AreCredentialsWrong = true });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        #endregion
    }
}
