using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services.Todo_s;
using WebApplication1.Services.Todo_s.Interfaces;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TodoController : ControllerBase
    {
        private readonly ITodoService _todoService;

        public TodoController(ITodoService todoService)
        {
            _todoService = todoService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTodos()
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
