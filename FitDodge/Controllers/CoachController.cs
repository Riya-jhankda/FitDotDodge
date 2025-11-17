using Application.DTO;
using Application.Interfaces;
using Domain.Entities;
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
    public class CoachController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IQrCodeService _qrService;

        public CoachController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IEmailService emailService, IQrCodeService qrService)
        {
            _db = db;
            _userManager = userManager;
            _emailService = emailService;
            _qrService = qrService;
        }

        [HttpPost("coach/mark-attendance")]
        [Authorize(Roles = UserRole.Coach)]
        public async Task<IActionResult> MarkStudentAttendance([FromBody] MarkAttendanceDto dto)
        {
            var coach = await _userManager.GetUserAsync(User);
            if (coach == null)
                return Unauthorized("Invalid coach.");

            // Step 1️⃣ - Validate class under coach’s school and assigned to this coach
            var classEntity = await _db.Classes
                .FirstOrDefaultAsync(c => c.ClassId == dto.ClassId
                                          && c.SchoolName == coach.SchoolName
                                          && c.CoachId == coach.Id);

            if (classEntity == null)
                return BadRequest("Invalid class or not assigned to you.");

            // Step 2️⃣ - Validate student via QR
            var student = await _db.Users
                .FirstOrDefaultAsync(u => u.QrCodeValue == dto.QrCodeValue
                                          && u.SchoolName == coach.SchoolName);

            if (student == null)
                return BadRequest("Invalid QR or student not found in your school.");

            // Step 3️⃣ - Check enrollment
            bool isEnrolled = await _db.ClassEnrollments
                .AnyAsync(e => e.UserId == student.Id && e.ClassId == dto.ClassId);

            if (!isEnrolled)
                return BadRequest("Student is not enrolled in this class.");

            // Step 4️⃣ - Check if already marked today
            var today = DateTime.UtcNow.Date;
            var attendance = await _db.Attendances
                .FirstOrDefaultAsync(a => a.UserId == student.Id && a.ClassId == dto.ClassId && a.Date == today);

            if (attendance != null)
                return Ok(new { message = "Attendance already marked for today." });

            // Step 5️⃣ - Mark present
            var newAttendance = new Attendance
            {
                UserId = student.Id,
                ClassId = dto.ClassId,
                Date = today,
                IsPresent = true
            };

            _db.Attendances.Add(newAttendance);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = $"Attendance marked for {student.UserName} in {classEntity.Name}.",
                student = student.UserName,
                className = classEntity.Name,
                date = today
            });
        }

        [HttpGet("{classId}/enrolled-users")]
        public async Task<IActionResult> GetEnrolledUsers(Guid classId)
        {
            var classEntity = await _db.Classes
                .Include(c => c.Coach)
                .FirstOrDefaultAsync(c => c.ClassId == classId);

            if (classEntity == null)
                return NotFound("Class not found");

            var enrollments = await _db.ClassEnrollments
                .Include(e => e.User)
                .Where(e => e.ClassId == classId && e.Status == "Active")
                .Select(e => new
                {
                    e.User.Id,
                    e.User.UserName,
                    e.User.Email,
                    e.User.Gender,
                    //e.User.ProfileImageUrl, // assuming you have a column for image path or URL
                    e.EnrolledOn
                })
                .ToListAsync();

            var response = new
            {
                classEntity.ClassId,
                classEntity.Name,
                CoachName = classEntity.Coach?.UserName,
                classEntity.ClassType,
                classEntity.StartTime,
                classEntity.EndTime,
                TotalEnrolled = enrollments.Count,
                EnrolledUsers = enrollments
            };

            return Ok(response);
        }

        // Only Coach of this class or Admin can create/update
        [Authorize(Roles = "Coach,Admin")]
        [HttpPost("{classId}/note")]
        public async Task<IActionResult> UpsertClassNote(Guid classId, [FromBody] UpsertClassNoteDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var classEntity = await _db.Classes
                .Include(c => c.Coach)
                .FirstOrDefaultAsync(c => c.ClassId == classId);

            if (classEntity == null) return NotFound("Class not found.");

            // If user is not Admin, ensure they are the coach of the class
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && classEntity.CoachId != userId)
                return Forbid("Only the coach for this class or an admin can update notes.");

            // Update fields (overwrite)
            classEntity.CoachNote = dto.CoachNote;
            classEntity.Info = dto.Info;
            classEntity.NoteLastUpdated = DateTime.UtcNow;

            _db.Classes.Update(classEntity);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Class note updated." });
        }

        // Only Coach of this class or Admin can delete/clear note
        [Authorize(Roles = "Coach,Admin")]
        [HttpDelete("{classId}/note")]
        public async Task<IActionResult> DeleteClassNote(Guid classId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var classEntity = await _db.Classes.FirstOrDefaultAsync(c => c.ClassId == classId);
            if (classEntity == null) return NotFound("Class not found.");

            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && classEntity.CoachId != userId)
                return Forbid("Only the coach for this class or an admin can delete notes.");

            classEntity.CoachNote = null;
            classEntity.Info = null;
            classEntity.NoteLastUpdated = DateTime.UtcNow;

            _db.Classes.Update(classEntity);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Class note cleared." });
        }


    }
}
