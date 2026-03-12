namespace Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Constants;

public readonly struct WdrbeQuestManagerMessages
{
    public static string ModelIsRequired => "Model is required";
    public static string QuestHasBeenCreated => "Quest already exist";

    public static string SystemCouldNotCreateQuest =>
        "System could not create quest at this time, Please try again or kindly contact customer support for further assistance";

    public static string EventTopicNotFound => "Event topic not found";
    public static string EventTopicAlreadyCreated => "Event topic already created for a quest";
    public static string QuestIdIsRequired => "Quest id is required";
    public static string InvalidQuestId => "Invalid quest id";
    public static string InvalidTaskId => "Invalid task id";
    public static string QuestNotFound => "Quest not found";
    public static string TaskNotFound => "Task not found";

    public static string SystemCouldNotGetQuest =>
        "System could not get quest at this time, Please try again or kindly contact customer support for further assistance";

    public static string UserIdentifierNotFound => "User identifier not found. Ensure you provided a valid user identifier from the {{event-topic}} payload";
}