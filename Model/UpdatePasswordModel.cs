namespace AzureAdUserwebAPI.Model
{
    public class UpdatePasswordModel
    {
        public string UserPrincipalName { get; set; } // e.g., user@yourtenant.onmicrosoft.com
        public string NewPassword { get; set; }
    }
}
