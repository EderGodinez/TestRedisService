using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services.Todo_s;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MovieController : ControllerBase
    {
        private readonly ITodoService _todoService;

        public MovieController(ITodoService todoService)
        {
            _todoService = todoService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMovies()
        {
            try
            {
                var (todos, executionTime) = await _todoService.Get();

                Response.Headers.Add("X-Execution-Time", executionTime.TotalMilliseconds.ToString("F2") + "ms");

                return Ok(todos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error"+ex.Message);
            }
        }
    }
}
