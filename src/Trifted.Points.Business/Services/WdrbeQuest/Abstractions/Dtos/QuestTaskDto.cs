using System.ComponentModel.DataAnnotations;

namespace Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Dtos;

public class QuestTaskDto
{
    /// <summary>
    /// Gets or sets the Event Topic allocated to action taken on the quest.
    /// This property is used to detatermine Event Topic on this quest.
    /// </summary>
    [Required]
    public string EventTopic { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the type associated with the request.
    /// This property is used to define or categorize the specific purpose or classification of the request.
    /// </summary>
    [Required]
    public string TaskName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the point allocated to action taken on the quest.
    /// This property is used to detatermine points valuation for each action performed on this quest.
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Point Per Action must be greater than 0.")]
    public int PointPerAction { get; set; }
    /// <summary>
    /// Gets or sets the maximum action required to complete this quest.
    /// This property is used to define the count of action required to complete the quest
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Maximum Action must be greater than 0.")]
    public int MaxAction { get; set; }
    /// <summary>
    /// Gets or sets the unique identifier associated with the user.
    /// This identifier is used to distinguish and authenticate the user within the system.
    /// </summary>
    [Required]
    public string UserIdentifier { get; set; } = string.Empty;
}