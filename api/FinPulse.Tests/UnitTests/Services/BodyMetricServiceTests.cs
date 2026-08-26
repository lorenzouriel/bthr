using FluentAssertions;
using FinPulse.Api.Services;
using FinPulse.Api.DTOs;
using FinPulse.Tests.Helpers;
using FinPulse.Tests.Helpers.Builders;

namespace FinPulse.Tests.UnitTests.Services;

public class BodyMetricServiceTests : ServiceTestBase
{
    private readonly BodyMetricService _sut;

    public BodyMetricServiceTests()
    {
        _sut = new BodyMetricService(Context);
    }

    #region CreateBodyMetricAsync Tests

    [Fact]
    public async Task CreateBodyMetricAsync_WithValidRequest_CreatesSuccessfully()
    {
        var userId = 1;
        var request = new CreateBodyMetricRequest
        {
            MeasuredDate = DateTime.UtcNow.Date,
            WeightKg = 75.5m,
            HeightCm = 178m
        };

        var result = await _sut.CreateBodyMetricAsync(userId, request);

        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.UserId.Should().Be(userId);
        result.WeightKg.Should().Be(75.5m);

        var record = await Context.BodyMetrics.FindAsync(result.Id);
        record!.Status.Should().Be(1);
    }

    #endregion

    #region GetUserBodyMetricsAsync Tests

    [Fact]
    public async Task GetUserBodyMetricsAsync_ReturnsOnlyUserRecords()
    {
        var userId = 1;
        var otherUserId = 2;

        await Context.BodyMetrics.AddRangeAsync(
            new BodyMetricBuilder().WithUserId(userId).WithMeasuredDate(new DateTime(2026, 1, 1)).Build(),
            new BodyMetricBuilder().WithUserId(userId).WithMeasuredDate(new DateTime(2026, 2, 1)).Build());
        await Context.BodyMetrics.AddAsync(new BodyMetricBuilder().WithUserId(otherUserId).Build());
        await Context.SaveChangesAsync();

        var result = await _sut.GetUserBodyMetricsAsync(userId);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(b => b.UserId.Should().Be(userId));
    }

    [Fact]
    public async Task GetUserBodyMetricsAsync_FiltersOutDeletedRecords()
    {
        var userId = 1;
        var active = new BodyMetricBuilder().WithUserId(userId).AsActive().Build();
        var deleted = new BodyMetricBuilder().WithUserId(userId).AsDeleted().Build();

        await Context.BodyMetrics.AddAsync(active);
        await Context.BodyMetrics.AddAsync(deleted);
        await Context.SaveChangesAsync();

        var result = await _sut.GetUserBodyMetricsAsync(userId);

        result.Should().HaveCount(1);
        result.Should().NotContain(b => b.Id == deleted.Id);
    }

    #endregion

    #region UpdateBodyMetricAsync Tests

    [Fact]
    public async Task UpdateBodyMetricAsync_WithValidRequest_UpdatesSuccessfully()
    {
        var userId = 1;
        var record = new BodyMetricBuilder().WithUserId(userId).Build();
        await Context.BodyMetrics.AddAsync(record);
        await Context.SaveChangesAsync();

        var request = new UpdateBodyMetricRequest { WeightKg = 80m };
        var result = await _sut.UpdateBodyMetricAsync(userId, record.Id, request);

        result.Should().NotBeNull();
        result!.WeightKg.Should().Be(80m);
    }

    [Fact]
    public async Task UpdateBodyMetricAsync_WithNonExistentRecord_ReturnsNull()
    {
        var result = await _sut.UpdateBodyMetricAsync(1, 999, new UpdateBodyMetricRequest());
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateBodyMetricAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        var ownerId = 1;
        var otherUserId = 2;
        var record = new BodyMetricBuilder().WithUserId(ownerId).Build();
        await Context.BodyMetrics.AddAsync(record);
        await Context.SaveChangesAsync();

        var act = async () => await _sut.UpdateBodyMetricAsync(otherUserId, record.Id, new UpdateBodyMetricRequest());

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to update this body metric record");
    }

    #endregion

    #region DeleteBodyMetricAsync Tests

    [Fact]
    public async Task DeleteBodyMetricAsync_SoftDeletesRecord()
    {
        var userId = 1;
        var record = new BodyMetricBuilder().WithUserId(userId).AsActive().Build();
        await Context.BodyMetrics.AddAsync(record);
        await Context.SaveChangesAsync();

        var result = await _sut.DeleteBodyMetricAsync(userId, record.Id);

        result.Should().BeTrue();
        var deleted = await Context.BodyMetrics.FindAsync(record.Id);
        deleted!.Status.Should().Be(0);
    }

    [Fact]
    public async Task DeleteBodyMetricAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        var ownerId = 1;
        var otherUserId = 2;
        var record = new BodyMetricBuilder().WithUserId(ownerId).Build();
        await Context.BodyMetrics.AddAsync(record);
        await Context.SaveChangesAsync();

        var act = async () => await _sut.DeleteBodyMetricAsync(otherUserId, record.Id);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to delete this body metric record");
    }

    #endregion
}
