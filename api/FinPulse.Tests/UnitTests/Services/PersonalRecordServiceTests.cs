using FluentAssertions;
using FinPulse.Api.Services;
using FinPulse.Api.DTOs;
using FinPulse.Tests.Helpers;
using FinPulse.Tests.Helpers.Builders;

namespace FinPulse.Tests.UnitTests.Services;

public class PersonalRecordServiceTests : ServiceTestBase
{
    private readonly PersonalRecordService _sut;

    public PersonalRecordServiceTests()
    {
        _sut = new PersonalRecordService(Context);
    }

    #region CreatePersonalRecordAsync Tests

    [Fact]
    public async Task CreatePersonalRecordAsync_WithValidRequest_CreatesSuccessfully()
    {
        var userId = 1;
        var request = new CreatePersonalRecordRequest
        {
            ExerciseName = "Bench Press",
            MetricType = "Max Weight",
            Value = 100m,
            Unit = "kg",
            AchievedDate = DateTime.UtcNow.Date
        };

        var result = await _sut.CreatePersonalRecordAsync(userId, request);

        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.UserId.Should().Be(userId);
        result.ExerciseName.Should().Be("Bench Press");
        result.Value.Should().Be(100m);

        var record = await Context.PersonalRecords.FindAsync(result.Id);
        record!.Status.Should().Be(1);
    }

    #endregion

    #region GetUserPersonalRecordsAsync Tests

    [Fact]
    public async Task GetUserPersonalRecordsAsync_ReturnsOnlyUserRecords()
    {
        var userId = 1;
        var otherUserId = 2;

        await Context.PersonalRecords.AddRangeAsync(
            new PersonalRecordBuilder().WithUserId(userId).Build(),
            new PersonalRecordBuilder().WithUserId(userId).Build());
        await Context.PersonalRecords.AddAsync(new PersonalRecordBuilder().WithUserId(otherUserId).Build());
        await Context.SaveChangesAsync();

        var result = await _sut.GetUserPersonalRecordsAsync(userId);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(r => r.UserId.Should().Be(userId));
    }

    [Fact]
    public async Task GetUserPersonalRecordsAsync_FiltersOutDeletedRecords()
    {
        var userId = 1;
        var active = new PersonalRecordBuilder().WithUserId(userId).AsActive().Build();
        var deleted = new PersonalRecordBuilder().WithUserId(userId).AsDeleted().Build();

        await Context.PersonalRecords.AddAsync(active);
        await Context.PersonalRecords.AddAsync(deleted);
        await Context.SaveChangesAsync();

        var result = await _sut.GetUserPersonalRecordsAsync(userId);

        result.Should().HaveCount(1);
        result.Should().NotContain(r => r.Id == deleted.Id);
    }

    [Fact]
    public async Task GetUserPersonalRecordsAsync_WhenNoRecords_ReturnsEmptyList()
    {
        var result = await _sut.GetUserPersonalRecordsAsync(1);
        result.Should().BeEmpty();
    }

    #endregion

    // Note: PersonalRecord is append-only by design (Decision 2, DESIGN_BODY_MODULE_API.md) —
    // IPersonalRecordService has no Update/Delete methods, so no such tests exist here.
}
