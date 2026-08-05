
using AILogBook.Models;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace AILogBook.Controllers
{
    public class ChatThemeController : Controller
    {
        public readonly IConfiguration configuration;
        public readonly HttpClient httpClient;
        public readonly string ConnectionString;

        //Initializes the HTTP client for API calls and retrieves the DB connection string
        public ChatThemeController(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            configuration = config;
            httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri("https://Surveyxan.com/cloudapp/app27/CRUD_API26/");
            ConnectionString = configuration.GetConnectionString("DefaultConnection");
        }

        //2026Apr16 Loads the Theme index page with a list of all existing themes.
        public async Task<IActionResult> ChatTheme()
        {
            await LoadTheme();
            return View("ChatTheme");
        }

        //2026Apr16 Loads the Theme index page with a list of all existing themes.
        private async Task LoadTheme()
        {
            string Qry = "Select * from ChatSettings";
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
                    var ThemeList = JsonConvert.DeserializeObject<List<ChatSettings>>(JsonData);
                    ViewBag.ThemeList = ThemeList;
                }
            }
        }

        //2026Apr1 Pratik Opens the form to add a new Theme.
        public async Task<IActionResult> AddTheme()
        {
            return View("AddTheme", new ChatSettings());
        }

        //2026Apr16 Pratik Saves a new Theme to the database.
        [HttpPost]
        public async Task<IActionResult> AddTheme(ChatSettings chatSettings, IFormFile VKSLogoUpload, IFormFile AIIconUpload, IFormFile RespIconUpload, IFormFile BackgroundUpload)
        {
            ModelState.Remove("AutoId");
            string path = "~/images/";
            if (VKSLogoUpload != null) chatSettings.VKSLogofile = path + VKSLogoUpload.FileName;
            if (AIIconUpload != null) chatSettings.AI_Icon = path + AIIconUpload.FileName;
            if (RespIconUpload != null) chatSettings.Resp_Icon = path + RespIconUpload.FileName;
            if (BackgroundUpload != null) chatSettings.Background_Img = path + BackgroundUpload.FileName;


            string Qry = "Insert INTO ChatSettings (ThemeName, ChatTitle, VKSLogofile, AI_Icon, Resp_Icon, Background_Img, ChatWaitTimer, isTimer, Active) VALUES (" +
                $"'{chatSettings.ThemeName}', '{chatSettings.ChatTitle}', '{chatSettings.VKSLogofile}', '{chatSettings.AI_Icon}', '{chatSettings.Resp_Icon}', '{chatSettings.Background_Img}', '{chatSettings.ChatWaitTimer}', '{chatSettings.isTimer}', '{chatSettings.Active}')";

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
                    TempData["Msg"] = "Theme added successfully!";
                    return RedirectToAction("ChatTheme");
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
            await LoadTheme();
            return View("AddTheme", chatSettings);
        }

        //2026Apr16 Pratik Retrieves a specific Theme by ID and loads it into the edit form.
        [HttpGet]
        public async Task<IActionResult> EditTheme(int id)
        {
            string GetEditQry = $"Select * from ChatSettings Where AutoId = {id}";
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

            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<List<Result>>(responseData);
                if (result != null && result[0].OverAllError[0] == "1")
                {
                    var JsonData = result[0].NoOfRecordsAffected[0].ToString();
                    var ThemeList = JsonConvert.DeserializeObject<List<ChatSettings>>(JsonData);
                    var theme = ThemeList?.FirstOrDefault();
                    return View("AddTheme", theme);
                }
                
            }

            TempData["Msg"] = "Could not load theme data.";
            return RedirectToAction("ChatTheme");
        }

        //2026Apr16 Pratik Updates an existing Theme record.
        [HttpPost]
        public async Task<IActionResult> EditTheme(ChatSettings chatSettings, IFormFile VKSLogoUpload, IFormFile AIIconUpload, IFormFile RespIconUpload, IFormFile BackgroundUpload)
        {
            string path = "~/images/";

            if (VKSLogoUpload != null)
            {
                chatSettings.VKSLogofile = path + VKSLogoUpload.FileName;
            }

            if (AIIconUpload != null)
            {
                chatSettings.AI_Icon = path + AIIconUpload.FileName;
            }

            if (RespIconUpload != null)
            {
                chatSettings.Resp_Icon = path + RespIconUpload.FileName;
            }

            if (BackgroundUpload != null)
            {
                chatSettings.Background_Img = path + BackgroundUpload.FileName;
            }

            string UpdateQry = $@"UPDATE ChatSettings SET 
                    ThemeName = '{chatSettings.ThemeName}', 
                    ChatTitle = '{chatSettings.ChatTitle}', 
                    VKSLogofile = '{chatSettings.VKSLogofile}', 
                    AI_Icon = '{chatSettings.AI_Icon}', 
                    Resp_Icon = '{chatSettings.Resp_Icon}', 
                    Background_Img = '{chatSettings.Background_Img}', 
                    ChatWaitTimer = {chatSettings.ChatWaitTimer}, 
                    isTimer = {(chatSettings.isTimer ? 1 : 0)}, 
                    Active = {(chatSettings.Active ? 1 : 0)} 
                    WHERE AutoId = {chatSettings.AutoId}";

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

            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<List<Result>>(responseData);

                if (result != null && result[0].OverAllError[0] == "1")
                {
                    TempData["Msg"] = "Theme updated successfully!";
                    return RedirectToAction("ChatTheme");
                }
            }

            TempData["Msg"] = "Update failed. Check database logs.";
            await LoadTheme();
            return View("AddTheme", chatSettings);
        }

        //2026Apr16 Pratik Deletes an Theme from the system based on the AutoId.
        public async Task<IActionResult> DeleteTheme(int id)
        {
            string GetDeleteQry = $"Delete from ChatSettings Where AutoId = {id}";
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
                    TempData["Msg"] = "The Theme was deleted successfully!";
                }
                else
                {
                    TempData["Msg"] = "Error: Could not delete the Theme.";
                }
            }
            else
            {
                TempData["Msg"] = "Server Error. Please Contact administrator.";
            }
            return RedirectToAction("ChatTheme");
        }
    }
}
