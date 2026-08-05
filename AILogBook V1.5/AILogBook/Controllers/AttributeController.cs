using AILogBook.Models;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;

namespace AILogBook.Controllers
{
    // Custom attribute to check a valid user session exists before accessing these methods
    [SessionCheck]

    public class AttributeController : Controller
    {
        private readonly HttpClient httpClient;
        private readonly IConfiguration configuration;
        private readonly string ConnectionString;

        //Initializes the HTTP client for API calls and retrieves the DB connection string
        public AttributeController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            configuration = config;
            httpClient = httpClientFactory.CreateClient();
            //httpClient.BaseAddress = new Uri("https://localhost:7164/");
            //httpClient.BaseAddress = new Uri("https://surveyxan.com/cloudapp/app27/");
            //httpClient.BaseAddress = new Uri("https://surveyxan.com/cloudapp/app17/Study3/");
            httpClient.BaseAddress = new Uri("https://surveyxan.com/cloudapp/app27/CRUD_API26/");


            ConnectionString = configuration.GetConnectionString("DefaultConnection");
        }
        [HttpGet]
        //2026Apr16 Loads the Attribute and category.
        public async Task<IActionResult> Attribute()
        {
            await LoadDropdownAttribute();
            await LoadDropDownCategory();
            return View("Attributes");
        }

        //2026Apr16 Loads the Attribute.
        private async Task LoadDropdownAttribute()
        {
            string sql = "  Select *,c.CategoryName from Attribute a inner join Category c on c.AutoId = a.CategoryId" +
                " Order By CategoryId";
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

            var JsonContent = JsonConvert.SerializeObject(input);
            var Content = new StringContent(JsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("api/Data/CRUD_API", Content);
            //var response = await httpClient.PostAsync("api/testing/CRUD_API", Content);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<List<Result>>(data);

                if (result != null && result.Count > 0)
                {
                    string JsonData = result[0].NoOfRecordsAffected[0];
                    var AttriList = JsonConvert.DeserializeObject<List<Attributes>>(JsonData);
                    ViewBag.AttrList = AttriList;
                    ViewBag.AttributeCount = AttriList.Count;
                }
            }
            else
            {
                TempData["Msg"] = "Server Error. Please Contact administrator.";
            }
        }

        //2026Apr16 Loads the Attribute and category.
        private async Task LoadDropDownCategory()
        {
            string sql = "Select * from Category Where Active = '1' Order By CategoryName";
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

            var JsonContent = JsonConvert.SerializeObject(input);
            var content = new StringContent(JsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("api/Data/CRUD_API", content);
            //var response = await httpClient.PostAsync("api/testing/CRUD_API", content);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<List<Result>>(data);

                if (result.Count > 0 && result != null)
                {
                    string JsonData = result[0].NoOfRecordsAffected[0];
                    var CateList = JsonConvert.DeserializeObject<List<Categories>>(JsonData);
                    ViewBag.CateList = CateList;
                    ViewBag.CategoryCount = CateList.Count;
                }
            }
            else
            {
                TempData["Msg"] = "Server Error. Please Contact administrator.";
            }
        }

        //2026Apr1 Pratik Opens the form to add a new attributes.
        public async Task<IActionResult> AddAttributes()
        {
            await LoadDropDownCategory();
            return View("AddMultiAttribute");
        }

        //2026Apr16 Pratik Retrieves a specific Category by ID and loads it into the edit form.
        private async Task GetCategory(int id)
        {
            string sql = $"Select * from Category Where AutoId = {id}";
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

            var JsonContent = JsonConvert.SerializeObject(input);
            var content = new StringContent(JsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("api/Data/CRUD_API", content);
            //var response = await httpClient.PostAsync("api/testing/CRUD_API", content);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<List<Result>>(data);

                if (result.Count > 0 && result != null)
                {
                    string JsonData = result[0].NoOfRecordsAffected[0];
                    var CateList = JsonConvert.DeserializeObject<List<Categories>>(JsonData);
                    ViewBag.CateList = CateList;
                    ViewBag.CategoryCount = CateList.Count;
                }
            }
            else
            {
                TempData["Msg"] = "Server Error. Please Contact administrator.";
            }
        }

