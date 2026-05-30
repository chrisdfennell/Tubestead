using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tubestead.Api.Contracts;
using Tubestead.Domain;
using Tubestead.Infrastructure.Data;
using Tubestead.Infrastructure.Storage;

namespace Tubestead.Api.Controllers;

[ApiController]
[Route("api/videos")]
[Authorize]
public class VideosController(TubesteadDbContext db, IStorageService storage) : ControllerBase
{
    private bool IsAdmin => User.IsInRole(Roles.Admin);

    /// <summary>Library listing. Viewers see ready videos; admins also see in-progress
    /// and failed ones so they can monitor processing.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<VideoListItemDto>>> List(
        [FromQuery] string? search, CancellationToken ct)
    {
        var query = db.Videos.AsNoTracking();

        if (!IsAdmin)
            query = query.Where(v => v.Status == VideoStatus.Ready);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(v => EF.Functions.Like(v.Title, $"%{term}%"));
        }

        var rows = await query
            .OrderByDescending(v => v.CreatedUtc)
            .Select(v => new
            {
                v.Id, v.Title, v.Status, v.StatusMessage, v.DurationSeconds, v.ThumbnailPath, v.CreatedUtc,
            })
            .ToListAsync(ct);

        var items = rows.Select(v => new VideoListItemDto(
            v.Id, v.Title, v.Status, v.StatusMessage, v.DurationSeconds,
            v.ThumbnailPath == null ? null : $"/api/videos/{v.Id}/thumbnail",
            v.CreatedUtc));

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VideoDetailDto>> Get(Guid id, CancellationToken ct)
    {
        var video = await db.Videos.AsNoTracking()
            .Include(v => v.Renditions)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

        if (video is null)
            return NotFound();

        if (!IsAdmin && video.Status != VideoStatus.Ready)
            return NotFound();

        var dto = new VideoDetailDto(
            video.Id, video.Title, video.Description, video.Status, video.StatusMessage,
            video.DurationSeconds, video.Width, video.Height,
            video.ThumbnailPath == null ? null : $"/api/videos/{video.Id}/thumbnail",
            video.OriginalFileName, video.CreatedUtc,
            video.Renditions
                .OrderByDescending(r => r.IsOriginal)
                .ThenByDescending(r => r.Height)
                .Select(r => new RenditionDto(r.Id, r.Label, r.Format, r.Width, r.Height, r.SizeBytes, r.IsOriginal))
                .ToList());

        return Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var video = await db.Videos.FirstOrDefaultAsync(v => v.Id == id, ct);
        if (video is null)
            return NotFound();

        db.Videos.Remove(video);
        await db.SaveChangesAsync(ct);
        await storage.DeleteVideoDirectoryAsync(id, ct);

        return NoContent();
    }
}
