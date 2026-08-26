using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using FinPulse.Api.Controllers;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;
using FinPulse.Tests.Helpers;

namespace FinPulse.Tests.UnitTests.Controllers;

public class JournalEntriesControllerTests : ControllerTestBase
{
    private readonly Mock<IJournalEntryService> _serviceMock;
    private readonly JournalEntriesController _sut;

    public JournalEntriesControllerTests()
    {
        _serviceMock = new Mock<IJournalEntryService>();
        _sut = new JournalEntriesController(_serviceMock.Object);
    }

    #region GetJournalEntries Tests

    [Fact]
    public async Task GetJournalEntries_WhenUserOwnsResource_Returns200OkWithEntries()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var expected = new List<JournalEntryResponse> { new JournalEntryResponse { Id = 1, UserId = userId, Content = "Hello" } };

        _serviceMock.Setup(x => x.GetUserJournalEntriesAsync(userId, null, null)).ReturnsAsync(expected);

        var result = await _sut.GetJournalEntries(userId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetJournalEntries_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.GetJournalEntries(userId: 2);

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region CreateJournalEntry Tests

    [Fact]
    public async Task CreateJournalEntry_WhenUserOwnsResource_Returns201Created()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var request = new CreateJournalEntryRequest { EntryDate = DateTime.UtcNow, Content = "Hello" };
        var expected = new JournalEntryResponse { Id = 1, UserId = userId, Content = "Hello" };

        _serviceMock.Setup(x => x.CreateJournalEntryAsync(userId, request)).ReturnsAsync(expected);

        var result = await _sut.CreateJournalEntry(userId, request);

        var objResult = result.Should().BeOfType<ObjectResult>().Subject;
        objResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task CreateJournalEntry_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.CreateJournalEntry(userId: 2, new CreateJournalEntryRequest());

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region UpdateJournalEntry Tests

    [Fact]
    public async Task UpdateJournalEntry_WhenNotFound_Returns404NotFound()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        _serviceMock.Setup(x => x.UpdateJournalEntryAsync(userId, 999, It.IsAny<UpdateJournalEntryRequest>()))
            .ReturnsAsync((JournalEntryResponse?)null);

        var result = await _sut.UpdateJournalEntry(userId, 999, new UpdateJournalEntryRequest());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateJournalEntry_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.UpdateJournalEntry(userId: 2, entryId: 1, new UpdateJournalEntryRequest());

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region DeleteJournalEntry Tests

    [Fact]
    public async Task DeleteJournalEntry_WhenNotFound_Returns404NotFound()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        _serviceMock.Setup(x => x.DeleteJournalEntryAsync(userId, 999)).ReturnsAsync(false);

        var result = await _sut.DeleteJournalEntry(userId, 999);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteJournalEntry_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.DeleteJournalEntry(userId: 2, entryId: 1);

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion
}
