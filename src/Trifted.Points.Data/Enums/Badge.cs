using System.Runtime.Serialization;

namespace Trifted.Points.Data.Enums
{
    public enum Badge
    {
        [EnumMember(Value = "no badge")] NoBadge = 1,
        [EnumMember(Value = "starter badge")] Starter = 2,
        [EnumMember(Value = "stylist badge")] Stylist = 3,
    }
}
