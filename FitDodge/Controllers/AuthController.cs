using Application.DTO;
using Application.Interfaces;
using Domain.Entities;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FitDodge.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config; 
        private readonly IEmailService _emailService;


        public AuthController(UserManager<ApplicationUser> userManager, IConfiguration config,  IEmailService emailService)
        {
            _userManager = userManager;
            _config = config;
            _emailService = emailService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user == null)
                return Unauthorized("Invalid credentials.");

            if (!user.EmailConfirmed || !user.IsApproved)
                return Unauthorized("Your account is not approved yet.");

            if (!await _userManager.CheckPasswordAsync(user, dto.Password))
                return Unauthorized("Invalid credentials.");

            var roles = await _userManager.GetRolesAsync(user);

            // Only SuperAdmin or Admin can log in
            //if (!roles.Contains(UserRole.SuperAdmin) && !roles.Contains(UserRole.Admin))
            //    return Unauthorized("Only Admins or SuperAdmin can log in.");

            var token = GenerateJwtToken(user, roles);

            // if superadmin, no school name
            var response = new
            {
                token,
                role = roles.First(),
                email = user.Email,
                school = roles.Contains(UserRole.SuperAdmin) ? null : user.SchoolName
            };

            return Ok(response);
        }


        private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, roles.First())
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }



        //[HttpPost("forgot-password")]
        //public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        //{
        //    var firebaseApiKey = _config["Firebase:ApiKey"];
        //    var client = new HttpClient();
        //    var payload = new
        //    {
        //        requestType = "PASSWORD_RESET",
        //        email = dto.Email
        //    };
        //    var response = await client.PostAsJsonAsync(
        //        $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={firebaseApiKey}",
        //        payload
        //    );

        //    if (response.IsSuccessStatusCode)
        //        return Ok("Password reset email sent successfully.");
        //    else
        //        return BadRequest("Unable to send password reset email.");
        //}

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return BadRequest("User with this email doesn't exist.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var frontendUrl = _config["AppSettings:ClientUrl"]; // e.g. http://localhost:3000 or your deployed site
            var resetLink = $"{frontendUrl}/hosting-material/index.html?email={dto.Email}&token={Uri.EscapeDataString(token)}";

            var subject = "Dodge.Fit - Password Reset";
            var body = $@"
                <h2>Reset Your Password</h2>
                <p>Click the link below to reset your password:</p>
                <a href='{resetLink}'>Reset Password</a>
                <br/><br/>
                <p>If you didn't request this, you can safely ignore this email.</p>
            ";

            await _emailService.SendEmailAsync(dto.Email, subject, body);

            return Ok("Password reset link has been sent to your email.");
        }



        [HttpPost("register/admin")]
        [Authorize(Roles = UserRole.SuperAdmin)]
        public async Task<IActionResult> RegisterAdmin([FromBody] RegisterAdminDto dto)
        {
            // 1. Check if school already has an admin
            var existingAdmin = await _userManager.Users
                .FirstOrDefaultAsync(u => u.SchoolName == dto.SchoolName &&
                                          (u.EmailConfirmed || u.IsApproved));

            if (existingAdmin != null)
                return BadRequest(new { message = $"School '{dto.SchoolName}' already has an Admin." });

            // 2. Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return BadRequest(new { message = "Email already registered." });

            // 3. Create admin in .NET Identity
            var admin = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                SchoolName = dto.SchoolName,
                EmailConfirmed = true,
                IsApproved = true // since SuperAdmin is creating
            };

            var result = await _userManager.CreateAsync(admin, dto.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // 4. Assign Admin role
            await _userManager.AddToRoleAsync(admin, UserRole.Admin);

            // 6. Send Welcome Email with Password Info
            var subject = "Welcome to Dodge.Fit - Admin Account Created";
            var body = $@"
                <h2>Welcome to Dodge.Fit!</h2>
                <p>Hello Admin,</p>
                <p>Your account has been created by SuperAdmin</b>.</p>
                <p><b>Login Details:</b></p>
                <ul>
                    <li><b>Email:</b> {dto.Email}</li>
                    <li><b>Password:</b> {dto.Password}</li>
                </ul>
                <p>You can now log in using the credentials above.</p>
                <br/>
                <p>– Dodge.Fit Team</p>
            ";

            await _emailService.SendEmailAsync(dto.Email, subject, body);

            return Ok(new { message = $"Admin registered successfully for {dto.SchoolName}. An email with credentials has been sent." });
        }

        [HttpPost("register/coach")]
        [Authorize(Roles = UserRole.Admin)]
        public async Task<IActionResult> RegisterCoach([FromBody] RegisterCoachDto dto)
        {
            // 1. Get the currently logged-in Admin
            var admin = await _userManager.GetUserAsync(User);
            if (admin == null)
                return Unauthorized("Invalid admin.");

            // 2. Ensure admin has a school assigned
            var schoolName = admin.SchoolName;
            if (string.IsNullOrEmpty(schoolName))
                return BadRequest("Admin is not assigned to any school.");

            // 3. Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return BadRequest(new { message = "Email already registered." });

            // 4. Create the Coach
            var coach = new ApplicationUser
            {
                UserName = dto.Name,
                Email = dto.Email,
                SchoolName = schoolName,
                EmailConfirmed = true,
                IsApproved = true // since admin is creating themselves 
            };

            var result = await _userManager.CreateAsync(coach, dto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // 5. Assign Role
            await _userManager.AddToRoleAsync(coach, UserRole.Coach);

            // 6. Send Welcome Email with Password Info
            var subject = "Welcome to Dodge.Fit - Coach Account Created";
            var body = $@"
                <h2>Welcome to Dodge.Fit!</h2>
                <p>Hello {dto.Name} Coach,</p>
                <p>Your account has been created by your school admin for <b>{schoolName}</b>.</p>
                <p><b>Login Details:</b></p>
                <ul>
                    <li><b>Email:</b> {dto.Email}</li>
                    <li><b>Password:</b> {dto.Password}</li>
                </ul>
                <p>You can now log in using the credentials above.</p>
                <br/>
                <p>– Dodge.Fit Team</p>
            ";

            await _emailService.SendEmailAsync(dto.Email, subject, body);

            return Ok(new { message = $"Coach registered successfully under {schoolName}. An email with credentials has been sent." });
        }


        [HttpPost("register/user")]
        [Authorize(Roles = UserRole.Admin)]
        public async Task<IActionResult> RegisterUser([FromBody] RegisterUserDto dto)
        {
            // 1. Get the currently logged-in Admin
            var admin = await _userManager.GetUserAsync(User);
            if (admin == null)
                return Unauthorized("Invalid admin.");

            // 2. Ensure admin has a school assigned
            var schoolName = admin.SchoolName;
            if (string.IsNullOrEmpty(schoolName))
                return BadRequest("Admin is not assigned to any school.");

            // 3. Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return BadRequest(new { message = "Email already registered." });

            // 4. Create the User
            var user = new ApplicationUser
            {
                UserName = dto.Name,
                Email = dto.Email,
                SchoolName = schoolName,
                EmailConfirmed = true,
                IsApproved = true // since created by Admin
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // 5. Assign Role
            await _userManager.AddToRoleAsync(user, UserRole.User);

            // 6. Send Welcome Email with Login Info
            var subject = "Welcome to Dodge.Fit - User Account Created";
            var body = $@"
                <h2>Welcome to Dodge.Fit!</h2>
                <p>Hello {dto.Name},</p>
                <p>Your account has been created by your school admin for <b>{schoolName}</b>.</p>
                <p><b>Login Details:</b></p>
                <ul>
                    <li><b>Email:</b> {dto.Email}</li>
                    <li><b>Password:</b> {dto.Password}</li>
                </ul>
                <p>You can now log in using the credentials above.</p>
                <br/>
                <p>– Dodge.Fit Team</p>
            ";

            await _emailService.SendEmailAsync(dto.Email, subject, body);

            return Ok(new { message = $"User registered successfully under {schoolName}. An email with credentials has been sent." });
        }



        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return BadRequest("Invalid email.");

            var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok("Password has been reset successfully.");
        }

    }



}
