using Kanject.Core.Api.Abstractions.Models;
using Kanject.Core.ApiV2.Controller;
using Kanject.Identity.Abstractions.Security.SystemPermissions.Attributes;
using Kanject.NotificationHub.Abstractions.TemplateEngine.Interfaces;
using Kanject.NotificationHub.Abstractions.TemplateEngine.Models.NotificationTemplates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Interfaces;
using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Models;


namespace Trifted.Points.Api.Controllers.Admin;

/// <summary>
/// Controller for managing Wdrbe quests and event topics within the admin module.
/// </summary>
/// <remarks>
/// The <c>WdrbeQuestsController</c> provides endpoints to interact with the Wdrbe quests, including
/// retrieving available event topics and creating new quests.
/// </remarks>
[Route("api/admin/wdrbe-quests")]
[Authorize]
[ApiController]
[Module("wdrbe-quests")]
public class WdrbeQuestsController(
    IHubTemplateEngine hubTemplateEngine,
    IWdrbeQuestManagerService wdrbeQuestManagerService) : BaseController
{
    /// <summary>
    /// Retrieves a list of available event topics for Wdrbe quests.
    /// </summary>
    /// <returns>A response containing a collection of event topics for the Wdrbe quests.</returns>
    [HttpGet("topics")]
    [RequiresPermission(name: "view-topics", description: "View available event topics")]
    [ProducesResponseType(typeof(Response<IEnumerable<GetEventTopicsLovResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEventTopicsLovAsync()
    {
        var payload = await hubTemplateEngine.GetEventTopicsLovAsync();

        return hubTemplateEngine.HasError
            ? ApiErrorResponse(hubTemplateEngine.Errors)
            : ApiResponse(payload);
    }

    /// <summary>
    /// Creates a new Wdrbe quest.
    /// </summary>
    /// <param name="request">The request containing the details of the Wdrbe quest to be created.</param>
    /// <returns>A response containing the details of the created Wdrbe quest.</returns>
    [HttpPost]
    [RequiresPermission(name: "create-quest", description: "Create wdrbe quest")]
    [ProducesResponseType(typeof(Response<CreateWdrbeQuestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateWdrbeQuestAsync(CreateWdrbeQuestRequest? request)
    {
        var payload = await wdrbeQuestManagerService.CreateWdrbeQuestAsync(request, CurrentUserId);

        return wdrbeQuestManagerService.HasError
            ? ApiErrorResponse(hubTemplateEngine.Errors)
            : ApiResponse(payload);
    }
    /// <summary>
    /// Get Wdrbe quests.
    /// </summary>
    /// <returns>A response containing the details of the created Wdrbe quest.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(Response<List<WdrbeQuestResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> WdrbeQuestsAsync()
    {
        var payload = await wdrbeQuestManagerService.GetWdrbeQuestsAsync();

        return wdrbeQuestManagerService.HasError
            ? ApiErrorResponse(hubTemplateEngine.Errors)
            : ApiResponse(payload);
    }


    /// <summary>
    /// Get quests  by the questId
    /// </summary>
    /// <param name="questId"></param>
    /// <returns></returns>
    [HttpGet("{questId")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Response<WdrbeQuestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUsersQuestPointAsync([FromRoute] string questId)
    {
        var payload = await wdrbeQuestManagerService.GetQuestByIdAsync(questId);

        return wdrbeQuestManagerService.HasError
            ? ApiErrorResponse(hubTemplateEngine.Errors)
            : ApiResponse(payload);
    }
}