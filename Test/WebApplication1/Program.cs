
using Microsoft.Extensions.Configuration;
using WebApplication1.Services.Todo_s;

namespace WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            builder.Services.AddHttpClient();
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = "redis:6379";
            });
            builder.Services.AddSingleton<ITodoService, TodoService>();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
