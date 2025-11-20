using FriendBirthdayManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FriendBirthdayManager.Data;

/// <summary>
/// 友人情報のリポジトリ実装
/// </summary>
public class FriendRepository : IFriendRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<FriendRepository> _logger;

    public FriendRepository(AppDbContext context, ILogger<FriendRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Friend>> GetAllAsync()
    {
        try
        {
            return await _context.Friends
                .Include(f => f.Aliases)
                .OrderBy(f => f.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all friends");
            throw;
        }
    }

    public async Task<Friend?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.Friends
                .Include(f => f.Aliases)
                .FirstOrDefaultAsync(f => f.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get friend by ID: {FriendId}", id);
            throw;
        }
    }

    public async Task<int> AddAsync(Friend friend)
    {
        try
        {
            friend.CreatedAt = DateTime.UtcNow;
            friend.UpdatedAt = DateTime.UtcNow;

            _context.Friends.Add(friend);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Added friend: {FriendName} (ID: {FriendId})", friend.Name, friend.Id);
            return friend.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add friend: {FriendName}", friend.Name);
            throw;
        }
    }

    public async Task UpdateAsync(Friend friend)
    {
        try
        {
            friend.UpdatedAt = DateTime.UtcNow;

            _context.Friends.Update(friend);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated friend: {FriendName} (ID: {FriendId})", friend.Name, friend.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update friend: {FriendName} (ID: {FriendId})", friend.Name, friend.Id);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var friend = await _context.Friends.FindAsync(id);
            if (friend != null)
            {
                _context.Friends.Remove(friend);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted friend: {FriendName} (ID: {FriendId})", friend.Name, friend.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete friend with ID: {FriendId}", id);
            throw;
        }
    }

    public async Task<List<Friend>> SearchAsync(string keyword)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return await GetAllAsync();
            }

            // 特殊検索パターンをチェック（月:数字、日:数字）
            var monthMatch = System.Text.RegularExpressions.Regex.Match(keyword, @"月[:\s]*(\d+)");
            var dayMatch = System.Text.RegularExpressions.Regex.Match(keyword, @"日[:\s]*(\d+)");

            // 誕生月または誕生日での検索
            if (monthMatch.Success || dayMatch.Success)
            {
                IQueryable<Friend> query = _context.Friends;

                if (monthMatch.Success && int.TryParse(monthMatch.Groups[1].Value, out int month))
                {
                    if (month >= 1 && month <= 12)
                    {
                        query = query.Where(f => f.BirthMonth == month);
                        _logger.LogInformation("Searching by birth month: {Month}", month);
                    }
                }

                if (dayMatch.Success && int.TryParse(dayMatch.Groups[1].Value, out int day))
                {
                    if (day >= 1 && day <= 31)
                    {
                        query = query.Where(f => f.BirthDay == day);
                        _logger.LogInformation("Searching by birth day: {Day}", day);
                    }
                }

                return await query.Include(f => f.Aliases).ToListAsync();
            }

            // 通常の検索（名前、メモ、エイリアス）
            var friendIdsFromNameOrMemo = await _context.Friends
                .Where(f => f.Name.Contains(keyword) || (f.Memo != null && f.Memo.Contains(keyword)))
                .Select(f => f.Id)
                .ToListAsync();

            // エイリアスから検索
            var friendIdsFromAlias = await _context.Aliases
                .Where(a => a.AliasName.Contains(keyword))
                .Select(a => a.FriendId)
                .Distinct()
                .ToListAsync();

            // 結果をマージ
            var allFriendIds = friendIdsFromNameOrMemo.Union(friendIdsFromAlias).Distinct();

            var friends = await _context.Friends
                .Include(f => f.Aliases)
                .Where(f => allFriendIds.Contains(f.Id))
                .ToListAsync();

            _logger.LogInformation("Search completed for keyword: '{Keyword}', found {Count} friends", keyword, friends.Count);
            return friends;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search friends with keyword: {Keyword}", keyword);
            throw;
        }
    }

    public async Task<List<Friend>> GetUpcomingBirthdaysAsync(DateTime referenceDate, int count)
    {
        try
        {
            var friends = await _context.Friends
                .Include(f => f.Aliases)
                .Where(f => f.BirthMonth != null && f.BirthDay != null)
                .ToListAsync();

            // 日数を計算してソート
            var friendsWithDays = friends
                .Select(f => new
                {
                    Friend = f,
                    DaysUntil = f.CalculateDaysUntilBirthday(referenceDate)
                })
                .Where(x => x.DaysUntil.HasValue)
                .OrderBy(x => x.DaysUntil)
                .ThenBy(x => x.Friend.Name)
                .Take(count)
                .Select(x => x.Friend)
                .ToList();

            _logger.LogInformation("Retrieved {Count} upcoming birthdays", friendsWithDays.Count);
            return friendsWithDays;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get upcoming birthdays");
            throw;
        }
    }

    public async Task<List<Friend>> GetNotificationTargetsAsync(DateTime targetDate, int daysBefore)
    {
        try
        {
            var friends = await _context.Friends
                .Include(f => f.Aliases)
                .Where(f => f.NotifyEnabled && f.BirthMonth != null && f.BirthDay != null)
                .ToListAsync();

            // 通知対象をフィルタリング
            var targets = friends
                .Where(f =>
                {
                    var daysUntil = f.CalculateDaysUntilBirthday(targetDate);
                    return daysUntil.HasValue && daysUntil.Value >= 0 && daysUntil.Value <= daysBefore;
                })
                .ToList();

            _logger.LogInformation("Found {Count} notification targets for date: {Date}, daysBefore: {DaysBefore}",
                targets.Count, targetDate.ToString("yyyy-MM-dd"), daysBefore);

            return targets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification targets");
            throw;
        }
    }

}
