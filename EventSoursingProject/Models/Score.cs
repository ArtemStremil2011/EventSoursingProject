namespace EventSoursingProject.Models
{
    public class Score
    {
        public Guid ScoreId { get; set; }
        public string ScoreName { get; set; } = string.Empty;
        public Guid OwnerId { get; set; }
        public User? ScoreOwner { get; set; }
        public int LastEventSequence { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
