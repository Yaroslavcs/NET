using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LayeredArchitecture.Infrastructure.Identity;
using LayeredArchitecture.Infrastructure.Services;

namespace LayeredArchitecture.API.Controllers
{
    [ApiController]
    [Route("users")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IJwtTokenService jwtTokenService,
            ILogger<UsersController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtTokenService = jwtTokenService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = _userManager.Users.ToList();
                var userDtos = new List<object>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    userDtos.Add(new
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        EmailConfirmed = user.EmailConfirmed,
                        LockoutEnabled = user.LockoutEnabled,
                        LockoutEnd = user.LockoutEnd,
                        Roles = roles,
                        CreatedAt = user.CreatedAt,
                        UpdatedAt = user.UpdatedAt
                    });
                }

                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get users");
                return StatusCode(500, new { message = "Failed to get users" });
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetUser(string id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isAdmin = User.IsInRole("Admin");

                // Users can only view their own profile unless they're admin
                if (!isAdmin && currentUserId != id)
                {
                    return Forbid();
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var roles = await _userManager.GetRolesAsync(user);

                return Ok(new
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd = user.LockoutEnd,
                    Roles = roles,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user");
                return StatusCode(500, new { message = "Failed to get user" });
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isAdmin = User.IsInRole("Admin");

                // Users can only update their own profile unless they're admin
                if (!isAdmin && currentUserId != id)
                {
                    return Forbid();
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Update user properties
                user.FirstName = request.FirstName ?? user.FirstName;
                user.LastName = request.LastName ?? user.LastName;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(new { message = "Update failed", errors = result.Errors });
                }

                return Ok(new { message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update user");
                return StatusCode(500, new { message = "Failed to update user" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Revoke all refresh tokens before deletion
                await _jwtTokenService.RevokeAllRefreshTokensAsync(user);

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(new { message = "Delete failed", errors = result.Errors });
                }

                return Ok(new { message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete user");
                return StatusCode(500, new { message = "Failed to delete user" });
            }
        }

        [HttpGet("{id}/refreshtokens")]
        [Authorize]
        public async Task<IActionResult> GetUserRefreshTokens(string id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isAdmin = User.IsInRole("Admin");

                // Users can only view their own refresh tokens unless they're admin
                if (!isAdmin && currentUserId != id)
                {
                    return Forbid();
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var refreshTokens = await _jwtTokenService.GetUserRefreshTokensAsync(user);

                return Ok(refreshTokens);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user refresh tokens");
                return StatusCode(500, new { message = "Failed to get user refresh tokens" });
            }
        }

        [HttpGet("{id}/roles")]
        [Authorize]
        public async Task<IActionResult> GetUserRoles(string id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isAdmin = User.IsInRole("Admin");

                // Users can view their own roles, admins can view any user's roles
                if (!isAdmin && currentUserId != id)
                {
                    return Forbid();
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var roles = await _userManager.GetRolesAsync(user);

                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user roles");
                return StatusCode(500, new { message = "Failed to get user roles" });
            }
        }

        [HttpPost("{id}/roles")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddUserRole(string id, [FromBody] AddUserRoleRequest request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var roleExists = await _roleManager.RoleExistsAsync(request.Role);
                if (!roleExists)
                {
                    return BadRequest(new { message = "Role does not exist" });
                }

                var userInRole = await _userManager.IsInRoleAsync(user, request.Role);
                if (userInRole)
                {
                    return BadRequest(new { message = "User already has this role" });
                }

                var result = await _userManager.AddToRoleAsync(user, request.Role);
                if (!result.Succeeded)
                {
                    return BadRequest(new { message = "Failed to add role", errors = result.Errors });
                }

                return Ok(new { message = "Role added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add user role");
                return StatusCode(500, new { message = "Failed to add user role" });
            }
        }

        [HttpDelete("{id}/roles/{role}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveUserRole(string id, string role)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var userInRole = await _userManager.IsInRoleAsync(user, role);
                if (!userInRole)
                {
                    return BadRequest(new { message = "User does not have this role" });
                }

                // Prevent removing the last role
                var userRoles = await _userManager.GetRolesAsync(user);
                if (userRoles.Count <= 1)
                {
                    return BadRequest(new { message = "User must have at least one role" });
                }

                var result = await _userManager.RemoveFromRoleAsync(user, role);
                if (!result.Succeeded)
                {
                    return BadRequest(new { message = "Failed to remove role", errors = result.Errors });
                }

                return Ok(new { message = "Role removed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove user role");
                return StatusCode(500, new { message = "Failed to remove user role" });
            }
        }
    }
}