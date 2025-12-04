using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LayeredArchitecture.Infrastructure.DTOs;
using LayeredArchitecture.Infrastructure.Identity;
using LayeredArchitecture.Infrastructure.Services;

namespace LayeredArchitecture.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IJwtTokenService jwtTokenService,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtTokenService = jwtTokenService;
            _logger = logger;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return BadRequest(new { message = "User with this email already exists" });
                }

                var existingUserName = await _userManager.FindByNameAsync(request.UserName);
                if (existingUserName != null)
                {
                    return BadRequest(new { message = "User with this username already exists" });
                }

                // Create new user
                var user = new ApplicationUser
                {
                    UserName = request.UserName,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    EmailConfirmed = false // Email needs to be verified
                };

                var result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    return BadRequest(new { message = "Registration failed", errors = result.Errors });
                }

                // Assign default "User" role
                await _userManager.AddToRoleAsync(user, "User");

                // Generate email verification token
                var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                
                // TODO: Send verification email (mock for now)
                _logger.LogInformation($"Email verification token for {user.Email}: {emailToken}");

                return Ok(new { message = "Registration successful. Please check your email for verification." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed");
                return StatusCode(500, new { message = "Registration failed" });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                // Check if email is verified
                if (!user.EmailConfirmed)
                {
                    return Unauthorized(new { message = "Email not verified. Please check your email for verification link." });
                }

                var result = await _userManager.CheckPasswordAsync(user, request.Password);
                if (!result)
                {
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                // Get user roles
                var roles = await _userManager.GetRolesAsync(user);

                // Generate tokens
                var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(user, roles);
                var refreshToken = await _jwtTokenService.GenerateRefreshTokenAsync(user);

                // Set refresh token as HttpOnly cookie
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true, // Use HTTPS in production
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(7)
                };
                Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);

                return Ok(new AuthResponse
                {
                    AccessToken = accessToken,
                    ExpiresIn = 15, // minutes
                    User = new UserDto
                    {
                        Id = user.Id,
                        UserName = user.UserName ?? string.Empty,
                        Email = user.Email ?? string.Empty,
                        FirstName = user.FirstName,
                        LastName = user.LastName
                    },
                    Roles = roles.ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed");
                return StatusCode(500, new { message = "Login failed" });
            }
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var refreshToken = request.RefreshToken ?? Request.Cookies["refreshToken"];
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return Unauthorized(new { message = "Refresh token is required" });
                }

                var principal = _jwtTokenService.ValidateToken(refreshToken);
                if (principal == null)
                {
                    return Unauthorized(new { message = "Invalid refresh token" });
                }

                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid refresh token" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                // Validate refresh token
                var isValid = await _jwtTokenService.ValidateRefreshTokenAsync(user, refreshToken);
                if (!isValid)
                {
                    return Unauthorized(new { message = "Invalid refresh token" });
                }

                // Revoke old refresh token
                await _jwtTokenService.RevokeRefreshTokenAsync(user, refreshToken);

                // Generate new tokens
                var roles = await _userManager.GetRolesAsync(user);
                var newAccessToken = await _jwtTokenService.GenerateAccessTokenAsync(user, roles);
                var newRefreshToken = await _jwtTokenService.GenerateRefreshTokenAsync(user);

                // Update refresh token cookie
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(7)
                };
                Response.Cookies.Append("refreshToken", newRefreshToken, cookieOptions);

                return Ok(new AuthResponse
                {
                    AccessToken = newAccessToken,
                    ExpiresIn = 15,
                    User = new UserDto
                    {
                        Id = user.Id,
                        UserName = user.UserName ?? string.Empty,
                        Email = user.Email ?? string.Empty,
                        FirstName = user.FirstName,
                        LastName = user.LastName
                    },
                    Roles = roles.ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed");
                return StatusCode(500, new { message = "Token refresh failed" });
            }
        }

        [HttpPost("revoke-token")]
        [Authorize]
        public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var refreshToken = request.RefreshToken ?? Request.Cookies["refreshToken"];
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return BadRequest(new { message = "Refresh token is required" });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                await _jwtTokenService.RevokeRefreshTokenAsync(user, refreshToken);

                // Clear refresh token cookie
                Response.Cookies.Delete("refreshToken");

                return Ok(new { message = "Token revoked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token revocation failed");
                return StatusCode(500, new { message = "Token revocation failed" });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        // Revoke all refresh tokens for the user
                        await _jwtTokenService.RevokeAllRefreshTokensAsync(user);
                    }
                }

                // Clear refresh token cookie
                Response.Cookies.Delete("refreshToken");

                return Ok(new { message = "Logout successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed");
                return StatusCode(500, new { message = "Logout failed" });
            }
        }

        [HttpPost("verify-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return BadRequest(new { message = "Invalid email verification request" });
                }

                var result = await _userManager.ConfirmEmailAsync(user, request.Token);
                if (!result.Succeeded)
                {
                    return BadRequest(new { message = "Email verification failed", errors = result.Errors });
                }

                return Ok(new { message = "Email verified successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email verification failed");
                return StatusCode(500, new { message = "Email verification failed" });
            }
        }

        [HttpPost("resend-verification")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return Ok(new { message = "If the email exists, a verification link has been sent" });
                }

                if (user.EmailConfirmed)
                {
                    return BadRequest(new { message = "Email is already verified" });
                }

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                
                // TODO: Send verification email (mock for now)
                _logger.LogInformation($"Email verification token for {user.Email}: {token}");

                return Ok(new { message = "Verification email sent" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Resend verification failed");
                return StatusCode(500, new { message = "Resend verification failed" });
            }
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return Ok(new { message = "If the email exists, a password reset link has been sent" });
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                
                // TODO: Send password reset email (mock for now)
                _logger.LogInformation($"Password reset token for {user.Email}: {token}");

                return Ok(new { message = "Password reset email sent" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Forgot password failed");
                return StatusCode(500, new { message = "Forgot password failed" });
            }
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return BadRequest(new { message = "Invalid password reset request" });
                }

                var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
                if (!result.Succeeded)
                {
                    return BadRequest(new { message = "Password reset failed", errors = result.Errors });
                }

                // Revoke all refresh tokens after password reset
                await _jwtTokenService.RevokeAllRefreshTokensAsync(user);

                return Ok(new { message = "Password reset successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password reset failed");
                return StatusCode(500, new { message = "Password reset failed" });
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
                if (!result.Succeeded)
                {
                    return BadRequest(new { message = "Password change failed", errors = result.Errors });
                }

                // Revoke all refresh tokens after password change
                await _jwtTokenService.RevokeAllRefreshTokensAsync(user);

                return Ok(new { message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password change failed");
                return StatusCode(500, new { message = "Password change failed" });
            }
        }
    }
}