        //2026Apr16 Pratik Saves a new attributes to the database. 
        [HttpPost]
        public async Task<IActionResult> AddAttribute(Attributes attributes)
        {
            ModelState.Remove("AutoId");
            ModelState.Remove("UpdatedUser");
            ModelState.Remove("UpdatedDate");
            ModelState.Remove("UpdatedTime");
            if (ModelState.IsValid)
            {
                string loggedInUser = HttpContext.Session.GetString("UserName") ?? "System";
                attributes.UpdatedUser = loggedInUser;
                attributes.UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd");
                attributes.UpdatedTime = DateTime.Now.ToString("HH:mm:ss");

                string InsertQry = "Insert INTO Attribute (CategoryId, Attribute, Type,UpdatedUser, UpdatedDate, UpdatedTime, Active) VALUES (" +
                                $"{attributes.CategoryId}, '{attributes.Attribute}', '{attributes.Type}', '{attributes.UpdatedUser}', " +
                                $"'{attributes.UpdatedDate}', '{attributes.UpdatedTime}', '{attributes.Active}')";

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
                    if (result != null && result[0].OverAllError[0] == "1")
                    {
                        TempData["Msg"] = "Attribute Added Successfully!";
                        return RedirectToAction("Attribute");
                    }
                    else
                    {
                        TempData["Msg"] = $"Datebase Error: {result[0].ErrorMessage[0]}";
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

                await LoadDropDownCategory();
                TempData["Msg"] = "Please fix the following: \\n " + errorMessages;
                return View("AddAttribute", attributes);
            }
            await LoadDropDownCategory();
            return View("AddAttribute", attributes);
        }

        //2026Apr16 Pratik Retrieves a specific Category by ID and loads it into the edit form.
        [HttpGet]
        public async Task<IActionResult> AddMultiAttribute(int id)
        {
            ViewBag.SelectedCategoryId = id;
            await GetCategory(id);
            await LoadDropdownAttribute();
            //string sql = $"  Select *,c.CategoryName from Attribute a inner join Category c on c.AutoId = {id}" +
            //    " Order By Attribute";
            //var input = new InputsValue
            //{
            //    SQLStatements = new[] { sql },
            //    SQLReturntype = new[] { "0" },
            //    DBDetails = ConnectionString,
            //    DBProfile = "connect",
            //    multiuserflag = "",
            //    securitykey = "AuthenticationKey",
            //    securityvalue = "VKS_KEY",
            //    sqltimeout = "30",
            //    rollbackcommit = "0",
            //    encrypt = false
            //};

            //var JsonContent = JsonConvert.SerializeObject(input);
            //var Content = new StringContent(JsonContent, Encoding.UTF8, "application/json");

            //var response = await httpClient.PostAsync("api/Data/CRUD_API", Content);
            ////var response = await httpClient.PostAsync("api/testing/CRUD_API", Content);

            //if (response.IsSuccessStatusCode)
            //{
            //    var data = await response.Content.ReadAsStringAsync();
            //    var result = JsonConvert.DeserializeObject<List<Result>>(data);

            //    if (result != null && result.Count > 0)
            //    {
            //        string JsonData = result[0].NoOfRecordsAffected[0];
            //        var AttriList = JsonConvert.DeserializeObject<List<Attributes>>(JsonData);
            //        ViewBag.AttrList = AttriList;
            //        ViewBag.AttributeCount = AttriList.Count;
            //    }
            //}
            //else
            //{
            //    TempData["Msg"] = "Server Error. Please Contact administrator.";
            //}
            return View();
        }

        //2026Apr16 Pratik Saves a new multiple attributes to the database. 
        [HttpPost]
        public async Task<IActionResult> AddMultiAttribute(string CategoryId, string[] attributeNames, string[] attributeTypes, string[] activeStatuses)
        {
            List<string> QueryList = new List<string>();

            if (string.IsNullOrEmpty(CategoryId) || CategoryId == "0")
            {
                TempData["Msg"] = "Please select a valid Category.";
                await LoadDropDownCategory();
                await LoadDropdownAttribute();
                return View();
            }

            string UpdatedUser = HttpContext.Session.GetString("UserName") ?? "System";
            string UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd");
            string UpdatedTime = DateTime.Now.ToString("HH:mm:ss");
            string DeleteQry = $"Delete From attribute Where CategoryId = {CategoryId}";
            QueryList.Add(DeleteQry);

            for (int i = 0; i < attributeNames.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(attributeNames[i])) continue;

                string[] spMultiattri = attributeNames[i].Split(',');
                for(int j = 0; j < spMultiattri.Length; j++)
                {
                    if (string.IsNullOrWhiteSpace(spMultiattri[j])) continue;

                    string sql = "Insert INTO Attribute (CategoryId, Attribute, Type, UpdatedUser, UpdatedDate, UpdatedTime, Active) VALUES (" +
                             $"{CategoryId}, '{spMultiattri[j].Trim()}', '{attributeTypes[i]}', '{UpdatedUser}', " +
                             $"'{UpdatedDate}', '{UpdatedTime}', '{activeStatuses[i]}')";
                    QueryList.Add(sql);
                }
            }

            if (QueryList.Count > 0)
            {
                var input = new InputsValue
                {
                    SQLStatements = QueryList.ToArray(),
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
                var response = await httpClient.PostAsync("api/Data/CRUD_API", new StringContent(json, Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    TempData["Msg"] = "Attributes Added Successfully!";
                    return RedirectToAction("Attribute");
                }
            }
            return View();
        }


        //2026Apr16 Pratik Retrieves a specific attributes by categoryID and loads it into the edit form.
        [HttpGet]
        public async Task<JsonResult> GetAttributesByCategory(int id)
        {
            // 1. SQL Query to fetch attributes and join with Category
            string sql = $"Select a.*, c.CategoryName from Attribute a " +
                         $"inner join Category c on c.AutoId = a.CategoryId " +
                         $"Where a.CategoryId = {id} Order By a.Attribute";

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

            var jsonContent = JsonConvert.SerializeObject(input);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync("api/Data/CRUD_API", content);

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<List<Result>>(data);

                    if (result != null && result.Count > 0 && result[0].OverAllError[0] == "1")
                    {
                        string jsonData = result[0].NoOfRecordsAffected[0];
                        var list = JsonConvert.DeserializeObject<List<Attributes>>(jsonData);

                        return Json(list);
                    }
                }
                return Json(new List<Attributes>());
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        //2026Apr16 Pratik Deletes an attributes from the system based on the AutoId.
        public async Task<IActionResult> DeleteAttribute(int id)
        {
            string GetDeleteQry = $"Delete from Attribute Where AutoId = {id}";
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
                    TempData["Msg"] = "Attribute was deleted successfully!";
                }
                else
                {
                    TempData["Msg"] = "Error: Could not delete the attribute.";

                }
            }
            else
            {
                TempData["Msg"] = "Server Error. Please Contact administrator.";
            }
            return RedirectToAction("Attribute");
        }

        //2026Apr16 Pratik Exports all attribute data to an Excel file using ClosedXML.
        public async Task<IActionResult> Download()
        {
            string GetQry = "Select * from Attribute Where Active = '1'";
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
                        var WorkSheet = workBook.Worksheets.Add("Attribute");
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
                            string fileName = $"Attribute_{DateTime.Now:dd-MM-yyyy}.xlsx";

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
            return RedirectToAction("Main");
        }
    }
}
