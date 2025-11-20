using System.IO;
using System.Text;
using FriendBirthdayManager.Data;
using FriendBirthdayManager.Models;
using Microsoft.Extensions.Logging;

namespace FriendBirthdayManager.Services;

/// <summary>
/// CSV エクスポート/インポートサービスの実装
/// RFC 4180準拠、UTF-8 BOM付き、CRLF改行
/// </summary>
public class CsvService : ICsvService
{
    private readonly IFriendRepository _friendRepository;
    private readonly ILogger<CsvService> _logger;

    public CsvService(IFriendRepository friendRepository, ILogger<CsvService> logger)
    {
        _friendRepository = friendRepository;
        _logger = logger;
    }

    public async Task<bool> ExportAsync(string filePath)
    {
        try
        {
            _logger.LogInformation("Exporting friends to CSV: {FilePath}", filePath);

            var friends = await _friendRepository.GetAllAsync();

            // UTF-8 BOM付きでCSVファイルを作成
            using var writer = new StreamWriter(filePath, false, new UTF8Encoding(true));

            // ヘッダー行
            await writer.WriteAsync("name,birth_year,birth_month,birth_day,aliases,memo,notify_days_before,notify_enabled,notify_sound_enabled\r\n");

            // データ行
            foreach (var friend in friends)
            {
                var line = new[]
                {
                    EscapeCsvField(friend.Name),
                    friend.BirthYear?.ToString() ?? "",
                    friend.BirthMonth?.ToString() ?? "",
                    friend.BirthDay?.ToString() ?? "",
                    // エイリアスは"|"で区切る（カンマとの競合を避けるため）
                    EscapeCsvField(string.Join("|", friend.Aliases.Select(a => a.AliasName))),
                    EscapeCsvField(friend.Memo ?? ""),
                    friend.NotifyDaysBefore?.ToString() ?? "",
                    friend.NotifyEnabled ? "1" : "0",
                    friend.NotifySoundEnabled?.ToString() ?? ""
                };

                await writer.WriteAsync(string.Join(",", line) + "\r\n");
            }

            _logger.LogInformation("Exported {Count} friends to CSV", friends.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export CSV to {FilePath}", filePath);
            return false;
        }
    }

    public async Task<ImportResult> ImportAsync(string filePath)
    {
        var errors = new List<string>();
        var successCount = 0;
        var updateCount = 0;
        var failureCount = 0;

        try
        {
            _logger.LogInformation("Importing friends from CSV: {FilePath}", filePath);

            // 既存の友人一覧を取得（名前の重複チェック用）
            var existingFriends = await _friendRepository.GetAllAsync();
            var existingFriendsDict = existingFriends.ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);
            _logger.LogInformation("Found {Count} existing friends", existingFriendsDict.Count);

            // ファイルサイズチェック（10MB以上は警告）
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > 10 * 1024 * 1024)
            {
                errors.Add("警告: ファイルサイズが10MBを超えています。処理に時間がかかる可能性があります。");
            }

            // UTF-8でCSVファイルを読み込み（BOMがあれば自動的に処理される）
            using var reader = new StreamReader(filePath, Encoding.UTF8);

            // ヘッダー行をスキップ
            var header = await reader.ReadLineAsync();
            if (header == null)
            {
                errors.Add("CSVファイルが空です");
                return new ImportResult(0, 0, 0, errors);
            }

            var lineNumber = 1;

            while (!reader.EndOfStream)
            {
                lineNumber++;
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    var fields = ParseCsvLine(line);
                    var validationError = ValidateCsvFields(fields, lineNumber);

                    if (validationError != null)
                    {
                        errors.Add(validationError);
                        failureCount++;
                        continue;
                    }

                    // 既存の友人がいるかチェック
                    var friendName = fields[0].Trim();
                    Friend friend;
                    bool isUpdate = false;

                    if (existingFriendsDict.TryGetValue(friendName, out var existingFriend))
                    {
                        // 既存の友人を更新
                        friend = existingFriend;
                        isUpdate = true;
                        _logger.LogInformation("Updating existing friend: {FriendName} at line {LineNumber}", friendName, lineNumber);
                    }
                    else
                    {
                        // 新規友人を作成
                        friend = new Friend { Name = friendName };
                    }

                    // CSVの値でマージ（CSVに値があれば上書き、なければ既存の値を保持）
                    var birthYear = ParseNullableInt(fields[1]);
                    if (birthYear.HasValue) friend.BirthYear = birthYear;

                    var birthMonth = ParseNullableInt(fields[2]);
                    if (birthMonth.HasValue) friend.BirthMonth = birthMonth;

                    var birthDay = ParseNullableInt(fields[3]);
                    if (birthDay.HasValue) friend.BirthDay = birthDay;

                    if (!string.IsNullOrWhiteSpace(fields[5])) friend.Memo = fields[5];

                    var notifyDaysBefore = ParseNullableInt(fields[6]);
                    if (notifyDaysBefore.HasValue) friend.NotifyDaysBefore = notifyDaysBefore;

                    if (!string.IsNullOrWhiteSpace(fields[7])) friend.NotifyEnabled = fields[7] == "1";

                    if (!string.IsNullOrWhiteSpace(fields[8]))
                        friend.NotifySoundEnabled = fields[8] == "1" ? true : fields[8] == "0" ? false : null;

                    // エイリアスをマージ（重複しないように）
                    if (!string.IsNullOrWhiteSpace(fields[4]))
                    {
                        var newAliases = fields[4].Split('|', StringSplitOptions.RemoveEmptyEntries);
                        var existingAliasNames = new HashSet<string>(friend.Aliases.Select(a => a.AliasName), StringComparer.OrdinalIgnoreCase);

                        foreach (var alias in newAliases)
                        {
                            var aliasName = alias.Trim();
                            if (!existingAliasNames.Contains(aliasName))
                            {
                                friend.Aliases.Add(new Alias { AliasName = aliasName });
                            }
                        }
                    }

                    // データベースに追加または更新
                    if (isUpdate)
                    {
                        await _friendRepository.UpdateAsync(friend);
                        updateCount++;
                    }
                    else
                    {
                        await _friendRepository.AddAsync(friend);
                        existingFriendsDict.Add(friendName, friend);
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to import line {LineNumber}", lineNumber);
                    errors.Add($"行 {lineNumber}: {ex.Message}");
                    failureCount++;
                }
            }

            _logger.LogInformation("Import completed: {SuccessCount} new, {UpdateCount} updated, {FailureCount} failure",
                successCount, updateCount, failureCount);

            return new ImportResult(successCount, updateCount, failureCount, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import CSV from {FilePath}", filePath);
            errors.Add($"ファイル読み込みエラー: {ex.Message}");
            return new ImportResult(successCount, updateCount, failureCount, errors);
        }
    }

