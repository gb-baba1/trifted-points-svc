using Trifted.Points.Data.Entities.WdrbeQuest;

namespace Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Models;

public class CreateWdrbeQuestResponse
{
    /// <summary>
    /// Gets or sets the type associated with the request.
    /// This property is used to define or categorize the specific purpose or classification of the request.
    /// </summary>
    public string QuestName { get; set; } = string.Empty;

    public static explicit operator CreateWdrbeQuestResponse?(WdrbeQuestEntity? source)
    {
        return source is null
            ? null
            : new CreateWdrbeQuestResponse { QuestName = source.Name };
    }
}