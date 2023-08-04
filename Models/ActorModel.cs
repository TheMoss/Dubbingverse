namespace Dubbingverse.Models
{
	public class ActorModel
	{
		public string ActorName { get; set; }
		public List<string> CharactersList { get; set; } = new List<string>();

		public ActorModel(string actorName)
		{
			ActorName = actorName;
		}
	}
}
