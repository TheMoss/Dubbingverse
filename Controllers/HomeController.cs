using Dubbingverse.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Dubbingverse.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly HttpClient _httpClient = new HttpClient();	
		public List<string> TitleList { get; set; }

		public HomeController(ILogger<HomeController> logger)
		{
			TitleList = new List<string>();
			_logger = logger;
		}

		public IActionResult Index()
		{
			
			return View();
		}

		public IActionResult Privacy()
		{
			return View();
		}

		public IActionResult Results()
		{
			return View();
		}
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
		[HttpGet]
		public async Task<IActionResult> GetMovies(string searchedMovie)
		{
			
			string url = $"https://dubbingpedia.pl/w/api.php?action=query&list=search&srsearch={searchedMovie}&prop=links&format=json";

			var response = await _httpClient.GetStringAsync(url);
			var results = JsonSerializer.Deserialize<SearchResultsModel>(response);
			for( int i =0; i < results.query.search.Count(); i++)
			{
				TitleList.Add(results.query.search[i].title);
			}
			ViewData["TitleList"] = TitleList;
			return View("Results");
			
		}
	}
}