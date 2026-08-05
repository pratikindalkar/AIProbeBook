using AILogBook.Models;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.EMMA;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;
using System.Text;

namespace AILogBook.Controllers
{
    // Custom attribute to check a valid user session exists before accessing these methods
    [SessionCheck]

    public class APBProjectController : Controller
    {
        public readonly string ConnectionString;
        public readonly IConfiguration configuration;
        public readonly HttpClient httpClient;

        //Initializes the HTTP client for API calls and retrieves the DB connection string
        public APBProjectController(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            configuration = config;
            ConnectionString = configuration.GetConnectionString("Defaultconnection");
            httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri("https://surveyxan.com/cloudapp/app27/CRUD_API26/");
            //httpClient.BaseAddress = new Uri("https://localhost:7164/");
        }

        //2026Apr16 Loads the APB Projects index page with a list of all existing Projects.
        public async Task<IActionResult> Project()
        {
            await LoadData();
            return View();
        }


        private async Task LoadDropDownValue()
        {
            List<string> Query = new List<string>();
            string sql1 = "Select * from AIModel;";
            //string sql2 = "Select * from PromptList Where Active = '1' Order by AutoId desc";
            string sql2 = $@"Select *, CONCAT(CAST(a.AutoId as VARCHAR),' ', b.CategoryName) as PromptCategory from PromptList a 
                           left join Category b on a.CategoryID = b.AutoId Where a.Active = '1' Order by a.AutoId desc";
            Query.Add(sql1);
            Query.Add(sql2);
            var input = new InputsValue
            {
                SQLStatements = Query.ToArray(),
                SQLReturntype = new[] { "0", "0" },
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
                var data = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<List<Result>>(data);

                if (result != null && result[0].OverAllError[0] == "1")
                {
                    string jsonTableData = result[0].NoOfRecordsAffected[0];
                    var AIList = JsonConvert.DeserializeObject<List<AIModel>>(jsonTableData);
                    ViewBag.ModelList = AIList;
                }
                if (result != null && result[0].OverAllError[1] == "1")
                {
                    string jsonTableData = result[0].NoOfRecordsAffected[1];
                    var Prompt = JsonConvert.DeserializeObject<List<PromptList>>(jsonTableData);
                    ViewBag.PromptIds = Prompt;
                }
                else
                {
                    TempData["Msg"] = $"Database Error: {result[0].ErrorMessage[0]}";
                }
            }
        }

        //2026Apr16 Loads the APB Projects index page with a list of all existing Projects.
        private async Task LoadData()
        {
            //string sql = "SELECT a.*, m.AIName AS ModelName " +
            //     "FROM AIProbeBook_ProjectDetails a " +
            //     "LEFT JOIN AIModel m ON a.Model_Id = m.AutoId " +
            //     "ORDER BY a.AutoId DESC";
            // SQL Query 
            string sql = $@"SELECT a.AutoId, m.AIName AS ModelName, 
                        CONCAT(CAST(a.Prompt_Id AS VARCHAR), ' ', c.CategoryName) AS PromptCategory, a.*
                        FROM AIProbeBook_ProjectDetails a
                        LEFT JOIN AIModel m ON a.Model_Id = m.AutoId
                        INNER JOIN PromptList p ON a.Prompt_Id = p.AutoId
                        LEFT JOIN Category c ON p.CategoryID = c.AutoId
                        LEFT JOIN Attribute t ON p.TopicID = t.AutoId
                        ORDER BY a.AutoId DESC;";
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

            var response = await httpClient.PostAsync("api/Data/CRUD_API", content);
            //var response = await httpClient.PostAsync("api/testing/CRUD_API", content);


            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var apiResult = JsonConvert.DeserializeObject<List<Result>>(data);

                if (apiResult != null && apiResult.Count > 0)
                {
                    string jsonTableData = apiResult[0].NoOfRecordsAffected[0];
                    var AllList = JsonConvert.DeserializeObject<List<Projects>>(jsonTableData);
                    ViewBag.ProList = AllList;
                    ViewBag.TotalProjects = AllList.Count;
                }
            }
        }

        private async Task<string> CreateProjectKey()
        {
            string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            Random rd = new Random();
            string ProjectKey = "";

            for (int i = 0; i < 12; i++)
            {
                ProjectKey += chars[rd.Next(chars.Length)];
            }
            ViewBag.ProjectKey = ProjectKey;
            return ProjectKey;
        }

        //2026Apr1 Pratik Opens the form to add a new Projects.
        public async Task<IActionResult> AddProject()
        {
            await LoadDropDownValue();
            await CreateProjectKey();
            return View("AddProject");
        }

        //2026Apr16 Pratik Saves a new Project to the database. It check Prompt Id and Project Key not duplicate.
        [HttpPost]
        public async Task<IActionResult> AddProject(Projects projects)
        {
            //Check PromptId
            bool isValid = await IsPromptOrCodeDuplicate(projects);
            if (isValid)
            {
                await LoadDropDownValue();
                return View("AddProject", projects);
            }

            //Check ProjectKey
            string newKey = projects.ProjectKey;
            while (await IsProjectKeyDuplicate(newKey))
            {
                newKey = await CreateProjectKey();
            }
            projects.ProjectKey = newKey;

            if (ModelState.IsValid)
            {
                string sql = $"Insert Into AIProbeBook_ProjectDetails (Prompt_Id, Model_Id, ProjectKey, Project_Code, From_Date, To_Date, Active) " +
                             $"Values ({projects.Prompt_Id}, {projects.Model_Id}, '{projects.ProjectKey}', '{projects.Project_Code}', '{projects.From_Date}', '{projects.To_Date}', '{projects.Active}')";

                if (await ExecuteSqlAsync(sql))
                {
                    TempData["Msg"] = "Project Created Successfully!";
                    return RedirectToAction("Project");
                }
            }

            await LoadDropDownValue();
            return View("AddProject", projects);
        }

