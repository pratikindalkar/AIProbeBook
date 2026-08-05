using AILogBook.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;
using System.Text;

namespace AILogBook.Controllers
{
    [SessionCheck]
    public class UserLogController : Controller
    {
        private readonly IConfiguration configuration;
        private readonly string ConnectionString;
        private readonly HttpClient httpClient;
        public UserLogController(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            configuration = config;
            httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri("https://Surveyxan.com/cloudapp/app27/CRUD_API26/");
            ConnectionString = configuration.GetConnectionString("DefaultConnection");
        }
        public async Task<IActionResult> Index()
        {
            await GetUserLog_Data();
            return View("Index");
        }
        private async Task GetUserLog_Data()
        {
            string sql = "SELECT *,CONVERT(VARCHAR, Date, 105) AS FormattedDate FROM User_Log;";
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
                var result = await response.Content.ReadAsStringAsync();

                var outer = JsonConvert.DeserializeObject<dynamic>(result);

                string innerJson = outer[0].noOfRecordsAffected[0];

                var data = JsonConvert.DeserializeObject<List<UserMasterDto>>(innerJson);
                ViewBag.Data = data;
                ViewBag.logCount = data != null ? data.Count : 0;
            }
        }

        public async Task<IActionResult> ChatConversation()
        {
            string sql = "Select * from ChatConversation order by AutoId desc";
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
                var apiResult = JsonConvert.DeserializeObject<List<Result>>(data);

                if (apiResult != null && apiResult.Count > 0)
                {
                    string jsonTableData = apiResult[0].NoOfRecordsAffected[0];
                    var chatList = JsonConvert.DeserializeObject<List<ChatConversation>>(jsonTableData);
                    ViewBag.ChatConversation = chatList;
                    
                }
            }
            return View();
        }
        public async Task<IActionResult> ChatSession()
        {
            string sql = "Select *, CAST(Date AS DATE) as DateT from ChatSession order by AutoId desc";
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
                var apiResult = JsonConvert.DeserializeObject<List<Result>>(data);

                if (apiResult != null && apiResult.Count > 0)
                {
                    string jsonTableData = apiResult[0].NoOfRecordsAffected[0];
                    var sessionList = JsonConvert.DeserializeObject<List<ChatSession>>(jsonTableData);
                    ViewBag.chatSession = sessionList;
                    
                }
            }
            return View("ChatSession");
        }


        public async Task<IActionResult> DownloadChatSession(int id)
        {
            string sql = $"Select * from ChatSession Where AutoId = {id}";
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
                    string Data = result[0].NoOfRecordsAffected[0];
                    DataTable dt = JsonConvert.DeserializeObject<DataTable>(Data);

                    using (var workBook = new XLWorkbook())
                    {
                        var WorkSheet = workBook.Worksheets.Add("Chat");
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

                        //WorkSheet.Columns().AdjustToContents(); // Auto-size
                        //WorkSheet.SheetView.FreezeRows(1); // Keep header visible

                        using (var stream = new MemoryStream())
                        {
                            workBook.SaveAs(stream);
                            var contentt = stream.ToArray();
                            string fileName = $"ChatSession_Id_{id}_{DateTime.Now:dd-MM-yyyy}.xlsx";

                            return File(contentt, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                        }
                    }
                }
            }
            return View("ChatSession");
        }

    }
}
