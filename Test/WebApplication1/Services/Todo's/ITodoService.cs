using WebApplication1.Services.Todo_s.Interfaces;

namespace WebApplication1.Services.Todo_s
{
    public interface ITodoService
    {
        Task<(DTOMovie Todos, TimeSpan ExecutionTime)> Get();
    }
}
