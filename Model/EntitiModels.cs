using System.ComponentModel.DataAnnotations;

namespace AzureAdUserwebAPI.Model
{
    public class EntitiModels
    {
    }
    // Result classes
    public class PasswordUpdateResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }
    }

    public class PasswordResetResult : PasswordUpdateResult
    {
        public string NewPassword { get; set; }
    }

    public class PasswordValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; }
    }

    // Request DTOs
    public class UpdatePasswordRequest
    {
        [Required]
        public string NewPassword { get; set; }

        public bool ForceChangePasswordNextSignIn { get; set; } = false;
    }

}
