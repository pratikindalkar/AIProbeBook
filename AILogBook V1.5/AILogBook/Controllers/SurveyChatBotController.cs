using AILogBook.Models;
using AILogBook.Services;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Office2019.Drawing.Model3D;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace AILogBook.Controllers
{
    public class SurveyChatBotController : Controller
    {
        private readonly ChatService chatService;
        private readonly IConfiguration configuration;
        private readonly HttpClient httpClient;
        private readonly string ConnectionString;
        private readonly bool bnlResponse = false;

        //Initializes the HTTP client for API calls and retrieves the DB connection string
        public SurveyChatBotController(ChatService _chatService, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            chatService = _chatService;
            configuration = config;
            httpClient = httpClientFactory.CreateClient();

            httpClient.BaseAddress = new Uri("https://surveyxan.com/cloudapp/app27/CRUD_API26/");
            ConnectionString = configuration.GetConnectionString("DefaultConnection").ToString();
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> BotAsk([FromBody] UserResponseRequest request, string key, string respId, string QName, bool isChatSave)
        {
            string FinalPrompt = "";
            string pCode = "";
            string pKey = "";
            string aiName = "";
            string mId = "";
            string cName = "";
            string tName = "";
            string EndPrompt = "";
            int PromptId = 0;
            string surveySessionId = "";
            string EndPromptCompare = "";
            string BotReplyCompare = "";


            if (!string.IsNullOrEmpty(key))
            {
                List<string> GetDetails = await GetPromptDetails(key);
                FinalPrompt = GetDetails[0];
                pCode = GetDetails[1];
                pKey = GetDetails[2];
                aiName = GetDetails[3];
                mId = GetDetails[4];
                cName = GetDetails[5];
                tName = GetDetails[6];
                EndPrompt = GetDetails[7];
                PromptId = Convert.ToInt32(GetDetails[8]);
                surveySessionId = respId + pCode + QName;
            }

            List<ChatMessage> conversation = new List<ChatMessage>();
            if (request.response != "start")
            {
                conversation.Insert(0, new ChatMessage { role = "system", content = FinalPrompt, dateTime = DateTime.Now.ToString("dd-MM-yyyy hh:mm tt") });
                if (isChatSave)
                {
                    bool isPresent = await CheckSessionId(surveySessionId);
                    if (!isPresent)
                    {
                        await CreateChatSession(surveySessionId, respId, pCode, cName, tName, PromptId, FinalPrompt, mId);
                    }
                }
            }
            else
            {
                if (isChatSave)
                {
                    bool isPresent = await CheckSessionId(surveySessionId);
                    if (!isPresent)
                    {
                        await CreateChatSession(surveySessionId, respId, pCode, cName, tName, PromptId, FinalPrompt, mId);
                    }
                }
            }

            if (request.history != null)
            {
                if (request.history.Count != 0)
                {
                    int lastIndex = request.history.Count - 1;
                    var Lastitem = request.history[lastIndex];
                    if (Lastitem.prompt == EndPrompt)
                    {
                        string JsonData = await GetSurveyResponses(respId, key, QName);
                        if (!string.IsNullOrEmpty(JsonData))
                        {
                            return Content(JsonData, "application/json");
                        }
                    }
                    for (int i = 0; i < request.history.Count; i++)
                    {
                        var item = request.history[i];
                        if (!string.IsNullOrEmpty(item.prompt))
                        {
                            conversation.Add(new ChatMessage { role = "assistant", content = item.prompt, dateTime = item.dateTime });
                        }
                        if (!string.IsNullOrEmpty(item.response))
                        {
                            conversation.Add(new ChatMessage { role = "user", content = item.response, dateTime = item.dateTime });
                        }
                    }
                }
                else
                {
                    string JsonData = await GetSurveyResponses(respId, key, QName);
                    if (!string.IsNullOrEmpty(JsonData))
                    {
                        return Content(JsonData, "application/json");
                    }
                }
            }

            string userInput = request.response;
            if (userInput == "start" || conversation.Count == 0)
            {
                if (userInput == "start") { userInput = FinalPrompt; }
                conversation.Insert(0, new ChatMessage { role = "system", content = userInput, dateTime = DateTime.Now.ToString("dd-MM-yyyy hh:mm tt") });
            }
            else
            {
                conversation.Add(new ChatMessage { role = "user", content = userInput, dateTime = DateTime.Now.ToString("dd-MM-yyyy hh:mm tt") });
            }

            
            string botReply = await chatService.GetBotResponse(conversation, mId);

            conversation.Add(new ChatMessage { role = "assistant", content = botReply, dateTime = DateTime.Now.ToString("dd-MM-yyyy hh:mm tt") });

            List<object> historyList = new List<object>();
            for (int i = 0; i < conversation.Count; i++)
            {
                var message = conversation[i];

                if (message.role != "system")
                {
                    if (conversation[i].role == "system") continue;

                    if (i == conversation.Count - 1)
                    {
                        var historyItem = new
                        {
                            prompt = message.content,
                            dateTime = DateTime.Now.ToString("dd-MM-yyyy hh:mm tt")
                        };
                        historyList.Add(historyItem);
                    }
                    else
                    {
                        string userText = "";
                        int count = conversation.Count;
                        if (count > i)
                        {
                            if (conversation[i].role == "user") continue;
                            if (conversation[i].role == "assistant")
                            {
                                userText = conversation[i + 1].content;
                            }
                            var historyItem = new
                            {
                                prompt = message.content,
                                response = userText,
                                dateTime = message.dateTime
                            };
                            historyList.Add(historyItem);
                        }
                    }
                }
            }
            EndPromptCompare = EndPrompt.ToLower().TrimEnd();
            BotReplyCompare = botReply.ToLower().TrimEnd();

            if (BotReplyCompare == EndPromptCompare)
            {
                var responseObject = new { finished = true, prompt = botReply, history = historyList };
                string JsonData = JsonConvert.SerializeObject(responseObject);

                SaveSurveyResponses(respId, key, QName, JsonData);

                if (isChatSave)
                {
                    int currentChatId = conversation.Count(m => m.role == "user");
                    string LastMessageText = conversation[conversation.Count - 3].content;

                    await ChatConversation(PromptId, surveySessionId, currentChatId, respId, LastMessageText, request.response, mId, pCode);
                    await UpdateSessionEnd(surveySessionId, conversation);
                }

                return Json(new
                {
                    finished = true,
                    prompt = botReply,
                    history = historyList
                });
            }
            else
            {
                var responseObject = new { finished = false, prompt = botReply, history = historyList };
                string JsonData = JsonConvert.SerializeObject(responseObject);

                SaveSurveyResponses(respId, key, QName, JsonData);

                if (isChatSave)
                {
                    int currentChatId = conversation.Count(m => m.role == "user");
                    if (conversation.Count > 2)
                    {
                        string LastMessageText = conversation[conversation.Count - 3].content;
                        await ChatConversation(PromptId, surveySessionId, currentChatId, respId, LastMessageText, request.response, mId, pCode);
                    }
                }   
                
                return Json(new
                {
                    finished = false,
                    prompt = botReply,
                    history = historyList
                });
            }

        }


        private async Task SaveSurveyResponses(string RespId, string key, string QName, string JsonData)
        {
            List<string> QueryList = new List<string>();
            string Query1 = $"Update SurveyResponses Set Active = 'False' Where Respondent_Id = '{RespId}' AND QName = '{QName}' AND ProjectKey = '{key}'";
            string Query2 = @$"INSERT INTO SurveyResponses(Respondent_Id, ProjectKey, QName, JsonData, Active) Values 
                                ('{RespId}', '{key}', '{QName}', '{JsonData}', 'True')";
            QueryList.Add(Query1);
            QueryList.Add(Query2);

            var input = new InputsValue
            {
                SQLStatements = QueryList.ToArray(),
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
            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<List<Result>>(responseData);
                if (result != null && result[0].OverAllError[0] == "1")
                {
                }
            }
        }
        private async Task<string> GetSurveyResponses(string RespId, string key, string QName)
        {
            string data = "";
            string Query1 = $"Select * from SurveyResponses Where Respondent_Id = '{RespId}' AND QName = '{QName}' AND ProjectKey = '{key}' AND Active = '1'";

            var input = new InputsValue
            {
                SQLStatements = new[] { Query1 },
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
                    var Surveydata = JsonConvert.DeserializeObject<List<SurveyResponses>>(result[0].NoOfRecordsAffected[0]);
                    if(Surveydata.Count > 0)
                    {
                        string jsonText = Surveydata[0].JsonData.ToString();
                        return jsonText;
                    }
                }
            }
            return data;
        }
        private async Task<List<string>> GetPromptDetails(string key)
        {
            List<string> dataList = new List<string>();
            string getQry = $"SELECT * from ProjectPromptDetails_View WHERE ProjectKey = '{key}' And Active = '1'";

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
                    var GetId = JsonConvert.DeserializeObject<List<ProjectPromptDetails>>(result[0].NoOfRecordsAffected[0]);
                    var AllData = GetId.FirstOrDefault();
                    if (AllData != null && !string.IsNullOrEmpty(AllData.ProjectKey))
                    {
                        dataList.Add(AllData.FinalPrompt);
                        dataList.Add(AllData.Project_Code);
                        dataList.Add(AllData.ProjectKey);
                        dataList.Add(AllData.AIName);
                        dataList.Add(AllData.Model_Id.ToString());
                        dataList.Add(AllData.CategoryName);
                        dataList.Add(AllData.ShortTopic);
                        dataList.Add(AllData.FinalEndPrompt);
                        dataList.Add(AllData.AutoId.ToString());
                    }
                }
            }
            return dataList;
        }

        private async Task CreateChatSession(string surveySessionId, string RespId, string Pcode, string Cate, string Topi, int PromptId, string FinalP, string ModelId)
        {
            string StartTime = DateTime.Now.ToString("HH:mm:ss");
            string Dated = DateTime.Now.ToString("yyyy-MM-dd");
            FinalP = FinalP.Replace("'", "");

            string InsertQry = $@"INSERT INTO ChatSession(SurveySessionId, Respondent_Id, ProjectCode, Start_Time, End_Time, Date, Category, Topic, Prompt_Id, Rating, FinalPrompt, Usable, SelfEnded, Remarks, Model_Id, TotalWordCount, TotalCharCount, Questions, Responses, JsonData) 
                          VALUES ('{surveySessionId}', '{RespId}', '{Pcode}', '{StartTime}', '', '{Dated}', '{Cate}', '{Topi}', {PromptId}, '', '{FinalP}', '', '', '', '{ModelId}', '', '', '', '', '')";
            
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
            }
        }

        // Logs every exchange (turn) of the chat into the database, including word and character counts.
        public async Task ChatConversation(int PromptId, string surveySessionId, int ChatId, string RespId, string ChatAsk, string ChatResponse, string modelId, string Pcode)
        {
            int SessionId = await GetChatId(surveySessionId);
            string Date = DateTime.Now.ToString("yyyy-MM-dd");
            string Time = DateTime.Now.ToString("HH:mm:ss");

            int qCharCount = ChatAsk.Length;
            int qWordCount = ChatAsk.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;

            int rCharCount = ChatResponse.Length;
            int rWordCount = ChatResponse.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;

            string cAsk = ChatAsk.Replace("'", "''");
            string cResponse = ChatResponse.Replace("'", "''");

            string insertQry = $@"INSERT INTO ChatConversation (ChatSession_id, surveySessionId, Prompt_id, Chat_id, Respondent_Id, Question, UserResponse, ProjectCode, CreatedDate, CreatedTime, 
                Modelid, QuestionWordCount, QuestionCharCount, ResponseWordCount, ResponseCharCount) VALUES (
                {SessionId}, '{surveySessionId}', {PromptId}, {ChatId}, '{RespId}', N'{cAsk}', N'{cResponse}', '{Pcode}','{Date}', '{Time}', '{modelId}', 
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
        public async Task<int> GetChatId(string surveySessionId)
        {
            string insertQry = $@"Select * from ChatSession Where SurveySessionId = '{surveySessionId}' order by AutoId desc";

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
            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<List<Result>>(responseData);
                if (result != null && result[0].OverAllError[0] == "1")
                {
                    var GetId = JsonConvert.DeserializeObject<List<ChatSession>>(result[0].NoOfRecordsAffected[0]);
                    ViewBag.ChatSessionId = GetId[0].AutoId;
                    return GetId.FirstOrDefault().AutoId;
                }
            }
            return 0;
        }

        // Updates the ChatSession with end time and total stats when the user finishes.
        [HttpPost]
        public async Task<IActionResult> UpdateSessionEnd(string surveySessionId, List<ChatMessage> conversation)
        {
            string EndTime = DateTime.Now.ToString("HH:mm:ss");

            string AllConversation = "";
            string Questions = "";
            string Responses = "";
            List<string> QuestionsList = new List<string>();
            List<string> ResponseList = new List<string>();

            //string json = "[";
            var entries = new List<string>();
            string promptText = "";
            foreach (var msg in conversation)
            {
                if (msg.role != "system")
                {
                    AllConversation += msg.content;
                    string msgcontent = msg.content.Replace("\"", "\\\"");
                    //string label = (msg.role == "assistant") ? "Prompt" : "Response";

                    //entries.Add($"{{\"{label}\": \"{msgcontent}\"}}");
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
            string finalJson = "[" + string.Join(", ", entries) + "]";
            Questions = string.Join("$", QuestionsList);
            Responses = string.Join("$", ResponseList);
            //json += string.Join(",", entries) + "]";

            int charCount = AllConversation.Length;
            int wordCount = AllConversation.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;

            string updateQry = $"UPDATE ChatSession SET End_Time = '{EndTime}', TotalWordCount = {wordCount}, TotalCharCount = {charCount}, Questions = '{Questions}', Responses = '{Responses}', JsonData = '{finalJson}' WHERE SurveySessionId = '{surveySessionId}'";

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

        private async Task<bool> CheckSessionId(string SessionId)
        {
            string getQry = $"SELECT * from ChatSession WHERE SurveySessionId = '{SessionId}'";

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
                    var GetId = JsonConvert.DeserializeObject<List<ChatSession>>(result[0].NoOfRecordsAffected[0]);
                    var AllData = GetId.FirstOrDefault();
                    if (AllData != null && !string.IsNullOrEmpty(AllData.AutoId.ToString()))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
