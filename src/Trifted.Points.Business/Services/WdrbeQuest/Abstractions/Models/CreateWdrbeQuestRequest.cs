using System.ComponentModel.DataAnnotations;
using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Dtos;
using Trifted.Points.Data.Enums;

namespace Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Models;

public class CreateWdrbeQuestRequest
{

    /// <summary>
    /// Gets or sets the type associated with the request.
    /// This property is used to define or categorize the specific purpose or classification of the request.
    /// </summary>
    [Required]
    public string QuestName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description of the quest
    /// This property is used to define or categorize the specific purpose or classification of the request.
    /// </summary>
    [Required]
    public string QuestDescription { get; set; } = string.Empty;

    /// <summary>
    /// This defines the list of tasks associated with the quest. Each task represents a specific action or objective that needs to be completed as part of the quest. 
    /// The Tasks property is used to organize and manage the various tasks that contribute to the overall completion of the quest.
    /// </summary>
    public List<QuestTaskDto> Tasks { get; set; } = [];

    /// <summary>
    /// Gets or sets the country.
    /// This property is used to define the country for the quest classification.
    /// </summary>
    public Guid? CountryId { get; set; }
    /// <summary>
    /// Gets or sets the status of quest.
    /// This property is used to define the status for the quest.
    /// </summary>
    public bool IsActive { get; set; }
    /// <summary>
    /// Gets or sets the Badge.
    /// This property is used to define the Badge for the quest classification.
    /// </summary>
    public Badge Badge { get; set; }
}