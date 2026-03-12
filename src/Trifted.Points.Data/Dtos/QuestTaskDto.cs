namespace Trifted.Points.Data.Dtos;

public class QuestTaskDto
{
    /// <summary>
    /// Gets or sets the Id associated with the request.
    /// This property is used to define or categorize the specific purpose or classification of the Id.
    /// </summary>
    public string TaskId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the task associated with the request.
    /// This property is used to define or categorize the specific purpose or classification of the task.
    /// </summary>
    public string TaskName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the CountryId associated with the request.
    /// This property is used to define Country of the request.
    /// </summary>

    public Guid? CountryId { get; set; }

    /// <summary>
    /// Gets or sets the PointPerAction associated with the request.
    /// This property is used to define Point Per Action of the request.
    /// </summary>
    public int PointPerAction { get; set; }

    /// <summary>
    /// Gets or sets the MaxAction associated with the request.
    /// This property is used to define Maximum Action of the request.
    /// </summary>
    public int MaxAction { get; set; }

    /// <summary>
    /// Gets or sets the Points associated with the request.
    /// This property is used to define Points of the request.
    /// </summary>
    public int Points { get; set; }

    /// <summary>
    /// Gets or sets the LastUpdatedOn associated with the request.
    /// This property is used to define LastUpdatedOn of the request.
    /// </summary>
    public string LastUpdatedOn { get; set; } = string.Empty;
}