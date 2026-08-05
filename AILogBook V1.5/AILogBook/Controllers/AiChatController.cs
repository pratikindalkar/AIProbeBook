using AILogBook.Models;
using AILogBook.Services;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Office.SpreadSheetML.Y2023.MsForms;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2016.Drawing.Command;
using DocumentFormat.OpenXml.Office2019.Drawing.Model3D;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AILogBook.Controllers
{
    public class AiChatController : Controller
    {
        private readonly ChatService chatService;
        private readonly IConfiguration configuration;
        private readonly HttpClient httpClient;
        private readonly string ConnectionString;
        private readonly bool bnlResponse = false;

        //Initializes the HTTP client for API calls and retrieves the DB connection string
        public AiChatController(ChatService _chatService, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            chatService = _chatService;
            configuration = config;
            httpClient = httpClientFactory.CreateClient();

            httpClient.BaseAddress = new Uri("https://surveyxan.com/cloudapp/app27/CRUD_API26/");
            ConnectionString = configuration.GetConnectionString("DefaultConnection").ToString();
        }


        // Entry point for the chat interface. Validates project status and initializes session.
        public async Task<IActionResult> Index(string projectKey, int resId, string? Remark)
        {
            var existingConversation = GetConversation();
            // Reset existing conversation
            SaveConversation(new List<ChatMessage>());

            // Load visual settings (logos, colors, timers)
            await GetChatSettngs();

            // Validate the project key and dates; if invalid, redirect to error page
            bool validCode = await GetProjectKey(projectKey, resId, Remark);
            if (!validCode)
            {
                TempData["AlertMessage"] = "Project is not valid: It may be inactive, invalid, or the expiration date has passed.";
                return RedirectToAction("Error", "Home");
            }

            existingConversation = GetConversation();
            return View(existingConversation);
        }

        // Retrieves the current chat history from the user's session.
        private List<ChatMessage> GetConversation()
        {
            var json = HttpContext.Session.GetString("Conversation");
            if (string.IsNullOrEmpty(json))
                return new List<ChatMessage>();
            return JsonConvert.DeserializeObject<List<ChatMessage>>(json);
        }

        // Continue the current chat history to the session as a JSON string.
        private void SaveConversation(List<ChatMessage> conversation)
        {
            var json = JsonConvert.SerializeObject(conversation);
            HttpContext.Session.SetString("Conversation", json);
        }

        // Processes user input, gets the AI response, and saves the turn to the database.
        public async Task<IActionResult> AskBot(string UserInput, int PromptId, int SessionId, string mId, int resId, string Pcode)
        {
            if (string.IsNullOrEmpty(UserInput))
            {
                return RedirectToAction("Index");
            }

            var conversation = GetConversation();

            // 1. Add User message to session-based conversation
            var userMsg = new ChatMessage { role = "user", content = UserInput };
            conversation.Add(userMsg);

            // 2. Fetch response from the ChatService (OpenAI/Gemini/etc...)
            string botReply = await chatService.GetBotResponse(conversation, mId);

            // 3. Add Assistant message to conversation
            var botMsg = new ChatMessage { role = "assistant", content = botReply };
            conversation.Add(botMsg);
            SaveConversation(conversation);

            // 4. Log the individual turn (Question/Response) into ChatConversation table
            int currentChatId = conversation.Count(m => m.role == "user");

            // Index logic to find the previous text for context logging
            string LastMessageText = conversation[conversation.Count - 3].content;
            await ChatConversation(PromptId, SessionId, currentChatId, resId, LastMessageText, UserInput, mId, Pcode);

            return Json(new { botResponse = botReply });
        }

        // Validates that the project exists, is active, and the current date is within the project range.
        private async Task<bool> GetProjectKey(string pKey, int resId, string? Remark)
        {
            string getQry = $"SELECT * from AIProbeBook_ProjectDetails WHERE ProjectKey = '{pKey}' And Active = '1'";

            var input = new InputsValue
            {
                SQLStatements = new[] { getQry },
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
                    var GetId = JsonConvert.DeserializeObject<List<Projects>>(result[0].NoOfRecordsAffected[0]);
                    var AllData = GetId.FirstOrDefault();
                    if (AllData != null && !string.IsNullOrEmpty(AllData.ProjectKey))
                    {
                        DateTime FromDate = DateTime.Parse(AllData.From_Date).Date;
                        DateTime ToDate = DateTime.Parse(AllData.To_Date).Date;
                        DateTime Todays = DateTime.Today;

                        // Check if the current project is live based on dates
                        if (Todays >= FromDate && Todays <= ToDate)
                        {
                            int PromptId = AllData.Prompt_Id;
                            int ModelID = AllData.Model_Id;
                            string Pcode = AllData.Project_Code;
                            ViewBag.PKey = AllData.ProjectKey;
                            ViewBag.ResponId = resId;
                            ViewBag.ProjectCode = Pcode;
                            ViewBag.ModelId = ModelID;

                            // Initialize the specific prompt and session
                            await GetDataById(PromptId, ModelID, resId, Pcode, Remark);
                            return true;
                        }
                        return false;

                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return false;
        }

        // Fetches prompt details and sets up the System Message and the first AI message.
        private async Task GetDataById(int PromptId, int ModelId, int RespId, string Pcode, string? Remark)
        {
            var conversation = GetConversation();
            string getQry = @"SELECT p.*, c.CategoryName, t.ShortTopic FROM PromptList p 
                LEFT JOIN Category c ON p.CategoryID = c.AutoId
                LEFT JOIN Topics t ON p.TopicId = t.AutoId
                WHERE p.AutoId = " + PromptId;

            var input = new InputsValue
            {
                SQLStatements = new[] { getQry },
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
                    var GetId = JsonConvert.DeserializeObject<List<PromptList>>(result[0].NoOfRecordsAffected[0]);
                    var GetPrompt = GetId.FirstOrDefault();
                    ViewBag.PId = GetPrompt.AutoID;
                    string Cate = GetPrompt.CategoryName;
                    string Topi = GetPrompt.ShortTopic;
                    string Rate = GetPrompt.Rating;
                    string FinalP = GetPrompt.FinalPrompt;

                    // Populate UI metadata
                    ViewBag.TopicName = GetPrompt.ShortTopic;
                    ViewBag.CateName = GetPrompt.CategoryName;
                    ViewBag.FinalEndPrompt = GetPrompt.FinalEndPrompt;
                    ViewBag.Id = RespId;

                    //int num = await GetChatSessionId(GetPrompt.AutoID);
                    // Create the session in the database
                    await CreateChatSession(RespId, Pcode, Cate, Topi, PromptId, Rate, FinalP, ModelId, Remark);

                    // Add the hidden 'System' instruction for the AI
                    conversation.Add(new ChatMessage
                    {
                        role = "system",
                        content = FinalP
                    });

                    // Get the first response from AI based on system instructions
                    string Msg = await chatService.GetBotResponse(conversation, ModelId.ToString());
                    conversation.Add(new ChatMessage
                    {
                        role = "assistant",
                        content = Msg
                    });
                    SaveConversation(conversation);
                }
            }
        }

        // Inserts a new ChatSession record and returns the AutoId for session tracking.
        private async Task<int> CreateChatSession(int RespId, string Pcode, string Cate, string Topi, int PromptId, string Rate, string FinalP, int ModelId, string? Remark)
        {
            string SurveySessionId = RespId.ToString() + Pcode;
            string StartTime = DateTime.Now.ToString("HH:mm:ss");
            string Dated = DateTime.Now.ToString("yyyy-MM-dd");
            FinalP = FinalP.Replace("'", "");

            // Use OUTPUT in INSERTED to get the ID of the newly created session
            string InsertQry = $@"INSERT INTO ChatSession(SurveySessionId, Respondent_Id, ProjectCode, Start_Time, End_Time, Date, Category, Topic, Prompt_Id, Rating, FinalPrompt, Usable, SelfEnded, Remarks, Model_Id, TotalWordCount, TotalCharCount, Questions, Responses,JsonData) 
                          OUTPUT INSERTED.AutoId
                          VALUES ('{SurveySessionId}',{RespId}, '{Pcode}', '{StartTime}', '', '{Dated}', '{Cate}', '{Topi}', {PromptId}, '{Rate}', '{FinalP}', '', '', '{Remark}', {ModelId}, '', '', '', '', '');";
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

            var jsonContent = JsonConvert.SerializeObject(input);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("api/Data/CRUD_API", content);
            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<List<Result>>(responseData);
                if (result != null && result[0].OverAllError[0] == "1")
                {
                    string Qry = $"select Top 1 AutoId from ChatSession where SurveySessionId = '{SurveySessionId}' order by AutoId desc";
                    var input1 = new InputsValue
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

                    var jsonContent1 = JsonConvert.SerializeObject(input1);
                    var content1 = new StringContent(jsonContent1, Encoding.UTF8, "application/json");

                    var response1 = await httpClient.PostAsync("api/Data/CRUD_API", content1);
                    if (response1.IsSuccessStatusCode)
                    {
                        var responseData1 = await response1.Content.ReadAsStringAsync();
                        var result1 = JsonConvert.DeserializeObject<List<Result>>(responseData1);
                        if (result1 != null && result1[0].OverAllError[0] == "1")
                        {
                            var GetId = JsonConvert.DeserializeObject<List<ChatSession>>(result1[0].NoOfRecordsAffected[0]);
                            ViewBag.ChatSessionId = GetId[0].AutoId;
                            return GetId.FirstOrDefault().AutoId;
                        }
                    }                        
                }
            }
            return 0;
        }

        // Logs every exchange (turn) of the chat into the database, including word and character counts.
        public async Task ChatConversation(int PromptId, int SessionId, int ChatId, int RespId, string ChatAsk, string ChatResponse, string modelId, string Pcode)
        {
            string Date = DateTime.Now.ToString("yyyy-MM-dd");
            string Time = DateTime.Now.ToString("HH:mm:ss");
            string surveySessionId = RespId.ToString() + Pcode;
            int qCharCount = ChatAsk.Length;
            int qWordCount = ChatAsk.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;

            int rCharCount = ChatResponse.Length;
            int rWordCount = ChatResponse.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;

            string cAsk = ChatAsk.Replace("'", "''");
            string cResponse = ChatResponse.Replace("'", "''");

            string insertQry = $@"INSERT INTO ChatConversation (ChatSession_id, surveySessionId, Prompt_id, Chat_id, Respondent_Id, Question, UserResponse, ProjectCode, CreatedDate, CreatedTime, 
                Modelid, QuestionWordCount, QuestionCharCount, ResponseWordCount, ResponseCharCount) VALUES (
                {SessionId}, '{surveySessionId}', {PromptId}, {ChatId}, {RespId}, N'{cAsk}', N'{cResponse}', '{Pcode}','{Date}', '{Time}', '{modelId}', 
                {qWordCount}, {qCharCount}, {rWordCount}, {rCharCount})";

            var input = new InputsValue
            {
                SQLStatements = new[] { insertQry },
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

            var jsoncontent = JsonConvert.SerializeObject(input);
            var content = new StringContent(jsoncontent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("api/Data/CRUD_API", content);
        }


        // Updates the ChatSession with end time and total stats when the user finishes.
        [HttpPost]
        public async Task<IActionResult> UpdateSessionEnd(int sessionId)
        {
            var conversation = GetConversation();
            string EndTime = DateTime.Now.ToString("HH:mm:ss");
            string Questions = "";
            string Responses = "";
            List<string> QuestionsList = new List<string>();
            List<string> ResponseList = new List<string>();
            
            string AllConversation = "";
            //string json = "[";
            //var entries = new List<string>();

            //foreach (var msg in conversation)
            //{
            //    if (msg.role != "system")
            //    {
            //        AllConversation += msg.content;
            //        string msgcontent = msg.content.Replace("\"", "\\\"");
            //        string label = (msg.role == "assistant") ? "Prompt" : "Response";

            //        entries.Add($"{{\"{label}\": \"{msgcontent}\"}}");
            //    }
            //}

            //json += string.Join(",", entries) + "]";
            var entries = new List<string>();
            string promptText = "";
            foreach (var msg in conversation)
            {
                if (msg.role != "system")
                {
                    AllConversation += msg.content;
                    string msgcontent = msg.content.Replace("\"", "\\\"");
                    if (msg.role == "assistant")
                    {
                        promptText = msgcontent;
                        QuestionsList.Add($"[{QuestionsList.Count + 1}]" + msgcontent);
                    }
                    else if (msg.role == "user" && !string.IsNullOrEmpty(promptText))
                    {
                        entries.Add($"{{\"prompt\": \"{promptText}\",\"response\": \"{msgcontent}\"}}");
                        ResponseList.Add($"[{ResponseList.Count + 1}]" + msgcontent);
                        promptText = "";
                    }
                }
            }


            Questions = string.Join("$", QuestionsList);
            Responses = string.Join("$", ResponseList);

            string finalJson = "[" + string.Join(", ", entries) + "]";
            //json += string.Join(",", entries) + "]";

            int charCount = AllConversation.Length;
            int wordCount = AllConversation.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;

            string updateQry = $"UPDATE ChatSession SET End_Time = '{EndTime}', TotalWordCount = {wordCount}, TotalCharCount = {charCount}, Questions = '{Questions}', Responses = '{Responses}', JsonData = '{finalJson}' WHERE AutoId = {sessionId}";

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

            var jsonContent = JsonConvert.SerializeObject(input);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("api/Data/CRUD_API", content);
            return Json(new { success = response.IsSuccessStatusCode });
        }

        // Retrieves global UI settings for the chat window (branding, colors, logos).
        private async Task GetChatSettngs()
        {
            string getQry = "Select * from ChatSettings Where Active = '1'";
            var input = new InputsValue
            {
                SQLStatements = new[] { getQry },
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
                    var ChatSettings = JsonConvert.DeserializeObject<List<ChatSettings>>(result[0].NoOfRecordsAffected[0]);
                    var AllChatSettings = ChatSettings.FirstOrDefault();
                    if (AllChatSettings != null)
                    {
                        ViewBag.AI = AllChatSettings.AI_Icon;
                        ViewBag.Resp = AllChatSettings.Resp_Icon;
                        ViewBag.vksLogo = AllChatSettings.VKSLogofile;
                        ViewBag.ChatTitle = AllChatSettings.ChatTitle;
                        ViewBag.HeaderColor = AllChatSettings.ThemeName;
                        ViewBag.ChatBG = AllChatSettings.Background_Img;
                        int ChatWait = AllChatSettings.ChatWaitTimer;
                        ViewBag.isTimer = AllChatSettings.isTimer;
                        if (ChatWait == 0)
                        {
                            ViewBag.ChatWait = 1;
                        }
                        ViewBag.ChatWait = ChatWait;
                    }
                }
            }
        }

        private async Task<int> GetChatSessionId(int id)
        {
            int num = 0;
            string getQry = $"SELECT ISNULL(MAX(ChatSession_id), 0) + 1 AS ChatSession_id FROM ChatConversation WHERE Prompt_id = {id}";
            var input = new InputsValue
            {
                SQLStatements = new[] { getQry },
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
                    var GetId = JsonConvert.DeserializeObject<List<ChatConversation>>(result[0].NoOfRecordsAffected[0]);
                    var GetSession = GetId.FirstOrDefault();
                    ViewBag.ChatSessionId = GetSession.ChatSession_id.ToString();
                    return GetSession.ChatSession_id;
                }
            }
            return num;
        }

        // Clears the session and marks the current database session as 'Reset'.
        [HttpPost]
        public async Task<IActionResult> Reset(string projectKey, int resId, int sessionId)
        {
            HttpContext.Session.Remove("Conversation");

            string updateQry = $"UPDATE ChatSession SET Remarks = 'Reset' WHERE AutoId = {sessionId}";

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

            var jsonContent = JsonConvert.SerializeObject(input);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("api/Data/CRUD_API", content);

            return Json(new { success = response.IsSuccessStatusCode });
        }

    }
}
