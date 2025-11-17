using Application.DTO;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System.Globalization;
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

        [HttpPost("generateQr")]
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


        [HttpGet("profile/{id}")]
        public async Task<IActionResult> GetUserProfile(string id)
        {
            var user = await _db.Users
                .Include(u => u.ClassEnrollments)
                .ThenInclude(e => e.Class)
                .Include(u => u.Attendances)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound("User not found");

            // ✅ Physical calculations
            double? bmi = null;
            if (user.Height.HasValue && user.Weight.HasValue && user.Height.Value > 0)
            {
                // BMI = weight (kg) / height (m)^2
                double heightInMeters = user.Height.Value / 100;
                bmi = Math.Round(user.Weight.Value / (heightInMeters * heightInMeters), 2);
            }

            double? waistHipRatio = null;
            if (user.Waist.HasValue && user.Hip.HasValue && user.Hip.Value > 0)
            {
                waistHipRatio = Math.Round(user.Waist.Value / user.Hip.Value, 2);
            }

            // ✅ Attendance calculations
            int totalClasses = user.ClassEnrollments.Count;
            int attended = user.Attendances.Count(a => a.IsPresent);
            int missed = totalClasses - attended;

            var response = new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.Gender,
                user.Height,
                user.Weight,
                BMI = bmi,
                WaistHipRatio = waistHipRatio,
                user.Status,
                user.IsActive,
                TotalClasses = totalClasses,
                AttendedClasses = attended,
                MissedClasses = missed,
                user.QrCodeValue
            };

            return Ok(response);
        }


        [HttpGet("{userId}/AttendanceSummary")]
        public async Task<IActionResult> GetUserSummary(string userId)
        {
            var user = await _db.Users
                .Include(u => u.ClassEnrollments)
                    .ThenInclude(e => e.Class)
                        .ThenInclude(c => c.Coach)
                .Include(u => u.Attendances)
                    .ThenInclude(a => a.Class)
                        .ThenInclude(c => c.Coach)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("User not found.");

            var totalClassesEnrolled = user.ClassEnrollments.Count;

            // Classes user was marked present
            var attendedClasses = user.Attendances
                .Where(a => a.IsPresent)
                .Select(a => new
                {
                    ClassName = a.Class.Name,
                    CoachName = a.Class.Coach != null ? a.Class.Coach.UserName : "N/A",
                    Date = a.Date.ToString("yyyy-MM-dd"),
                    Day = a.Date.DayOfWeek.ToString()
                })
                .ToList();

            var totalClassesPresent = attendedClasses.Count;
            var totalClassesAbsent = totalClassesEnrolled - totalClassesPresent;

            var response = new
            {
                user.Id,
                user.UserName,
                user.Email,
                TotalClassesEnrolled = totalClassesEnrolled,
                TotalClassesPresent = totalClassesPresent,
                TotalClassesAbsent = totalClassesAbsent,
                AttendedClasses = attendedClasses
            };

            return Ok(response);
        }

        [HttpPut("EditProfile/{id}")]
        public async Task<IActionResult> UpdateUserProfile(string id, [FromBody] UpdateUserProfileDto dto)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
                return NotFound("User not found");

            // Update only allowed fields
            user.UserName = dto.UserName ?? user.UserName;
            user.Email = dto.Email ?? user.Email;
            user.Gender = dto.Gender ?? user.Gender;
            user.Height = dto.Height ?? user.Height;
            user.Weight = dto.Weight ?? user.Weight;

            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Profile updated successfully" });
        }

        [HttpPost("WorkoutLog")]
        public async Task<IActionResult> AddWorkoutLog([FromBody] AddWorkoutLogDto dto)
        {
            var log = new WorkoutLog
            {
                UserId = dto.UserId,
                Category = dto.Category,
                ExerciseName = dto.ExerciseName,
                Sets = dto.Sets,
                Reps = dto.Reps,
                Weight = dto.Weight,
                LoggedDate = DateTime.UtcNow
            };

            _db.WorkoutLogs.Add(log);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Exercise logged successfully" });
        }

        [HttpGet("logs/{userId}")]
        public async Task<IActionResult> GetWorkoutLogs(string userId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var query = _db.WorkoutLogs.AsQueryable();

            query = query.Where(l => l.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(l => l.LoggedDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(l => l.LoggedDate <= endDate.Value);

            var logs = await query
                .OrderByDescending(l => l.LoggedDate)
                .ToListAsync();

            return Ok(logs);
        }


        [HttpGet("{userId}/weekly-summary")]
        public async Task<IActionResult> GetWeeklySummary(string userId)
        {
            // Calculate current week (Mon–Sun)
            DateTime today = DateTime.Today;
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime weekStart = today.AddDays(-diff).Date;     // Monday
            DateTime weekEnd = weekStart.AddDays(7).Date;       // Next Monday

            // Load user with attendance + classes
            var attendances = await _db.Attendances
                .Include(a => a.Class)
                .Where(a => a.UserId == userId &&
                            a.Date >= weekStart &&
                            a.Date < weekEnd &&
                            a.IsPresent == true)
                .ToListAsync();

            // Load workouts logged this week
            var workoutLogs = await _db.WorkoutLogs
                .Where(w => w.UserId == userId &&
                            w.LoggedDate.Date >= weekStart &&
                            w.LoggedDate.Date < weekEnd)
                .ToListAsync();

            // -------------------------
            // 1️⃣ CLASSES ATTENDED
            // -------------------------
            int classesAttended = attendances.Count;

            // -------------------------
            // 2️⃣ DAYS ATTENDED (unique)
            // -------------------------
            int daysAttended = attendances
                .Select(a => a.Date.Date)
                .Distinct()
                .Count();

            // -------------------------
            // 3️⃣ WORKOUTS LOGGED
            // -------------------------
            int workoutsLogged = workoutLogs.Count;

            // -------------------------
            // 4️⃣ TOTAL HOURS (with overlap correction)
            // -------------------------
            double totalHours = 0;

            // Prepare list of intervals (per class)
            var intervals = attendances
                .Select(a => new
                {
                    Start = a.Class.StartTime,
                    End = a.Class.EndTime
                })
                .OrderBy(i => i.Start)
                .ToList();

            // Merge overlapping intervals
            if (intervals.Count > 0)
            {
                var merged = new List<(DateTime Start, DateTime End)>();
                var current = intervals[0];

                DateTime curStart = current.Start;
                DateTime curEnd = current.End;

                for (int i = 1; i < intervals.Count; i++)
                {
                    if (intervals[i].Start <= curEnd)
                    {
                        // Overlaps → merge
                        curEnd = intervals[i].End > curEnd ? intervals[i].End : curEnd;
                    }
                    else
                    {
                        // No overlap → store current and move to next
                        merged.Add((curStart, curEnd));
                        curStart = intervals[i].Start;
                        curEnd = intervals[i].End;
                    }
                }
                merged.Add((curStart, curEnd));

                // Calculate total hours
                foreach (var m in merged)
                {
                    totalHours += (m.End - m.Start).TotalHours;
                }
            }

            // -------------------------
            // 5️⃣ WEEKLY CONSISTENCY (7 bars → Mon to Sun)
            // -------------------------
            int[] weeklyConsistency = new int[7];

            var attendedDays = attendances
                .Select(a => a.Date.Date)
                .ToHashSet();

            for (int i = 0; i < 7; i++)
            {
                var day = weekStart.AddDays(i).Date;
                weeklyConsistency[i] = attendedDays.Contains(day) ? 1 : 0;
            }

            // -------------------------
            // FINAL RESPONSE
            // -------------------------
            var response = new
            {
                WeekStart = weekStart.ToString("yyyy-MM-dd"),
                WeekEnd = weekEnd.AddDays(-1).ToString("yyyy-MM-dd"),

                DaysAttended = daysAttended,
                ClassesAttended = classesAttended,
                WorkoutsLogged = workoutsLogged,
                TotalHours = Math.Round(totalHours, 2),

                WeeklyConsistency = weeklyConsistency
            };

            return Ok(response);
        }

        //endpoint to get note for the class
        [Authorize]
        [HttpGet("{classId}/note")]
        public async Task<IActionResult> GetClassNote(Guid classId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var classEntity = await _db.Classes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClassId == classId);

            if (classEntity == null) return NotFound("Class not found.");

            // Check enrollment
            var isEnrolled = await _db.ClassEnrollments
                .AnyAsync(e => e.ClassId == classId && e.UserId == userId && e.Status == "Active");

            if (!isEnrolled) return Forbid("You must be enrolled to view class notes.");

            var response = new
            {
                classEntity.ClassId,
                classEntity.Name,
                CoachName = classEntity.Coach != null ? classEntity.Coach.UserName : null,
                CoachNote = classEntity.CoachNote,
                Info = classEntity.Info,
                NoteLastUpdated = classEntity.NoteLastUpdated
            };

            return Ok(response);
        }

        [HttpGet("{userId}/progress/WorkoutHistory/Bars")]
        public async Task<IActionResult> GetUserProgress(string userId,[FromQuery] string startDate, [FromQuery] string endDate )
        {
            if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate))
                return BadRequest("startDate and endDate are required in yyyy-MM-dd format.");

            // --- Timezone handling: treat input as IST dates; convert to UTC range for DB ---
            TimeZoneInfo istZone = null;
            try
            {
                istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); // Windows
            }
            catch
            {
                istZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); // Linux fallback
            }

            if (!DateTime.TryParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startLocal)
                || !DateTime.TryParseExact(endDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endLocal))
            {
                return BadRequest("Dates must be in yyyy-MM-dd format.");
            }

            // Start inclusive at 00:00 IST, End inclusive at 23:59:59.999 IST
            var startIst = DateTime.SpecifyKind(startLocal.Date, DateTimeKind.Unspecified);
            var endIst = DateTime.SpecifyKind(endLocal.Date.AddDays(1).AddTicks(-1), DateTimeKind.Unspecified);

            var startUtc = TimeZoneInfo.ConvertTimeToUtc(startIst, istZone);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(endIst, istZone);

            // Fetch logs from DB (assume LoggedDate stored in UTC)
            var logs = await _db.WorkoutLogs
                .Where(w => w.UserId == userId && w.LoggedDate >= startUtc && w.LoggedDate <= endUtc)
                .AsNoTracking()
                .ToListAsync();

            // Convert log times to IST for grouping and output
            Func<DateTime, DateTime> toIst = (utc) =>
            {
                if (utc.Kind == DateTimeKind.Unspecified)
                    utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
                return TimeZoneInfo.ConvertTimeFromUtc(utc.ToUniversalTime(), istZone);
            };

            // Helper: date key in IST (yyyy-MM-dd)
            string GetIstDateKey(DateTime utc) => toIst(utc).ToString("yyyy-MM-dd");

            // 1) Workout History (bars) -> logs per day within range
            // Build full date list to include zero-days
            var dayCount = (endLocal.Date - startLocal.Date).Days + 1;
            var dateKeys = Enumerable.Range(0, dayCount)
                                     .Select(i => startLocal.Date.AddDays(i).ToString("yyyy-MM-dd"))
                                     .ToList();

            var logsByDate = logs.GroupBy(l => GetIstDateKey(l.LoggedDate))
                                 .ToDictionary(g => g.Key, g => g.ToList());

            var workoutHistory = dateKeys.Select(dk => new
            {
                Date = dk,
                Count = logsByDate.ContainsKey(dk) ? logsByDate[dk].Count : 0
            }).ToList();

            // 2) Muscle Groups: percentage based on sum(sets * reps) per category
            var categoryTotals = logs
                .GroupBy(l => l.Category ?? "Unknown")
                .Select(g => new
                {
                    Category = g.Key,
                    Volume = g.Sum(x => (long)x.Sets * (long)x.Reps) // sets*reps as proxy for volume
                })
                .ToList();

            long totalVolume = categoryTotals.Sum(c => c.Volume);
            var muscleGroups = categoryTotals.Select(c => new
            {
                MuscleGroup = c.Category,
                Volume = c.Volume,
                Percentage = totalVolume == 0 ? 0.0 : Math.Round((double)c.Volume / totalVolume * 100.0, 1)
            }).ToList();

            // 3) Past Workouts: grouped by date, show most recent 5 days (within the requested range)
            var pastWorkoutsGrouped = logs
                .OrderByDescending(l => l.LoggedDate)
                .GroupBy(l => GetIstDateKey(l.LoggedDate))
                .Select(g => new
                {
                    Date = DateTime.ParseExact(g.Key, "yyyy-MM-dd", CultureInfo.InvariantCulture)
                                  .ToString("dd MMM yyyy"),
                    Exercises = g.Select(x => FormatExerciseLine(x)).ToList()
                })
                .Take(5)
                .ToList();

            // 4) Raw logs (converted to IST in output)
            var rawLogs = logs
                .OrderByDescending(l => l.LoggedDate)
                .Select(l => new
                {
                    l.Id,
                    l.UserId,
                    Category = l.Category,
                    ExerciseName = l.ExerciseName,
                    l.Sets,
                    l.Reps,
                    l.Weight,
                    LoggedDateUtc = l.LoggedDate,
                    LoggedDateIst = toIst(l.LoggedDate).ToString("yyyy-MM-dd HH:mm:ss")
                })
                .ToList();

            // Final response
            var response = new
            {
                Range = new { Start = startLocal.ToString("yyyy-MM-dd"), End = endLocal.ToString("yyyy-MM-dd") },
                WorkoutHistory = workoutHistory,       // list of { Date, Count }
                MuscleGroups = muscleGroups,           // list of { MuscleGroup, Volume, Percentage }
                PastWorkouts = pastWorkoutsGrouped,    // list of last 5 days with exercises
                RawLogs = rawLogs                       // full raw logs (IST included)
            };

            return Ok(response);
        }

        // Helper to format a workout line for PastWorkouts UI
        private static string FormatExerciseLine(WorkoutLog w)
        {
            string weightPart = (w.Weight.HasValue && w.Weight.Value > 0)
                ? $" @ {w.Weight.Value}kg"
                : string.Empty;

            return $"{w.ExerciseName} - {w.Sets}×{w.Reps}{weightPart}";
        }

    }


}