    /// <summary>
    /// CSVフィールドをエスケープ（RFC 4180準拠）
    /// Excel数式インジェクション対策も実施
    /// </summary>
    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        // Excel数式インジェクション対策: =, +, -, @ で始まる場合は先頭にシングルクォートを追加
        if (field.StartsWith("=") || field.StartsWith("+") || field.StartsWith("-") || field.StartsWith("@"))
        {
            field = "'" + field;
        }

        // カンマ、ダブルクォート、改行を含む場合はダブルクォートで囲む
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            // ダブルクォートを2つにエスケープ
            field = field.Replace("\"", "\"\"");
            return $"\"{field}\"";
        }

        return field;
    }

    /// <summary>
    /// CSV行をパース（RFC 4180準拠）
    /// </summary>
    private List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var currentField = new StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // 次の文字もダブルクォートの場合はエスケープされたダブルクォート
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++; // 次のダブルクォートをスキップ
                    }
                    else
                    {
                        // ダブルクォート終了
                        inQuotes = false;
                    }
                }
                else
                {
                    currentField.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    // ダブルクォート開始
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    // フィールド区切り
                    fields.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }
        }

        // 最後のフィールドを追加
        fields.Add(currentField.ToString());

        // フィールド数が足りない場合は空文字列で埋める
        while (fields.Count < 9)
        {
            fields.Add("");
        }

        return fields;
    }

    /// <summary>
    /// CSVフィールドをバリデーション
    /// </summary>
    private string? ValidateCsvFields(List<string> fields, int lineNumber)
    {
        // 必須チェック: name
        if (string.IsNullOrWhiteSpace(fields[0]))
            return $"行 {lineNumber}: 名前は必須です";

        // 長さチェック
        if (fields[0].Length > 200)
            return $"行 {lineNumber}: 名前が長すぎます（最大200文字）";

        // birth_year 検証
        if (!string.IsNullOrWhiteSpace(fields[1]))
        {
            if (!int.TryParse(fields[1], out int year) || year < 1900 || year > 2100)
                return $"行 {lineNumber}: 誕生年が不正です（1900-2100）";
        }

        // birth_month 検証
        if (!string.IsNullOrWhiteSpace(fields[2]))
        {
            if (!int.TryParse(fields[2], out int month) || month < 1 || month > 12)
                return $"行 {lineNumber}: 誕生月が不正です（1-12）";
        }

        // birth_day 検証
        if (!string.IsNullOrWhiteSpace(fields[3]))
        {
            if (!int.TryParse(fields[3], out int day) || day < 1 || day > 31)
                return $"行 {lineNumber}: 誕生日が不正です（1-31）";
        }

        // notify_days_before 検証
        if (!string.IsNullOrWhiteSpace(fields[6]))
        {
            if (!int.TryParse(fields[6], out int days) || days < 1 || days > 30)
                return $"行 {lineNumber}: 通知日数が不正です（1-30）";
        }

        // notify_enabled 検証
        if (!string.IsNullOrWhiteSpace(fields[7]))
        {
            if (fields[7] != "0" && fields[7] != "1")
                return $"行 {lineNumber}: 通知有効フラグが不正です（0 or 1）";
        }

        // notify_sound_enabled 検証
        if (!string.IsNullOrWhiteSpace(fields[8]))
        {
            if (fields[8] != "0" && fields[8] != "1")
                return $"行 {lineNumber}: 音声通知フラグが不正です（0 or 1）";
        }

        return null;
    }

    /// <summary>
    /// Nullable int をパース
    /// </summary>
    private int? ParseNullableInt(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (int.TryParse(value, out int result))
            return result;

        return null;
    }
}
