using Kanject.Core.Api.Abstractions.Models;
using Kanject.Core.ApiV2.Controller;
using Kanject.Identity.Abstractions.Security.SystemPermissions.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trifted.Points.Business.Services.UserQuest.Abstractions.Interfaces;
using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Models;

namespace Trifted.Points.Api.Controllers.User;

/// <summary>
/// Controller for managing a users's Wdrbe quests.
/// </summary>
/// <remarks>
/// The <c>UserQuestsController</c> provides endpoints to interact with the user's Wdrbe quests
/// </remarks>
[Route("api/user/wdrbe-quests")]
[Authorize]
[ApiController]
[Module("wdrbe-quests")]
public class UserQuestsController(IUserQuestManagerService userQuestManagerService) : BaseController
{
    /// <summary>
    /// Get quest of a user
    /// </summary>
    /// <returns></returns>
    [HttpGet()]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Response<GetWdrbeQuestTasksResonse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUsersQuestPointAsync()
    {
        var payload = await userQuestManagerService.GetUsersQuestPointAsync(CurrentUserId);

        return userQuestManagerService.HasError
            ? ApiErrorResponse(userQuestManagerService.Errors)
            : ApiResponse(payload);
    }
}