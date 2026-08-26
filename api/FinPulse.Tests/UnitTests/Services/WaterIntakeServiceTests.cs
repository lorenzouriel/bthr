using FluentAssertions;
using FinPulse.Api.Services;
using FinPulse.Api.DTOs;
using FinPulse.Tests.Helpers;
using FinPulse.Tests.Helpers.Builders;

namespace FinPulse.Tests.UnitTests.Services;

public class WaterIntakeServiceTests : ServiceTestBase
{
    private readonly WaterIntakeService _sut;

    public WaterIntakeServiceTests()
    {
        _sut = new WaterIntakeService(Context);
    }

    #region CreateWaterIntakeAsync Tests

    [Fact]
    public async Task CreateWaterIntakeAsync_WithValidRequest_CreatesSuccessfully()
    {
        var userId = 1;
        var request = new CreateWaterIntakeRequest
        {
            IntakeDate = DateTime.UtcNow.Date,
            AmountMl = 500
        };

        var result = await _sut.CreateWaterIntakeAsync(userId, request);

        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.UserId.Should().Be(userId);
        result.AmountMl.Should().Be(500);

        var record = await Context.WaterIntakes.FindAsync(result.Id);
        record!.Status.Should().Be(1);
    }

    #endregion

    #region GetUserWaterIntakeAsync Tests

    [Fact]
    public async Task GetUserWaterIntakeAsync_ReturnsOnlyUserRecords()
    {
        var userId = 1;
        var otherUserId = 2;

        await Context.WaterIntakes.AddRangeAsync(
            new WaterIntakeBuilder().WithUserId(userId).WithIntakeDate(new DateTime(2026, 1, 1)).Build(),
            new WaterIntakeBuilder().WithUserId(userId).WithIntakeDate(new DateTime(2026, 1, 2)).Build());
        await Context.WaterIntakes.AddAsync(new WaterIntakeBuilder().WithUserId(otherUserId).Build());
        await Context.SaveChangesAsync();

        var result = await _sut.GetUserWaterIntakeAsync(userId);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(w => w.UserId.Should().Be(userId));
    }

    [Fact]
    public async Task GetUserWaterIntakeAsync_FiltersOutDeletedRecords()
    {
        var userId = 1;
        var active = new WaterIntakeBuilder().WithUserId(userId).AsActive().Build();
        var deleted = new WaterIntakeBuilder().WithUserId(userId).AsDeleted().Build();

        await Context.WaterIntakes.AddAsync(active);
        await Context.WaterIntakes.AddAsync(deleted);
        await Context.SaveChangesAsync();

        var result = await _sut.GetUserWaterIntakeAsync(userId);

        result.Should().HaveCount(1);
        result.Should().NotContain(w => w.Id == deleted.Id);
    }

    #endregion

    #region UpdateWaterIntakeAsync Tests

    [Fact]
    public async Task UpdateWaterIntakeAsync_WithValidRequest_UpdatesSuccessfully()
    {
        var userId = 1;
        var record = new WaterIntakeBuilder().WithUserId(userId).Build();
        await Context.WaterIntakes.AddAsync(record);
        await Context.SaveChangesAsync();

        var request = new UpdateWaterIntakeRequest { AmountMl = 2000 };
        var result = await _sut.UpdateWaterIntakeAsync(userId, record.Id, request);

        result.Should().NotBeNull();
        result!.AmountMl.Should().Be(2000);
    }

    [Fact]
    public async Task UpdateWaterIntakeAsync_WithNonExistentRecord_ReturnsNull()
    {
        var result = await _sut.UpdateWaterIntakeAsync(1, 999, new UpdateWaterIntakeRequest());
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateWaterIntakeAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        var ownerId = 1;
        var otherUserId = 2;
        var record = new WaterIntakeBuilder().WithUserId(ownerId).Build();
        await Context.WaterIntakes.AddAsync(record);
        await Context.SaveChangesAsync();

        var act = async () => await _sut.UpdateWaterIntakeAsync(otherUserId, record.Id, new UpdateWaterIntakeRequest());

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to update this water intake record");
    }

    #endregion

    #region DeleteWaterIntakeAsync Tests

    [Fact]
    public async Task DeleteWaterIntakeAsync_SoftDeletesRecord()
    {
        var userId = 1;
        var record = new WaterIntakeBuilder().WithUserId(userId).AsActive().Build();
        await Context.WaterIntakes.AddAsync(record);
        await Context.SaveChangesAsync();

        var result = await _sut.DeleteWaterIntakeAsync(userId, record.Id);

        result.Should().BeTrue();
        var deleted = await Context.WaterIntakes.FindAsync(record.Id);
        deleted!.Status.Should().Be(0);
    }

    [Fact]
    public async Task DeleteWaterIntakeAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        var ownerId = 1;
        var otherUserId = 2;
        var record = new WaterIntakeBuilder().WithUserId(ownerId).Build();
        await Context.WaterIntakes.AddAsync(record);
        await Context.SaveChangesAsync();

        var act = async () => await _sut.DeleteWaterIntakeAsync(otherUserId, record.Id);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to delete this water intake record");
    }

    #endregion
}
