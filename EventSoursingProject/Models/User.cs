namespace EventSoursingProject.Models
{
    public class User
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<Score> UserScores { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
}
