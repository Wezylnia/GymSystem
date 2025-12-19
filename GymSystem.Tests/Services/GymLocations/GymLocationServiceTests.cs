using AutoFixture;
using AutoFixture.AutoMoq;
using AutoMapper;
using FluentAssertions;
using GymSystem.Application.Abstractions.Services.IGymLocationService.Contract;
using GymSystem.Application.Services.GymLocations;
using GymSystem.Common.Factory.Managers;
using GymSystem.Common.Factory.Repository;
using GymSystem.Common.Factory.Utility;
using GymSystem.Common.Helpers;
using GymSystem.Common.Models;
using GymSystem.Common.Repositories;
using GymSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;

namespace GymSystem.Tests.Services.GymLocations;

public class GymLocationServiceTests {
    private readonly IFixture _fixture;
    private readonly Mock<IRepositoryFactory> _mockRepositoryFactory;
    private readonly Mock<IRepository<GymLocation>> _mockGymLocationRepository;
    private readonly Mock<UtilityFactory<GymLocationService>> _mockUtilityFactory;
    private readonly Mock<IServiceResponseHelper> _mockResponseHelper;
    private readonly Mock<ILogger<GymLocationService>> _mockLogger;
    private readonly Mock<IMapper> _mockMapper;
    private readonly TestGymLocationServiceFactory _testFactory;
    private readonly GymLocationService _sut; // System Under Test

