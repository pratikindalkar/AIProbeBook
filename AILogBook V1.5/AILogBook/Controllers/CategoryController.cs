using AILogBook.Models;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.EMMA;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;
using System.Diagnostics;
using System.Text;

namespace AILogBook.Controllers
{
    // Custom attribute to check a valid user session exists before accessing these methods
    [SessionCheck]
    public class CategoryController : Controller
    {
        private readonly HttpClient httpClient;
        private readonly IConfiguration configuration;
        private readonly string ConnectionString;

        //Initializes the HTTP client for API calls and retrieves the DB connection string
        public CategoryController(IHttpClientFactory httpClientFactory, IConfiguration config)
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
        //2026Apr16 Loads the Category index page with a list of all existing category.
        public async Task<IActionResult> Index()
        {
            await LoadDropDownCategory();
            return View("Categories");
        }

        //2026Apr16 Loads the Category index page with a list of all existing category.
        private async Task LoadDropDownCategory()
        {
            //string sql = "Select * from Category Where Active = '1'";
            string sql = $@"SELECT b.AutoId,
                            b.CategoryName, 
                            COUNT(a.CategoryID) AS Count, 
                            b.UpdatedUser,
	                        b.UpdatedDate,
	                        b.UpdatedTime,
	                        b.Active 
                    FROM PromptList a 
                    Right JOIN Category b ON a.CategoryID = b.AutoId 
                    GROUP BY b.CategoryName, b.UpdatedUser, b.AutoId,b.UpdatedDate, b.UpdatedTime, b.Active";
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
                    ViewBag.TotalCate = CateList.Count;
                }
            }
            else
            {
                TempData["Msg"] = "Server Error. Please Contact administrator.";
            }
        }

        //2026Apr1 Pratik Opens the form to add a new category.
        public async Task<IActionResult> AddCategory()
        {
            return View("AddCategory");
        }

        //2026Apr16 Pratik Saves a new category to the database.
        [HttpPost]
        public async Task<IActionResult> AddCategory(Categories categories)
        {
            ModelState.Remove("AutoId");
            ModelState.Remove("UpdatedUser");
            ModelState.Remove("UpdatedDate");
            ModelState.Remove("UpdatedTime");

            if (ModelState.IsValid)
            {
                string loggedInUser = HttpContext.Session.GetString("UserName") ?? "System";
                categories.UpdatedUser = loggedInUser;

                categories.UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd");
                categories.UpdatedTime = DateTime.Now.ToString("HH:mm:ss");
                string Qry = "Insert INTO Category (CategoryName, UpdatedUser, UpdatedDate, UpdatedTime, Active) VALUES (" +
                    $"'{categories.CategoryName}', '{categories.UpdatedUser}', '{categories.UpdatedDate}', '{categories.UpdatedTime}', '{categories.Active}')";

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
                        TempData["Msg"] = "Category added successfully!";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        string msg = $"Database Error: {result[0].ErrorMessage[0]}";
                        if (msg.Contains("The duplicate key value"))
                        {
                            TempData["Msg"] = $"Duplicate Error: The Category Name {categories.CategoryName} already exists.";
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
            await LoadDropDownCategory();
            return View("AddCategory", categories);
        }

        //2026Apr16 Pratik Retrieves a specific Category by ID and loads it into the edit form.
        public async Task<IActionResult> EditCategory(int id)
        {
            string GetEditQry = $"Select * from Category Where AutoId = {id}";
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
                if(result != null && result[0].OverAllError[0] == "1")
                {
                    var CateList = JsonConvert.DeserializeObject<List<Categories>>(result[0].NoOfRecordsAffected[0]);
                    var cate = CateList.FirstOrDefault();
                    return View("AddCategory",cate);
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
            return RedirectToAction("Index");
        }

        //2026Apr16 Pratik Saves a new Category to the database. 
        [HttpPost]
        public async Task<IActionResult> EditCategory(Categories categories)
        {
            ModelState.Remove("UpdatedUser");
            ModelState.Remove("UpdatedDate");
            ModelState.Remove("Updatedtime");

            if (ModelState.IsValid)
            {
                string loggedInUser = HttpContext.Session.GetString("UserName") ?? "System";
                categories.UpdatedUser = loggedInUser;
                categories.UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd");
                categories.UpdatedTime = DateTime.Now.ToString("HH:mm:ss");
                string UpdateQry = $"Update Category SET CategoryName = '{categories.CategoryName}', UpdatedUser = '{categories.UpdatedUser}', " +
                    $"UpdatedDate = '{categories.UpdatedDate}', UpdatedTime = '{categories.UpdatedTime}', " +
                    $"Active = '{categories.Active}' Where AutoId = '{categories.AutoId}'";
               
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

                    if(result != null && result[0].OverAllError[0] == "1")
                    {
                        TempData["Msg"] = "Category updated successfully!";
                        return RedirectToAction("Index");
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

                await LoadDropDownCategory();
                TempData["Msg"] = "Please fix the following: \\n " + errorMessages;
                return View("AddCategory", categories);
            }
            await LoadDropDownCategory();
            return View("AddCategory", categories);
        }

        //2026Apr16 Pratik Deletes an category from the system based on the AutoId.
        public async Task<IActionResult> DeleteCategory(int id)
        {
            bool checkAttri = await CheckAttribute(id);
            if (checkAttri)
            {
                TempData["Msg"] = "Category is being used in the Attribute. Please delete the associated attributes first.";
                return RedirectToAction("Index");
            }

            bool check = await CheckData(id);
            if (check)
            {
                TempData["Msg"] = "Category is being used in the Prompt List. Please delete the associated prompts first.";
                return RedirectToAction("Index");
            }


            string GetDeleteQry = $"Delete from Category Where AutoId = {id}";
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
                if(result != null && result[0].OverAllError[0] == "1")
                {
                    TempData["Msg"] = "The Category was deleted successfully!";
                }
                else
                {
                    TempData["Msg"] = "Error: Could not delete the category.";
                }
            }
            else
            {
                TempData["Msg"] = "Server Error. Please Contact administrator.";
            }
            return RedirectToAction("Index");
        }

        //2026Apr16 Pratik Check category used in Prompt List
        public async Task<bool> CheckData(int id)
        {
            string CheckSql = $"Select count(*) as Count from PromptList where CategoryId = {id}";
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
                    if(getcount > 0)
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

        //2026Apr16 Pratik Check category used in attributes
        public async Task<bool> CheckAttribute(int id)
        {
            string CheckSql = $"Select count(*) as Count from Attribute where CategoryId = {id}";
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
                    if(getcount > 0)
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

        //2026Apr16 Pratik Exports all attribute data to an Excel file using ClosedXML.
        public async Task<IActionResult> Download()
        {
            string GetQry = "Select * from Category Where Active = '1'";
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
                        var WorkSheet = workBook.Worksheets.Add("Category");
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
                            string fileName = $"Category_{DateTime.Now:dd-MM-yyyy}.xlsx";

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
            return RedirectToAction("Index");
        }
    }
}