        //2026Apr16 Pratik Retrieves a specific Project by ID and loads it into the edit form.
        public async Task<IActionResult> EditProject(int id)
        {
            await LoadDropDownValue();
            string GetEditQry = $"Select * from AIProbeBook_ProjectDetails Where AutoId = {id}";
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
                    var ProList = JsonConvert.DeserializeObject<List<Projects>>(result[0].NoOfRecordsAffected[0]);
                    var Pro = ProList.FirstOrDefault();
                    ViewBag.ProjectKey = Pro.ProjectKey;
                    return View("AddProject", Pro);
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
            return RedirectToAction("Project");
        }

        //2026Apr16 Pratik Updates an existing Project record.
        [HttpPost]
        public async Task<IActionResult> EditProject(Projects projects)
        {
            bool isValid = await IsPromptOrCodeDuplicate(projects);
            if (isValid)
            {
                await LoadDropDownValue();
                ViewBag.ProjectKey = projects.ProjectKey;
                return View("AddProject", projects);
            }

            if (ModelState.IsValid)
            {
                string sql = $"Update AIProbeBook_ProjectDetails SET " +
                             $"Prompt_Id={projects.Prompt_Id}, " +
                             $"Model_Id={projects.Model_Id}, " +
                             $"Project_Code='{projects.Project_Code}', " +
                             $"From_Date='{projects.From_Date}', " +
                             $"To_Date='{projects.To_Date}', " +
                             $"Active='{projects.Active}' " +
                             $"Where AutoId={projects.AutoId}";
                bool isReturn = await ExecuteSqlAsync(sql);
                if (isReturn)
                {
                    TempData["Msg"] = "Project Updated Successfully!";
                    return RedirectToAction("Project");
                }
            }

            await LoadDropDownValue();
            return View("AddProject", projects);
        }

        //2026Apr16 Pratik Deletes an Project from the system based on the AutoId.
        public async Task<IActionResult> DeleteProject(int id)
        {
            string GetDeleteQry = $"Delete from AIProbeBook_ProjectDetails Where AutoId = {id}";
            bool isValid = await ExecuteSqlAsync(GetDeleteQry);
            if (isValid)
            {
                TempData["Msg"] = "Project deleted successfully!";
                return RedirectToAction("Project");
            }
            else
            {
                TempData["Msg"] = "Error: Could not delete the project.";
            }
            return RedirectToAction("Project");
        }

        //2026Apr16 Pratik check PromptId.
        private async Task<bool> IsPromptOrCodeDuplicate(Projects projects)
        {
            string sql = $"Select * from AIProbeBook_ProjectDetails Where (Project_Code = '{projects.Project_Code}' OR Prompt_Id = {projects.Prompt_Id})";
            if (projects.AutoId > 0) sql += $" AND AutoId != {projects.AutoId}";

            var list = await FetchDataAsync(sql);
            if (list != null && list.Count > 0)
            {
                var first = list.First();
                TempData["Msg"] = first.Project_Code == projects.Project_Code
                    ? $"Validation Error: Project Code already exists."
                    : $"Validation Error: Prompt ID is already assigned.";
                ViewBag.ProjectKey = projects.ProjectKey;
                return true;
            }
            return false;
        }


        //2026Apr16 Pratik check Project Key.
        private async Task<bool> IsProjectKeyDuplicate(string projectKey)
        {
            string sql = $"Select * from AIProbeBook_ProjectDetails Where ProjectKey = '{projectKey}'";
            var list = await FetchDataAsync(sql);
            return list != null && list.Count > 0;
        }

        private async Task<List<Projects>> FetchDataAsync(string sql)
        {
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

            var response = await httpClient.PostAsJsonAsync("api/Data/CRUD_API", input);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<Result>>();
                if (result != null && result[0].OverAllError[0] == "1")
                {
                    return JsonConvert.DeserializeObject<List<Projects>>(result[0].NoOfRecordsAffected[0]);
                }
            }
            return new List<Projects>();
        }
        private async Task<bool> ExecuteSqlAsync(string sql)
        {
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

            var response = await httpClient.PostAsJsonAsync("api/Data/CRUD_API", input);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<Result>>();
                return result != null && result[0].OverAllError[0] == "1";
            }
            return false;
        }

        //2026Apr16 Pratik Exports all Project data to an Excel file using ClosedXML.
        public async Task<IActionResult> Download()
        {
            string GetQry = "Select * from AIProbeBook_ProjectDetails Where Active = '1'";
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
                        var WorkSheet = workBook.Worksheets.Add("AIProbeBook ProjectDetails");
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
                            string fileName = $"AIProbeBook_ProjectDetails_{DateTime.Now:dd-MM-yyyy}.xlsx";

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
            return RedirectToAction("Project");
        }
    }
}
