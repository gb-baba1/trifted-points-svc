using System.ComponentModel.DataAnnotations;
using Trifted.Points.Data.Dtos;
using Trifted.Points.Data.Enums;

namespace Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Dtos
{
    public class UpdateWdrbeQuestDto
    {
        [Required]
        public string QuestId { get; set; } = string.Empty;

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
        /// <summary>
        /// Gets or sets the tasks.
        /// This property is used to define the tasks for the quest classification.
        /// </summary>

        public List<UpdateQuestTaskDto> Tasks { get; set; } = []; 
    }
}
