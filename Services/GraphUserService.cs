using AzureAdUserwebAPI.Model;
using Microsoft.Graph.Models;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Azure.Identity;
using Azure.Core;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Microsoft.Graph.Models.TermStore;
using Group = Microsoft.Graph.Models.Group;
using System.Reflection;
using Microsoft.Graph.Models.ExternalConnectors;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace AzureAdUserwebAPI.Services
{
    public class GraphUserService
    {
        private readonly GraphServiceClient _graphClient;
        //private readonly ILogger<AzureADPasswordService> _logger;
        public GraphUserService(IConfiguration configuration)
        {
            var clientId = configuration["AzureAd:ClientId"];
            var tenantId = configuration["AzureAd:TenantId"];
            var clientSecret = configuration["AzureAd:ClientSecret"];

            var clientSecretCredential = new ClientSecretCredential(
                tenantId, clientId, clientSecret);

            _graphClient = new GraphServiceClient(clientSecretCredential,
               new[] { "https://graph.microsoft.com/.default" });

            // getToken(clientSecretCredential);



        }


        // Activate a role in Azure AD
        public async Task<DirectoryRole?> ActiveADRoleAsync(string roleName)
        {
            // Step 1: Check if the role is already active
            var activeRoles = await _graphClient.DirectoryRoles.GetAsync();
            var existingRole = activeRoles?.Value?.FirstOrDefault(r => r.DisplayName == roleName);

            if (existingRole != null)
                return existingRole;

            // Step 2: If not active, find template
            var templates = await _graphClient.DirectoryRoleTemplates.GetAsync();
            var template = templates?.Value?.FirstOrDefault(t => t.DisplayName == roleName);

            if (template == null)
                throw new Exception($"Role template '{roleName}' not found.");

            // Step 3: Activate the role
            var newRole = await _graphClient.DirectoryRoles.PostAsync(new DirectoryRole
            {
                RoleTemplateId = template.Id
            });

            return newRole;
        }
        public async Task<String> getToken(ClientSecretCredential clientSecretCredential)
        {
            //        var token = clientSecretCredential
            //.GetTokenAsync(new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" }));

            //        string strtoken = token.Token;

            TokenRequestContext tokenRequestContext = new TokenRequestContext(
            new[] { "https://graph.microsoft.com/.default" });

            AccessToken token = await clientSecretCredential.GetTokenAsync(tokenRequestContext);

            //Console.WriteLine("Access Token:");
            //Console.WriteLine(token.Token); // 

            string strtoken = token.Token;
            return strtoken;



        }
        public async Task<string> CreateGroupAsync(GraphServiceClient graphClient, string groupName)
        {
            var newGroup = new Group
            {
                DisplayName = groupName,
                MailEnabled = false,
                MailNickname = groupName.Replace(" ", "").ToLower(),
                SecurityEnabled = true,
                GroupTypes = new List<string>() // leave empty for security group
            };

            var createdGroup = await graphClient.Groups.PostAsync(newGroup);
            return createdGroup?.Id;
        }
        /*//Group.ReadWrite.All
        //OUTPUT CODE
        //{
        //  "displayName": "TestGroup",
        //  "mailEnabled": false,
        //  "mailNickname": "testgroup",
        //  "securityEnabled": true
        //}*/





        // Create a new user in Azure AD
        public async Task<bool> CreateUserAsync(UserSignupModel model)
        {
            try
            {
                bool userExists = false;

                try
                {
                    var existingUser = await _graphClient.Users[model.UserPrincipalName].GetAsync();
                    userExists = existingUser != null;
                }
                catch (Microsoft.Graph.Models.ODataErrors.ODataError ex)
                {
                    if (ex.ResponseStatusCode != 404)
                        throw new Exception($"User '{model.UserPrincipalName}' already existed in Azure AD.");// rethrow if it's a real error
                }

                if (userExists)
                {
                    return false;
                }

                var user = new User
                {
                    AccountEnabled = true,
                    DisplayName = model.DisplayName,
                    MailNickname = model.UserPrincipalName.Split('@')[0],
                    UserPrincipalName = model.UserPrincipalName,
                    PasswordProfile = new PasswordProfile
                    {
                        ForceChangePasswordNextSignIn = false,
                        Password = model.Password
                    },
                    OnPremisesExtensionAttributes = new OnPremisesExtensionAttributes
                    {
                        ExtensionAttribute1 = model.ExtensionAttribute1,
                        ExtensionAttribute2 = model.ExtensionAttribute2
                    }
                };

                // Fix: Use the PostAsync method directly on the Users property of _graphClient
                var createdUser = await _graphClient.Users.PostAsync(user);



                // 2. Add to group
                if (!string.IsNullOrEmpty(model.GroupName))
                {
                    //var groups = (await _graphClient.Groups
                    //    .GetAsync(config=>config.QueryParameters.Filter= $"displayName eq '{model.GroupName}'"));
                    //var group = groups?.Value?.FirstOrDefault();

                    var allGroups = await _graphClient.Groups.GetAsync();
                    var group = allGroups?.Value?.FirstOrDefault(g => g.DisplayName == model.GroupName);


                    if (group != null)
                    {
                        //await _graphClient.Groups[group.Id].Members.References.Request()
                        //    .AddAsync(new DirectoryObject { Id = createdUser.Id });


                        var reference = new ReferenceCreate
                        {
                            OdataId = $"https://graph.microsoft.com/v1.0/directoryObjects/{createdUser.Id}"
                        };

                        var directoryObject = new DirectoryObject { Id = createdUser.Id };
                        await _graphClient.Groups[group.Id].Members.Ref.PostAsync(reference);
                    }
                }

                // 3. Assign role
                if (!string.IsNullOrEmpty(model.RoleName))
                {
                    //var roles = await _graphClient.DirectoryRoles.GetAsync(config =>
                    //{
                    //    config.QueryParameters.Filter = $"displayName eq '{model.RoleName}'";
                    //});
                    //var role = roles?.Value?.FirstOrDefault();

                    var allRoles = await _graphClient.DirectoryRoles.GetAsync();
                    var role = allRoles?.Value?.FirstOrDefault(r => r.DisplayName == model.RoleName);

                    // Activate if not available
                    if (role == null)
                    {
                        var alltemplates = await _graphClient.DirectoryRoleTemplates.GetAsync();
                        var template = allRoles?.Value?.FirstOrDefault(r => r.DisplayName == model.RoleName);


                        //var templates = await _graphClient.DirectoryRoleTemplates.GetAsync(config =>
                        //{
                        //    config.QueryParameters.Filter = $"displayName eq '{model.RoleName}'";
                        //});
                        //var template = templates?.Value?.FirstOrDefault();

                        if (template != null)
                        {
                            await _graphClient.DirectoryRoles.PostAsync(new DirectoryRole
                            {
                                RoleTemplateId = template.Id
                            });

                            // Retry fetch
                            //allRoles = await _graphClient.DirectoryRoles.GetAsync(config =>
                            //{
                            //    config.QueryParameters.Filter = $"displayName eq '{model.RoleName}'";
                            //});
                            //role = allRoles?.Value?.FirstOrDefault();

                            allRoles = await _graphClient.DirectoryRoles.GetAsync();
                            role = allRoles?.Value?.FirstOrDefault(r => r.DisplayName == model.RoleName);


                        }
                    }

                    if (role != null)
                    {
                        var reference = new ReferenceCreate
                        {
                            OdataId = $"https://graph.microsoft.com/v1.0/directoryObjects/{createdUser.Id}"
                        };

                        var directoryObject = new DirectoryObject { Id = createdUser.Id };
                        await _graphClient.DirectoryRoles[role.Id].Members.Ref.PostAsync(reference);
                    }

                }


                /*
                // 3. Assign role
                if (!string.IsNullOrEmpty(model.RoleName))
                {
                    var roles = await _graphClient.DirectoryRoles
                        .Request()
                        .Filter($"displayName eq '{model.RoleName}'")
                        .GetAsync();

                    var role = roles.FirstOrDefault();

                    // Activate if not available
                    if (role == null)
                    {
                        var template = await _graphClient.DirectoryRoleTemplates
                            .Request()
                            .Filter($"displayName eq '{model.RoleName}'")
                            .GetAsync();

                        if (template.FirstOrDefault() != null)
                        {
                            await _graphClient.DirectoryRoles.Request()
                                .AddAsync(new DirectoryRole { RoleTemplateId = template.First().Id });

                            role = (await _graphClient.DirectoryRoles
                                .Request()
                                .Filter($"displayName eq '{model.RoleName}'")
                                .GetAsync())
                                .FirstOrDefault();
                        }
                    }
                    if (role != null)
                    {
                        await _graphClient.DirectoryRoles[role.Id].Members.References
                            .Request()
                            .AddAsync(new DirectoryObject { Id = createdUser.Id });
                    }
                }
                */

                return true;
            }
            catch (Exception ex)
            {
                // You can log ex.Message here
                return false;
            }
        }
        public async Task<bool> DeleteUserAsync(string userPrincipalName)
        {
            try
            {
                await _graphClient.Users[userPrincipalName].DeleteAsync();
                return true;
            }
            catch (Exception ex)
            {
                // You can log ex.Message here
                return false;
            }
        }
        public async Task<bool> UpdateUserInfoAsync(UpdateUserinfoModel request)
        {
            try
            {

                bool userExists = false;

                try
                {
                    var existingUser = await _graphClient.Users[request.UserPrincipalName].GetAsync();
                    userExists = existingUser != null;
                }
                catch (Microsoft.Graph.Models.ODataErrors.ODataError ex)
                {
                    if (ex.ResponseStatusCode == 404)
                        throw new Exception($"User '{request.UserPrincipalName}' not found in Azure AD.");// rethrow if it's a real error
                }

                //if (userExists)
                //{
                //    return false;
                //}


                // Step 0: Check if user exists
                // var user = await _graphClient.Users[request.UserPrincipalName].GetAsync();

                //if (user == null)
                //  throw new Exception($"User '{request.UserPrincipalName}' not found in Azure AD.");

                // Step 1: Update basic user fields
                var userUpdate = new User
                {
                    DisplayName = request.DisplayName,
                    JobTitle = request.JobTitle,
                    Department = request.Department,
                    CompanyName = request.CompanyName,
                    LegalAgeGroupClassification = request.LegalAgeGroupClassification,
                    UsageLocation = request.UsageLocation,
                    OnPremisesExtensionAttributes = new OnPremisesExtensionAttributes
                    {
                        ExtensionAttribute1 = request.ExtensionAttribute1,
                        ExtensionAttribute2 = request.ExtensionAttribute2
                    }
                };

                await _graphClient.Users[request.UserPrincipalName]
                    .PatchAsync(userUpdate);

                // Step 2: Set manager (if provided)
                if (!string.IsNullOrWhiteSpace(request.ManagerUserPrincipalName))
                {
                    var managerUser = await _graphClient.Users[request.ManagerUserPrincipalName].GetAsync();
                    if (managerUser == null)
                        throw new Exception($"Manager '{request.ManagerUserPrincipalName}' not found.");

                    var managerReference = new ReferenceUpdate
                    {
                        OdataId = $"https://graph.microsoft.com/v1.0/users/{managerUser.Id}"
                    };

                    await _graphClient.Users[request.UserPrincipalName].Manager.Ref
                        .PutAsync(managerReference);
                }
                return true;
            }
            catch (Exception ex)
            {
                // You can log ex.Message here
                return false;
            }
        }
        /***
         * {
              "userPrincipalName": "john@yourtenant.onmicrosoft.com",
              "jobTitle": "Senior Engineer",
              "department": "Engineering",
              "companyName": "TechCorp",
              "managerUserPrincipalName": "manager@yourtenant.onmicrosoft.com",
              "legalAgeGroupClassification": "MinorWithParentalConsent",
              "usageLocation": "IN"
            }
         * 
         * ***/




        // Schema Extension for custom user fields
        private const string SchemaId = "ext5x9ir5we_extkrianthivardhan1214";//"extkrianthivardhan1214"; // Must be globally unique
        public async Task<string> RegisterSchemaAsync()
        {
            var schema = new SchemaExtension
            {
                Id = SchemaId,
                Description = "Custom fields for user profile",
                TargetTypes = new List<string> { "User" },
                Properties = new List<ExtensionSchemaProperty>
            {
                new ExtensionSchemaProperty { Name = "employeeCode", Type = "String" },
                new ExtensionSchemaProperty { Name = "region", Type = "String" },
                new ExtensionSchemaProperty { Name = "isVip", Type = "Boolean" }
            }
               // Status = "Available"
            };

            var result = await _graphClient.SchemaExtensions.PostAsync(schema);
            return $"Schema registered: {result.Id}"; //ext5x9ir5we_extkrianthivardhan1214
        }
        public async Task UpdateUserCustomFieldsAsync(string userId, UserCustomFieldsRequestModel request)
        {
            var userUpdate = new User
            {
                AdditionalData = new Dictionary<string, object>
            {
                { $"{SchemaId}_employeeCode", request.EmployeeCode },
                { $"{SchemaId}_region", request.Region },
                { $"{SchemaId}_isVip", request.IsVip }
            }
            };

            await _graphClient.Users[userId].PatchAsync(userUpdate);
        }
        public async Task<Dictionary<string, object>> GetUserCustomFieldsAsync(string userId)
        {
            var user = await _graphClient.Users[userId]
                .GetAsync(config =>
                {
                    config.QueryParameters.Select = new[] {
                    "id",
                    "displayName",
                    $"{SchemaId}_employeeCode",
                    $"{SchemaId}_region",
                    $"{SchemaId}_isVip"
                    };
                });

            var data = user?.AdditionalData;

            if (data != null)
            {
                data.TryGetValue($"{SchemaId}_employeeCode", out var employeeCode);
                data.TryGetValue($"{SchemaId}_region", out var region);
                data.TryGetValue($"{SchemaId}_isVip", out var isVip);

                return new Dictionary<string, object>
    {
        { "employeeCode", employeeCode },
        { "region", region },
        { "isVip", isVip }
    };
            }
            else
            {
                throw new Exception("No extension data found.");
            }

            return (Dictionary<string, object>)data;
        }




        // Reset Password
        /* OLD CODE
        //public async Task<bool> ResetPassword(UpdatePasswordModel model)
        //{
        //    try
        //    {
        //        var user = new User
        //        {
        //            PasswordProfile = new PasswordProfile
        //            {
        //                Password = model.NewPassword,
        //                ForceChangePasswordNextSignIn = false

        //            }
        //        };

        //        // Fix: Use the UpdateAsync method directly on the Users property of _graphClient
        //        await _graphClient.Users[model.UserPrincipalName]
        //            .PatchAsync(user); // Replace 'Request().UpdateAsync' with 'PatchAsync'
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        // You can log ex.Message here
        //        return false;
        //    }
        //}
        */
        public async Task<PasswordUpdateResult> UpdatePasswordByEmailAsync(string email, string newPassword, bool forceChangePasswordNextSignIn = false)
        {
            try
            {
                // _logger.LogInformation($"Attempting to update password for user by email: {email}");

                // Find user by email first
                var users = await _graphClient.Users.GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Filter = $"mail eq '{email}' or userPrincipalName eq '{email}'";
                });

                if (users?.Value?.Count == 0)
                {
                    //_logger.LogWarning($"User not found with email: {email}");
                    return new PasswordUpdateResult
                    {
                        Success = false,
                        ErrorMessage = "User not found with the provided email address"
                    };
                }

                var user = users.Value[0];
                return await UpdatePasswordAsync(user.Id, newPassword, forceChangePasswordNextSignIn);
            }
            catch (ServiceException ex)
            {
                //_logger.LogError(ex, $"Error finding user by email {email}: {ex.Error?.Code} - {ex.Error?.Message}");
                return new PasswordUpdateResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to find user: {ex.Message}",
                    //ErrorCode = ex.Code
                };
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, $"Unexpected error updating password for user by email {email}");
                return new PasswordUpdateResult
                {
                    Success = false,
                    ErrorMessage = "An unexpected error occurred while updating the password"
                };
            }
        }
        private PasswordValidationResult ValidatePasswordStrength(string password)
        {
            var result = new PasswordValidationResult { IsValid = true, Errors = new List<string>() };

            if (string.IsNullOrEmpty(password))
            {
                result.IsValid = false;
                result.Errors.Add("Password cannot be empty");
                return result;
            }

            if (password.Length < 8)
            {
                result.IsValid = false;
                result.Errors.Add("Password must be at least 8 characters long");
            }

            if (password.Length > 128)
            {
                result.IsValid = false;
                result.Errors.Add("Password cannot exceed 128 characters");
            }

            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one uppercase letter");
            }

            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one lowercase letter");
            }

            if (!Regex.IsMatch(password, @"[0-9]"))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one number");
            }

            if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one special character");
            }

            return result;
        }
        public async Task<PasswordUpdateResult> UpdatePasswordAsync(string userId, string newPassword, bool forceChangePasswordNextSignIn = false)
        {
            try
            {
                //_logger.LogInformation($"Attempting to update password for user: {userId}");

                // Validate password strength
                var passwordValidation = ValidatePasswordStrength(newPassword);
                if (!passwordValidation.IsValid)
                {
                    // _logger.LogWarning($"Password validation failed for user {userId}: {string.Join(", ", passwordValidation.Errors)}");
                    return new PasswordUpdateResult
                    {
                        Success = false,
                        ErrorMessage = $"Password validation failed: {string.Join(", ", passwordValidation.Errors)}"
                    };
                }

                var user = new User
                {
                    PasswordProfile = new PasswordProfile
                    {
                        Password = newPassword,
                        ForceChangePasswordNextSignIn = forceChangePasswordNextSignIn
                    }
                };

                await _graphClient.Users[userId].PatchAsync(user);

                //_logger.LogInformation($"Successfully updated password for user: {userId}");
                return new PasswordUpdateResult
                {
                    Success = true,
                    Message = "Password updated successfully"
                };
            }
            catch (ServiceException ex)
            {
                //_logger.LogError(ex, $"Error updating password for user {userId}: {ex.Error?.Code} - {ex.Error?.Message}");
                return new PasswordUpdateResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to update password: {ex.Message}",
                    // ErrorCode = ex.Error?.Code
                };
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, $"Unexpected error updating password for user {userId}");
                return new PasswordUpdateResult
                {
                    Success = false,
                    ErrorMessage = "An unexpected error occurred while updating the password"
                };
            }
        }

        public async Task<string> ResetPassword(PasswordResetRequest request)
        {
            try
            {
                string flag = "";
                // Validate input
                //if (string.IsNullOrEmpty(request.UserPrincipalName) && string.IsNullOrEmpty(request.UserId))
                //{
                //    return BadRequest("Either UserPrincipalName or UserId must be provided");
                //}

                // Get user
                User user = null;
                if (!string.IsNullOrEmpty(request.UserId))
                {
                    user = await _graphClient.Users[request.UserId].GetAsync();
                }
                else if (!string.IsNullOrEmpty(request.UserPrincipalName))
                {
                    var users = await _graphClient.Users.GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Filter = $"userPrincipalName eq '{request.UserPrincipalName}'";
                    });
                    user = users?.Value?.FirstOrDefault();
                }

                if (user == null)
                {
                    return flag= "User not found"; // NotFound("User not found");
                }

                // Generate new password
                string newPassword = GenerateSecurePassword();

                // Reset password
                var passwordProfile = new PasswordProfile
                {
                    Password = newPassword,
                    ForceChangePasswordNextSignIn = request.ForceChangePasswordNextSignIn ?? true
                };

                var userUpdate = new User
                {
                    PasswordProfile = passwordProfile
                };

                await _graphClient.Users[user.Id].PatchAsync(userUpdate);
                return "Password reset successfully";
                //_logger.LogInformation($"Password reset successful for user: {user.UserPrincipalName}");

                //return Ok(new PasswordResetResponse
                //{
                //    Success = true,
                //    Message = "Password reset successfully",
                //    UserId = user.Id,
                //    UserPrincipalName = user.UserPrincipalName,
                //    NewPassword = newPassword,
                //    ForceChangePasswordNextSignIn = request.ForceChangePasswordNextSignIn ?? true
                //});
            }
            catch (ServiceException ex)
            {
                //_logger.LogError(ex, "Graph API error during password reset");
                return "Graph API error during password reset";// StatusCode(500, new { error = "Graph API error", details = ex.Error.Message });
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Unexpected error during password reset");
                return "Internal server error"; // StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }
        private string GenerateSecurePassword()
        {
            const int length = 12;
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string symbols = "!@#$%^&*";

            var random = new Random();
            var password = new StringBuilder();

            // Ensure at least one character from each category
            password.Append(uppercase[random.Next(uppercase.Length)]);
            password.Append(lowercase[random.Next(lowercase.Length)]);
            password.Append(digits[random.Next(digits.Length)]);
            password.Append(symbols[random.Next(symbols.Length)]);

            // Fill remaining characters
            string allChars = uppercase + lowercase + digits + symbols;
            for (int i = 4; i < length; i++)
            {
                password.Append(allChars[random.Next(allChars.Length)]);
            }

            // Shuffle the password
            var shuffled = password.ToString().ToCharArray();
            for (int i = shuffled.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            return new string(shuffled);
        }
    }
}
public class PasswordResetRequest
{
    public string? UserPrincipalName { get; set; }
    public string? UserId { get; set; }
    public bool? ForceChangePasswordNextSignIn { get; set; }
}
