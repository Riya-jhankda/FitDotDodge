using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Domain.Entities;
using FitDodge.Filters;
using Application.DTO;

namespace FitDodge.Controllers
{
    [ApiController]
    [Route("api/scanner")]
    public class ScannerController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ScannerController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpPost("mark-attendance")]
        [ScannerAuth]
        public async Task<IActionResult> MarkAttendance([FromBody] ScannerAttendanceDto dto)
        {
            var scanner = HttpContext.Items["Scanner"] as ScannerDevice;

            if (scanner == null)
                return Unauthorized("Scanner authentication failed.");

            // Load scanner's school
            var school = await _db.Schools
                .FirstOrDefaultAsync(s => s.SchoolId == scanner.SchoolId);

            if (school == null)
                return BadRequest("Scanner is not mapped to any valid school.");

            // Validate class belongs to school using SchoolName
            var classEntity = await _db.Classes
                .FirstOrDefaultAsync(c => c.ClassId == dto.ClassId
                                          && c.SchoolName == school.Name);

            if (classEntity == null)
                return BadRequest("This class does not belong to this scanner's school.");

            // Identify user by QR + same school
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.QrCodeValue == dto.QrCodeValue
                                          && u.SchoolId == scanner.SchoolId);

            if (user == null)
                return BadRequest("Invalid QR. User not found in this school.");

            // Check enrollment
            bool isEnrolled = await _db.ClassEnrollments
                .AnyAsync(e => e.ClassId == dto.ClassId && e.UserId == user.Id);

            if (!isEnrolled)
                return BadRequest("User is not enrolled in this class.");

            // Check if already marked today
            var today = DateTime.UtcNow.Date;

            var attendance = await _db.Attendances
                .FirstOrDefaultAsync(a => a.UserId == user.Id
                                       && a.ClassId == dto.ClassId
                                       && a.Date == today);

            if (attendance != null)
            {
                return Ok(new
                {
                    message = "Attendance already marked.",
                    user = user.UserName,
                    className = classEntity.Name,
                    date = today
                });
            }

            // Mark attendance
            var newAttendance = new Attendance
            {
                UserId = user.Id,
                ClassId = dto.ClassId,
                Date = today,
                IsPresent = true
            };

            _db.Attendances.Add(newAttendance);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Attendance marked successfully.",
                user = user.UserName,
                className = classEntity.Name,
                date = today
            });
        }


        [HttpGet("today-classes")]
        [ScannerAuth]
        public async Task<IActionResult> GetTodayClasses()
        {
            var scanner = HttpContext.Items["Scanner"] as ScannerDevice;

            if (scanner == null)
                return Unauthorized("Scanner authentication failed.");

            // get school
            var school = await _db.Schools
                .FirstOrDefaultAsync(s => s.SchoolId == scanner.SchoolId);

            if (school == null)
                return BadRequest("Invalid school mapped to this scanner.");

            var today = DateTime.UtcNow.Date;

            var classes = await _db.Classes
                .Where(c =>
                    c.SchoolName == school.Name &&
                    c.StartTime.Date <= today &&
                    c.EndTime.Date >= today
                )
                .Include(c => c.Coach)
                .Select(c => new
                {
                    c.ClassId,
                    c.Name,
                    c.ClassType,
                    StartTime = c.StartTime.ToString("HH:mm"),
                    EndTime = c.EndTime.ToString("HH:mm"),
                    CoachName = c.Coach != null ? c.Coach.UserName : "Unknown"
                })
                .OrderBy(c => c.StartTime)
                .ToListAsync();

            return Ok(classes);
        }

    }
}
