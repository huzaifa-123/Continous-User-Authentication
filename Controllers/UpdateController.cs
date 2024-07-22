using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FYP.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class WebsiteController : ControllerBase
    {
        [HttpPost("update")]
        public IActionResult Update([FromBody] WebsiteData model)
        {
            // Handle the update logic using the model data
            return Ok("Update successful");
        }
    }
    public class UpdateController : ControllerBase
    {
        private const string JsonFilePath = "./data.json";
        private const string CsvFilePath = "./data.csv";

        [HttpPost("update")]
        public async Task<IActionResult> UpdateWebsiteData([FromBody] WebsiteData websiteData)
        {
            if (websiteData == null)
            {
                return BadRequest("Invalid website data");
            }

            // Read existing JSON data
            List<WebsiteData> existingData;
            try
            {
                existingData = JsonConvert.DeserializeObject<List<WebsiteData>>(await System.IO.File.ReadAllTextAsync(JsonFilePath)) ?? new List<WebsiteData>();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to read JSON file: {ex.Message}");
            }

            // Add new data
            existingData.Add(websiteData);

            // Save updated JSON data
            try
            {
                await System.IO.File.WriteAllTextAsync(JsonFilePath, JsonConvert.SerializeObject(existingData));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to write JSON file: {ex.Message}");
            }

            // Update CSV file
            await UpdateCsvFile(existingData);

            return Ok("Data updated successfully");
        }

        private async Task UpdateCsvFile(List<WebsiteData> data)
        {
            var csvContent = "Website Name,Start Time,End Time\n" +
                string.Join("\n", data.Select(d => $"{d.WebsiteName},{d.StartTime},{d.EndTime}"));

            try
            {
                await System.IO.File.WriteAllTextAsync(CsvFilePath, csvContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update CSV file: {ex.Message}");
            }
        }
       
    }
}
