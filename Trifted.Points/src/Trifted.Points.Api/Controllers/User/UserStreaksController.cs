using Kanject.Core.Api.Abstractions.Models;
using Kanject.Core.ApiV2.Controller;
using Kanject.Identity.Abstractions.Security.SystemPermissions.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trifted.Points.Business.Services.UserQuest.Abstractions.Interfaces;
using Trifted.Points.Business.Services.UserStreak.Abstractions.Dtos;
using Trifted.Points.Business.Services.UserStreak.Abstractions.Interfaces;
using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Models;

namespace Trifted.Points.Api.Controllers.User;

/// <summary>
/// Controller for managing a users's streaks.
/// </summary>
/// <remarks>
/// The <c>UserStreaksController</c> provides endpoints to interact with the user's streaks.
/// </remarks>
[Route("api/user/streaks")]
[Authorize]
[ApiController]
[Module("streaks")]
public class UserStreaksController(IUserStreakManagerService userStreakManagerService) : BaseController
{
    /// <summary>
    /// Get streak of a user
    /// </summary>
    /// <returns></returns>
    [HttpGet()]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Response<UserStreakResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUsersQuestPointAsync()
    {
        var payload = await userStreakManagerService.GetUserCurrentStreak(CurrentUserId);

        return userStreakManagerService.HasError
            ? ApiErrorResponse(userStreakManagerService.Errors)
            : ApiResponse(payload);
    }
}