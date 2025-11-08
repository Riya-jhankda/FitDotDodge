using Application.DTO;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System.Security.Claims;

namespace FitDodge.Controllers
{
    [Authorize(Roles = UserRole.Admin)]
    [ApiController]
    [Route("api/[controller]")]
    public class ClassesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public ClassesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IEmailService emailService)
        {
            _db = db;
            _userManager = userManager;
            _emailService = emailService;
        }

        // ==============================
        // 📘 Create Class (Admin only)
        // ==============================
        [HttpPost("create")]
        public async Task<IActionResult> CreateClass([FromBody] CreateClassDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.EndTime <= dto.StartTime)
                return BadRequest("EndTime must be after StartTime.");

            var coach = await _userManager.FindByIdAsync(dto.CoachId);
            if (coach == null)
                return BadRequest("Coach not found.");

            var roles = await _userManager.GetRolesAsync(coach);
            if (!roles.Contains(UserRole.Coach))
                return BadRequest("Selected user is not a Coach.");

            // Ensure the coach belongs to the same school as the admin
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var admin = await _userManager.FindByIdAsync(adminId);

            if (admin?.SchoolName == null)
                return BadRequest("Admin school not found.");

            if (coach.SchoolName != admin.SchoolName)
                return BadRequest("Coach must belong to the same school as the Admin.");

            // Prevent overlapping class names for the same school
            bool classExists = await _db.Classes
                .AnyAsync(c => c.Name == dto.Name && c.CoachId == dto.CoachId);
            if (classExists)
                return BadRequest("A class with the same name already exists for this coach.");

            var entity = new Domain.Entities.Class
            {
                ClassId = Guid.NewGuid(),
                Name = dto.Name.Trim(),
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                CoachId = dto.CoachId,
                ClassType = dto.ClassType,
                SchoolName = admin.SchoolName //as coach belongs to same school as admin
            };

            _db.Classes.Add(entity);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Class created successfully", id = entity.ClassId });
        }

        // ======================================
        // 📋 Get Coaches List (for Admin dropdown)
        // ======================================
        [HttpGet("coaches")]
        public async Task<IActionResult> GetCoaches(
            [FromQuery] string? Nameofcoach = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var admin = await _userManager.FindByIdAsync(adminId);
            if (admin == null)
                return Unauthorized("Admin not found.");

            var coachUsers = await _userManager.GetUsersInRoleAsync(UserRole.Coach);
            var query = coachUsers.AsQueryable();

            // Limit to same school
            if (!string.IsNullOrWhiteSpace(admin.SchoolName))
                query = query.Where(u => u.SchoolName == admin.SchoolName);

            // Search by name or email
            if (!string.IsNullOrWhiteSpace(Nameofcoach))
            {
                Nameofcoach = Nameofcoach.Trim();
                query = query.Where(u =>
                    (u.UserName != null && u.UserName.Contains(Nameofcoach)) ||
                    (u.Email != null && u.Email.Contains(Nameofcoach)));
            }

            var total = query.Count();
            var items = query
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    id = u.Id,
                    name = u.UserName,
                    email = u.Email
                })
                .ToList();

            return Ok(new
            {
                results = items,
                pagination = new { more = (page * pageSize) < total }
            });
        }


        [HttpGet("total-users")]
        //[Authorize(Roles = UserRole.Admin)]
        public async Task<IActionResult> GetTotalUsers()
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var admin = await _userManager.FindByIdAsync(adminId);
            if (admin == null || string.IsNullOrEmpty(admin.SchoolName))
                return BadRequest("Admin not found or no school assigned.");

            var usersInSchool = await _userManager.GetUsersInRoleAsync(UserRole.User);
            var total = usersInSchool.Count(u => u.SchoolName == admin.SchoolName);

            return Ok(new { totalUsers = total });
        }

        [HttpGet("total-coaches")]
        //[Authorize(Roles = UserRole.Admin)]
        public async Task<IActionResult> GetTotalCoaches()
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var admin = await _userManager.FindByIdAsync(adminId);
            if (admin == null || string.IsNullOrEmpty(admin.SchoolName))
                return BadRequest("Admin not found or no school assigned.");

            var coachesInSchool = await _userManager.GetUsersInRoleAsync(UserRole.Coach);
            var total = coachesInSchool.Count(c => c.SchoolName == admin.SchoolName);

            return Ok(new { totalCoaches = total });
        }

        [HttpGet("today-classes")]
        //[Authorize(Roles = UserRole.Admin)]
        public async Task<IActionResult> GetTodayClasses()
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var admin = await _userManager.FindByIdAsync(adminId);
            if (admin == null || string.IsNullOrEmpty(admin.SchoolName))
                return BadRequest("Admin not found or no school assigned.");

            var today = DateTime.UtcNow.Date;

            // Replace the ambiguous Include call with a fully qualified namespace to resolve the ambiguity
            var classesToday = await _db.Classes
                .Include(c => c.Coach) // Replace this line
                .Where(c =>
                    c.Coach != null &&
                    c.Coach.SchoolName == admin.SchoolName &&
                    c.StartTime.Date <= today && c.EndTime.Date >= today)
                .Select(c => new
                {
                    c.Name,
                    c.ClassType,
                    c.StartTime,
                    c.EndTime,
                    CoachName = c.Coach.UserName
                })
                .ToListAsync();

            return Ok(new
            {
                totalClassesToday = classesToday.Count,
                //classes = classesToday
            });
        }

        //add users to class 
        [HttpPost("admin/add-user-to-class")]
        //[Authorize(Roles = UserRole.Admin)]
        public async Task<IActionResult> AddUserToClass([FromBody] AddUserToClassDto dto)
        {
            // 1. Get currently logged-in Admin
            var admin = await _userManager.GetUserAsync(User);
            if (admin == null)
                return Unauthorized("Invalid admin.");

            var schoolName = admin.SchoolName;
            if (string.IsNullOrEmpty(schoolName))
                return BadRequest("Admin is not assigned to any school.");

            // 2. Validate class under same school
            var classEntity = await _db.Classes
                .FirstOrDefaultAsync(c => c.ClassId == dto.ClassId && c.SchoolName == schoolName);

            if (classEntity == null)
                return BadRequest("Invalid class or class not in your school.");

            // 3. Check if user exists
            var user = await _userManager.FindByEmailAsync(dto.Email);
            bool isNewUser = false;

            if (user == null)
            {
                isNewUser = true;

                // 4a. Create new user
                var tempPassword = "DodgeFit@" + Guid.NewGuid().ToString("N")[..6];

                user = new ApplicationUser
                {
                    UserName = dto.Name,
                    Email = dto.Email,
                    SchoolName = schoolName,
                    IsApproved = true,
                    EmailConfirmed = true,
                    Status = dto.Status ?? "Active",
                    Gender = dto.Gender,
                    Height = dto.Height,
                    Weight = dto.Weight,
                    DateOfBirth = dto.DateOfBirth,
                    PhoneNumber = dto.PhoneNumber,
                    ProfilePictureUrl = dto.ProfilePictureUrl
                };

                var createResult = await _userManager.CreateAsync(user, tempPassword);
                if (!createResult.Succeeded)
                    return BadRequest(createResult.Errors);

                await _userManager.AddToRoleAsync(user, UserRole.User);

                // 4b. Send registration email
                var subject = "Welcome to Dodge.Fit - Your Account Has Been Created";
                var body = $@"
                    <h2>Welcome to Dodge.Fit!</h2>
                    <p>Hello {dto.Name},</p>
                    <p>You have been registered at <b>{schoolName}</b> by your school admin and added to the class <b>{classEntity.Name}</b>.</p>
                    <p><b>Login Details:</b></p>
                    <ul>
                        <li><b>Email:</b> {dto.Email}</li>
                        <li><b>Password:</b> {tempPassword}</li>
                    </ul>
                    <p>You can log in using the Dodge.Fit app and update your profile anytime.</p>
                    <br/>
                    <p>– Dodge.Fit Team</p>
                ";

                await _emailService.SendEmailAsync(dto.Email, subject, body);
            }
            else
            {
                // 4c. Update optional details
                user.UserName = dto.Name ?? user.UserName;
                user.PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber;
                user.Gender = dto.Gender ?? user.Gender;
                user.Height = dto.Height ?? user.Height;
                user.Weight = dto.Weight ?? user.Weight;
                user.DateOfBirth = dto.DateOfBirth ?? user.DateOfBirth;
                user.ProfilePictureUrl = dto.ProfilePictureUrl ?? user.ProfilePictureUrl;
                user.Status = dto.Status ?? user.Status;
                await _userManager.UpdateAsync(user);
            }

            // 5. Enroll user in the class (if not already)
            var alreadyEnrolled = await _db.ClassEnrollments
                .AnyAsync(e => e.UserId == user.Id && e.ClassId == dto.ClassId);

            if (!alreadyEnrolled)
            {
                var enrollment = new ClassEnrollment
                {
                    UserId = user.Id,
                    ClassId = dto.ClassId,
                    EnrolledOn = DateTime.UtcNow
                };
                _db.ClassEnrollments.Add(enrollment);
                await _db.SaveChangesAsync();
            }

            // 6. Response
            return Ok(new
            {
                message = isNewUser
                    ? $"User {dto.Name} registered and enrolled in class {classEntity.Name}."
                    : $"User {dto.Name} enrolled in class {classEntity.Name}."
            });
        }




    }

}
