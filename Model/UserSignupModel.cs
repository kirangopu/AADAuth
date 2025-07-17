namespace AzureAdUserwebAPI.Model
{
    public class UserSignupModel
    {
        public string DisplayName { get; set; }
        public string UserPrincipalName { get; set; }
        public string Password { get; set; }
        public string GroupName { get; set; } = "PocAADGrp";
        public string RoleName { get; set; }


        // Custom fields using built-in extension attributes
        public string ExtensionAttribute1 { get; set; }
        public string ExtensionAttribute2 { get; set; }
    }
}
