using FurManual.Data;
using FurManual.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IO;
using System.Linq;

namespace FurManual.Pages
{
    public class LecturesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        private const long MaxFileSize = 50L * 1024 * 1024; // 50 MB
        public const int PageSize = 15;

        public LecturesModel(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public IList<Lecture> Lectures { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortOrder { get; set; } = "newest";

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int TotalPages { get; set; }

        [BindProperty]
        public IFormFile? UploadedFile { get; set; }

        [BindProperty]
        public string? NewLectureTitle { get; set; }

        [BindProperty]
        public int? LectureId { get; set; }

        public async Task OnGetAsync()
        {
            await LoadLecturesAsync();
        }

        public async Task<IActionResult> OnGetGridAsync()
        {
            await LoadLecturesAsync();
            return Partial("_LecturesGrid", this);
        }

        private async Task LoadLecturesAsync()
        {
            var query = _context.Lectures
                .Include(l => l.CreatedBy)
                .Include(l => l.UpdatedBy)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = $"%{SearchTerm}%";

                query = query.Where(l => EF.Functions.ILike(l.Title, term) ||
                                         (l.OriginalFileName != null && EF.Functions.ILike(l.OriginalFileName, term)));
            }

            query = SortOrder switch
            {
                "oldest" => query.OrderBy(l => l.CreatedAt),
                _ => query.OrderByDescending(l => l.CreatedAt),
            };

            var totalItems = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

            if (CurrentPage < 1) CurrentPage = 1;
            if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

            Lectures = await query
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (User.Identity?.IsAuthenticated != true) return Forbid();

            ModelState.Remove(nameof(SearchTerm));
            ModelState.Remove(nameof(SortOrder));
            ModelState.Remove(nameof(CurrentPage));

            bool isEdit = LectureId.HasValue && LectureId.Value > 0;
            Lecture? existingLecture = null;

            if (isEdit)
            {
                existingLecture = await _context.Lectures.FindAsync(LectureId.GetValueOrDefault());
                if (existingLecture == null) return NotFound();
            }

            if (string.IsNullOrWhiteSpace(NewLectureTitle))
            {
                ModelState.AddModelError(nameof(NewLectureTitle), "��������� ��������.");
                return await RefreshPage();
            }
            if (NewLectureTitle.Length > 100)
            {
                ModelState.AddModelError(nameof(NewLectureTitle), "�������� ������� ������� (����. 100 ��������).");
                return await RefreshPage();
            }

            if (!isEdit && UploadedFile == null)
            {
                ModelState.AddModelError(nameof(UploadedFile), "�������� ���� ��� ��������.");
                return await RefreshPage();
            }

            string? finalFilePath = null;
            string? finalOriginalName = null;

            string? newPhysicalFilePath = null;
            string? oldPhysicalFilePathToDelete = null;

            // --- ���� ���������� ����� �� ���� ---
            if (UploadedFile != null)
            {
                if (UploadedFile.Length > MaxFileSize)
                {
                    ModelState.AddModelError(nameof(UploadedFile), "���� ������� ������� (����. 50 ��).");
                    return await RefreshPage();
                }

                var extension = System.IO.Path.GetExtension(UploadedFile.FileName).ToLower();
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(nameof(UploadedFile), "�������� ������ �����.");
                    return await RefreshPage();
                }

                using (var stream = UploadedFile.OpenReadStream())
                {
                    var signatures = new Dictionary<string, byte[][]>
                    {
                        { ".pdf", new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } } },
                        { ".docx", new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
                        { ".doc", new[] { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } } }
                    };

                    var headerBytes = new byte[8];
                    int bytesRead = stream.Read(headerBytes, 0, headerBytes.Length);

                    string? realExtension = null;
                    foreach (var kvp in signatures)
                    {
                        if (kvp.Value.Any(sig => headerBytes.Take(sig.Length).SequenceEqual(sig)))
                        {
                            realExtension = kvp.Key;
                            break;
                        }
                    }

                    if (realExtension == null)
                    {
                        ModelState.AddModelError(nameof(UploadedFile), "���������� ����� �� ������������� �� ������ �� �������������� �������� (PDF, Word).");
                        return await RefreshPage();
                    }

                    stream.Position = 0;

                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "lectures");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = $"{Guid.NewGuid()}{realExtension}";

