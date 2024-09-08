using Microsoft.Extensions.Caching.Distributed;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;
using WebApplication1.Services.Todo_s.Interfaces;

namespace WebApplication1.Services.Todo_s
{
    public class TodoService : ITodoService
    {
        private readonly IHttpClientFactory _httpClient;
        private readonly IDistributedCache _cache;
        private readonly ILogger<TodoService> _logger;

        public TodoService(IHttpClientFactory httpClient, IDistributedCache cache, ILogger<TodoService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
        }

        public async Task<(DTOMovie Todos, TimeSpan ExecutionTime)> Get()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            string cacheKey = "MoviesList";
            DTOMovie listMovies;

            try
            {
                var redisMoviesInfo = await _cache.GetAsync(cacheKey);

                if (redisMoviesInfo != null)
                {
                    byte[] decompressedBirds = Decompress(redisMoviesInfo);
                    listMovies = JsonSerializer.Deserialize<DTOMovie>(decompressedBirds);
                }
                else
                {
                    listMovies = await FetchMoviesFromApi();

                    if (listMovies != null)
                    {
                        byte[] serializedBirds = JsonSerializer.SerializeToUtf8Bytes(listMovies);
                        byte[] compressedBirds = Compress(serializedBirds);
                        var options = new DistributedCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromSeconds(60))
                            .SetSlidingExpiration(TimeSpan.FromSeconds(60));
                        await _cache.SetAsync(cacheKey, compressedBirds, options);
                    }
                }

                stopwatch.Stop();
                return (listMovies ?? CreateDefaultDTOMovie(), stopwatch.Elapsed);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching movies from API");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Get method");
                throw;
            }
        }

        private async Task<DTOMovie> FetchMoviesFromApi()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.themoviedb.org/3/movie/now_playing");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("MyApp", "1.0"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiJ9.eyJhdWQiOiI0N2JiZDZkMTQ5NzM0OTM1NzI5NDQyMmI1MjY1YmIwYSIsIm5iZiI6MTcyNTc1NDY5MC4zMzY0OTksInN1YiI6IjY2ODIzMTQ2MTVlOWUyOWZlYzY1YWViMyIsInNjb3BlcyI6WyJhcGlfcmVhZCJdLCJ2ZXJzaW9uIjoxfQ.L3LVcFSHTcLrQ11K9h7YEFTKRSjGr8zLomVfeB3eYT4");

            var client = _httpClient.CreateClient();
            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Respuesta API: {content}");

                try
                {
                    var listBirds = JsonSerializer.Deserialize<DTOMovie>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (listBirds != null)
                    {
                        return listBirds;
                    }
                    else
                    {
                        _logger.LogWarning("La deserialización resultó en null o no hay resultados.");
                        return CreateDefaultDTOMovie();
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError($"Error al deserializar JSON: {ex.Message}");
                    _logger.LogError($"JSON problemático: {content}");
                    return CreateDefaultDTOMovie();
                }
            }
            else
            {
                _logger.LogError($"Error en la solicitud API: {response.StatusCode}");
                return CreateDefaultDTOMovie();
            }
        }

        private DTOMovie CreateDefaultDTOMovie()
        {
            return new DTOMovie
            {
                dates = new Dates(),
                page = 0,
                results = new List<Result>(),
                total_pages = 0,
                total_results = 0
            };
        }
        private static byte[] Compress(byte[] data)
        {
            using var memoryStream = new MemoryStream();
            using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Fastest))
            {
                gzipStream.Write(data, 0, data.Length);
            }
            return memoryStream.ToArray();
        }
        private static byte[] Decompress(byte[] data)
        {
            using var compressedStream = new MemoryStream(data);
            using var decompressStream = new MemoryStream();
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            {
                gzipStream.CopyTo(decompressStream);
            }
            return decompressStream.ToArray();
        }
    }
    
}
