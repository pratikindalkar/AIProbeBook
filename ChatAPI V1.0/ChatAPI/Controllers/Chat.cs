using ChatAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Data;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace ChatAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Chat : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly IHttpClientFactory httpClientFactory;
        //private const string OpenAIUrl = "https://api.openai.com/v1/chat/completions";

        public Chat(IConfiguration config, IHttpClientFactory _httpClientFactory)
        {
            configuration = config;
            httpClientFactory = _httpClientFactory;
        }
        [HttpPost("Ask")]
        public async Task<IActionResult> AskAI([FromBody] UserRequest request)
        {
            if (request.Messages == null || request.Messages.Count == 0)
            {
                return BadRequest("Messages cannot be empty.");
            }

            DataTable dt = await FillDataTable(request.Model);
            if (dt == null || dt.Rows.Count == 0)
            {
                return BadRequest("Invalid Model ID.");
            }

            string MetaData = dt.Rows[0]["MetaData"].ToString();
            string OpenAIUrl = dt.Rows[0]["APIUrl1"].ToString();
            string APIKey = dt.Rows[0]["APIKey"].ToString();

            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", APIKey);

            //var openAiPayload = new
            //{
            //    model = "gpt-5-mini",
            //    messages = request.Messages,
            //    temperature = 1,
            //    max_completion_tokens = 8192,
            //    reasoning_effort = "low",
            //    verbosity = "high"
            //};
            //var openAiPayload = new
            //{
            //    MetaData,
            //    messages = request.Messages,
            //};

            try
            {
                var payload = Newtonsoft.Json.Linq.JObject.Parse(MetaData);
                payload["messages"] = Newtonsoft.Json.Linq.JToken.FromObject(request.Messages);
                var jsonContent = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(OpenAIUrl, jsonContent);

                //var jsonContent = JsonConvert.SerializeObject(openAiPayload);
                //var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                //var response = await client.PostAsync(OpenAIUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var Result = await response.Content.ReadFromJsonAsync<OpenAiResponse>();
                    var botReply = Result?.Choices?.FirstOrDefault()?.Message?.content;
                    return Ok(new { reply = botReply });
                }
                else
                {
                    return StatusCode((int)response.StatusCode, "Error from OpenAI Provider");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error : {ex.Message}");
            }
        }

        private async Task<DataTable> FillDataTable(string mId)
        {
            DataTable dt = new DataTable();
            string connString = configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection con = new SqlConnection(connString))
            {
                string qry = $"SELECT * FROM AIModel WHERE AutoId = {mId}";
                using (SqlCommand cmd = new SqlCommand(qry, con))
                {
                    cmd.Parameters.AddWithValue("@mId", mId);
                    SqlDataAdapter sda = new SqlDataAdapter(cmd);
                    await con.OpenAsync();
                    sda.Fill(dt);
                }
            }
            return dt;
        }
    }
}