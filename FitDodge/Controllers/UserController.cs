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

    [ApiController]
    [Route("api/[controller]")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IQrCodeService _qrService;

        public UserController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IEmailService emailService, IQrCodeService qrService)
        {
            _db = db;
            _userManager = userManager;
            _emailService = emailService;
            _qrService = qrService;
        }

        [HttpGet("user/upcoming-classes")]
        [Authorize(Roles = UserRole.User)]
        public async Task<IActionResult> GetUpcomingClassesForUser()
        {
            // 1. Identify current logged-in user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized("Invalid user.");

            var schoolName = user.SchoolName;
            if (string.IsNullOrEmpty(schoolName))
                return BadRequest("User is not assigned to any school.");

            // 2. Query all future classes user is enrolled in (same school)
            var upcomingClasses = await _db.ClassEnrollments
                .Where(e => e.UserId == user.Id
                            && e.Class.SchoolName == schoolName
                            && e.Class.StartTime > DateTime.UtcNow)
                .Select(e => new
                {
                    e.Class.ClassId,
                    e.Class.Name,
                    e.Class.StartTime,
                    e.Class.EndTime,
                    CoachName = e.Class.Coach.UserName,
                    CoachEmail = e.Class.Coach.Email,
                    e.Class.ClassType,
                    e.Class.SchoolName
                })
                .OrderBy(c => c.StartTime)
                .ToListAsync();

            if (!upcomingClasses.Any())
                return Ok(new { message = "No upcoming classes found." });

            return Ok(upcomingClasses);
        }

        [HttpGet("user/past-classes")]
        [Authorize(Roles = UserRole.User)]
        public async Task<IActionResult> GetPastClassesForUser()
        {
            // 1️⃣ Identify the current logged-in user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized("Invalid user.");

            var schoolName = user.SchoolName;
            if (string.IsNullOrEmpty(schoolName))
                return BadRequest("User is not assigned to any school.");

            // 2️⃣ Query all past classes user was enrolled in (same school)
            var pastClasses = await _db.ClassEnrollments
                .Where(e => e.UserId == user.Id
                            && e.Class.SchoolName == schoolName
                            && e.Class.EndTime < DateTime.UtcNow)
                .Select(e => new
                {
                    e.Class.ClassId,
                    e.Class.Name,
                    e.Class.StartTime,
                    e.Class.EndTime,
                    CoachName = e.Class.Coach.UserName,
                    CoachEmail = e.Class.Coach.Email,
                    e.Class.ClassType,
                    e.Class.SchoolName
                })
                .OrderByDescending(c => c.StartTime)
                .ToListAsync();

            if (!pastClasses.Any())
                return Ok(new { message = "No past classes found." });

            return Ok(pastClasses);
        }

        [HttpPost("generate")]
        [Authorize(Roles = UserRole.User)]
        public async Task<IActionResult> GenerateQr()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found.");

            // If already has a QR, just return it
            if (!string.IsNullOrEmpty(user.QrCodeValue))
            {
                return Ok(new
                {
                    message = "QR already exists.",
                    qrValue = user.QrCodeValue,
                    qrImage = user.QrCodeImageBase64
                });
            }

            // Generate unique QR value
            string qrValue = $"QR-{Guid.NewGuid()}";


            // Generate QR image
            string qrImageUrl = _qrService.GenerateQrImageBase64(qrValue);

            // Generate QR image
            user.QrCodeValue = qrValue;
            user.QrCodeImageBase64 = qrImageUrl;

            await _userManager.UpdateAsync(user);

            return Ok(new
            {
                message = "QR generated successfully.",
                qrValue,
                qrImageUrl
            });
        }


    }
}
