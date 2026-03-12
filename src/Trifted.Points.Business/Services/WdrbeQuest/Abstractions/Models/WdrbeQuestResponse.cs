using Trifted.Points.Data.Enums;

namespace Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Models;

public class WdrbeQuestResponse : GetWbdrbeQuestResponse
{
    /// <summary>
    /// Gets or sets the Quest Tasks.
    /// </summary>
    public List<GetWdrbeQuestTasksResponse> TaskQuest { get; set; } = [];
}