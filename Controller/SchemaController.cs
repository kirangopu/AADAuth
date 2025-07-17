using AzureAdUserwebAPI.Model;
using AzureAdUserwebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureAdUserwebAPI.Controller
{
    public class SchemaController : ControllerBase
    {
        private readonly GraphUserService _graphUserService;

        public SchemaController(GraphUserService graphUserService)
        {
            _graphUserService = graphUserService;
        }

        [HttpPost("regschema")]
        public async Task<IActionResult> RegisterSchema()
        {
            try
            {
                var result = await _graphUserService.RegisterSchemaAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPatch("{userId}/updschemafields")]
        public async Task<IActionResult> UpdateUserCustomFields(string userId, [FromBody] UserCustomFieldsRequestModel request)
        {
            try
            {
                await _graphUserService.UpdateUserCustomFieldsAsync(userId, request);
                return Ok(new { message = "Custom fields updated." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{userId}/getschema")]
        public async Task<IActionResult> GetUserCustomFields(string userId)
        {
            try
            {
                var data = await _graphUserService.GetUserCustomFieldsAsync(userId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
