using Microsoft.AspNetCore.Mvc;
using NZNewsApi.Models;
using NZNewsApi.Dtos;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using StackExchange.Redis;
using NZNewsApi.Services.Interfaces;



namespace NZNewsApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NewsController : ControllerBase
    {
        private readonly INewsStoryService _newStoryService;

        public NewsController(INewsStoryService newsStoryService)
        {
            _newStoryService = newsStoryService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(int page = 1, int pageSize = 10, string storyType = "new", string search = null)
        {
            // Validate input parameters
            if (page < 1 || pageSize < 1)
            {
                return BadRequest("Page and pageSize must be greater than zero.");
            }

            try
            {
                var result =  await _newStoryService.Get(page, pageSize, storyType, search);

                return Ok(result);
            }
            catch (HttpRequestException e)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, $"Error fetching data: {e.Message}");
            }
            catch (JsonException e)
            {
                return BadRequest($"Error processing response data: {e.Message}");
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {e.Message}");
            }

        }

    }
}
