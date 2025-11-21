namespace FriendBirthdayManager.Validation;

/// <summary>
/// 友人データのバリデーション結果
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }

    public static ValidationResult Success() => new ValidationResult { IsValid = true };
    public static ValidationResult Failure(string errorMessage) => new ValidationResult { IsValid = false, ErrorMessage = errorMessage };
}

/// <summary>
/// 友人データのバリデーションを行うクラス
/// </summary>
public static class FriendValidator
{
    /// <summary>
    /// 誕生年を検証
    /// </summary>
    public static ValidationResult ValidateBirthYear(string? birthYear)
    {
        if (string.IsNullOrWhiteSpace(birthYear))
        {
            return ValidationResult.Success();
        }

        if (!int.TryParse(birthYear, out var year) || year < Constants.DateValidation.MinYear || year > Constants.DateValidation.MaxYear)
        {
            return ValidationResult.Failure($"誕生年は{Constants.DateValidation.MinYear}～{Constants.DateValidation.MaxYear}の範囲で入力してください。");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// 誕生月を検証
    /// </summary>
    public static ValidationResult ValidateBirthMonth(string? birthMonth)
    {
        if (string.IsNullOrWhiteSpace(birthMonth))
        {
            return ValidationResult.Success();
        }

        if (!int.TryParse(birthMonth, out var month) || month < Constants.DateValidation.MinMonth || month > Constants.DateValidation.MaxMonth)
        {
            return ValidationResult.Failure($"誕生月は{Constants.DateValidation.MinMonth}～{Constants.DateValidation.MaxMonth}の範囲で入力してください。");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// 誕生日を検証
    /// </summary>
    public static ValidationResult ValidateBirthDay(string? birthDay)
    {
        if (string.IsNullOrWhiteSpace(birthDay))
        {
            return ValidationResult.Success();
        }

        if (!int.TryParse(birthDay, out var day) || day < Constants.DateValidation.MinDay || day > Constants.DateValidation.MaxDay)
        {
            return ValidationResult.Failure($"誕生日は{Constants.DateValidation.MinDay}～{Constants.DateValidation.MaxDay}の範囲で入力してください。");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// 誕生日の組み合わせの妥当性を検証
    /// </summary>
    public static ValidationResult ValidateBirthdateCombination(int? birthYear, int? birthMonth, int? birthDay)
    {
        if (!birthMonth.HasValue || !birthDay.HasValue)
        {
            return ValidationResult.Success();
        }

        try
        {
            var testYear = birthYear ?? 2000;
            _ = new DateTime(testYear, birthMonth.Value, birthDay.Value);
            return ValidationResult.Success();
        }
        catch (ArgumentOutOfRangeException)
        {
            return ValidationResult.Failure("指定された誕生月と誕生日の組み合わせは無効です。");
        }
    }

    /// <summary>
    /// 通知日数インデックスを日数に変換
    /// </summary>
    public static int? ConvertNotifyIndexToDays(int notifyDaysBeforeIndex)
    {
        if (notifyDaysBeforeIndex <= 0 || notifyDaysBeforeIndex >= Constants.Notification.DaysBeforeMapping.Length)
        {
            return null; // デフォルト使用
        }

        return Constants.Notification.DaysBeforeMapping[notifyDaysBeforeIndex];
    }

    /// <summary>
    /// 通知日数を日数インデックスに変換
    /// </summary>
    public static int ConvertNotifyDaysToIndex(int? notifyDaysBefore)
    {
        if (!notifyDaysBefore.HasValue)
        {
            return 0; // デフォルト
        }

        var index = Array.IndexOf(Constants.Notification.DaysBeforeMapping, notifyDaysBefore.Value);
        return index >= 0 ? index : 0;
    }
}