    public GymLocationServiceTests() {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        // Configure AutoFixture to handle circular references
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // Create mocks
        _mockRepositoryFactory = new Mock<IRepositoryFactory>();
        _mockGymLocationRepository = new Mock<IRepository<GymLocation>>();
        _mockUtilityFactory = new Mock<UtilityFactory<GymLocationService>>();
        _mockResponseHelper = new Mock<IServiceResponseHelper>();
        _mockLogger = new Mock<ILogger<GymLocationService>>();
        _mockMapper = new Mock<IMapper>();

        // Setup factory behaviors
        _mockRepositoryFactory.Setup(x => x.CreateRepository<GymLocation>()).Returns(_mockGymLocationRepository.Object);

        _mockUtilityFactory.Setup(x => x.CreateServiceResponseHelper()).Returns(_mockResponseHelper.Object);
        _mockUtilityFactory.Setup(x => x.CreateLogger()).Returns(_mockLogger.Object);
        _mockUtilityFactory.Setup(x => x.CreateMapper()).Returns(_mockMapper.Object);

        // Create test factory
        _testFactory = new TestGymLocationServiceFactory(
            _mockUtilityFactory.Object,
            _mockRepositoryFactory.Object,
            Mock.Of<IServiceProvider>());

        // Create System Under Test
        _sut = new GymLocationService(_testFactory);
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WhenGymLocationsExist_ShouldReturnSuccessWithGymLocations() {
        // Arrange
        var gymLocations = _fixture.CreateMany<GymLocation>(3).ToList();
        var gymLocationDtos = _fixture.CreateMany<GymLocationDto>(3).ToList();
        var expectedResponse = _fixture.Build<ServiceResponse<List<GymLocationDto>>>()
            .With(x => x.Data, gymLocationDtos)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var mockQueryable = gymLocations.AsQueryable().BuildMock();
        _mockGymLocationRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<List<GymLocationDto>>(It.IsAny<List<GymLocation>>())).Returns(gymLocationDtos);
        _mockResponseHelper.Setup(x => x.SetSuccess(gymLocationDtos)).Returns(expectedResponse);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().HaveCount(3);
        _mockGymLocationRepository.Verify(x => x.QueryNoTracking(), Times.Once);
        _mockMapper.Verify(x => x.Map<List<GymLocationDto>>(It.IsAny<List<GymLocation>>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoActiveGymLocations_ShouldReturnEmptyList() {
        // Arrange
        var gymLocations = new List<GymLocation>();
        var gymLocationDtos = new List<GymLocationDto>();
        var expectedResponse = _fixture.Build<ServiceResponse<List<GymLocationDto>>>()
            .With(x => x.Data, gymLocationDtos)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var mockQueryable = gymLocations.AsQueryable().BuildMock();
        _mockGymLocationRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<List<GymLocationDto>>(It.IsAny<List<GymLocation>>())).Returns(gymLocationDtos);
        _mockResponseHelper.Setup(x => x.SetSuccess(gymLocationDtos)).Returns(expectedResponse);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var exception = _fixture.Create<Exception>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Spor salonlarý getirilemedi")
            .With(x => x.ErrorCode, "GYMLOCATION_GETALL_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<List<GymLocationDto>>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (List<GymLocationDto>)null)
            .Create();

        _mockGymLocationRepository.Setup(x => x.QueryNoTracking()).Throws(exception);
        _mockResponseHelper
            .Setup(x => x.SetError<List<GymLocationDto>>(null, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().NotBeNull();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenGymLocationExists_ShouldReturnSuccessWithGymLocation() {
        // Arrange
        var gymLocationId = _fixture.Create<int>();
        var gymLocation = _fixture.Build<GymLocation>()
            .With(x => x.Id, gymLocationId)
            .With(x => x.IsActive, true)
            .Create();
        var gymLocationDto = _fixture.Build<GymLocationDto>()
            .With(x => x.Id, gymLocationId)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<GymLocationDto?>>()
            .With(x => x.Data, gymLocationDto)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var gymLocations = new List<GymLocation> { gymLocation };
        var mockQueryable = gymLocations.AsQueryable().BuildMock();

        _mockGymLocationRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<GymLocationDto>(It.IsAny<GymLocation>())).Returns(gymLocationDto);
        _mockResponseHelper.Setup(x => x.SetSuccess<GymLocationDto?>(gymLocationDto)).Returns(expectedResponse);

        // Act
        var result = await _sut.GetByIdAsync(gymLocationId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(gymLocationId);
        _mockMapper.Verify(x => x.Map<GymLocationDto>(It.IsAny<GymLocation>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenGymLocationNotFound_ShouldReturnError() {
        // Arrange
        var gymLocationId = _fixture.Create<int>();
        var gymLocations = new List<GymLocation>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Spor salonu bulunamadý")
            .With(x => x.ErrorCode, "GYMLOCATION_NOTFOUND")
            .With(x => x.StatusCode, 404)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<GymLocationDto?>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (GymLocationDto)null)
            .Create();

        var mockQueryable = gymLocations.AsQueryable().BuildMock();
        _mockGymLocationRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockResponseHelper
            .Setup(x => x.SetError<GymLocationDto?>(null, "Spor salonu bulunamadý", 404, "GYMLOCATION_NOTFOUND"))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.GetByIdAsync(gymLocationId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.ErrorCode.Should().Be("GYMLOCATION_NOTFOUND");
        _mockMapper.Verify(x => x.Map<GymLocationDto>(It.IsAny<GymLocation>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var gymLocationId = _fixture.Create<int>();
        var exception = _fixture.Create<Exception>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Spor salonu getirilemedi")
            .With(x => x.ErrorCode, "GYMLOCATION_GET_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<GymLocationDto?>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (GymLocationDto)null)
            .Create();

        _mockGymLocationRepository.Setup(x => x.QueryNoTracking()).Throws(exception);
        _mockResponseHelper
            .Setup(x => x.SetError<GymLocationDto?>(null, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.GetByIdAsync(gymLocationId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().NotBeNull();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidDto_ShouldReturnSuccessWithCreatedGymLocation() {
        // Arrange
        var gymLocationDto = _fixture.Build<GymLocationDto>()
            .Without(x => x.Id)
            .Create();
        var gymLocationId = _fixture.Create<int>();
        var gymLocation = _fixture.Build<GymLocation>()
            .With(x => x.Id, gymLocationId)
            .With(x => x.IsActive, true)
            .Create();
        var createdGymLocationDto = _fixture.Build<GymLocationDto>()
            .With(x => x.Id, gymLocationId)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<GymLocationDto>>()
            .With(x => x.Data, createdGymLocationDto)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Message, "Spor salonu oluþturuldu")
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        _mockMapper
            .Setup(x => x.Map<GymLocation>(
                It.IsAny<GymLocationDto>(),
                It.IsAny<Action<IMappingOperationOptions<object, GymLocation>>>()))
            .Returns(gymLocation)
            .Verifiable();

        _mockMapper
            .Setup(x => x.Map<GymLocationDto>(It.IsAny<GymLocation>()))
            .Returns(createdGymLocationDto)
            .Verifiable();

        _mockGymLocationRepository
            .Setup(x => x.AddAsync(It.IsAny<GymLocation>()))
            .ReturnsAsync(gymLocation)
            .Verifiable();

        _mockGymLocationRepository
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1)
            .Verifiable();

        _mockResponseHelper
            .Setup(x => x.SetSuccess(It.IsAny<GymLocationDto>(), "Spor salonu oluþturuldu"))
            .Returns(expectedResponse)
            .Verifiable();

        // Act
        var result = await _sut.CreateAsync(gymLocationDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(gymLocationId);
        result.Message.Should().Be("Spor salonu oluþturuldu");

        // Verify all mocks were called
        _mockMapper.Verify();
        _mockGymLocationRepository.Verify();
        _mockResponseHelper.Verify();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenRepositoryThrowsException_ShouldReturnError() {
        // Arrange
        var gymLocationDto = _fixture.Build<GymLocationDto>()
            .Without(x => x.Id)
            .Create();
        var gymLocation = _fixture.Build<GymLocation>()
            .With(x => x.IsActive, true)
            .Create();
        var exception = _fixture.Create<Exception>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Spor salonu oluþturulamadý")
            .With(x => x.ErrorCode, "GYMLOCATION_CREATE_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<GymLocationDto>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (GymLocationDto)null)
            .Create();

        _mockMapper
            .Setup(x => x.Map<GymLocation>(
                It.IsAny<GymLocationDto>(),
                It.IsAny<Action<IMappingOperationOptions<object, GymLocation>>>()))
            .Returns(gymLocation);

        _mockGymLocationRepository.Setup(x => x.AddAsync(It.IsAny<GymLocation>())).ThrowsAsync(exception);

        _mockResponseHelper
            .Setup(x => x.SetError<GymLocationDto>(null, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.CreateAsync(gymLocationDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().NotBeNull();

        _mockGymLocationRepository.Verify(x => x.AddAsync(It.IsAny<GymLocation>()), Times.Once);
        _mockGymLocationRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidDto_ShouldReturnSuccessWithUpdatedGymLocation() {
        // Arrange
        var gymLocationId = _fixture.Create<int>();
        var gymLocationDto = _fixture.Build<GymLocationDto>()
            .With(x => x.Id, gymLocationId)
            .Create();
        var existingGymLocation = _fixture.Build<GymLocation>()
            .With(x => x.Id, gymLocationId)
            .With(x => x.IsActive, true)
            .Create();
        var updatedGymLocationDto = _fixture.Build<GymLocationDto>()
            .With(x => x.Id, gymLocationId)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<GymLocationDto>>()
            .With(x => x.Data, updatedGymLocationDto)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Message, "Spor salonu güncellendi")
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var gymLocations = new List<GymLocation> { existingGymLocation };
        var mockQueryable = gymLocations.AsQueryable().BuildMock();

        _mockGymLocationRepository.Setup(x => x.Query()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map(gymLocationDto, existingGymLocation)).Returns(existingGymLocation);
        _mockMapper.Setup(x => x.Map<GymLocationDto>(It.IsAny<GymLocation>())).Returns(updatedGymLocationDto);
        _mockGymLocationRepository.Setup(x => x.UpdateAsync(It.IsAny<GymLocation>())).ReturnsAsync(existingGymLocation);
        _mockGymLocationRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _mockResponseHelper.Setup(x => x.SetSuccess(updatedGymLocationDto, "Spor salonu güncellendi")).Returns(expectedResponse);

        // Act
        var result = await _sut.UpdateAsync(gymLocationId, gymLocationDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Message.Should().Be("Spor salonu güncellendi");
        _mockGymLocationRepository.Verify(x => x.UpdateAsync(It.IsAny<GymLocation>()), Times.Once);
        _mockGymLocationRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenGymLocationNotFound_ShouldReturnError() {
        // Arrange
        var gymLocationId = _fixture.Create<int>();
        var gymLocationDto = _fixture.Build<GymLocationDto>()
            .With(x => x.Id, gymLocationId)
            .Create();
        var gymLocations = new List<GymLocation>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Spor salonu bulunamadý")
            .With(x => x.ErrorCode, "GYMLOCATION_NOTFOUND")
            .With(x => x.StatusCode, 404)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<GymLocationDto>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (GymLocationDto)null)
            .Create();

        var mockQueryable = gymLocations.AsQueryable().BuildMock();
        _mockGymLocationRepository.Setup(x => x.Query()).Returns(mockQueryable);
        _mockResponseHelper
            .Setup(x => x.SetError<GymLocationDto>(null, "Spor salonu bulunamadý", 404, "GYMLOCATION_NOTFOUND"))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.UpdateAsync(gymLocationId, gymLocationDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.ErrorCode.Should().Be("GYMLOCATION_NOTFOUND");
        _mockGymLocationRepository.Verify(x => x.UpdateAsync(It.IsAny<GymLocation>()), Times.Never);
        _mockGymLocationRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var gymLocationId = _fixture.Create<int>();
        var gymLocationDto = _fixture.Build<GymLocationDto>()
            .With(x => x.Id, gymLocationId)
            .Create();
        var exception = _fixture.Create<Exception>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Spor salonu güncellenemedi")
            .With(x => x.ErrorCode, "GYMLOCATION_UPDATE_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<GymLocationDto>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (GymLocationDto)null)
            .Create();

        _mockGymLocationRepository.Setup(x => x.Query()).Throws(exception);
        _mockResponseHelper
            .Setup(x => x.SetError<GymLocationDto>(null, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.UpdateAsync(gymLocationId, gymLocationDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenGymLocationExists_ShouldReturnSuccessAndSoftDelete() {
        // Arrange
        var gymLocationId = _fixture.Create<int>();
        var gymLocation = _fixture.Build<GymLocation>()
            .With(x => x.Id, gymLocationId)
            .With(x => x.IsActive, true)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, true)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Message, "Spor salonu silindi")
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var gymLocations = new List<GymLocation> { gymLocation };
        var mockQueryable = gymLocations.AsQueryable().BuildMock();

        _mockGymLocationRepository.Setup(x => x.Query()).Returns(mockQueryable);
        _mockGymLocationRepository.Setup(x => x.UpdateAsync(It.IsAny<GymLocation>())).ReturnsAsync(gymLocation);
        _mockGymLocationRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _mockResponseHelper.Setup(x => x.SetSuccess(true, "Spor salonu silindi")).Returns(expectedResponse);

        // Act
        var result = await _sut.DeleteAsync(gymLocationId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().BeTrue();
        result.Message.Should().Be("Spor salonu silindi");
        gymLocation.IsActive.Should().BeFalse();
        gymLocation.UpdatedAt.Should().NotBeNull();
        _mockGymLocationRepository.Verify(x => x.UpdateAsync(It.Is<GymLocation>(g => g.IsActive == false)), Times.Once);
        _mockGymLocationRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenGymLocationNotFound_ShouldReturnError() {
        // Arrange
        var gymLocationId = _fixture.Create<int>();
        var gymLocations = new List<GymLocation>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Spor salonu bulunamadý")
            .With(x => x.ErrorCode, "GYMLOCATION_NOTFOUND")
            .With(x => x.StatusCode, 404)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, false)
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .Create();

        var mockQueryable = gymLocations.AsQueryable().BuildMock();
        _mockGymLocationRepository.Setup(x => x.Query()).Returns(mockQueryable);
        _mockResponseHelper
            .Setup(x => x.SetError<bool>(false, "Spor salonu bulunamadý", 404, "GYMLOCATION_NOTFOUND"))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.DeleteAsync(gymLocationId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Data.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.ErrorCode.Should().Be("GYMLOCATION_NOTFOUND");
        _mockGymLocationRepository.Verify(x => x.UpdateAsync(It.IsAny<GymLocation>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var gymLocationId = _fixture.Create<int>();
        var exception = _fixture.Create<Exception>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Spor salonu silinemedi")
            .With(x => x.ErrorCode, "GYMLOCATION_DELETE_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, false)
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .Create();

        _mockGymLocationRepository.Setup(x => x.Query()).Throws(exception);
        _mockResponseHelper
            .Setup(x => x.SetError<bool>(false, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.DeleteAsync(gymLocationId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Data.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    #endregion
}

/// <summary>
/// Test implementation of BaseFactory for testing purposes
/// </summary>
public class TestGymLocationServiceFactory : BaseFactory<GymLocationService> {
    public TestGymLocationServiceFactory(
        UtilityFactory<GymLocationService> utilityFactory,
        IRepositoryFactory repositoryFactory,
        IServiceProvider serviceProvider)
        : base(utilityFactory, repositoryFactory, serviceProvider) {
    }

    public override UtilityFactory<GymLocationService> CreateUtilityFactory() => utilityFactory;
    public override IRepositoryFactory CreateRepositoryFactory() => repositoryFactory;
}
