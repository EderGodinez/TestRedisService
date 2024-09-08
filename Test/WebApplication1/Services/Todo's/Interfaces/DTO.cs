namespace WebApplication1.Services.Todo_s.Interfaces
{
    public class Response
    {
        public List<Todo>? Todos { get; set; }
        public int? Total { get; set; }
        public int? Skip { get; set; }
        public int? Limit { get; set; }
    }

    public class Todo
    {
        public int Id { get; set; }
        public string todo { get; set; }
        public bool Completed { get; set; }
        public int UserId { get; set; }
    }

}
