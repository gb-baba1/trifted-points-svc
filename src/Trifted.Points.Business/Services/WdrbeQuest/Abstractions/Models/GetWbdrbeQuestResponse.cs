
using Trifted.Points.Data.Enums;

namespace Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Models
{
    public class GetWbdrbeQuestResponse
    {
        /// <summary>
        /// Gets or sets the id associated with the request.
        /// This property is used to define or categorize the specific purpose or classification of the id.
        /// </summary>
        public string QuestId { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the type associated with the request.
        /// This property is used to define or categorize the specific purpose or classification of the request.
        /// </summary>
        public string QuestName { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the description associated with the request.
        /// This property is used to define or categorize the specific purpose or classification of the request.
        /// </summary>
        public string QuestDescription { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the Total Points.
        /// This property is used to define Total Points.
        /// </summary>
        public int TotalPoints { get; set; }
        /// <summary>
        ///  badge associated with this quest after completion
        /// </summary>
        public Badge Badge { get; set; }
        /// <summary>
        /// status of quest
        /// </summary>
        public bool IsActive { get; set; }
        /// <summary>
        /// the date record was last updated
        /// </summary>

        public string LastUpdatedOn { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the CountryId associated with the request.
        /// This property is used to define Country of the request.
        /// </summary>
        public Guid? CountryId { get; set; }
    }
}
