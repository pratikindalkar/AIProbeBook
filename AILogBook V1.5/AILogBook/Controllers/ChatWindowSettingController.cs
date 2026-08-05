using AILogBook.Models;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AILogBook.Controllers
{
    [SessionCheck]
    public class ChatWindowSettingController : Controller
    {
        public readonly string ConnectionString;
        public readonly IConfiguration configuration;
        public readonly HttpClient httpClient;
        public ChatWindowSettingController(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            configuration = config;
            ConnectionString = configuration.GetConnectionString("Defaultconnection");
            httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri("https://surveyxan.com/cloudapp/app27/CRUD_API26/");
        }
        public async Task<IActionResult> ChatWindow(int id)
        {
            await LoadDropDownAIModel();

            bool projectExists = await ProjectDetails(id);
            if (!projectExists)
            {
                return RedirectToAction("Main", "Main");
            }
            return View();
        }

        private async Task LoadDropDownAIModel()
        {
            string sql = "Select * from AIModel";
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
                var result = JsonConvert.DeserializeObject<List<Result>>(data);

                if (result != null && result[0].OverAllError[0] == "1")
                {
                    string jsonTableData = result[0].NoOfRecordsAffected[0];
                    var AIList = JsonConvert.DeserializeObject<List<AIModel>>(jsonTableData);
                    ViewBag.ModelList = AIList;
                }
                else
                {
                    TempData["Msg"] = $"Database Error: {result[0].ErrorMessage[0]}";
                }
            }
        }
        private async Task<bool> ProjectDetails(int id)
        {
            string sql = $"Select * from AIProbeBook_ProjectDetails Where Prompt_Id = {id}";

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

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<List<Result>>(data);

                if (result != null && result[0].OverAllError[0] == "1")
                {
                    string jsonTableData = result[0].NoOfRecordsAffected[0];
                    var ChatData = JsonConvert.DeserializeObject<List<Projects>>(jsonTableData);
                    if(ChatData != null && ChatData.Count > 0)
                    {
                        string ProjectKey = ChatData[0].ProjectKey.ToString();
                        ViewBag.ProjectKey = ProjectKey;
                        ViewBag.SelectedModelId = ChatData[0].Model_Id;
                        ViewBag.PromptId = ChatData[0].Prompt_Id;
                        return true;
                    }
                }
            }
            TempData["Msg"] = "Project details not found!";
            return false;
        }
        [HttpPost]
        public IActionResult StartChatRedirect(int Respondent_Id, string ProjectKey)
        {
            return RedirectToAction("Index", "AiChat", new
            {
                projectKey = ProjectKey,
                resId = Respondent_Id
            });
        }

        public async Task<IActionResult> GenerateAndDownloadLink(int Respondent_Id, int NoOfLinks, string ProjectKey, string PromptId)
        {
            List<string> Catetopic = await GetCateTopic(PromptId);
            string category = Catetopic.Count > 0 ? Catetopic[0] : "N/A";
            string topic = Catetopic.Count > 1 ? Catetopic[1] : "N/A";

            if (Respondent_Id <= 0 || NoOfLinks <= 0)
            {
                TempData["Msg"] = "Please enter a valid Respondent ID and Number of Links.";
                return RedirectToAction("ChatWindow", new { id = Respondent_Id });
            }
            using(var workBook = new XLWorkbook())
            {
                var workSheet = workBook.Worksheets.Add("Respondent Links");

                workSheet.Cell(1, 1).Value = "Respondent ID";
                workSheet.Cell(1, 2).Value = "Chat Links";
                workSheet.Cell(1, 3).Value = "Category";
                workSheet.Cell(1, 4).Value = "Topic";

                // Range(firstRow, firstColumn, lastRow, lastColumn)
                workSheet.Range(1, 1, 1, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#df5015");
                workSheet.Row(1).Style.Font.FontColor = XLColor.White;
                workSheet.Row(1).Style.Font.Bold = true;
                //workSheet.Row(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                //workSheet.Cell(1, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                //workSheet.Cell(1, 1).Style.Border.OutsideBorderColor = XLColor.White;
                //workSheet.Cell(1, 2).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                //workSheet.Cell(1, 2).Style.Border.OutsideBorderColor = XLColor.White;

                workSheet.SheetView.FreezeRows(1);

                string BaseUrl = $"https://surveyxan.com/cloudapp/app36/AIProbeBook/AiChat/Index/{ProjectKey}";
                for(int i = 0; i < NoOfLinks; i++)
                {
                    int currentRow = i + 2;

                    int currentId = Respondent_Id + i;
                    string finalLink = $"{BaseUrl}/{currentId}";
                    workSheet.Cell(currentRow, 1).Value = currentId;
                    workSheet.Cell(currentRow, 2).Value = finalLink;
                    workSheet.Cell(currentRow, 3).Value = Catetopic[0];
                    workSheet.Cell(currentRow, 4).Value = Catetopic[1];

                    workSheet.Cell(currentRow, 2).SetHyperlink(new XLHyperlink(finalLink));
                    workSheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    workSheet.Cell(1, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    workSheet.Cell(1, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    workSheet.Cell(1, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    workSheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    workSheet.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    workSheet.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    workSheet.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                workSheet.Columns().AdjustToContents();
                using (var stream = new MemoryStream())
                {
                    workBook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"AI Probe Book Links {DateTime.Now:yyyy-MM-dd}.xlsx";

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName
                    );
                }
            }
        }
        private async Task<List<string>> GetCateTopic(string PId)
        {
            List<string> Catetopic = new List<string>();
            if (string.IsNullOrEmpty(PId)) return Catetopic;

            string sql = $@"Select c.CategoryName, t.ShortTopic from PromptList p inner join Category c on p.CategoryID = c.AutoId 
                    inner join Topics t on p.TopicID = t.AutoId Where p.AutoId = {PId}";

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

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<List<Result>>(data);

                if (result != null && result[0].OverAllError[0] == "1")
                {
                    string jsonTableData = result[0].NoOfRecordsAffected[0];
                    var ChatData = JsonConvert.DeserializeObject<List<PromptList>>(jsonTableData);
                    if (ChatData != null && ChatData.Count > 0)
                    {
                        string Category = ChatData[0].CategoryName.ToString();
                        string Topic = ChatData[0].ShortTopic.ToString();
                        Catetopic.Add(Category);
                        Catetopic.Add(Topic);
                    }
                }
            }
            return Catetopic;
        }
    }
}
