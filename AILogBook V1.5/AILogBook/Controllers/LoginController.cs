using AILogBook.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace AILogBook.Controllers
{
    public class LoginController : Controller
    {
        private readonly IConfiguration configuration;
        private readonly string ConnectionString;
        private readonly HttpClient httpClient;
        //public SignUpController(UserMasterDal dal) { 
        //    _dal = dal;
        //}

        public LoginController(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            configuration = config;
            httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri("https://Surveyxan.com/cloudapp/app27/CRUD_API26/");
            ConnectionString = configuration.GetConnectionString("DefaultConnection");
        }
        #region This Region for Sign up

        //17 March 2026 Shubham for Sign up View.
        public IActionResult SignUp()
        {
            TempData["CurrentPage"] = "SignUp";
            return View();
        }

        //17 March 2026 Shubham Task to store username, pasword. If exists throw error
        public async Task<IActionResult> Create(UserMasterDto UMasterDto)
        {
            //string currentpage = TempData["CurrentPage"].ToString();
            string? username = UMasterDto.Login_Name;
            string? password = UMasterDto.User_Password;
            string? type = UMasterDto.Type;
            if (await GetByName(username))
            {
                TempData["Error"] = "User already exists!";
                return View("SignUp");
            }
            await User_Signup(username, password, type);
            TempData["Success"] = "Registration Successful!";
            return View("SignIn");
        }

        //17 March 2026 Shubham Functions to To SQL queires fire when Sign up process on going
        public async Task User_Signup(string loginName, string password, string type)
        {
            //To store Username and password
            string sql = "INSERT INTO User_Master(Login_Name, User_Password,Type) VALUES('" + loginName + "', '" + password + "','" + type + "')";

            //To default insert sections name, type with username in user_controls table.
            string sql2 = "INSERT INTO User_Controls(Login_Name, Section,Type) VALUES('" + loginName + "', 'AI Model','" + type + "')";
            string sql3 = "INSERT INTO User_Controls(Login_Name, Section,Type) VALUES('" + loginName + "', 'Category','" + type + "')";
            string sql4 = "INSERT INTO User_Controls(Login_Name, Section,Type) VALUES('" + loginName + "', 'Attribute','" + type + "')";
            string sql5 = "INSERT INTO User_Controls(Login_Name, Section,Type) VALUES('" + loginName + "', 'Topic','" + type + "')";
            string sql6 = "INSERT INTO User_Controls(Login_Name, Section,Type) VALUES('" + loginName + "', 'Chat Conversation','" + type + "')";
            string sql7 = "INSERT INTO User_Controls(Login_Name, Section,Type) VALUES('" + loginName + "', 'Chat session','" + type + "')";
            var input = new UserMasterDto
            {
                SQLStatements = new[] { sql, sql2, sql3, sql4, sql5, sql6, sql7 },
                SQLReturntype = new[] { "0" },
                DBDetails = ConnectionString,
                DBProfile = "connect",
                multiuserflag = "",
                securitykey = "AuthenticationKey",
                securityvalue = "VKS_KEY",
                sqltimeout = "30",
                rollbackcommit = "0",
                encrypt = false
            };

            var jsonContent = JsonConvert.SerializeObject(input);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("api/Data/CRUD_API", content);


            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();

                var outer = JsonConvert.DeserializeObject<dynamic>(result);

                string message = outer[0].errorMessage[0];

                if (message == "Successful")
                {
                    TempData["Success"] = "User inserted successfully!";
                }
                else
                {
                    TempData["Error"] = message;
                }
            }
            else
            {
                TempData["Error"] = "API failed!";
            }
        }

        //17 March 2026 Shubham Functions to get usernames
        public async Task<bool> GetByName(string username)
        {
            string sql = "SELECT 1 FROM User_Master WHERE Login_Name = '" + username + "' AND active = 1";

            var input = new UserMasterDto
            {
                SQLStatements = new[] { sql },
                SQLReturntype = new[] { "0" },
                DBDetails = ConnectionString,
                DBProfile = "connect",
                multiuserflag = "",
                securitykey = "AuthenticationKey",
                securityvalue = "VKS_KEY",
                sqltimeout = "30",
                rollbackcommit = "0",
                encrypt = false
            };

            var jsonContent = JsonConvert.SerializeObject(input);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("api/Data/CRUD_API", content);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();

                var outer = JsonConvert.DeserializeObject<dynamic>(result);

                string innerJson = outer[0].noOfRecordsAffected[0];

                if (string.IsNullOrWhiteSpace(innerJson) || innerJson == "[]")
                {
                    return false; // user NOT found
                }

                return true; // user found
            }

            return false; // API failed
        }

        #endregion

        #region This Region is for Sign in User and default data in user log
        //17 March 2026 Shubham for Sign in View.
        public IActionResult SignIn()
        {
            TempData["CurrentPage"] = "SignIn";
            return View();
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            return RedirectToAction("SignIn");
        }

        //17 March 2026 Shubham for Sign in Validate input data with SQL.
        public async Task<bool> ValidateData(string username, string password)
        {
            string sql = "SELECT 1 FROM User_Master WHERE Login_Name = '" + username + "' AND User_Password = '" + password + "' AND active = 1";

            var input = new UserMasterDto
            {
                SQLStatements = new[] { sql },
                SQLReturntype = new[] { "0" },
                DBDetails = ConnectionString,
                DBProfile = "connect",
                multiuserflag = "",
                securitykey = "AuthenticationKey",
                securityvalue = "VKS_KEY",
                sqltimeout = "30",
                rollbackcommit = "0",
                encrypt = false
            };

            var jsonContent = JsonConvert.SerializeObject(input);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("api/Data/CRUD_API", content);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();

                var outer = JsonConvert.DeserializeObject<dynamic>(result);

                string innerJson = outer[0].noOfRecordsAffected[0];

                if (string.IsNullOrWhiteSpace(innerJson) || innerJson == "[]")
                {
                    return false; // user NOT found
                }

                return true; // user found
            }

            return false; // API failed
        }
        public async Task<IActionResult> UserSign(UserMasterDto UMasterDto)
        {
            string? username = UMasterDto.Login_Name;
            string? password = UMasterDto.User_Password;
            if (await ValidateData(username, password))
            {
                Insert_logData(username);
                HttpContext.Session.SetString("UserName", username);
                return RedirectToAction("Main","Main");
            }
            else
            {
                if (username == null || password == null)
                {
                    TempData["ValidateUser"] = "All fields are required";
                    return RedirectToAction("SignIn");
                }
                else
                {
                    TempData["ValidateUser"] = "Username and password incorrect";
                    return RedirectToAction("SignIn");
                }
            }
        }
        public IActionResult DummyWelcome()
        {
            return View();
        }

        //18 March 2026 Shubham for User log. When sign in Successful default values will add in user log.
        public async Task Insert_logData(string loginName)
        {
            string sql = "INSERT INTO User_Log(Login_Name,Module,Category,Topic_id,Session_created) VALUES('" + loginName + "','','','','')";
            var input = new UserMasterDto
            {
                SQLStatements = new[] { sql },
                SQLReturntype = new[] { "0" },
                DBDetails = ConnectionString,
                DBProfile = "connect",
                multiuserflag = "",
                securitykey = "AuthenticationKey",
                securityvalue = "VKS_KEY",
                sqltimeout = "30",
                rollbackcommit = "0",
                encrypt = false
            };

            var jsonContent = JsonConvert.SerializeObject(input);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("api/Data/CRUD_API", content);


            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();

                var outer = JsonConvert.DeserializeObject<dynamic>(result);

                string message = outer[0].errorMessage[0];

                if (message == "Successful")
                {
                    TempData["Success"] = "Data inserted in userlog successfully!";
                }
                else
                {
                    TempData["Error"] = message;
                }
            }
            else
            {
                TempData["Error"] = "API failed!";
            }
        }

        #endregion


        //public IActionResult Create()
        //{
        //    return View();
        //}

        //[HttpPost]
        //public IActionResult Create(UserMasterDto UMasterDto)
        //{
        //    string ?username = UMasterDto.username;

        //    if (_dal.GetByName(UMasterDto.username))
        //    {
        //        TempData["Error"] = "User already exists!";
        //        return RedirectToAction("Signup");
        //    }
        //    _dal.Insert(UMasterDto);
        //    return RedirectToAction("Signin");
        //}

        //[HttpPost]
        //public IActionResult Signin(UserMasterDto UMasterDto)
        //{
        //    string? username = UMasterDto.username;
        //    string? password = UMasterDto.password;

        //    if (_dal.ValidateData(UMasterDto.username, UMasterDto.password))
        //    {
        //        return RedirectToAction("DummyWelcome");
        //    }
        //    else
        //    {
        //        TempData["ValidateUser"] = "Username and password incorrect";
        //        return RedirectToAction("Signin");

        //    }
        //}

    }
}