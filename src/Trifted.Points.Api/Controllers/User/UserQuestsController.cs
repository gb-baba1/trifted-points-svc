using Kanject.Core.Api.Abstractions.Models;
using Kanject.Core.ApiV2.Controller;
using Kanject.Identity.Abstractions.Security.SystemPermissions.Attributes;
using Kanject.NotificationHub.Abstractions.TemplateEngine.Interfaces;
using Kanject.NotificationHub.Abstractions.TemplateEngine.Models.NotificationTemplates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Interfaces;
using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Models;

namespace Trifted.Identity.Api.Controllers.User;

/// <summary>
/// Controller for managing Wdrbe quests and event topics within the admin module.
/// </summary>
/// <remarks>
/// The <c>WdrbeQuestsController</c> provides endpoints to interact with the Wdrbe quests, including
/// retrieving available event topics and creating new quests.
/// </remarks>
[Route("api/user/wdrbe-quests")]
[Authorize]
[ApiController]
[Module("wdrbe-quests")]
public class UserQuestsController(
    IHubTemplateEngine hubTemplateEngine,
    IWdrbeQuestManagerService wdrbeQuestManagerService) : BaseController
{
    /// <summary>
    /// Get quest of a user
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Response<UserPointResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUserAccount([FromRoute] Guid id)
    {
        var payload = await wdrbeQuestManagerService.GetUsersQuestPointAsync(id);

        return wdrbeQuestManagerService.HasError
            ? ApiErrorResponse(hubTemplateEngine.Errors)
            : ApiResponse(payload);
    }
}