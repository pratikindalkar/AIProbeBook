using AILogBook.Models;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.EMMA;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;
using System.Text;
using static System.Net.WebRequestMethods;

namespace AILogBook.Controllers
{
    // Custom attribute to check a valid user session exists before accessing these methods
    [SessionCheck]

    
    public class AIModelController : Controller
    {
        private readonly HttpClient httpClient;
        private readonly IConfiguration configuration;
        private readonly string ConnectionString;

        //Initializes the HTTP client for API calls and retrieves the DB connection string
        public AIModelController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            configuration = config;
            httpClient = httpClientFactory.CreateClient();
            //httpClient.BaseAddress = new Uri("https://surveyxan.com/cloudapp/app27/");
            //httpClient.BaseAddress = new Uri("https://surveyxan.com/cloudapp/app17/Study3/");
            //httpClient.BaseAddress = new Uri("https://localhost:7164/");
            // Setting the Base Address for the CRUD API
            httpClient.BaseAddress = new Uri("https://surveyxan.com/cloudapp/app27/CRUD_API26/");
            ConnectionString = configuration.GetConnectionString("DefaultConnection");
        }

        //2026Apr16 Loads the main AI Model index page with a list of all existing models.
        [HttpGet]
        public async Task<IActionResult> AIModel()
        {
            // Fetch and prepare the list of models
            await LoadDropDownAIModel();
            return View("AIModel");
        }

