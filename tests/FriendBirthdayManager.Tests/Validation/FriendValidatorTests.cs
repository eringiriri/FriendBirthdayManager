using FluentAssertions;
using FriendBirthdayManager.Validation;
using Xunit;

namespace FriendBirthdayManager.Tests.Validation;

public class FriendValidatorTests
{
    // 基準日: 2026-07-16
    private static readonly DateTime Today = new(2026, 7, 16);

    [Fact]
    public void TryCalculateBirthYearFromAge_今年の誕生日が過ぎている場合_今年から年齢を引く()
    {
        var result = FriendValidator.TryCalculateBirthYearFromAge("35", "3", "10", Today, out var birthYear);

        result.Should().BeTrue();
        birthYear.Should().Be(1991); // 2026 - 35
    }

    [Fact]
    public void TryCalculateBirthYearFromAge_今年の誕生日がまだの場合_さらに1年引く()
    {
        var result = FriendValidator.TryCalculateBirthYearFromAge("35", "12", "25", Today, out var birthYear);

        result.Should().BeTrue();
        birthYear.Should().Be(1990); // 2026 - 35 - 1
    }

    [Fact]
    public void TryCalculateBirthYearFromAge_今日が誕生日の場合_今年から年齢を引く()
    {
        var result = FriendValidator.TryCalculateBirthYearFromAge("35", "7", "16", Today, out var birthYear);

        result.Should().BeTrue();
        birthYear.Should().Be(1991); // 誕生日当日は年齢が既に上がっている扱い
    }

    [Fact]
    public void TryCalculateBirthYearFromAge_月日が未入力の場合_今年から年齢を引く()
    {
        var result = FriendValidator.TryCalculateBirthYearFromAge("35", null, null, Today, out var birthYear);

        result.Should().BeTrue();
        birthYear.Should().Be(1991);
    }

    [Fact]
    public void TryCalculateBirthYearFromAge_月のみ入力の場合_補正せず今年から年齢を引く()
    {
        var result = FriendValidator.TryCalculateBirthYearFromAge("35", "12", null, Today, out var birthYear);

        result.Should().BeTrue();
        birthYear.Should().Be(1991);
    }

    [Fact]
    public void TryCalculateBirthYearFromAge_2月29日生まれで平年の場合_2月28日として補正する()
    {
        // 2026年は平年。基準日3/1時点で2/29(→2/28扱い)の誕生日は過ぎている
        var result = FriendValidator.TryCalculateBirthYearFromAge("30", "2", "29", new DateTime(2026, 3, 1), out var birthYear);

        result.Should().BeTrue();
        birthYear.Should().Be(1996); // 2026 - 30
    }

    [Fact]
    public void TryCalculateBirthYearFromAge_年齢0歳を許容する()
    {
        var result = FriendValidator.TryCalculateBirthYearFromAge("0", "1", "1", Today, out var birthYear);

        result.Should().BeTrue();
        birthYear.Should().Be(2026);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("-1")]
    [InlineData("151")]
    [InlineData("12.5")]
    public void TryCalculateBirthYearFromAge_無効な年齢はfalseを返す(string? age)
    {
        var result = FriendValidator.TryCalculateBirthYearFromAge(age, "7", "16", Today, out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void TryCalculateBirthYearFromAge_計算結果が1900年未満になる年齢はfalseを返す()
    {
        // 2026年に150歳 → 1876年は保存可能範囲（1900-2100）外
        var result = FriendValidator.TryCalculateBirthYearFromAge("150", null, null, Today, out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void TryCalculateBirthYearFromAge_誕生日補正で1900年未満になる場合もfalseを返す()
    {
        // 2026 - 126 = 1900だが、誕生日がまだなら1899になり範囲外
        var result = FriendValidator.TryCalculateBirthYearFromAge("126", "12", "25", Today, out _);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("6", "31")]  // 6月31日は存在しない
    [InlineData("2", "30")]  // 2月30日は存在しない
    public void TryCalculateBirthYearFromAge_無効な月日の組み合わせは年のみで計算する(string month, string day)
    {
        var result = FriendValidator.TryCalculateBirthYearFromAge("35", month, day, Today, out var birthYear);

        result.Should().BeTrue();
        birthYear.Should().Be(1991); // 補正なしで 2026 - 35
    }
}
