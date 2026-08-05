using AILogBook.Models;
using AILogBook.Services;
using AspNetCoreGeneratedDocument;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Reflection;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace AILogBook.Controllers
{
    // Custom attribute to check a valid user session exists before accessing these methods
    [SessionCheck]
    public class MainController : Controller
    {
        private readonly IConfiguration configuration;
        private readonly string ConnectionString;
        private readonly HttpClient httpClient;

        //Initializes the HTTP client for API calls and retrieves the DB connection string
        public MainController(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            configuration = config;
            httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri("https://Surveyxan.com/cloudapp/app27/CRUD_API26/");
            ConnectionString = configuration.GetConnectionString("DefaultConnection");
        }

        //2026Apr16 Loads the Prompt List data in grid
        public async Task<IActionResult> Main(string? aModel, string? Cate, string? Rate)
        {

            await LoadDropDownCategory();
            await LoadLastFilter();
            //string Query = "Select *, c.CategoryName, t.ShortTopic from PromptList p " +
            //    "inner join Category c on p.CategoryID = c.AutoId " +
            //    "inner join Topics t on p.TopicID = t.AutoId Where (p.Active = '1' Or p.Active = '0') ";
            List<string> QueryList = new List<string>();
            List<string> ReturnTypes = new List<string>();

            string Query = @"SELECT a.AutoId, b.CategoryName, d.ShortTopic, Count(c.Prompt_id) AS Count, 
                 a.FinalPrompt, a.CharCountPrompt, a.WordCountPrompt, a.Rating, a.UpdatedUser, a.UpdatedDate, a.UpdatedTime, a.Active 
                 FROM PromptList a 
                 INNER JOIN Category b ON a.CategoryID = b.AutoId 
                 INNER JOIN Topics d ON a.TopicID = d.AutoId  
                 LEFT JOIN ChatSession c ON a.AutoId = c.Prompt_id ";

            string filters = " WHERE 1=1 ";

            string currentCate = !string.IsNullOrEmpty(Cate) ? Cate : "";
            if (string.IsNullOrEmpty(currentCate) && ViewBag.Filter != null)
            {
                var Filter = ViewBag.Filter as List<AILogBook.Models.LastFilter>;
                currentCate = Filter.Count > 0 ? Filter[0].Filter1 : "";
            }
            if (!string.IsNullOrEmpty(currentCate))
            {
                filters += $" AND a.CategoryID = {currentCate}";
            }


            string currentRate = !string.IsNullOrEmpty(Rate) ? Rate : "";
            if (string.IsNullOrEmpty(currentRate) && ViewBag.Filter != null)
            {
                var Filter = ViewBag.Filter as List<AILogBook.Models.LastFilter>;
                currentRate = Filter.Count > 0 ? Filter[0].Filter2 : "";
            }
            if (!string.IsNullOrEmpty(currentRate))
            {
                filters += $" AND a.Rating = {currentRate}";
            }

            string groupAndOrder = @" GROUP BY a.AutoId, b.CategoryName, d.ShortTopic, a.FinalPrompt, a.CharCountPrompt, a.WordCountPrompt, a.Rating, 
                         a.UpdatedUser, a.UpdatedDate, a.UpdatedTime, a.Active 
                         ORDER BY a.AutoId DESC";

            Query = Query + filters + groupAndOrder;

            QueryList.Add(Query);
            ReturnTypes.Add("0");

            if (!string.IsNullOrEmpty(Cate) || !string.IsNullOrEmpty(Rate))
            {
                QueryList.Add("UPDATE ApplyFilter SET Active = '0'");
                ReturnTypes.Add("0");

                string filterInsert = $@"INSERT INTO ApplyFilter (Filter1, Filter2, Active) 
                                 VALUES ('{currentCate}', '{currentRate}', '1')";
                QueryList.Add(filterInsert);
                ReturnTypes.Add("0");
            }

            var input = new InputsValue
            {
                SQLStatements = QueryList.ToArray(),
                SQLReturntype = ReturnTypes.ToArray(),
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
                    var promptLogList = JsonConvert.DeserializeObject<List<PromptList>>(jsonTableData);
                    ViewBag.PromptList = promptLogList;
                }
            }
            await LoadLastFilter();
            return View("Main");
        }
        private async Task LoadLastFilter()
        {
            string sql = "Select * from ApplyFilter Where Active = '1'";
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
                    var FilterList = JsonConvert.DeserializeObject<List<LastFilter>>(jsonTableData);
                    if(FilterList != null && FilterList.Count > 0)
                    {
                        ViewBag.Filter = FilterList;
                        ViewBag.SelectedCate = FilterList[0].Filter1;
                        ViewBag.SelectedRate = FilterList[0].Filter2;
                    }                   

                }
                else
                {
                    TempData["Msg"] = $"Database Error: {result[0].ErrorMessage[0]}";
                }
            }
        }
        [HttpGet]
        public async Task<IActionResult> Reset()
        {
            string sql = "Update ApplyFilter set Active = '0'";
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
            }
            return RedirectToAction("Main");
        }

        //2026Apr16 Loads the main category in dropdown
        private async Task LoadDropDownCategory()
        {
            string sql = "Select * from Category Where Active = '1'";
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
                }
            }
        }

        //2026Apr16 Loads the main Topics in dropdown
        private async Task LoadDropDownTopics()
        {
            string sql = "Select * from Topics Where Active = '1'";
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
                    var TopicList = JsonConvert.DeserializeObject<List<Topics>>(JsonData);
                    ViewBag.TopicList = TopicList;
                }
            }
        }

        //2026Apr16 Loads the main attributes
        private async Task LoadDropdownAttribute(string id)
        {
            string sql = $"Select * from Attribute Where CategoryId = '{id}' And Active = '1' Order By Attribute";
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

        //2026Apr16 Loads the main promptview index page with a list of all existing promptlist.
        public async Task<IActionResult> PromptView()
        {
            //await LoadDropDownAIModel();
            await LoadDropDownCategory();
            await LoadDropDownTopics();
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AutoGen(PromptList promptList, string Id)
        {
            ModelState.Clear();
            if (Id == "3" || Id == "4")
            {
                await LoadDropdownAttribute(promptList.CategoryID.ToString());
                var allAttributes = ViewBag.AttrList as List<AILogBook.Models.Attributes>;

                if (allAttributes != null)
                {
                    if (Id == "3")
                    {
                        var posAttrNames = allAttributes.Where(a => a.Type == "Positive").Select(a => a.Attribute);
                        promptList.PositiveAttributes = string.Join(", ", posAttrNames);
                    }
                    if (Id == "4")
                    {
                        var negAttrNames = allAttributes.Where(a => a.Type == "Negative").Select(a => a.Attribute);
                        promptList.NegativeAttributes = string.Join(", ", negAttrNames);
                    }
                }

            }
            else if (Id == "7" || Id == "8")
            {
                if(Id == "7")
                {
                    string Text = $"Initial Prompt: {promptList.InitialPrompt} \n\n" +
                    $"Blocked Prompt: {promptList.BlockedPrompt} \n\n" +
                    $"User Ignore Prompt: {promptList.UserIgnorePrompt} \n\n" +
                    (!string.IsNullOrWhiteSpace(promptList.Prompt1) ? $"Prompt 1: {promptList.Prompt1} \n\n" : "") +
                    (!string.IsNullOrWhiteSpace(promptList.Prompt2) ? $"Prompt 2: {promptList.Prompt2} \n\n" : "") +
                    (!string.IsNullOrWhiteSpace(promptList.Prompt3) ? $"Prompt 3: {promptList.Prompt3} \n\n" : "") +
                    $"Minimum Prompt: {promptList.MinPromptText} \n\n" +
                    $"Maximum Prompt: {promptList.MaxPromptText} \n\n" +
                    (!string.IsNullOrWhiteSpace(promptList.EnglishLanguagePrompt) ? $"English Language Prompt: {promptList.EnglishLanguagePrompt} \n\n" : "") +
                    (!string.IsNullOrWhiteSpace(promptList.OtherLanguagePrompt) ? $"Other Language Prompt: {promptList.OtherLanguagePrompt} \n\n" : "") +
                    $"End Prompt: {promptList.EndPrompt} \n\n" +
                    $"End Prompt Message: {promptList.FinalEndPrompt}";

                    promptList.MergePrompt = Text;
                }
                if(Id == "8")
                {
                    string Text = $"{promptList.InitialPrompt} \n\n" +
                    $"{promptList.BlockedPrompt} \n\n" +
                    $"{promptList.UserIgnorePrompt} \n\n" +
                    $"{promptList.Prompt1} \n\n"+
                    $"{promptList.Prompt2} \n\n" +
                    $"{promptList.Prompt3} \n\n" +
                    $"{promptList.MinPromptText} \n\n" +
                    $"{promptList.MaxPromptText} \n\n" +
                    $"{promptList.EnglishLanguagePrompt} \n\n "+
                    $"{promptList.OtherLanguagePrompt} \n\n" +
                    $"{promptList.EndPrompt} \n\n" +
                    $"{promptList.FinalEndPrompt}";

                    promptList.FinalPrompt = Text;
                }
                //if (Id == "8") promptList.FinalPrompt = Text;
            }
            else
            {
                string Query = $"Select * from PromptTemplate Where PId = '{Id}' And Active = '1'";
                var input = new InputsValue
                {
                    SQLStatements = new[] { Query },
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

                var JsonString = JsonConvert.SerializeObject(input);
                var json = new StringContent(JsonString, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("api/Data/CRUD_API", json);
                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<List<Result>>(responseData);
                    if (result != null && result[0].OverAllError[0] == "1")
                    {
                        var PromptDataList = JsonConvert.DeserializeObject<List<PromptTemplate>>(result[0].NoOfRecordsAffected[0]);
                        var PromptResult = PromptDataList?.FirstOrDefault();
                        if (PromptResult != null)
                        {
                            if (Id == "1")
                            {
                                string Text = PromptResult.Message;
                                await LoadDropDownCategory();
                                var categories = ViewBag.CateList as List<AILogBook.Models.Categories>;
                                var selectedCategory = categories?.FirstOrDefault(c => c.AutoId == promptList.CategoryID);
                                string categoryName = selectedCategory?.CategoryName ?? "";
                                Text = Text.Replace("{C}", categoryName);

                                await LoadDropdownAttribute(promptList.CategoryID.ToString());
                                var allAttributes = ViewBag.AttrList as List<AILogBook.Models.Attributes>;

                                if (allAttributes != null)
                                {
                                    var posAttrNames = allAttributes.Where(a => a.Type == "Positive").Select(a => a.Attribute);
                                    var negAttrNames = allAttributes.Where(a => a.Type == "Negative").Select(a => a.Attribute);

                                    Text = Text.Replace("<+>", string.Join(", ", posAttrNames));
                                    Text = Text.Replace("<->", string.Join(", ", negAttrNames));
                                }

                                await LoadDropDownTopics();
                                var TopicList = ViewBag.TopicList as List<AILogBook.Models.Topics>;
                                var SelectedTopic = TopicList.FirstOrDefault(t => t.AutoId == promptList.TopicID);
                                string topic = SelectedTopic?.Topic ?? "";
                                Text = Text.Replace("{T}", topic);

                                promptList.InitialPrompt = Text;

                            }
                            else if (Id == "2")
                            {
                                promptList.EndPrompt = PromptResult.Message;
                            }
                            else if (Id == "5")
                            {
                                string Text = PromptResult.Message;
                                string MinVal = promptList.MinLength;
                                Text = Text.Replace("{}", MinVal);
                                promptList.MinPromptText = Text;
                            }
                            else if (Id == "6")
                            {
                                string Text = PromptResult.Message;
                                string MaxVal = promptList.MaxLength;
                                Text = Text.Replace("{}", MaxVal);
                                promptList.MaxPromptText = Text;
                            }
                            else if (Id == "9")
                            {
                                string Text = PromptResult.Message;
                                promptList.FinalEndPrompt = Text;
                            }
                            else if (Id == "10")
                            {
                                string Text = PromptResult.Message;
                                promptList.BlockedPrompt = Text;
                            }
                            else if (Id == "11")
                            {
                                string Text = PromptResult.Message;
                                promptList.UserIgnorePrompt = Text;
                            }
                            else if (Id == "12")
                            {
                                string Text = PromptResult.Message;
                                promptList.EnglishLanguagePrompt = Text;
                            }
                            else if (Id == "13")
                            {
                                string Text = PromptResult.Message;
                                promptList.OtherLanguagePrompt = Text;
                            }
                        }
                    }
                }
            }
            //await LoadDropDownAIModel();
            await LoadDropDownCategory();
            await LoadDropDownTopics();

            return Json(new
            {
                initialPrompt = promptList.InitialPrompt,
                endPrompt = promptList.EndPrompt,
                positiveAttributes = promptList.PositiveAttributes,
                negativeAttributes = promptList.NegativeAttributes,
                minPromptText = promptList.MinPromptText,
                maxPromptText = promptList.MaxPromptText,
                mergePrompt = promptList.MergePrompt,
                finalPrompt = promptList.FinalPrompt,
                finalEndPrompt = promptList.FinalEndPrompt,
                blockedPrompt = promptList.BlockedPrompt,
                userIgnorePrompt = promptList.UserIgnorePrompt,
                englishLangPrompt = promptList.EnglishLanguagePrompt,
                otherLangPrompt = promptList.OtherLanguagePrompt
            });
        }

        //2026Apr1 Pratik Opens the form to add a new promptlist.
        public async Task<IActionResult> SaveAs(int id)
        {
            string GetQry = "Select * from PromptList Where AutoId = " + id + "";
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
                    var PromptL = JsonConvert.DeserializeObject<List<PromptList>>(result[0].NoOfRecordsAffected[0]);
                    var Prompt = PromptL.FirstOrDefault();
                    if (Prompt != null)
                    {
                        Prompt.AutoID = 0;
                    }
                    //await LoadDropDownAIModel();
                    await LoadDropDownCategory();
                    await LoadDropDownTopics();
                    return View("PromptView", Prompt);
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

        //2026Apr16 Pratik Saves a new prompt to the database.
        [HttpPost]
        public async Task<IActionResult> SavePrompt(PromptList promptList)
        {
            ModelState.Remove("AIName");
            ModelState.Remove("UpdatedUser");
            ModelState.Remove("UpdatedDate");
            ModelState.Remove("UpdatedTime");
            ModelState.Remove("PositiveAttributes");
            ModelState.Remove("NegativeAttributes");
            ModelState.Remove("Prompt1");
            ModelState.Remove("Prompt2");
            ModelState.Remove("Prompt3");
            ModelState.Remove("MinPromptText");
            ModelState.Remove("MaxPromptText");
            ModelState.Remove("EnglishLanguagePrompt");
            ModelState.Remove("OtherLanguagePrompt");
            ModelState.Remove("CharCountPrompt");
            ModelState.Remove("WordCountPrompt");
            ModelState.Remove("Rating");
            if (ModelState.IsValid)
            {
                promptList.UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd");
                promptList.UpdatedTime = DateTime.Now.ToString("HH:mm:ss");

                int charCount = promptList.FinalPrompt?.Length ?? 0;
                promptList.CharCountPrompt = charCount.ToString();
                string loggedInUser = HttpContext.Session.GetString("UserName") ?? "System";
                int words = promptList.FinalPrompt.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
                promptList.WordCountPrompt = words.ToString();

                string InsertQry = $@"INSERT INTO PromptList (InitialPrompt, CategoryID, TopicID, Prompt1, Prompt2, Prompt3, 
                                    PositiveAttributes, NegativeAttributes, BlockedPrompt, UserIgnorePrompt, MinLength, MaxLength, 
                                    MinPromptText, MaxPromptText, EnglishLanguagePrompt, OtherLanguagePrompt, CharCountPrompt, WordCountPrompt, 
                                    EndPrompt, FinalEndPrompt, MergePrompt, FinalPrompt, Rating, 
                                    UpdatedUser, UpdatedDate, UpdatedTime, Active) VALUES (
                                    '{promptList.InitialPrompt?.Replace("'", "''")}', 
                                    '{promptList.CategoryID}', 
                                    '{promptList.TopicID}', 
                                    '{promptList.Prompt1?.Replace("'", "''")}', 
                                    '{promptList.Prompt2?.Replace("'", "''")}', 
                                    '{promptList.Prompt3?.Replace("'", "''")}', 
                                    '{promptList.PositiveAttributes?.Replace("'", "''")}', 
                                    '{promptList.NegativeAttributes?.Replace("'", "''")}', 
                                    '{promptList.BlockedPrompt}', 
                                    '{promptList.UserIgnorePrompt}', 
                                    '{promptList.MinLength}', 
                                    '{promptList.MaxLength}', 
                                    '{promptList.MinPromptText?.Replace("'", "''")}', 
                                    '{promptList.MaxPromptText?.Replace("'", "''")}', 
                                    '{promptList.EnglishLanguagePrompt?.Replace("'", "''")}', 
                                    '{promptList.OtherLanguagePrompt?.Replace("'", "''")}', 
                                    '{promptList.CharCountPrompt}', 
                                    '{promptList.WordCountPrompt}', 
                                    '{promptList.EndPrompt?.Replace("'", "''")}',
                                    '{promptList.FinalEndPrompt?.Replace("'", "''")}', 
                                    '{promptList.MergePrompt?.Replace("'", "''")}', 
                                    '{promptList.FinalPrompt?.Replace("'", "''")}', 
                                    '{promptList.Rating}', 
                                    '{loggedInUser}', 
                                    '{DateTime.Now:yyyy-MM-dd}', 
                                    '{DateTime.Now:HH:mm:ss}', 
                                    '{promptList.Active}')";

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

                var jsonstring = JsonConvert.SerializeObject(input);
                var json = new StringContent(jsonstring, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("api/Data/CRUD_API", json);
                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<List<Result>>(responseData);
                    if (result != null && result[0].OverAllError[0] == "1")
                    {
                        TempData["Msg"] = "New Prompt Saved Successfully!";
                        return RedirectToAction("Main");
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
                var ErrorMessage = string.Join("\\n", ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage));
                TempData["Msg"] = ErrorMessage;

                TempData["Msg"] = "Please fix the following: \\n " + ErrorMessage;
            }
            //await LoadDropDownAIModel();
            await LoadDropDownCategory();
            await LoadDropDownTopics();

            return View("PromptView", promptList);
        }

        //2026Apr16 Pratik Retrieves a specific promptList by ID and loads it into the edit form.
        public async Task<IActionResult> EditPrompt(int id)
        {
            string GetQry = "Select * from PromptList Where AutoId = " + id + "";
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
                    var PromptL = JsonConvert.DeserializeObject<List<PromptList>>(result[0].NoOfRecordsAffected[0]);
                    var Prompt = PromptL.FirstOrDefault();
                    //await LoadDropDownAIModel();
                    await LoadDropDownCategory();
                    await LoadDropDownTopics();
                    return View("PromptView", Prompt);
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

        //2026Apr16 Pratik Updates an existing promptList record.
        [HttpPost]
        public async Task<IActionResult> EditPrompt(PromptList promptList)
        {
            ModelState.Remove("AIName");
            ModelState.Remove("UpdatedUser");
            ModelState.Remove("UpdatedDate");
            ModelState.Remove("UpdatedTime");
            ModelState.Remove("PositiveAttributes");
            ModelState.Remove("NegativeAttributes");
            ModelState.Remove("Prompt1");
            ModelState.Remove("Prompt2");
            ModelState.Remove("Prompt3");
            ModelState.Remove("MinPromptText");
            ModelState.Remove("MaxPromptText");
            ModelState.Remove("EnglishLanguagePrompt");
            ModelState.Remove("OtherLanguagePrompt");
            ModelState.Remove("CharCountPrompt");
            ModelState.Remove("WordCountPrompt");
            ModelState.Remove("Rating");

            if (ModelState.IsValid)
            {
                //promptList.UpdatedUser = "Admin";
                string loggedInUser = HttpContext.Session.GetString("UserName") ?? "System";
                promptList.UpdatedUser = loggedInUser;
                promptList.UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd");
                promptList.UpdatedTime = DateTime.Now.ToString("HH:mm:ss");

                int CharCount = promptList.FinalPrompt.Length;
                promptList.CharCountPrompt = CharCount.ToString();

                int wordCount = promptList.FinalPrompt.ToString().Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
                promptList.WordCountPrompt = wordCount.ToString();

                string UpdateQry = $"UPDATE PromptList SET InitialPrompt = '{promptList.InitialPrompt.Replace("'", "''")}', " +
                        $"CategoryID = {promptList.CategoryID}, TopicID = {promptList.TopicID}, " +
                        $"Prompt1 = '{promptList.Prompt1?.Replace("'", "''")}', Prompt2 = '{promptList.Prompt2?.Replace("'", "''")}', Prompt3 = '{promptList.Prompt3?.Replace("'", "''")}', " +
                        $"BlockedPrompt = '{promptList.BlockedPrompt.Replace("'", "''")}', UserIgnorePrompt = '{promptList.UserIgnorePrompt?.Replace("'", "''")}', " +
                        $"PositiveAttributes = '{promptList.PositiveAttributes?.Replace("'", "''")}', NegativeAttributes = '{promptList.NegativeAttributes?.Replace("'", "''")}', " +
                        $"MinLength = {promptList.MinLength}, MaxLength = {promptList.MaxLength}, " +
                        $"MinPromptText = '{promptList.MinPromptText?.Replace("'", "''")}', MaxPromptText = '{promptList.MaxPromptText?.Replace("'", "''")}', " +
                        $"EnglishLanguagePrompt = '{promptList.EnglishLanguagePrompt?.Replace("'", "''")}', OtherLanguagePrompt = '{promptList.OtherLanguagePrompt?.Replace("'", "''")}', " +
                        $"CharCountPrompt = {promptList.CharCountPrompt}, WordCountPrompt = {promptList.WordCountPrompt}, " +
                        $"EndPrompt = '{promptList.EndPrompt?.Replace("'", "''")}', FinalEndPrompt = '{promptList.FinalEndPrompt?.Replace("'", "''")}', MergePrompt = '{promptList.MergePrompt?.Replace("'", "''")}', " +
                        $"FinalPrompt = '{promptList.FinalPrompt?.Replace("'", "''")}', Rating = {promptList.Rating}, " +
                        $"UpdatedUser = '{promptList.UpdatedUser}', UpdatedDate = '{promptList.UpdatedDate}', UpdatedTime = '{promptList.UpdatedTime}', Active = '{promptList.Active}' " +
                        $"WHERE AutoId = {promptList.AutoID}";

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
                        TempData["Msg"] = "Prompt Details updated successfully!";
                        return RedirectToAction("Main");
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
            //await LoadDropDownAIModel();
            await LoadDropDownCategory();
            await LoadDropDownTopics();

            return View("PromptView", promptList);
        }

        //2026Apr16 Pratik Deletes an prompt from the system based on the AutoId.
        public async Task<IActionResult> DeletePrompt(int id)
        {
            string DeleteQry = $"Delete From PromptList Where AutoId = {id}";
            var input = new InputsValue
            {
                SQLStatements = new[] { DeleteQry },
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
                    TempData["Msg"] = "Prompt Details was deleted successfully!";
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
            //await LoadDropDownAIModel();
            await LoadDropDownCategory();
            await LoadDropDownTopics();

            return RedirectToAction("Main");
        }
        
        public async Task<IActionResult> Chat(int id)
        {
            string Qry = $"Select * from PromptList Where AutoId = {id}";
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
                    var Data = JsonConvert.DeserializeObject<List<PromptList>>(result[0].OverAllError[0]);
                    var MainData = Data.FirstOrDefault();
                    if (MainData != null)
                    {
                        string initialPro = MainData.InitialPrompt;
                        string EndPro = MainData.EndPrompt;
                        string MinProMsg = MainData.MinPromptText;
                        string MaxProMsg = MainData.MaxPromptText;
                    }
                }
            }
            return View();
        }

        //2026Apr16 Pratik Exports all PromptList data to an Excel file using ClosedXML.
        public async Task<IActionResult> Download()
        {
            string GetQry = "Select * from PromptList Where Active = '1'";
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
                        var WorkSheet = workBook.Worksheets.Add("PromptData");
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
                            var content = stream.ToArray();
                            string fileName = $"PromptList_{DateTime.Now:dd-MM-yyyy}.xlsx";

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

        public async Task<IActionResult> DownloadConversation(int id)
        {
            string sql = $"Select * from ChatConversation Where Prompt_id = {id} Order by ChatSession_id desc";
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
                        var WorkSheet = workBook.Worksheets.Add("PromptData");
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
                            string fileName = $"ChatConversation_PromptId_{id}_{DateTime.Now:dd-MM-yyyy}.xlsx";

                            return File(contentt, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                        }
                    }
                }
            }
            return View("ChatSession");
        }
    }
}
