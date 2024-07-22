using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FYP.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public DataController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost]
        public async Task<IActionResult> PostData([FromBody] DataModel data)
        {
            try
            {
                // Validate input
                if (data == null)
                {
                    return BadRequest("Data is null");
                }

                var client = _httpClientFactory.CreateClient();

                // Replace with your actual Node.js API endpoint
                var apiUrl = "http://localhost:8080/ws"; // Assuming this is your Node.js endpoint

                // Convert DataModel to JSON string
                string jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(data);

                // Create StringContent with JSON content
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Send POST request
                var response = await client.PostAsync(apiUrl, content);

                // Check response status
                if (response.IsSuccessStatusCode)
                {
                    return Ok("Data sent successfully to Node.js server");
                }
                else
                {
                    return StatusCode((int)response.StatusCode, response.ReasonPhrase);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error sending data: {ex.Message}");
            }
        }
    }
}

// Define your data model class here
public class DataModel
    {
        public bool IsAnomalous { get; set; }
        public string forbiddenword { get; set; }
        public string DeviceName { get; set; }
    }


