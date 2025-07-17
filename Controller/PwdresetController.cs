using System.Reflection;
using AzureAdUserwebAPI.Model;
using AzureAdUserwebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureAdUserwebAPI.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class PwdresetController : ControllerBase
    {
        private readonly GraphUserService _graphUserService;

        public PwdresetController(GraphUserService graphUserService)
        {
            _graphUserService = graphUserService;
        }

        [HttpPost("updpwd")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordModel model)
        {
            var success = await _graphUserService.UpdatePasswordByEmailAsync(model.UserPrincipalName, model.NewPassword, true);
            return Ok(new { isPasswordUpdated = success });
        }

        [HttpPost("resetpwd")]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordResetRequest request)
        {
            var success = await _graphUserService.ResetPassword(request);
            return Ok(success);
        }
    }
}
