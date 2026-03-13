using Kanject.Core.Api.Abstractions.Models;
using Kanject.Core.ApiV2.Controller;
using Kanject.Identity.Abstractions.Security.SystemPermissions.Attributes;
using Kanject.NotificationHub.Abstractions.TemplateEngine.Models.NotificationTemplates;
using Kanject.ServerlessEventHub.Provider.AwsSns.Abstractions.DataStore;
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
public class WdrbeQuestsController(IServerlessEventHubDataStore serverlessEventHubDataStore, IWdrbeQuestManagerService wdrbeQuestManagerService) : BaseController
{

    /// <summary>
    /// Retrieves a list of available event topics for Wdrbe quests.
    /// </summary>
    /// <returns>A response containing a collection of event topics for the Wdrbe quests.</returns>
    [HttpGet("topics")]
    [ProducesResponseType(typeof(Response<IEnumerable<GetEventTopicsLovResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEventTopicsLovAsync()
    {
        var payload = await serverlessEventHubDataStore.GetServiceTopicsWithParametersAsync();

        return serverlessEventHubDataStore.HasError
            ? ApiErrorResponse(serverlessEventHubDataStore.Errors)
            : ApiResponse(payload);
    }

    /// <summary>
    /// Creates a new wdrbe quest.
    /// </summary>
    /// <param name="request">The request containing the details of the Wdrbe quest to be created.</param>
    /// <returns>A response containing the details of the created Wdrbe quest.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Response<CreateWdrbeQuestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateWdrbeQuestAsync(CreateWdrbeQuestRequest? request)
    {
        var payload = await wdrbeQuestManagerService.CreateWdrbeQuestAsync(request, CurrentUserId);

        return wdrbeQuestManagerService.HasError
            ? ApiErrorResponse(wdrbeQuestManagerService.Errors)
            : ApiResponse(payload);
    }
    /// <summary>
    /// Get wdrbe quests.
    /// </summary>
    /// <returns>A response containing the details of the created Wdrbe quest.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(Response<List<WdrbeQuestResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> WdrbeQuestsAsync()
    {
        var payload = await wdrbeQuestManagerService.GetWdrbeQuestsAsync();

        return wdrbeQuestManagerService.HasError
            ? ApiErrorResponse(wdrbeQuestManagerService.Errors)
            : ApiResponse(payload);
    }

    /// <summary>
    /// Get task by quest id.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="questId"></param>
    /// <returns></returns>
    [HttpGet("{questId}/task/{taskId}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Response<GetWdrbeQuestTasksResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetWdrbeQuestTaskByIdAsync([FromRoute] string taskId, [FromRoute] string questId)
    {
        var payload = await wdrbeQuestManagerService.WdrbeQuestTaskByIdAsync(taskId, questId);

        return wdrbeQuestManagerService.HasError
            ? ApiErrorResponse(wdrbeQuestManagerService.Errors)
            : ApiResponse(payload);
    }
    /// <summary>
    /// Get quest by id.
    /// </summary>
    /// <param name="questId"></param>
    /// <returns></returns>
    [HttpGet("{questId}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Response<GetWdrbeQuestTasksResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetQuestByIdAsync([FromRoute] string questId)
    {
        var payload = await wdrbeQuestManagerService.GetQuestByIdAsync(questId);

        return wdrbeQuestManagerService.HasError
            ? ApiErrorResponse(wdrbeQuestManagerService.Errors)
            : ApiResponse(payload);
    }
    /// <summary>
    /// Delete quest by id.
    /// </summary>
    /// <param name="questId"></param>
    /// <returns></returns>
    [HttpDelete("{questId}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Response<GetWbdrbeQuestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveWdrbeQuestByIdAsync([FromRoute] string questId)
    {
        var payload = await wdrbeQuestManagerService.RemoveWdrbeQuestByIdAsync(questId);

        return wdrbeQuestManagerService.HasError
            ? ApiErrorResponse(wdrbeQuestManagerService.Errors)
            : ApiResponse(payload);
    }

    /// <summary>
    /// Update wdrbe quest.
    /// </summary>
    /// <param name="request">The request containing the details of the Wdrbe quest to be updated.</param>
    /// <returns>A response containing the details of the updated Wdrbe quest.</returns>
    [HttpPut]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Response<GetWbdrbeQuestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateWdrbeQuestByIdAsync([FromBody] UpdateWdrbeQuestRequest request)
    {
        var payload = await wdrbeQuestManagerService.UpdateWdrbeQuestByIdAsync(request);

        return wdrbeQuestManagerService.HasError
            ? ApiErrorResponse(wdrbeQuestManagerService.Errors)
            : ApiResponse(payload);
    }
}