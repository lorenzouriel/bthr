using FluentAssertions;
using Microsoft.Extensions.Configuration;
using FinPulse.Api.Services;
using System.IdentityModel.Tokens.Jwt;

namespace FinPulse.Tests.UnitTests.Services;

public class JwtServiceTests
{
    private readonly JwtService _sut;
    private readonly IConfiguration _configuration;

    public JwtServiceTests()
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = "ThisIsAVerySecretKeyForTestingPurposesOnly-MustBe64CharactersLong!",
            ["Jwt:Issuer"] = "FinPulseTestIssuer",
            ["Jwt:Audience"] = "FinPulseTestAudience",
            ["Jwt:ExpirationMinutes"] = "60"
        });
        _configuration = configBuilder.Build();
        _sut = new JwtService(_configuration);
    }

    [Fact]
    public void GenerateToken_CreatesValidToken()
    {
        // Arrange
        const int userId = 123;

        // Act
        var token = _sut.GenerateToken(userId);

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3); // JWT has 3 parts: header.payload.signature
    }

    [Fact]
    public void GenerateToken_TokenContainsUserIdClaim()
    {
        // Arrange
        const int userId = 456;

        // Act
        var token = _sut.GenerateToken(userId);

        // Assert - Parse the token to verify it contains the user ID
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub");
        subClaim.Should().NotBeNull();
        subClaim!.Value.Should().Be(userId.ToString());
    }

    [Fact]
    public void GenerateToken_TokenContainsJtiClaim()
    {
        // Arrange
        const int userId = 789;

        // Act
        var token = _sut.GenerateToken(userId);

        // Assert - Verify token has a unique JTI (JWT ID) claim
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var jtiClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "jti");
        jtiClaim.Should().NotBeNull();
        jtiClaim!.Value.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        const string invalidToken = "invalid.token.here";

        // Act
        var result = _sut.ValidateToken(invalidToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GenerateToken_ForDifferentUsers_CreatesUniqueTokens()
    {
        // Arrange & Act
        var token1 = _sut.GenerateToken(1);
        var token2 = _sut.GenerateToken(2);

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GenerateToken_CalledTwiceForSameUser_CreatesUniqueTokens()
    {
        // Arrange
        const int userId = 100;

        // Act
        var token1 = _sut.GenerateToken(userId);
        var token2 = _sut.GenerateToken(userId);

        // Assert - Different JTI (JWT ID) makes them unique
        token1.Should().NotBe(token2);

        // Verify both tokens contain the same user ID but different JTI
        var handler = new JwtSecurityTokenHandler();
        var jwtToken1 = handler.ReadJwtToken(token1);
        var jwtToken2 = handler.ReadJwtToken(token2);

        var jti1 = jwtToken1.Claims.First(c => c.Type == "jti").Value;
        var jti2 = jwtToken2.Claims.First(c => c.Type == "jti").Value;

        jti1.Should().NotBe(jti2); // Different JTI values
    }

    [Fact]
    public void GenerateToken_TokenHasCorrectIssuerAndAudience()
    {
        // Arrange
        const int userId = 200;

        // Act
        var token = _sut.GenerateToken(userId);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be("FinPulseTestIssuer");
        jwtToken.Audiences.Should().Contain("FinPulseTestAudience");
    }

    [Fact]
    public void GenerateToken_TokenHasExpirationTime()
    {
        // Arrange
        const int userId = 300;
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = _sut.GenerateToken(userId);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.ValidTo.Should().BeAfter(beforeGeneration);
        jwtToken.ValidTo.Should().BeCloseTo(beforeGeneration.AddMinutes(60), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ValidateToken_WithValidToken_ReturnsUserId()
    {
        // Arrange
        const int userId = 400;
        var token = _sut.GenerateToken(userId);

        // Act
        var result = _sut.ValidateToken(token);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(userId);
    }
}
