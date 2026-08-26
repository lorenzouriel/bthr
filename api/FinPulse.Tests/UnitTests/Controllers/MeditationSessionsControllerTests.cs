using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using FinPulse.Api.Controllers;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;
using FinPulse.Tests.Helpers;

namespace FinPulse.Tests.UnitTests.Controllers;

public class MeditationSessionsControllerTests : ControllerTestBase
{
    private readonly Mock<IMeditationSessionService> _serviceMock;
    private readonly MeditationSessionsController _sut;

    public MeditationSessionsControllerTests()
    {
        _serviceMock = new Mock<IMeditationSessionService>();
        _sut = new MeditationSessionsController(_serviceMock.Object);
    }

    #region GetMeditationSessions Tests

    [Fact]
    public async Task GetMeditationSessions_WhenUserOwnsResource_Returns200OkWithSessions()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var expected = new List<MeditationSessionResponse> { new MeditationSessionResponse { Id = 1, UserId = userId, MeditationType = "Guided" } };

        _serviceMock.Setup(x => x.GetUserMeditationSessionsAsync(userId, null, null)).ReturnsAsync(expected);

        var result = await _sut.GetMeditationSessions(userId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetMeditationSessions_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.GetMeditationSessions(userId: 2);

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region CreateMeditationSession Tests

    [Fact]
    public async Task CreateMeditationSession_WhenUserOwnsResource_Returns201Created()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var request = new CreateMeditationSessionRequest { SessionDate = DateTime.UtcNow, DurationMinutes = 20, MeditationType = "Guided" };
        var expected = new MeditationSessionResponse { Id = 1, UserId = userId, MeditationType = "Guided" };

        _serviceMock.Setup(x => x.CreateMeditationSessionAsync(userId, request)).ReturnsAsync(expected);

        var result = await _sut.CreateMeditationSession(userId, request);

        var objResult = result.Should().BeOfType<ObjectResult>().Subject;
        objResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task CreateMeditationSession_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.CreateMeditationSession(userId: 2, new CreateMeditationSessionRequest());

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region UpdateMeditationSession Tests

    [Fact]
    public async Task UpdateMeditationSession_WhenNotFound_Returns404NotFound()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        _serviceMock.Setup(x => x.UpdateMeditationSessionAsync(userId, 999, It.IsAny<UpdateMeditationSessionRequest>()))
            .ReturnsAsync((MeditationSessionResponse?)null);

        var result = await _sut.UpdateMeditationSession(userId, 999, new UpdateMeditationSessionRequest());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateMeditationSession_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.UpdateMeditationSession(userId: 2, sessionId: 1, new UpdateMeditationSessionRequest());

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region DeleteMeditationSession Tests

    [Fact]
    public async Task DeleteMeditationSession_WhenNotFound_Returns404NotFound()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        _serviceMock.Setup(x => x.DeleteMeditationSessionAsync(userId, 999)).ReturnsAsync(false);

        var result = await _sut.DeleteMeditationSession(userId, 999);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteMeditationSession_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.DeleteMeditationSession(userId: 2, sessionId: 1);

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion
}
