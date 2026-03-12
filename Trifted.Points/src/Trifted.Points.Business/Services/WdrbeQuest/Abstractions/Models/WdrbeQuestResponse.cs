namespace Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Models;

public class WdrbeQuestResponse
{
    /// <summary>
    /// Gets or sets the Total Points.
    /// This property is used to define Total Points.
    /// </summary>
    public int TotalPoints { get; set; }

    /// <summary>
    /// Gets or sets the Quest Tasks.
    /// </summary>
    public List<GetWdrbeQuestTasksResponse> TaskQuest { get; set; } = [];
}