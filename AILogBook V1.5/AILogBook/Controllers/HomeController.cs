using AILogBook.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AILogBook.Controllers
{
    public class HomeController : Controller
    {
        private readonly HttpClient httpClient;
        private readonly string ConnectionString;
        private readonly IConfiguration configuration;

        public HomeController(IHttpClientFactory httpClientFactory, IConfiguration config)
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
        public async Task<ActionResult> Index(string? SelectedModelName, string? SelectedCateName)
        {
           //bool mainOpen = TempData["MainOpen"] as bool? ?? false;
           // bool aiOpen = TempData["AIOpen"] as bool? ?? false;
           // bool catOpen = TempData["CatOpen"] as bool? ?? false;
           // bool attrOpen = TempData["AttrOpen"] as bool? ?? false; 

           // if (!string.IsNullOrEmpty(activeMenu))
           // {
           //     if (activeMenu == "Main") mainOpen = !mainOpen;
           //     if (activeMenu == "AI") aiOpen = !aiOpen;
           //     if (activeMenu == "Cat") catOpen = !catOpen;
           //     if (activeMenu == "Attribute") attrOpen = !attrOpen; 
           // }

           // TempData["MainOpen"] = mainOpen;
           // TempData["AIOpen"] = aiOpen;
           // TempData["CatOpen"] = catOpen;  
           // TempData["AttrOpen"] = attrOpen; 

           // TempData.Keep();

            await LoadDropDownAIModel();
            await LoadDropDownCategory();

            var MainModelData = new List<PromptList>();
            string APIUrl = "api/Data/CRUD_API";

            if(!string.IsNullOrEmpty(SelectedModelName) || !string.IsNullOrEmpty(SelectedCateName))
            {
                APIUrl = $"api/Data/CRUD_API?SelectedModelName={SelectedModelName}&SelectedCateName={SelectedCateName}";
            }

            var MainModelresponse = await httpClient.GetAsync(APIUrl);
            if (MainModelresponse.IsSuccessStatusCode)
            {
                var MMdata = await MainModelresponse.Content.ReadAsStringAsync();
                if (MMdata.Trim().StartsWith("["))
                {
                    MainModelData = JsonConvert.DeserializeObject<List<PromptList>>(MMdata);
                }
                else
                {
                    var singleItem = JsonConvert.DeserializeObject<PromptList>(MMdata);
                    MainModelData = new List<PromptList> { singleItem };
                }
            }

            ViewBag.MainModelList = MainModelData;
            return View("Index");
        }

        private List<T> DeserializeHelper<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<T>();
            json = json.Trim();

            if (json.StartsWith("["))
            {
                return JsonConvert.DeserializeObject<List<T>>(json) ?? new List<T>();
            }
            else
            {
                var singleItem = JsonConvert.DeserializeObject<T>(json);
                return new List<T> { singleItem };
            }
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
                var apiResult = JsonConvert.DeserializeObject<List<Result>>(data);

                if (apiResult != null && apiResult.Count > 0)
                {
                    string jsonTableData = apiResult[0].NoOfRecordsAffected[0];
                    var modelList = JsonConvert.DeserializeObject<List<AIModel>>(jsonTableData);
                    ViewBag.ModelList = modelList;
                }
            }
        }

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

                if(result.Count > 0 && result != null)
                {
                    string JsonData = result[0].NoOfRecordsAffected[0];
                    var CateList = JsonConvert.DeserializeObject<List<Categories>>(JsonData);
                    ViewBag.CateList = CateList;
                }
            }
        }
        //private async Task LoadDropdownAttribute()
        //{
        //    string sql = "Select * from Attribute Where Active = '1'";
        //    var input = new InputsValue
        //    {
        //        SQLStatements = new[] { sql },
        //        SQLReturntype = new[] { "0" },
        //        DBDetails = ConnectionString,
        //        DBProfile = "connect",
        //        multiuserflag = "",
        //        securitykey = "AuthenticationKey",
        //        securityvalue = "VKS_KEY",
        //        sqltimeout = "30",
        //        rollbackcommit = "0",
        //        encrypt = false
        //    };

        //    var JsonContent = JsonConvert.SerializeObject(input);
        //    var content = new StringContent(JsonContent, Encoding.UTF8, "application/json");

        //    var response = await httpClient.PostAsync("api/Data/CRUD_API", content);
        //    //var response = await httpClient.PostAsync("api/testing/CRUD_API", content);

        //    if (response.IsSuccessStatusCode)
        //    {
        //        var data = await response.Content.ReadAsStringAsync();
        //        var result = JsonConvert.DeserializeObject<List<Result>>(data);

        //        if(result != null && result.Count > 0)
        //        {
        //            string JsonData = result[0].NoOfRecordsAffected[0];
        //            var AttriList = JsonConvert.DeserializeObject<List<Attributes>>(JsonData);
        //            ViewBag.AttrList = AttriList;
        //        }
        //    }
        //}
        //private async Task LoadDropdownTopics()
        //{
        //    string sql = "Select * from Topics Where Active = '1'";
        //    var input = new InputsValue
        //    {
        //        SQLStatements = new[] { sql },
        //        SQLReturntype = new[] { "0" },
        //        DBDetails = ConnectionString,
        //        DBProfile = "connect",
        //        multiuserflag = "",
        //        securitykey = "AuthenticationKey",
        //        securityvalue = "VKS_KEY",
        //        sqltimeout = "30",
        //        rollbackcommit = "0",
        //        encrypt = false
        //    };

        //    var jsonContent = JsonConvert.SerializeObject(input);
        //    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        //    var response = await httpClient.PostAsync("api/Data/CRUD_API", content);
        //    //var response = await httpClient.PostAsync("api/testing/CRUD_API",content);

        //    if (response.IsSuccessStatusCode)
        //    {
        //        var data = await response.Content.ReadAsStringAsync();
        //        var result = JsonConvert.DeserializeObject<List<Result>>(data);
        //        if(result != null && result.Count > 0)
        //        {
        //            string JsonData = result[0].NoOfRecordsAffected[0];
        //            var TopicList = JsonConvert.DeserializeObject<List<Topics>>(JsonData);
        //            ViewBag.TopicList = TopicList;
        //        }
        //    }
        //}
        public async Task<IActionResult> PromptView()
        {
            TempData["MenuExpanded"] = true;
            TempData.Keep("MenuExpanded");

            await LoadDropDownAIModel();
            await LoadDropDownCategory();
            //await LoadDropdownTopics();
            //await LoadDropdownAttribute();
            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        //public async Task<IActionResult> AIModel()
        //{
        //    bool aiOpen = TempData["AIOpen"] as bool? ?? false;

        //    TempData["AIOpen"] = !aiOpen;
        //    TempData["AttrOpen"] = false;
        //    TempData["MainOpen"] = false;
        //    TempData["CatOpen"] = false;

        //    TempData.Keep();

        //    await LoadDropDownAIModel();
        //    return View();
        //}
        //public async Task<IActionResult> AddAIModel()
        //{
        //    return View();
        //}
        //public async Task<IActionResult> Categories()
        //{
        //    bool catOpen = TempData["CatOpen"] as bool? ?? false;

        //    TempData["CatOpen"] = !catOpen;
        //    TempData["AIOpen"] = false;
        //    TempData["AttrOpen"] = false;
        //    TempData["MainOpen"] = false;

        //    TempData.Keep();

        //    await LoadDropDownAIModel();
        //    return View();
        //}
        //public async Task<IActionResult> AddCategories()
        //{
        //    return View();
        //}
        //public async Task<IActionResult> Attributes()
        //{
        //    bool attrOpen = TempData["AttrOpen"] as bool? ?? false; // Added this

        //    TempData["AttrOpen"] = !attrOpen;
        //    TempData["CatOpen"] = false;
        //    TempData["AIOpen"] = false;
        //    TempData["MainOpen"] = false;

        //    TempData.Keep("AttrOpen");
        //    TempData.Keep("CatOpen");
        //    TempData.Keep("AIOpen");
        //    TempData.Keep("MainOpen");

        //    await LoadDropdownAttribute();
        //    return View("Attributes");
        //}
        //public async Task<IActionResult> AddAttributes()
        //{
        //    TempData["AttrOpen"] = true;
        //    TempData.Keep();

        //    await LoadDropDownCategory();
        //    return View();
        //}

        //[HttpPost]
        //public async Task<IActionResult> AddAttributes(Attributes attributes)
        //{
        //    TempData["AttrOpen"] = true;
        //    TempData.Keep();
        //    List<string> msgList = new List<string>(); 

        //    if (attributes.CategoryId <= 0)
        //    {
        //        msgList.Add("Please Select category Id");
        //        ModelState.AddModelError("CategoryId", "Required");
        //        ViewBag.messageList = msgList;
        //        await LoadDropDownCategory();  
        //        return View("AddAttributes", attributes); 
        //    }

        //    ModelState.Remove("AutoId");
        //    ModelState.Remove("UpdatedDate");
        //    ModelState.Remove("UpdatedTime");
        //    ModelState.Remove("UpdatedUser");

        //    if (ModelState.IsValid)
        //    {
        //        attributes.UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd");
        //        attributes.UpdatedTime = DateTime.Now.ToString("HH:mm:ss");
        //        attributes.UpdatedUser = "Admin";

        //        string Qry = $"Insert INTO Attribute (CategoryId, Attribute, Type, UpdatedUser, UpdatedDate, UpdatedTime, Active) VALUES " +
        //            $"('{attributes.CategoryId}', '{attributes.Attribute}', '{attributes.Type}', '{attributes.UpdatedUser}', '{attributes.UpdatedDate}', '{attributes.UpdatedTime}', '{attributes.Active}')";
        //        var input = new InputsValue
        //        {
        //            SQLStatements = new[] { Qry },
        //            SQLReturntype = new[] { "0" },
        //            DBDetails = ConnectionString,
        //            DBProfile = "connect",
        //            multiuserflag = "",
        //            securitykey = "AuthenticationKey",
        //            securityvalue = "VKS_KEY",
        //            sqltimeout = "30",
        //            rollbackcommit = "0",
        //            encrypt = false
        //        };


        //        var jsonContent = JsonConvert.SerializeObject(input);
        //        var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

        //        //var response = await httpClient.PostAsync("api/Data/CRUD_API", content);
        //        var response = await httpClient.PostAsync("api/testing/CRUD_API", content);


        //        if (response.IsSuccessStatusCode)
        //        {
        //            var responseData = await response.Content.ReadAsStringAsync();
        //            var result = JsonConvert.DeserializeObject<List<Result>>(responseData);

        //            if (result != null && result[0].OverAllError[0] == "1")
        //            {
        //                return RedirectToAction("Attributes");
        //            }
        //            else
        //            {
        //                msgList.Add("Database Error: " + result[0].ErrorMessage[0]);
        //            }
        //        }
        //        else
        //        {
        //            msgList.Add("Server Error. Please Contact administrator.");
        //        }
        //    }

        //    ViewBag.messageList = msgList;
        //    await LoadDropDownCategory();
        //    return View("AddAttributes", attributes);
        //}

        //public async Task<IActionResult> EditAttribute(int id)
        //{
        //    Attributes attributes = null;
        //    if(id <= 0)
        //    {
        //        return NotFound();
        //    }
        //    var response = await httpClient.GetAsync($"api/Main/GetAttributeById/{id}");
        //    if (response.IsSuccessStatusCode)
        //    {
        //        var data = await response.Content.ReadAsStringAsync();
        //        attributes = JsonConvert.DeserializeObject<Attributes>(data);
        //    }
        //    if(attributes == null)
        //    {
        //        return NotFound();
        //    }
        //    await LoadDropDownCategory();
        //    return View("AddAttributes", attributes);
        //}

        //[HttpPost]
        //public async Task<IActionResult> EditAttribute(Attributes attributes)
        //{
        //    if(attributes.AutoId <= 0)
        //    {
        //        ModelState.AddModelError("CategoryId", "Please select a category.");
        //    }
        //    ModelState.Remove("UpdatedDate");
        //    ModelState.Remove("UpdatedTime");
        //    ModelState.Remove("UpdatedUser");
        //    if (ModelState.IsValid)
        //    {
        //        attributes.UpdatedUser = "Admin";
        //        attributes.UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd");
        //        attributes.UpdatedTime = DateTime.Now.ToString("HH:mm:ss");

        //        var json = JsonConvert.SerializeObject(attributes);
        //        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        //        var response = await httpClient.PutAsync($"api/Main/UpdateAttribute/{attributes.AutoId}", content);
        //        if (response.IsSuccessStatusCode)
        //        {
        //            TempData["Msg"] = "Attribute updated successfully!";
        //            await LoadDropdownAttribute();
        //            return RedirectToAction("Attributes");
        //        }
        //        else
        //        {
        //            ViewBag.messageList = new List<string> { "Error updating record." };
        //        }
        //    }            
        //    await LoadDropDownCategory();
        //    return View("AddAttributes", attributes);
        //}

        //public async Task<IActionResult> DeleteAttribute(int id)
        //{
        //    var response = await httpClient.DeleteAsync($"api/Main/DeleteAttribute/{id}");
        //    if (response.IsSuccessStatusCode)
        //    {
        //        TempData["Msg"] = "The attribute was deleted successfully!";
        //    }
        //    else
        //    {
        //        TempData["Msg"] = "Error: Could not delete the attribute.";
        //    }
        //    return RedirectToAction("Attributes");
        //}

    }
}
