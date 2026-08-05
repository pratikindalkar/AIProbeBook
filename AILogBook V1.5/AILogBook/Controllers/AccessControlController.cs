using AILogBook.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace AILogBook.Controllers
{
    public class AccessControlController : Controller
    {
        private readonly IConfiguration configuration;
        private readonly string ConnectionString;
        private readonly HttpClient httpClient;
        public AccessControlController(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            configuration = config;
            httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri("https://Surveyxan.com/cloudapp/app27/CRUD_API26/");
            ConnectionString = configuration.GetConnectionString("DefaultConnection");
        }
        #region Region for Access Control page, User wise access control - Specific user for specific access
        //23 March 2026 Shubham Access control View page (To update access of Specific user)
        public async Task<IActionResult> Access()
        {
            //await AccessControl();
            await LoginNameList();
            return View("Access");
        }

        //23 March 2026 Shubham Get data from user control table (UserWise data)
        [HttpPost]
        public async Task<IActionResult> UserAccess(AccessConrolDto acdto)
        {
            // Now it will have form values
            var loginName = acdto.Login_Name;
            await LoginNameList();
            //await UserControl(loginName);
            string sql = "Select * from User_Controls where Login_Name = '" + loginName + "';";
            var input = new AccessConrolDto
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

                var data = JsonConvert.DeserializeObject<List<AccessConrolDto>>(innerJson);
                ViewBag.Data = data;
                if (loginName != null)
                {
                    ViewBag.username = loginName;
                }
            }
            return View("Access");
        }


        //23 March 2026 Shubham Update Access Controls by user name  wise. Specific Access to sepecific person
        [HttpPost]
        public async Task<IActionResult> UpdateUser_Controls(List<AccessConrolDto> data, int index, string Login_Name)
        {
            var selectedRow = data[index];

            int autoid = selectedRow.Auto_Id;
            bool Add = selectedRow.Add;
            bool Edit = selectedRow.Edit;
            bool Download = selectedRow.Download;
            bool M_Active = selectedRow.M_active;
            //bool Update = selectedRow.Update;
            bool Delete = selectedRow.Delete;

            string sql = "UPDATE User_Controls SET " +
                         "[Add] = " + (Add ? 1 : 0) + ", " +
                         "[Edit] = " + (Edit ? 1 : 0) + ", " +
                         "[Delete] = " + (Delete ? 1 : 0) + ", " +
                         "[Download] = " + (Download ? 1 : 0) + ", " +
                         "[M_active] = " + (M_Active ? 1 : 0) + " " +
                         "WHERE Auto_Id = " + autoid;

            var input = new AccessConrolDto
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
                TempData["SuccessMsg"] = "Data Updated";

                await LoginNameList();

                // Reload table
                return await UserAccess(new AccessConrolDto { Login_Name = Login_Name });
            }

            return Json(new { success = false });
        }


        //23 March 2026 Shubham For Search box to get all users name who signed up
        private async Task LoginNameList()
        {
            string sql = "select Auto_Id, Login_Name from User_Master where Active = 1;";
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
            //var response = await httpClient.PostAsync("api/testing/CRUD_API", content);


            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();

                var outer = JsonConvert.DeserializeObject<dynamic>(result);

                string innerJson = outer[0].noOfRecordsAffected[0];

                var data = JsonConvert.DeserializeObject<List<UserMasterDto>>(innerJson);
                ViewBag.LoginName = data;
                ViewBag.loginsCount = data != null ? data.Count : 0;
            }
        }

        #endregion

        #region for master access control, Category wise access control.
        //23 March 2026 Shubham Master Access control View page (To update access of for all but category wise)
        public async Task<IActionResult> Master()
        {
            await AccessControl();
            return View("Master");
        }

        //Shubham 23 March 2026 Logic is for overall Access control. User category (admin, executive) wise give access
        //Get data from access control table
        private async Task AccessControl()
        {
            string sql = "Select * from AccessControl;";
            var input = new AccessConrolDto
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

                var data = JsonConvert.DeserializeObject<List<AccessConrolDto>>(innerJson);
                ViewBag.Data = data;
            }
        }

        //public async Task<IActionResult> AccessControl()
        //{
        //    string sql = "SELECT * FROM AccessControl";

        //    var input = new UserMasterDto
        //    {
        //        SQLStatements = new[] { sql },
        //        SQLReturntype = new[] { "0" }, // for SELECT
        //        DBDetails = ConnectionString,
        //        DBProfile = "connect",
        //        securitykey = "AuthenticationKey",
        //        securityvalue = "VKS_KEY"
        //    };

        //    var response = await httpClient.PostAsync("api/Data/CRUD_API",
        //        new StringContent(JsonConvert.SerializeObject(input), Encoding.UTF8, "application/json"));

        //    var result = await response.Content.ReadAsStringAsync();
        //    Console.WriteLine(result);
        //    var json = JsonConvert.DeserializeObject<dynamic>(result);
        //    var data = JsonConvert.DeserializeObject<List<AccessConrolDto>>(json.Table.ToString()); return View(data);
        //}

        //23 March 2026 Shubham Update Access Controls by Category wise (Admin, executive)
        public async Task<IActionResult> User_Access(List<AccessConrolDto> data)
        {
            foreach (var item in data)
            {
                string sql = "UPDATE AccessControl " +
                "SET [Add] = " + (item.Add ? 1 : 0) + ", " +
                "[Edit] = " + (item.Edit ? 1 : 0) + ", " +
                "[Update] = " + (item.Update ? 1 : 0) + ", " +
                "[Delete] = " + (item.Delete ? 1 : 0) + " " +
                "WHERE Section = '" + item.Section + "' " +
                "AND Type = '" + item.Type + "'";
                string sql2 = "UPDATE User_Controls " +
                "SET [Add] = " + (item.Add ? 1 : 0) + ", " +
                "[Edit] = " + (item.Edit ? 1 : 0) + ", " +
                "[Update] = " + (item.Update ? 1 : 0) + ", " +
                "[Delete] = " + (item.Delete ? 1 : 0) + " " +
                "WHERE Section = '" + item.Section + "' " +
                "AND Type = '" + item.Type + "'";

                var input = new AccessConrolDto
                {
                    SQLStatements = new[] { sql, sql2 },
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

                await httpClient.PostAsync("api/Data/CRUD_API", content);
            }

            TempData["SuccessMsg"] = "All data saved successfully";
            return RedirectToAction("Master");
        }

        #endregion



        //25March 2026 Shubham User Approval Logic here ---Pending
        #region Region for User_Master Active control
        public async Task<IActionResult> UserActivePage()
        {
            await ActiveControl();
            return View("UserActivePage");
        }
        private async Task ActiveControl()
        {
            string sql = "Select * from User_Master;";
            var input = new AccessConrolDto
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

                var data = JsonConvert.DeserializeObject<List<AccessConrolDto>>(innerJson);
                ViewBag.Data = data;
            }
        }

        #endregion

        public async Task<IActionResult> User()
        {
            string Qry = "Select * from User_Master";
            var input = new InputsValue
            {
                SQLStatements = new[] { Qry },
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
            var json = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("api/Data/CRUD_API", json);
            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<List<Result>>(responseData);

                if (result != null && result[0].OverAllError[0] == "1")
                {
                    string JsonData = result[0].NoOfRecordsAffected[0].ToString();
                    var LogList = JsonConvert.DeserializeObject<List<UserMasterDto>>(JsonData);
                    ViewBag.LoginUser = LogList;
                    ViewBag.LoginCount = LogList.Count;
                }
            }
            return View("User");
        }
        public async Task<IActionResult> AddUser()
        {
            return View("AddUser");
        }
        [HttpPost]
        public async Task<IActionResult> AddUser(UserMasterDto userMasterDto)
        {
            ModelState.Remove("Auto_Id");

            if (ModelState.IsValid)
            {
                string Qry = "Insert INTO User_Master (Login_Name, User_Password, Type, Level, Active) VALUES (" +
                    $"'{userMasterDto.Login_Name}', '{userMasterDto.User_Password}', '{userMasterDto.Type}', '{userMasterDto.Level}', '{userMasterDto.active}')";

                var input = new InputsValue
                {
                    SQLStatements = new[] { Qry },
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

                var json = JsonConvert.SerializeObject(input);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("api/Data/CRUD_API", content);
                //var response = await httpClient.PostAsync("api/testing/CRUD_API", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<List<Result>>(responseData);
                    if (result != null && result[0].OverAllError[0] == "1")
                    {
                        TempData["Msg"] = "User added successfully!";
                        return RedirectToAction("User");
                    }
                    else
                    {
                        string msg = $"Database Error: {result[0].ErrorMessage[0]}";
                        if (msg.Contains("The duplicate key value"))
                        {
                            TempData["Msg"] = $"Duplicate Error: The User Name {userMasterDto.Login_Name} already exists.";
                        }
                        else
                        {
                            TempData["Msg"] = $"Database Error: {result[0].ErrorMessage[0]}";
                        }
                    }
                }
                else
                {

                    TempData["Msg"] = "Server Error. Please Contact administrator.";
                }
            }
            else
            {
                var errorMessages = string.Join(" \\n ", ModelState.Values
                                        .SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage));

                TempData["Msg"] = "Please fix the following: \\n " + errorMessages;

            }
            await User();
            return View("AddUser", userMasterDto);
        }

        public async Task<IActionResult> EditUser(int id)
        {
            string GetEditQry = $"Select * from User_Master Where Auto_Id = {id}";
            var input = new InputsValue
            {
                SQLStatements = new[] { GetEditQry },
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

            var Jsoncontent = JsonConvert.SerializeObject(input);
            var content = new StringContent(Jsoncontent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("api/Data/CRUD_API", content);
            //var response = await httpClient.PostAsync("api/testing/CRUD_API", content);

            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<List<Result>>(responseData);
                if (result != null && result[0].OverAllError[0] == "1")
                {
                    var LogList = JsonConvert.DeserializeObject<List<UserMasterDto>>(result[0].NoOfRecordsAffected[0]);
                    var User = LogList.FirstOrDefault();
                    return View("AddUser", User);
                }
                else
                {
                    TempData["Msg"] = $"Database Error: {result[0].ErrorMessage[0]}";
                }
            }
            else
            {
                TempData["Msg"] = "Server Error. Please Contact administrator.";
            }
            return RedirectToAction("User");
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(UserMasterDto userMasterDto)
        {

            if (ModelState.IsValid)
            {
                string UpdateQry = $"Update User_Master SET Login_Name = '{userMasterDto.Login_Name}', User_Password = '{userMasterDto.User_Password}', Type = '{userMasterDto.Type}', " +
                    $"Level = {userMasterDto.Level},Active = '{userMasterDto.active}' Where Auto_Id = '{userMasterDto.Auto_Id}'";

                var input = new InputsValue
                {
                    SQLStatements = new[] { UpdateQry },
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
                //var response = await httpClient.PostAsync("api/testing/CRUD_API", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<List<Result>>(responseData);

                    if (result != null && result[0].OverAllError[0] == "1")
                    {
                        TempData["Msg"] = "User updated successfully!";
                        return RedirectToAction("User");
                    }
                    else
                    {
                        TempData["Msg"] = $"Database Error: {result[0].ErrorMessage[0]}";
                    }
                }
                else
                {
                    TempData["Msg"] = "Server Error. Please Contact administrator.";
                }
            }
            else
            {
                var errorMessages = string.Join(" \\n ", ModelState.Values
                                        .SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage));

                TempData["Msg"] = "Please fix the following: \\n " + errorMessages;
            }
            await User();
            return View("AddUser", userMasterDto);
        }

        public async Task<IActionResult> DeleteUser(int id)
        {
            string GetDeleteQry = $"Delete from User_Master Where Auto_Id = {id}";
            var input = new InputsValue
            {
                SQLStatements = new[] { GetDeleteQry },
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
            //var response = await httpClient.PostAsync("api/testing/CRUD_API", content);

            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<List<Result>>(responseData);
                if (result != null && result[0].OverAllError[0] == "1")
                {
                    TempData["Msg"] = "The User was deleted successfully!";
                }
                else
                {
                    TempData["Msg"] = "Error: Could not delete the User.";
                }
            }
            else
            {
                TempData["Msg"] = "Server Error. Please Contact administrator.";
            }
            return RedirectToAction("User");
        }

    } 
}