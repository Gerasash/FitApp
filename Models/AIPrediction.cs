using SQLite;

namespace FitApp.Models;

[Table("AIPredictions")]
public class AIPrediction
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int ExerciseId { get; set; }
    public double PredictedWeight { get; set; }
    public int PredictedReps { get; set; }
    public string Trend { get; set; } = string.Empty;
    public string InsightText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
