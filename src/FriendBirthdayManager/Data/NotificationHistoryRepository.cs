using FriendBirthdayManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FriendBirthdayManager.Data;

/// <summary>
/// 通知履歴のリポジトリ実装
/// </summary>
public class NotificationHistoryRepository : INotificationHistoryRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<NotificationHistoryRepository> _logger;

    public NotificationHistoryRepository(AppDbContext context, ILogger<NotificationHistoryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddAsync(int friendId, string notificationDate)
    {
        try
        {
            var history = new NotificationHistory
            {
                FriendId = friendId,
                NotificationDate = notificationDate,
                NotifiedAt = DateTime.UtcNow
            };

            _context.NotificationHistories.Add(history);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Added notification history: FriendId={FriendId}, Date={Date}",
                friendId, notificationDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add notification history: FriendId={FriendId}, Date={Date}",
                friendId, notificationDate);
            throw;
        }
    }

    public async Task<bool> IsNotifiedAsync(int friendId, string notificationDate)
    {
        try
        {
            var exists = await _context.NotificationHistories
                .AnyAsync(h => h.FriendId == friendId && h.NotificationDate == notificationDate);

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check notification history: FriendId={FriendId}, Date={Date}",
                friendId, notificationDate);
            return false;
        }
    }

    public async Task CleanupOldHistoryAsync()
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.Date.AddDays(-30).ToString("yyyy-MM-dd");

            var oldHistories = await _context.NotificationHistories
                .Where(h => string.Compare(h.NotificationDate, cutoffDate) < 0)
                .ToListAsync();

            if (oldHistories.Count > 0)
            {
                _context.NotificationHistories.RemoveRange(oldHistories);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cleaned up {Count} old notification history records", oldHistories.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old notification history");
        }
    }
}
