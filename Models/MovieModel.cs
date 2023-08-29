namespace Dubbingverse.Models
{
    public class MovieModel
    {
        public string Title { get; set; }
        public int PageID { get; set; }

        public List<ActorModel> ActorsAndCharacters { get; set; }

        public string ShortDescription { get;set; }
    }
}
