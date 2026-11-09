using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GhiChuPro.Data;
using GhiChuPro.Models;

namespace GhiChuPro.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public HomeController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var notes = await _context.Notes
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.UpdatedAt)
            .ToListAsync();

        return View(notes);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string title, string content, string color, bool isPinned, DateTime? startDate, DateTime? endDate, int progress)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(content))
        {
            return RedirectToAction(nameof(Index));
        }

        // Clamp progress between 0 and 100
        progress = Math.Clamp(progress, 0, 100);

        var note = new Note
        {
            UserId = userId,
            Title = title?.Trim() ?? string.Empty,
            Content = content?.Trim() ?? string.Empty,
            Color = string.IsNullOrEmpty(color) ? "#ffffff" : color,
            IsPinned = isPinned,
            StartDate = startDate,
            EndDate = endDate,
            Progress = progress,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string title, string content, string color, bool isPinned, DateTime? startDate, DateTime? endDate, int progress)
    {
        var userId = _userManager.GetUserId(User);
        var note = await _context.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (note == null) return NotFound();

        progress = Math.Clamp(progress, 0, 100);

        note.Title = title?.Trim() ?? string.Empty;
        note.Content = content?.Trim() ?? string.Empty;
        note.Color = string.IsNullOrEmpty(color) ? "#ffffff" : color;
        note.IsPinned = isPinned;
        note.StartDate = startDate;
        note.EndDate = endDate;
        note.Progress = progress;
        note.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickUpdateProgress(int id, int progress)
    {
        var userId = _userManager.GetUserId(User);
        var note = await _context.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (note != null)
        {
            note.Progress = Math.Clamp(progress, 0, 100);
            note.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePin(int id)
    {
        var userId = _userManager.GetUserId(User);
        var note = await _context.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (note != null)
        {
            note.IsPinned = !note.IsPinned;
            note.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User);
        var note = await _context.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (note != null)
        {
            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
