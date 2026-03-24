using System.Runtime.Serialization;

namespace Trifted.Points.Data.Enums
{
    public enum AppFeature
    {
        [EnumMember(Value = "none")] None = 0,
        [EnumMember(Value = "home")] Home = 1,
        [EnumMember(Value = "wardrobe")] Wardrobe = 2,
        [EnumMember(Value = "upload item")] UploadItem = 3,
        [EnumMember(Value = "create outfit")] CreateOutfit = 4,
        [EnumMember(Value = "plan outfit")] PlanOutfit = 5,
        [EnumMember(Value = "preloved")] Preloved = 6,
        [EnumMember(Value = "create post")] CreatePost = 7,
        [EnumMember(Value = "search")] Search = 8,
        [EnumMember(Value = "profile")] Profile = 9,
        [EnumMember(Value = "ai stylist")] AiStylist = 10
    }
}