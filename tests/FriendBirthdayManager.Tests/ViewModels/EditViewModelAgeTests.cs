using FluentAssertions;
using FriendBirthdayManager.Data;
using FriendBirthdayManager.Services;
using FriendBirthdayManager.Validation;
using FriendBirthdayManager.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FriendBirthdayManager.Tests.ViewModels;

public class EditViewModelAgeTests
{
    private static EditViewModel CreateViewModel() => new(
        Mock.Of<IFriendRepository>(),
        Mock.Of<ILocalizationService>(),
        Mock.Of<IServiceProvider>(),
        Mock.Of<ILogger<EditViewModel>>());

    [Fact]
    public void 年齢を入力すると生まれ年が自動設定される()
    {
        var vm = CreateViewModel();

        vm.Age = "35";

        FriendValidator.TryCalculateBirthYearFromAge("35", null, null, DateTime.Today, out var expected);
        vm.BirthYear.Should().Be(expected.ToString());
    }

    [Fact]
    public void 年齢入力後に月日を入力すると生まれ年が再計算される()
    {
        var vm = CreateViewModel();

        vm.Age = "35";
        vm.BirthMonth = "12";
        vm.BirthDay = "25";

        FriendValidator.TryCalculateBirthYearFromAge("35", "12", "25", DateTime.Today, out var expected);
        vm.BirthYear.Should().Be(expected.ToString());
    }

    [Fact]
    public void 生まれ年を手動編集すると年齢入力はクリアされる()
    {
        var vm = CreateViewModel();
        vm.Age = "35";

        vm.BirthYear = "1980";

        vm.Age.Should().BeNull();
        vm.BirthYear.Should().Be("1980");
    }

    [Fact]
    public void 無効な年齢では生まれ年は変更されない()
    {
        var vm = CreateViewModel();
        vm.BirthYear = "1990";

        vm.Age = "abc";

        vm.BirthYear.Should().Be("1990");
    }
}
