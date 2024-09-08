using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly MemoryCache _cacheMemory;
        private readonly ILogger<TodoService> _logger;

        public TodoService(IHttpClientFactory httpClient, IDistributedCache cache, ILogger<TodoService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
            _cacheMemory = new MemoryCache(new MemoryCacheOptions());
        }

        public async Task<(DTOMovie Todos, TimeSpan ExecutionTime)> Get()
        {
            string cacheKey = "MoviesList";
            DTOMovie listMovies;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            

            try
            {
                //Se verifica si el cache en memoria tiene el valor
                if (_cacheMemory.TryGetValue(cacheKey, out byte[] cachedData))
                {
                    byte[] decompressedMovies = Decompress(cachedData);
                    listMovies = JsonSerializer.Deserialize<DTOMovie>(decompressedMovies);
                    _logger.LogInformation("Data retrieved from cache Memory");
                }
                else 
                {
                    //Se verifica si el cache en redis tiene el valor
                    var redisMoviesInfo = await _cache.GetAsync(cacheKey);
                    if (redisMoviesInfo != null)
                    {
                        byte[] decompressedMovies = Decompress(redisMoviesInfo);
                        listMovies = JsonSerializer.Deserialize<DTOMovie>(decompressedMovies);
                        _logger.LogInformation("Data retrieved from redis cache");

                    }
                    else
                    {
                        //Si no se encuentra en cache, se obtiene de la API
                        listMovies = await FetchMoviesFromApi();
                        _logger.LogInformation("Data retrieved from API");

                        if (listMovies != null)
                        {
                            byte[] serializedMovies = JsonSerializer.SerializeToUtf8Bytes(listMovies);
                            byte[] compressedMovies = Compress(serializedMovies);
                            //CONFIGURACION DE CACHE EN REDIS
                            var options = new DistributedCacheEntryOptions()
                                .SetAbsoluteExpiration(TimeSpan.FromSeconds(60))
                                .SetSlidingExpiration(TimeSpan.FromSeconds(60));
                            ///CONFIGURACION DE CACHE EN MEMORIA
                            var cacheMemoryOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromSeconds(10))
                            .SetSlidingExpiration(TimeSpan.FromSeconds(10));
                            await _cache.SetAsync(cacheKey, compressedMovies, options);
                            _cacheMemory.Set(cacheKey, compressedMovies, cacheMemoryOptions);
                        }
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
