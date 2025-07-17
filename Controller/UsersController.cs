using System.Reflection;
using AzureAdUserwebAPI.Model;
using AzureAdUserwebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureAdUserwebAPI.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly GraphUserService _graphUserService;

        public UsersController(GraphUserService graphUserService)
        {
            _graphUserService = graphUserService;
        }

        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] UserSignupModel model)
        {
            var success = await _graphUserService.CreateUserAsync(model);
            return Ok(new { isInserted = success });
            //return Ok($"Received id:");
        }

        

        [HttpPost("deluser")]
        public async Task<IActionResult> DelUser([FromBody] UpdatePasswordModel model)
        {
            var success = await _graphUserService.DeleteUserAsync(model.UserPrincipalName);
            return Ok(new { isUserDeleted = success });
        }


        [HttpPost("activeadrole")]
        public async Task<IActionResult> ActiveADRole([FromBody] ADRolesModel model)
        {
            var success = await _graphUserService.ActiveADRoleAsync(model.RoleName);
            return Ok(new { isRoleActivated = success });
        }

        [HttpPut("updUser")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserinfoModel model)
        {
            var success = await _graphUserService.UpdateUserInfoAsync(model);
            return Ok(new { isUserUpdated = success });
        }


       
    }
}
