namespace Dubbingverse.Models
{
    public class MovieModel
    {
        public string Title { get; set; }
        public int PageID { get; set; }

        public MovieModel(string title, int pageId)
        {
            Title = title;
            PageID = pageId;
        }
    }
}
