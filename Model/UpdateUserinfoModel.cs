namespace AzureAdUserwebAPI.Model
{
    public class UpdateUserinfoModel
    {
        public string? UserPrincipalName { get; set; }

        // New field for display name
        public string? DisplayName { get; set; }

        // Job info
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public string? CompanyName { get; set; }

        // Manager
        public string? ManagerUserPrincipalName { get; set; }

        // Parental controls
        public string? LegalAgeGroupClassification { get; set; } // Example: "MinorWithParentalConsent"

        // Usage location
        public string? UsageLocation { get; set; } // Example: "IN", "US", "DE"

        // Custom fields using built-in extension attributes
        public string ExtensionAttribute1 { get; set; }
        public string ExtensionAttribute2 { get; set; }
    }
}
