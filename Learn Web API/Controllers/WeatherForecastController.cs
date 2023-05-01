using AutoMapper;
using Learn_Web_API.Models;
using Microsoft.AspNetCore.Mvc;

namespace Learn_Web_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("/get")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
        [HttpGet]
        [Route("/cookie")]
        public async Task GetCookie()
        {
            using var connection = new Context();
            var config = new MapperConfiguration(cfg => cfg.CreateMap<Product, ProductDTO>()
            .ForMember("Number", opt => opt.MapFrom(c => c.Id))
            .ForMember("NameId", opt => opt.MapFrom(c => c.Name + c.Id)));
            var mapper = new Mapper(config);
            var users = mapper.Map<List<ProductDTO>>(connection.Products);
            foreach (var v in users)
            {
                Console.WriteLine($"{v.Name}, {v.Number}, {v.NameId}");
            }
        }
    }
}