        //2026Apr16 Loads the main AI Model index page with a list of all existing models.
        private async Task LoadDropDownAIModel()
        {
            // SQL Query to get all models sorted by name
            string sql = "Select * from AIModel order by AIName";
            // The Input object for the external CRUD API
            var input = new InputsValue
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

            // Serialize input to JSON and prepare the HTTP content
            var jsonContent = JsonConvert.SerializeObject(input);

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Post to the API
            var response = await httpClient.PostAsync("api/Data/CRUD_API", content);
            //var response = await httpClient.PostAsync("api/testing/CRUD_API", content);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var apiResult = JsonConvert.DeserializeObject<List<Result>>(data);
                if (apiResult != null && apiResult.Count > 0)
                {
                    // The API returns table data as a JSON string inside the first result record
                    string jsonTableData = apiResult[0].NoOfRecordsAffected[0];
                    var modelList = JsonConvert.DeserializeObject<List<AIModel>>(jsonTableData);

                    // Pass data to the View using ViewBag
                    ViewBag.ModelList = modelList;
                    ViewBag.TotalCount = modelList.Count;

                    // Logic to count how many URLs are active all models
                    int ActiveUrlCount = 0;
                    foreach(var m in modelList)
                    {
                        if (!string.IsNullOrEmpty(m.APIUrl1)) ActiveUrlCount++;
                        if (!string.IsNullOrEmpty(m.APIUrl2)) ActiveUrlCount++;
                        if (!string.IsNullOrEmpty(m.APIUrl3)) ActiveUrlCount++;
                    }
                    ViewBag.ActiveUrlCount = ActiveUrlCount;
                }
            }
        }

        //2026Apr1 Pratik Opens the form to add a new AI Model.
        public async Task<IActionResult> AddAIModel()
        {
            //Load the AIModel
            await LoadDropDownAIModel();
            return View("AddAIModel");
        }

        ////2026Apr16 Pratik Saves a new AI Model to the database.
        [HttpPost]
        public async Task<IActionResult> AddAIModel(AIModel aiModel)
        {
            // Remove non-mandatory fields from validation check
            ModelState.Remove("AutoId");
            ModelState.Remove("APIUrl2");
            ModelState.Remove("APIUrl3");

            if (ModelState.IsValid)
            {
                // Insert Query
                string InsertQry = $"Insert INTO AIModel (AIName, APIUrl1, APIUrl2, APIUrl3, MetaData, APIKey) VALUES (" +
                                $"'{aiModel.AIName}', '{aiModel.APIUrl1}', '{aiModel.APIUrl2}', '{aiModel.APIUrl3}', '{aiModel.MetaData}', '{aiModel.APIKey}')";

                var input = new InputsValue
                {
                    SQLStatements = new[] { InsertQry },
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

                    // OverAllError "1" indicates success in this API structure
                    if (result != null && result[0].OverAllError[0] == "1")
                    {
                        TempData["Msg"] = "AIModel Added Successfully!";
                        return RedirectToAction("AIModel");
                    }
                    else
                    {
                        // Specific handling for duplicate record errors
                        string msg = $"Database Error: {result[0].ErrorMessage[0]}";
                        if (msg.Contains("The duplicate key value"))
                        {
                            TempData["Msg"] = $"Duplicate Error: The AI Name {aiModel.AIName} already exists.";
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
                // Collect and display validation errors if the form is not filled correctly
                var errorMessages = string.Join(" \\n ", ModelState.Values
                                        .SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage));

                TempData["Msg"] = "Please fix the following: \\n " + errorMessages;
                return View("AddAIModel", aiModel);
            }
            await LoadDropDownAIModel();
            return View("AddAIModel", aiModel); 
        }

        //2026Apr16 Pratik Retrieves a specific AI Model by ID and loads it into the edit form.
        public async Task<IActionResult> EditAIModel(int id)
        {
            string GetEditQry = $"Select * from AIModel Where AutoId = {id}";
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
                    var AIList = JsonConvert.DeserializeObject<List<AIModel>>(result[0].NoOfRecordsAffected[0]);
                    var AI = AIList.FirstOrDefault();
                    await LoadDropDownAIModel();
                    // Reuses the Add view for Editing
                    return View("AddAIModel", AI);
                }
                else
                {
                    TempData["Msg"] = "Server Error. Please Contact administrator.";
                }
            }
            else
            {
                TempData["Msg"] = "Server Error. Please Contact administrator.";
            }
            return RedirectToAction("AIModel");
        }

        //2026Apr16 Pratik Updates an existing AI Model record.

        [HttpPost]
        public async Task<IActionResult> EditAIModel(AIModel aIModel)
        {
            ModelState.Remove("APIUrl2");
            ModelState.Remove("APIUrl3");

            if (ModelState.IsValid)
            {
                // Update Query
                string updateQry = "Update AIModel SET " +
                    $"AIName = '{aIModel.AIName}', APIUrl1 = '{aIModel.APIUrl1}' , APIUrl2 = '{aIModel.APIUrl2}', " +
                    $"APIUrl3 = '{aIModel.APIUrl3}', MetaData = '{aIModel.MetaData}', APIKey = '{aIModel.APIKey}' Where AutoId = '{aIModel.AutoId}'";

                var input = new InputsValue
                {
                    SQLStatements = new[] { updateQry },
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
                        TempData["Msg"] = "AIModel updated successfully!";
                        return RedirectToAction("AIModel");
                    }
                    else
                    {
                        string msg = $"Database Error: {result[0].ErrorMessage[0]}";
                        if (msg.Contains("The duplicate key value"))
                        {
                            TempData["Msg"] = $"Duplicate Error: The AI Name {aIModel.AIName} already exists.";
                        }
                        else
                        {
                            TempData["Msg"] = $"Database Error: {result[0].ErrorMessage[0]}";
                        }
                        return RedirectToAction("AIModel");
                    }
                }
                else
                {
                    TempData["Msg"] = "Server Error. Please Contact administrator.";
                    return RedirectToAction("AIModel");
                }
            }
            else
            {
                var errorMessages = string.Join(" \\n ", ModelState.Values
                                        .SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage));

                TempData["Msg"] = "Please fix the following: \\n " + errorMessages;
                return View("AddAIModel", aIModel);
            }
        }

        //2026Apr16 Pratik Deletes an AI Model from the system based on the AutoId.
        public async Task<IActionResult> DeleteAIModel(int id)
        {
            string GetDeleteQry = $"Delete from AIModel Where AutoId = {id}";
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
                    TempData["Msg"] = "AIModel was deleted successfully!";
                }
                else
                {
                    TempData["Msg"] = "Error: Could not delete the AIModel.";

                }
            }
            else
            {
                TempData["Msg"] = "Server Error. Please Contact administrator.";
            }
            return RedirectToAction("AIModel");
        }

        //2026Apr16 Pratik Exports all AI Model data to an Excel file using ClosedXML.
        public async Task<IActionResult> Download()
        {
            string GetQry = "Select * from AIModel";
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
                    // Convert the JSON result back into a DataTable for Excel processing
                    string Data = result[0].NoOfRecordsAffected[0];
                    DataTable dt = JsonConvert.DeserializeObject<DataTable>(Data);

                    using (var workBook = new XLWorkbook())
                    {
                        var WorkSheet = workBook.Worksheets.Add("AIModel");

                        // Create the header row and apply styling (Orange background, white bold text)
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

                        // Insert the actual data rows starting from cell A2
                        WorkSheet.Cell(2, 1).InsertData(dt.Rows);

                        WorkSheet.Columns().AdjustToContents(); // Auto-size
                        WorkSheet.SheetView.FreezeRows(1); // Keep header visible

                        // Stream the file back to the browser for download
                        using (var stream = new MemoryStream())
                        {
                            workBook.SaveAs(stream);
                            var content = stream.ToArray();
                            string fileName = $"AIModel_{DateTime.Now:dd-MM-yyyy}.xlsx";

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
            return RedirectToAction("AIModel");
        }
    }
}
