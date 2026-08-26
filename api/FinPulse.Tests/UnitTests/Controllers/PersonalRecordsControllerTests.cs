using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using FinPulse.Api.Controllers;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;
using FinPulse.Tests.Helpers;

namespace FinPulse.Tests.UnitTests.Controllers;

public class PersonalRecordsControllerTests : ControllerTestBase
{
    private readonly Mock<IPersonalRecordService> _serviceMock;
    private readonly PersonalRecordsController _sut;

    public PersonalRecordsControllerTests()
    {
        _serviceMock = new Mock<IPersonalRecordService>();
        _sut = new PersonalRecordsController(_serviceMock.Object);
    }

    #region GetPersonalRecords Tests

    [Fact]
    public async Task GetPersonalRecords_WhenUserOwnsResource_Returns200OkWithRecords()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var expected = new List<PersonalRecordResponse>
        {
            new PersonalRecordResponse { Id = 1, UserId = userId, ExerciseName = "Bench Press" }
        };

        _serviceMock.Setup(x => x.GetUserPersonalRecordsAsync(userId, null, null)).ReturnsAsync(expected);

        var result = await _sut.GetPersonalRecords(userId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetPersonalRecords_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.GetPersonalRecords(userId: 2);

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region CreatePersonalRecord Tests

    [Fact]
    public async Task CreatePersonalRecord_WhenUserOwnsResource_Returns201Created()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var request = new CreatePersonalRecordRequest
        {
            ExerciseName = "Bench Press",
            MetricType = "Max Weight",
            Value = 100m,
            Unit = "kg",
            AchievedDate = DateTime.UtcNow
        };
        var expected = new PersonalRecordResponse { Id = 1, UserId = userId, ExerciseName = "Bench Press" };

        _serviceMock.Setup(x => x.CreatePersonalRecordAsync(userId, request)).ReturnsAsync(expected);

        var result = await _sut.CreatePersonalRecord(userId, request);

        var objResult = result.Should().BeOfType<ObjectResult>().Subject;
        objResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task CreatePersonalRecord_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.CreatePersonalRecord(userId: 2, new CreatePersonalRecordRequest());

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    // Note: PersonalRecordsController has no Update/Delete actions (Decision 2,
    // DESIGN_BODY_MODULE_API.md) — a PUT/DELETE request to this resource returns
    // the framework's default 404 for an unmatched route, so no such tests exist here.
}
