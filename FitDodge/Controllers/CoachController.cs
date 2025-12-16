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

        //Convert UTC → IST
        private DateTime TodayIST()
        {
            // IST = UTC + 5:30
            return DateTime.UtcNow.AddHours(5).AddMinutes(30).Date;
        }


        public CoachController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IEmailService emailService, IQrCodeService qrService)
        {
            _db = db;
            _userManager = userManager;
            _emailService = emailService;
            _qrService = qrService;
        }

        //[HttpPost("coach/mark-attendance")]
        //[Authorize(Roles = UserRole.Coach)]
        //public async Task<IActionResult> MarkStudentAttendance([FromBody] MarkAttendanceDto dto)
        //{
        //    var coach = await _userManager.GetUserAsync(User);
        //    if (coach == null)
        //        return Unauthorized("Invalid coach.");

        //    // Step 1️⃣ - Validate class under coach’s school and assigned to this coach
        //    var classEntity = await _db.Classes
        //        .FirstOrDefaultAsync(c => c.ClassId == dto.ClassId
        //                                  && c.SchoolName == coach.SchoolName
        //                                  && c.CoachId == coach.Id);

        //    if (classEntity == null)
        //        return BadRequest("Invalid class or not assigned to you.");

        //    // Step 2️⃣ - Validate student via QR
        //    var student = await _db.Users
        //        .FirstOrDefaultAsync(u => u.QrCodeValue == dto.QrCodeValue
        //                                  && u.SchoolName == coach.SchoolName);

        //    if (student == null)
        //        return BadRequest("Invalid QR or student not found in your school.");

        //    // Step 3️⃣ - Check enrollment
        //    bool isEnrolled = await _db.ClassEnrollments
        //        .AnyAsync(e => e.UserId == student.Id && e.ClassId == dto.ClassId);

        //    if (!isEnrolled)
        //        return BadRequest("Student is not enrolled in this class.");

        //    // Step 4️⃣ - Check if already marked today
        //    var today = DateTime.UtcNow.Date;
        //    var attendance = await _db.Attendances
        //        .FirstOrDefaultAsync(a => a.UserId == student.Id && a.ClassId == dto.ClassId && a.Date == today);

        //    if (attendance != null)
        //        return Ok(new { message = "Attendance already marked for today." });

        //    // Step 5️⃣ - Mark present
        //    var newAttendance = new Attendance
        //    {
        //        UserId = student.Id,
        //        ClassId = dto.ClassId,
        //        Date = today,
        //        IsPresent = true
        //    };

        //    _db.Attendances.Add(newAttendance);
        //    await _db.SaveChangesAsync();

        //    return Ok(new
        //    {
        //        message = $"Attendance marked for {student.UserName} in {classEntity.Name}.",
        //        student = student.UserName,
        //        className = classEntity.Name,
        //        date = today
        //    });
        //}

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

        //coach profile with total classes and total users across all classes

        [HttpGet("profile/{coachId}")]
        public async Task<IActionResult> GetCoachProfile(string coachId)
        {
            var coach = await _db.Users
                .FirstOrDefaultAsync(c => c.Id == coachId && c.Role == UserRole.Coach);

            if (coach == null)
                return NotFound("Coach not found.");

            // 1️⃣ Total classes assigned
            int totalClassesAssigned = await _db.Classes
                .CountAsync(c => c.CoachId == coachId);

            // 2️⃣ Total users managed (unique users across all classes)
            int totalUsers = await _db.ClassEnrollments
                .Where(e => e.Class.CoachId == coachId && e.Status == "Active")
                .Select(e => e.UserId)
                .Distinct()
                .CountAsync();

            // 3️⃣ Attendance statistics
            var attendanceRecords = await _db.Attendances
                .Where(a => a.Class.CoachId == coachId)
                .ToListAsync();

            int totalSessions = attendanceRecords.Count;                  // present + absent
            int totalPresent = attendanceRecords.Count(a => a.IsPresent); // only present

            double avgAttendance = 0;

            if (totalSessions > 0)
                avgAttendance = Math.Round((double)totalPresent / totalSessions * 100, 2);

            var profile = new
            {
                coach.Id,
                coach.UserName,
                coach.Email,
                coach.ProfilePictureUrl,
                coach.Expertise,
                coach.Bio,
                coach.Achievements,

                TotalClassesAssigned = totalClassesAssigned,
                TotalUsers = totalUsers,
                AvgAttendancePercent = avgAttendance
            };

            return Ok(profile);
        }



        //[HttpGet("classes/today/{coachId}")]
        //public async Task<IActionResult> GetTodayClasses(string coachId)
        //{
        //    var today = DateTime.UtcNow.Date;

        //    var classes = await _db.Classes
        //        .Where(c => c.CoachId == coachId &&
        //                    c.StartTime.Date == today)
        //        .OrderBy(c => c.StartTime)
        //        .Select(c => new
        //        {
        //            c.ClassId,
        //            c.Name,
        //            c.ClassType,
        //            c.StartTime,
        //            c.EndTime,
        //            TotalUsers = _db.ClassEnrollments
        //                        .Count(e => e.ClassId == c.ClassId && e.Status == "Active")
        //        })
        //        .ToListAsync();

        //    return Ok(classes);
        //}


        [HttpGet("classes/today/{coachId}")]
        public async Task<IActionResult> GetTodaysClasses(string coachId)
        {
            var today = TodayIST();

            var classes = await _db.Classes
                .Where(c => c.CoachId == coachId &&
                            c.StartTime.Date <= today &&
                            c.EndTime.Date >= today)
                .OrderBy(c => c.StartTime)
                .Select(c => new
                {
                    c.ClassId,
                    c.Name,
                    c.ClassType,
                    c.StartTime,
                    c.EndTime
                })
                .ToListAsync();

            return Ok(classes);
        }


        [HttpGet("classes/upcoming/{coachId}")]
        public async Task<IActionResult> GetUpcomingClasses(string coachId)
        {
            var today = TodayIST();

            var classes = await _db.Classes
                .Where(c => c.CoachId == coachId &&
                           (
                               c.StartTime.Date > today ||
                               (c.StartTime.Date <= today && c.EndTime.Date > today)
                           ))
                .OrderBy(c => c.StartTime)
                .Select(c => new
                {
                    c.ClassId,
                    c.Name,
                    c.ClassType,
                    c.StartTime,
                    c.EndTime
                })
                .ToListAsync();

            return Ok(classes);
        }


        [HttpGet("classes/past/{coachId}")]
        public async Task<IActionResult> GetPastClasses(string coachId)
        {
            var today = TodayIST();

            var classes = await _db.Classes
                .Where(c => c.CoachId == coachId &&
                            c.EndTime.Date < today)
                .OrderByDescending(c => c.EndTime)
                .Select(c => new
                {
                    c.ClassId,
                    c.Name,
                    c.ClassType,
                    c.StartTime,
                    c.EndTime
                })
                .ToListAsync();

            return Ok(classes);
        }


        [HttpPost("attendance/manual")]
        [Authorize(Roles = UserRole.Coach)]
        public async Task<IActionResult> ManualAttendance([FromBody] ManualAttendanceDto dto)
        {
            var coach = await _userManager.GetUserAsync(User);
            if (coach == null)
                return Unauthorized("Unauthorized coach.");

            // Validate class belongs to coach
            var classEntity = await _db.Classes
                .FirstOrDefaultAsync(c => c.ClassId == dto.ClassId && c.CoachId == coach.Id);

            if (classEntity == null)
                return BadRequest("This class is not assigned to you.");

            var date = dto.Date.Date;

            foreach (var s in dto.Students)
            {
                // Verify user exists & belongs to same school
                var student = await _db.Users.FirstOrDefaultAsync(u =>
                    u.Id == s.UserId && u.SchoolName == coach.SchoolName);

                if (student == null) continue;

                // Check enrollment
                bool isEnrolled = await _db.ClassEnrollments
                    .AnyAsync(e => e.UserId == s.UserId && e.ClassId == dto.ClassId);

                if (!isEnrolled) continue;

                // Check if attendance already exists
                var attendance = await _db.Attendances
                    .FirstOrDefaultAsync(a => a.UserId == s.UserId &&
                                              a.ClassId == dto.ClassId &&
                                              a.Date == date);

                if (attendance == null)
                {
                    // Create new record
                    attendance = new Attendance
                    {
                        UserId = s.UserId,
                        ClassId = dto.ClassId,
                        Date = date,
                        IsPresent = s.IsPresent
                    };
                    _db.Attendances.Add(attendance);
                }
                else
                {
                    // Update existing record
                    attendance.IsPresent = s.IsPresent;
                    _db.Attendances.Update(attendance);
                }
            }

            await _db.SaveChangesAsync();

            return Ok(new { message = "Attendance updated successfully." });
        }

        [HttpGet("class/{classId}/present")]
        [Authorize(Roles = UserRole.Coach)]
        public async Task<IActionResult> GetPresentStudents(Guid classId, [FromQuery] DateTime date)
        {
            var coach = await _userManager.GetUserAsync(User);

            if (coach == null)
                return Unauthorized("Invalid coach.");

            // Validate class belongs to coach
            var classEntity = await _db.Classes
                .FirstOrDefaultAsync(c => c.ClassId == classId && c.CoachId == coach.Id);

            if (classEntity == null)
                return BadRequest("Invalid class or not assigned to this coach.");

            var presentStudents = await _db.Attendances
                .Where(a => a.ClassId == classId && a.Date == date.Date && a.IsPresent)
                .Include(a => a.User)
                .Select(a => new
                {
                    a.User.Id,
                    a.User.UserName,
                    a.User.ProfilePictureUrl,
                    a.User.Email
                })
                .ToListAsync();

            return Ok(presentStudents);
        }


        [HttpGet("class/{classId}/absent")]
        [Authorize(Roles = UserRole.Coach)]
        public async Task<IActionResult> GetAbsentStudents(Guid classId, [FromQuery] DateTime date)
        {
            var coach = await _userManager.GetUserAsync(User);

            if (coach == null)
                return Unauthorized("Invalid coach.");

            // Validate class belongs to coach
            var classEntity = await _db.Classes
                .FirstOrDefaultAsync(c => c.ClassId == classId && c.CoachId == coach.Id);

            if (classEntity == null)
                return BadRequest("Invalid class or not assigned to this coach.");

            // All enrolled students
            var enrolledUsers = await _db.ClassEnrollments
                .Where(e => e.ClassId == classId && e.Status == "Active")
                .Include(e => e.User)
                .Select(e => e.User)
                .ToListAsync();

            // Present on that date
            var presentUserIds = await _db.Attendances
                .Where(a => a.ClassId == classId && a.Date == date.Date && a.IsPresent)
                .Select(a => a.UserId)
                .ToListAsync();

            // Absent = enrolled but not in present list
            var absentStudents = enrolledUsers
                .Where(u => !presentUserIds.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.ProfilePictureUrl,
                    u.Email
                })
                .ToList();

            return Ok(absentStudents);
        }

        [Authorize]
        [HttpPut("edit-profile")]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateUserProfileDto dto)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (userId == null)
                return Unauthorized("Unable to identify the logged-in user.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound("User not found");

            // Update allowed fields
            user.UserName = dto.FullName ?? user.UserName;
            user.Email = dto.Email ?? user.Email;
            user.NormalizedEmail = dto.Email?.ToUpper() ?? user.NormalizedEmail;

            user.PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber;
            user.Gender = dto.Gender ?? user.Gender;
            user.DateOfBirth = dto.DateOfBirth ?? user.DateOfBirth;
            user.Height = dto.Height ?? user.Height;
            user.Weight = dto.Weight ?? user.Weight;

            // Location → SchoolName (if allowed)
            user.SchoolName = dto.Location ?? user.SchoolName;

            // Profile picture
            user.ProfilePictureUrl = dto.ProfilePictureUrl ?? user.ProfilePictureUrl;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Profile updated successfully." });
        }





    }
}
