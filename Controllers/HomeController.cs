using Dubbingverse.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Dubbingverse.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly HttpClient _httpClient = new HttpClient();	
		public List<MovieModel> MoviesList { get; set; }
		public string MovieTitle { get; set; }
		public string ShortDescription { get; set; } = "Opis niedostępny";
		public List<ActorModel> ActorsAndCharactersList { get; set; } = new List<ActorModel>();

		public HomeController(ILogger<HomeController> logger)
		{
		 MoviesList = new List<MovieModel>();
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
                MoviesList.Add(new MovieModel(results.query.search[i].title, results.query.search[i].pageid));
			}			
			ViewData["MoviesList"] = MoviesList;
			return View("Results");
			
		}
		public string MatchPageText(string httpResponse)
		{
			string regexPattern = @"<(.+)>";
			var regex = new Regex(regexPattern); //extract only text content from the parsed page response
			MatchCollection matches = regex.Matches(httpResponse);
			string matchResult = matches[0].ToString();
			return matchResult;
		}

		public string MatchMovieTitle(string httpResponse)
		{
			string titlePattern = "\"title\":\"(.*?)\",";
			var regex = new Regex(titlePattern);
			MatchCollection matches = regex.Matches(httpResponse);
			return matches[0].Groups[1].ToString();
		}

		public List<ActorModel> CreateActorsAndCharactersList(XPathNavigator navigator) {
			
			string actorsExpression = "/div/ul/li/a/text()";
			string charactersExpression = "../..//b/text()";			

			XPathNodeIterator nodes = navigator.Select(actorsExpression);
			

			List<ActorModel> actors = new List<ActorModel>();
			while (nodes.MoveNext())
			{
				XPathNavigator actorNavigator = nodes.Current.Clone();
				XPathNodeIterator characterNodes = actorNavigator.Select(charactersExpression);
				ActorModel actor = new ActorModel(nodes.Current.Value);
				
					while (characterNodes.MoveNext())
					{
						actor.CharactersList.Add(characterNodes.Current.Value);
					}
					actors.Add(actor);
				
				
			}

			for (int i =0; i< actors.Count; i++)
			{
				var actor = actors[i];
				if (actor.CharactersList.Count == 0 )
				{
					actors.Remove(actor);
				}
			}
			return actors;
		}
		public string GetShortDescription(XPathNavigator navigator)
		{
			string descriptionExpression = "//p[string-length()>500]";
			XPathNodeIterator nodes = navigator.Select(descriptionExpression);
			var descriptionNodes = nodes.Current.Clone();
			var description = descriptionNodes.SelectSingleNode(descriptionExpression).Value;
			return description;
		}
		public async Task<IActionResult> GetMovieInformation(int pageId)
		{
			string url = $"https://dubbingpedia.pl/w/api.php?action=parse&format=json&pageid={pageId}";
			string httpResponse = await _httpClient.GetStringAsync(url);
			MovieTitle = MatchMovieTitle(httpResponse);
			byte[] byteArray = Encoding.UTF8.GetBytes(Regex.Unescape(MatchPageText(httpResponse)));//extract just the parsed page
			MemoryStream stream = new MemoryStream(byteArray);
			var doc = new XPathDocument(stream);
			var navigator = doc.CreateNavigator();
			ActorsAndCharactersList = CreateActorsAndCharactersList(navigator);//select actors, characters and the description
			ShortDescription = GetShortDescription(navigator);

			ViewData["MovieTitle"] = MovieTitle;
			ViewData["ShortDescription"] = ShortDescription;
			ViewData["ActorsAndCharactersList"] = ActorsAndCharactersList;
			return View("Details");
        }

		
	}
}