                    newPhysicalFilePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(newPhysicalFilePath, FileMode.Create))
                    {
                        await stream.CopyToAsync(fileStream);
                    }

                    finalFilePath = "/uploads/lectures/" + uniqueFileName;
                    finalOriginalName = UploadedFile.FileName;
                }
            }

            // --- ���� ���������� ������ ��� �� ---
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int currentUserId)) return Forbid();

            if (isEdit && existingLecture != null)
            {
                existingLecture.Title = NewLectureTitle.Trim();
                existingLecture.UpdatedAt = DateTime.UtcNow;
                existingLecture.UpdatedById = currentUserId;

                if (finalFilePath != null)
                {
                    var oldRelativePath = existingLecture.FilePath.TrimStart('/').Replace('/', System.IO.Path.DirectorySeparatorChar);
                    oldPhysicalFilePathToDelete = System.IO.Path.Combine(_environment.WebRootPath, oldRelativePath);

                    existingLecture.FilePath = finalFilePath;
                    existingLecture.OriginalFileName = finalOriginalName;
                }

                _context.Lectures.Update(existingLecture);
            }
            else
            {
                var lecture = new Lecture
                {
                    Title = NewLectureTitle.Trim(),
                    FilePath = finalFilePath!,
                    OriginalFileName = finalOriginalName,
                    CreatedById = currentUserId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Lectures.Add(lecture);
            }

            // --- ���������� ���������� � �� � ������� ������ ---
            try
            {
                await _context.SaveChangesAsync();

                if (oldPhysicalFilePathToDelete != null && System.IO.File.Exists(oldPhysicalFilePathToDelete))
                {
                    System.IO.File.Delete(oldPhysicalFilePathToDelete);
                }
            }
            catch (Exception)
            {
                if (newPhysicalFilePath != null && System.IO.File.Exists(newPhysicalFilePath))
                {
                    System.IO.File.Delete(newPhysicalFilePath);
                }

                ModelState.AddModelError("", "��������� ������ ��� ���������� ������ � ����. ����������, ���������� ��� ���.");
                return await RefreshPage();
            }

            return RedirectToPage(new { SortOrder, SearchTerm, CurrentPage });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            if (User.Identity?.IsAuthenticated != true) return Forbid();

            var lecture = await _context.Lectures.FindAsync(id);
            if (lecture != null)
            {
                var relativePath = lecture.FilePath.TrimStart('/').Replace('/', System.IO.Path.DirectorySeparatorChar);
                var fullPath = System.IO.Path.Combine(_environment.WebRootPath, relativePath);

                _context.Lectures.Remove(lecture);

                try
                {
                    await _context.SaveChangesAsync();

                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "�� ������� ������� ������ �� ���� ������. ����������, ���������� �����.");
                    return await RefreshPage();
                }
            }

            return RedirectToPage(new { SearchTerm, SortOrder, CurrentPage });
        }

        public async Task<IActionResult> OnGetDownloadAsync(int id)
        {
            var lecture = await _context.Lectures.FindAsync(id);
            if (lecture == null)
            {
                return NotFound();
            }

            var relativePath = lecture.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var filePath = Path.Combine(_environment.WebRootPath, relativePath);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            string contentType = "application/octet-stream";
            var extension = Path.GetExtension(filePath).ToLower();

            switch (extension)
            {
                case ".pdf": contentType = "application/pdf"; break;
                case ".doc": contentType = "application/msword"; break;
                case ".docx": contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"; break;
            }

            return PhysicalFile(filePath, contentType, lecture.OriginalFileName);
        }

        private async Task<PageResult> RefreshPage()
        {
            await OnGetAsync();
            return Page();
        }
    }
}