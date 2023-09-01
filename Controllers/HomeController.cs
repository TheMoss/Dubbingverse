using Dubbingverse.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.XPath;

namespace Dubbingverse.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly HttpClient _httpClient = new HttpClient();
		public List<MovieModel> MoviesList { get; set; }


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
			for (int i = 0; i < results.query.search.Count(); i++)
			{
				MoviesList.Add(new MovieModel()
				{
					Title = results.query.search[i].title,
					PageID = results.query.search[i].pageid
				});
			}
			ViewData["MoviesList"] = MoviesList;
			return View("Results");
		}
		public string MatchPageText(string httpResponse)
		{
			string pageTextExpression = @"<(.+)>";
			var regex = new Regex(pageTextExpression); //extract only text content from the parsed page response
			MatchCollection matches = regex.Matches(httpResponse);
			return matches[0].ToString();
		}

		public string MatchMovieTitle(string httpResponse)
		{
			string movieTitleExpression = "\"title\":\"(.*?)\",";
			var regex = new Regex(movieTitleExpression);
			MatchCollection matches = regex.Matches(httpResponse);
			return matches[0].Groups[1].ToString();
		}

		public List<ActorModel> CreateActorsAndCharactersList(XPathNavigator navigator)
		{

			string actorsExpression = "/div/ul/li/a/text()";
			string charactersExpression = "../..//b/text()";

			XPathNodeIterator nodes = navigator.Select(actorsExpression);

			List<ActorModel> actorsList = new List<ActorModel>();
			while (nodes.MoveNext())
			{
				XPathNavigator actorNavigator = nodes.Current.Clone();
				XPathNodeIterator characterNodes = actorNavigator.Select(charactersExpression);
				ActorModel actor = new ActorModel(nodes.Current.Value);

				while (characterNodes.MoveNext())
				{
					actor.CharactersList.Add(characterNodes.Current.Value);
				}
				actorsList.Add(actor);
			}

			for (int i = 0; i < actorsList.Count; i++)
			{
				var actor = actorsList[i];
				if (actor.CharactersList.Count == 0)
				{
					actorsList.Remove(actor);
				}
			}
			return actorsList;
		}
		public string GetShortDescription(XPathNavigator navigator)
		{
			string descriptionExpression = "//p[string-length()>500]";
			return navigator.SelectSingleNode(descriptionExpression).Value;//select vs selectsinglenode
		}

		public async Task<IActionResult> GetMovieInformation(int pageId)
		{
			MovieModel yourMovie = new MovieModel();

			string url = $"https://dubbingpedia.pl/w/api.php?action=parse&format=json&pageid={pageId}";
			string httpResponse = await _httpClient.GetStringAsync(url);
			yourMovie.Title = MatchMovieTitle(httpResponse);
			byte[] byteArray = Encoding.UTF8.GetBytes(Regex.Unescape(MatchPageText(httpResponse)));//extract just the parsed page
			MemoryStream stream = new MemoryStream(byteArray);
			var doc = new XPathDocument(stream);
			var navigator = doc.CreateNavigator();
			yourMovie.ActorsAndCharacters = CreateActorsAndCharactersList(navigator);//select actors, characters and the description
			yourMovie.ShortDescription = GetShortDescription(navigator);

			ViewData["MovieTitle"] = yourMovie.Title;
			ViewData["ShortDescription"] = yourMovie.ShortDescription;
			ViewData["ActorsAndCharactersList"] = yourMovie.ActorsAndCharacters;
			return View("Details");
		}

		public async Task<IActionResult> GetActorInformation(string actorName)
		{
			string url = $"https://dubbingpedia.pl/w/api.php?action=query&list=search&srsearch={actorName}&prop=links&format=json";

			var response = await _httpClient.GetStringAsync(url);
			var results = JsonSerializer.Deserialize<SearchResultsModel>(response);
			var actorId = results.query.search[0].pageid.ToString();
			string actorUrl = $"https://dubbingpedia.pl/w/api.php?action=parse&format=json&pageid={actorId}";

			string httpResponse = await _httpClient.GetStringAsync(actorUrl);//match page and parse
			byte[] byteArray = Encoding.UTF8.GetBytes(Regex.Unescape(MatchPageText(httpResponse)));//extract just the parsed page
			MemoryStream stream = new MemoryStream(byteArray);
			var doc = new XPathDocument(stream);
			var navigator = doc.CreateNavigator();

			ActorModel actor = new ActorModel(actorName);

			ViewData["ActorName"] = actor.ActorName;

			GetAllMovies(navigator, actor);
			ViewData["ActorRolesList"] = actor.CharactersList;

			return View("Actor");
		}


		public ActorModel GetAllMovies(XPathNavigator navigator, ActorModel actor)
		{
			string actorMoviesParagraphExpression = "//span[@id=\"Filmy\"]/following::ul[1]";
			string actorMoviesExpression = "//span[@id=\"Filmy\"]/following::ul[1]/li";

			var movies = navigator.Select(actorMoviesParagraphExpression);//finds movies in paragraph


			while (movies.MoveNext())
			{
				XPathNavigator movieNavigator = movies.Current.Clone();

				XPathNodeIterator movieNodes = movieNavigator.Select(actorMoviesExpression);


				while (movieNodes.MoveNext())
				{
					actor.CharactersList.Add(movieNodes.Current.Value);
				}

			}
			return actor;
		}
	}
}