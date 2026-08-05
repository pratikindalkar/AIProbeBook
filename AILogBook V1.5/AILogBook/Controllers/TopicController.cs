using AILogBook.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;
using System.Text;

namespace AILogBook.Controllers
{
    // Custom attribute to check a valid user session exists before accessing these methods
    [SessionCheck]
    public class TopicController : Controller
    {
        public readonly IConfiguration configuration;
        public readonly HttpClient httpClient;
        public readonly string ConnectionString;

        //Initializes the HTTP client for API calls and retrieves the DB connection string
        public TopicController(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            configuration = config;
            httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri("https://Surveyxan.com/cloudapp/app27/CRUD_API26/");
            ConnectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IActionResult> Topic()
        {
            await LoadTopic();
            return View();
        }

        //2026Apr16 Loads the main topic index page with a list of all existing topic.
        private async Task LoadTopic()
        {
            string Qry = "Select * from Topics Where Active = '1'";
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

                if(result != null && result[0].OverAllError[0] == "1")
                {
                    string JsonData = result[0].NoOfRecordsAffected[0].ToString();
                    var TopicList = JsonConvert.DeserializeObject<List<Topics>>(JsonData);
                    ViewBag.TopicList = TopicList;
                    ViewBag.TopicCount = TopicList.Count;

                }
            }
        }

        //2026Apr1 Pratik Opens the form to add a new topic.
        public async Task<IActionResult> AddTopic()
        {
            return View("AddTopic");
        }

        //2026Apr16 Pratik Saves a new topic to the database.
        [HttpPost]
        public async Task<IActionResult> AddTopic(Topics topics)
        {
            ModelState.Remove("AutoId");
            ModelState.Remove("UpdatedUser");
            ModelState.Remove("UpdatedDate");
            ModelState.Remove("UpdatedTime");

            if (ModelState.IsValid)
            {
                string loggedInUser = HttpContext.Session.GetString("UserName") ?? "System";
                topics.UpdatedUser = loggedInUser;
                topics.UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd");
                topics.UpdatedTime = DateTime.Now.ToString("HH:mm:ss");
                string Qry = "Insert INTO Topics (Topic, ShortTopic, UpdatedUser, UpdatedDate, UpdatedTime, Active) VALUES (" +
                    $"'{topics.Topic}', '{topics.ShortTopic}', '{topics.UpdatedUser}', '{topics.UpdatedDate}', '{topics.UpdatedTime}', '{topics.Active}')";

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
                        TempData["Msg"] = "Topic added successfully!";
                        return RedirectToAction("Topic");
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
            await LoadTopic();
            return View("AddTopic", topics);
        }

        //2026Apr16 Pratik Retrieves a specific topic by ID and loads it into the edit form.
        public async Task<IActionResult> EditTopic(int id)
        {
            string GetEditQry = $"Select * from Topics Where AutoId = {id}";
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
                    var TopicList = JsonConvert.DeserializeObject<List<Topics>>(result[0].NoOfRecordsAffected[0]);
                    var topic = TopicList.FirstOrDefault();
                    return View("AddTopic", topic);
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
            return RedirectToAction("Topic");
        }

        //2026Apr16 Pratik Updates an existing topic record.
        [HttpPost]
        public async Task<IActionResult> EditTopic(Topics topics)
        {
            ModelState.Remove("UpdatedUser");
            ModelState.Remove("UpdatedDate");
            ModelState.Remove("UpdatedTime");

            if (ModelState.IsValid)
            {
                string loggedInUser = HttpContext.Session.GetString("UserName") ?? "System";
                topics.UpdatedUser = loggedInUser;
                topics.UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd");
                topics.UpdatedTime = DateTime.Now.ToString("HH:mm:ss");
                string UpdateQry = $"Update Topics SET Topic = '{topics.Topic}',ShortTopic = '{topics.ShortTopic}', UpdatedUser = '{topics.UpdatedUser}', " +
                    $"UpdatedDate = '{topics.UpdatedDate}', UpdatedTime = '{topics.UpdatedTime}', " +
                    $"Active = '{topics.Active}' Where AutoId = '{topics.AutoId}'";

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
                        TempData["Msg"] = "Topic updated successfully!";
                        return RedirectToAction("Topic");
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
                return View("AddTopic", topics);
            }
            await LoadTopic();
            return View("AddTopic", topics);
        }

        //2026Apr16 Pratik Deletes an topic from the system based on the AutoId.
        public async Task<IActionResult> DeleteTopic(int id)
        {
            bool check = await CheckData(id);
            if (check)
            {
                TempData["Msg"] = "Topic is being used in the Prompt List. Please delete the associated prompts first.";
                return RedirectToAction("Topic");
            }
            string GetDeleteQry = $"Delete from Topics Where AutoId = {id}";
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
                    TempData["Msg"] = "The topic was deleted successfully!";
                }
                else
                {
                    TempData["Msg"] = "Error: Could not delete the topic.";
                }
            }
            else
            {
                TempData["Msg"] = "Server Error. Please Contact administrator.";
            }
            return RedirectToAction("Topic");
        }

        //2026Apr16 Pratik Exports all AI Model data to an Excel file using ClosedXML.
        public async Task<IActionResult> Download()
        {
            string GetQry = "Select * from Topics Where Active = '1'";
            var input = new InputsValue
            {
                SQLStatements = new[] { GetQry },
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

            var jsonString = JsonConvert.SerializeObject(input);
            var json = new StringContent(jsonString, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("api/Data/CRUD_API", json);
            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<List<Result>>(responseData);
                if (result != null && result[0].OverAllError[0] == "1")
                {
                    string Data = result[0].NoOfRecordsAffected[0];
                    DataTable dt = JsonConvert.DeserializeObject<DataTable>(Data);

                    using (var workBook = new XLWorkbook())
                    {
                        var WorkSheet = workBook.Worksheets.Add("Topic");
                        for (int i = 0; i < dt.Columns.Count; i++)
                        {
                            var cell = WorkSheet.Cell(1, i + 1);
                            cell.Value = dt.Columns[i].ColumnName;

                            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#df5015");
                            cell.Style.Font.FontColor = XLColor.White;
                            cell.Style.Font.Bold = true;
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            cell.Style.Border.OutsideBorderColor = XLColor.White;
                        }
                        WorkSheet.Cell(2, 1).InsertData(dt.Rows);

                        WorkSheet.Columns().AdjustToContents(); // Auto-size
                        WorkSheet.SheetView.FreezeRows(1); // Keep header visible

                        using (var stream = new MemoryStream())
                        {
                            workBook.SaveAs(stream);
                            var content = stream.ToArray();
                            string fileName = $"Topic_{DateTime.Now:dd-MM-yyyy}.xlsx";

                            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                        }
                    }
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
            return RedirectToAction("Topic");
        }

        //2026Apr16 Pratik check prompt id used for the topic.
        public async Task<bool> CheckData(int id)
        {
            string CheckSql = $"Select count(*) as Count from PromptList where TopicID = {id}";
            var input = new InputsValue
            {
                SQLStatements = new[] { CheckSql },
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
                    var Count = JsonConvert.DeserializeObject<List<Categories>>(result[0].NoOfRecordsAffected[0]);
                    int getcount = Convert.ToInt32(Count[0].count);
                    if (getcount > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return false;
        }
    }
}
