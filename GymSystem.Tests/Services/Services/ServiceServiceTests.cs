using AutoFixture;
using AutoFixture.AutoMoq;
using AutoMapper;
using FluentAssertions;
using GymSystem.Application.Abstractions.Contract.Service;
using GymSystem.Application.Services.Services;
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

namespace GymSystem.Tests.Services.Services;

public class ServiceServiceTests {
    private readonly IFixture _fixture;
    private readonly Mock<IRepositoryFactory> _mockRepositoryFactory;
    private readonly Mock<IRepository<Service>> _mockServiceRepository;
    private readonly Mock<UtilityFactory<ServiceService>> _mockUtilityFactory;
    private readonly Mock<IServiceResponseHelper> _mockResponseHelper;
    private readonly Mock<ILogger<ServiceService>> _mockLogger;
    private readonly Mock<IMapper> _mockMapper;
    private readonly TestServiceServiceFactory _testFactory;
    private readonly ServiceService _sut; // System Under Test

    public ServiceServiceTests() {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        // Configure AutoFixture to handle circular references
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // Create mocks
        _mockRepositoryFactory = new Mock<IRepositoryFactory>();
        _mockServiceRepository = new Mock<IRepository<Service>>();
        _mockUtilityFactory = new Mock<UtilityFactory<ServiceService>>();
        _mockResponseHelper = new Mock<IServiceResponseHelper>();
        _mockLogger = new Mock<ILogger<ServiceService>>();
        _mockMapper = new Mock<IMapper>();

        // Setup factory behaviors
        _mockRepositoryFactory.Setup(x => x.CreateRepository<Service>()).Returns(_mockServiceRepository.Object);

        _mockUtilityFactory.Setup(x => x.CreateServiceResponseHelper()).Returns(_mockResponseHelper.Object);
        _mockUtilityFactory.Setup(x => x.CreateLogger()).Returns(_mockLogger.Object);
        _mockUtilityFactory.Setup(x => x.CreateMapper()).Returns(_mockMapper.Object);

        // Create test factory
        _testFactory = new TestServiceServiceFactory(
            _mockUtilityFactory.Object,
            _mockRepositoryFactory.Object,
            Mock.Of<IServiceProvider>());

        // Create System Under Test
        _sut = new ServiceService(_testFactory);
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WhenServicesExist_ShouldReturnSuccessWithServices() {
        // Arrange
        var services = _fixture.CreateMany<Service>(3).ToList();
        var serviceDtos = _fixture.CreateMany<ServiceDto>(3).ToList();
        var expectedResponse = _fixture.Build<ServiceResponse<List<ServiceDto>>>()
            .With(x => x.Data, serviceDtos)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var mockQueryable = services.AsQueryable().BuildMock();
        _mockServiceRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<List<ServiceDto>>(It.IsAny<List<Service>>())).Returns(serviceDtos);
        _mockResponseHelper.Setup(x => x.SetSuccess(serviceDtos)).Returns(expectedResponse);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().HaveCount(3);
        _mockServiceRepository.Verify(x => x.QueryNoTracking(), Times.Once);
        _mockMapper.Verify(x => x.Map<List<ServiceDto>>(It.IsAny<List<Service>>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoActiveServices_ShouldReturnEmptyList() {
        // Arrange
        var services = new List<Service>();
        var serviceDtos = new List<ServiceDto>();
        var expectedResponse = _fixture.Build<ServiceResponse<List<ServiceDto>>>()
            .With(x => x.Data, serviceDtos)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var mockQueryable = services.AsQueryable().BuildMock();
        _mockServiceRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<List<ServiceDto>>(It.IsAny<List<Service>>())).Returns(serviceDtos);
        _mockResponseHelper.Setup(x => x.SetSuccess(serviceDtos)).Returns(expectedResponse);

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
            .With(x => x.ErrorMessage, "Hizmetler getirilemedi")
            .With(x => x.ErrorCode, "SERVICE_GETALL_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<List<ServiceDto>>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (List<ServiceDto>)null)
            .Create();

        _mockServiceRepository.Setup(x => x.QueryNoTracking()).Throws(exception);
        _mockResponseHelper
            .Setup(x => x.SetError<List<ServiceDto>>(null, It.IsAny<ErrorInfo>()))
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
    public async Task GetByIdAsync_WhenServiceExists_ShouldReturnSuccessWithService() {
        // Arrange
        var serviceId = _fixture.Create<int>();
        var service = _fixture.Build<Service>()
            .With(x => x.Id, serviceId)
            .With(x => x.IsActive, true)
            .Create();
        var serviceDto = _fixture.Build<ServiceDto>()
            .With(x => x.Id, serviceId)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<ServiceDto?>>()
            .With(x => x.Data, serviceDto)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var services = new List<Service> { service };
        var mockQueryable = services.AsQueryable().BuildMock();

        _mockServiceRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<ServiceDto>(It.IsAny<Service>())).Returns(serviceDto);
        _mockResponseHelper.Setup(x => x.SetSuccess<ServiceDto?>(serviceDto)).Returns(expectedResponse);

        // Act
        var result = await _sut.GetByIdAsync(serviceId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(serviceId);
        _mockMapper.Verify(x => x.Map<ServiceDto>(It.IsAny<Service>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenServiceNotFound_ShouldReturnError() {
        // Arrange
        var serviceId = _fixture.Create<int>();
        var services = new List<Service>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Hizmet bulunamadý")
            .With(x => x.ErrorCode, "SERVICE_NOTFOUND")
            .With(x => x.StatusCode, 404)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<ServiceDto?>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (ServiceDto)null)
            .Create();

        var mockQueryable = services.AsQueryable().BuildMock();
        _mockServiceRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockResponseHelper
            .Setup(x => x.SetError<ServiceDto?>(null, "Hizmet bulunamadý", 404, "SERVICE_NOTFOUND"))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.GetByIdAsync(serviceId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.ErrorCode.Should().Be("SERVICE_NOTFOUND");
        _mockMapper.Verify(x => x.Map<ServiceDto>(It.IsAny<Service>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var serviceId = _fixture.Create<int>();
        var exception = _fixture.Create<Exception>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Hizmet getirilemedi")
            .With(x => x.ErrorCode, "SERVICE_GET_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<ServiceDto?>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (ServiceDto)null)
            .Create();

        _mockServiceRepository.Setup(x => x.QueryNoTracking()).Throws(exception);
        _mockResponseHelper
            .Setup(x => x.SetError<ServiceDto?>(null, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.GetByIdAsync(serviceId);

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
    public async Task CreateAsync_WithValidDto_ShouldReturnSuccessWithCreatedService() {
        // Arrange
        var serviceDto = _fixture.Build<ServiceDto>()
            .Without(x => x.Id)
            .Create();
        var serviceId = _fixture.Create<int>();
        var service = _fixture.Build<Service>()
            .With(x => x.Id, serviceId)
            .With(x => x.IsActive, true)
            .Create();
        var createdServiceDto = _fixture.Build<ServiceDto>()
            .With(x => x.Id, serviceId)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<ServiceDto>>()
            .With(x => x.Data, createdServiceDto)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Message, "Hizmet oluþturuldu")
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        _mockMapper
            .Setup(x => x.Map<Service>(
                It.IsAny<ServiceDto>(),
                It.IsAny<Action<IMappingOperationOptions<object, Service>>>()))
            .Returns(service)
            .Verifiable();

        _mockMapper
            .Setup(x => x.Map<ServiceDto>(It.IsAny<Service>()))
            .Returns(createdServiceDto)
            .Verifiable();

        _mockServiceRepository
            .Setup(x => x.AddAsync(It.IsAny<Service>()))
            .ReturnsAsync(service)
            .Verifiable();

        _mockServiceRepository
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1)
            .Verifiable();

        _mockResponseHelper
            .Setup(x => x.SetSuccess(It.IsAny<ServiceDto>(), "Hizmet oluþturuldu"))
            .Returns(expectedResponse)
            .Verifiable();

        // Act
        var result = await _sut.CreateAsync(serviceDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(serviceId);
        result.Message.Should().Be("Hizmet oluþturuldu");

        // Verify all mocks were called
        _mockMapper.Verify();
        _mockServiceRepository.Verify();
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
        var serviceDto = _fixture.Build<ServiceDto>()
            .Without(x => x.Id)
            .Create();
        var service = _fixture.Build<Service>()
            .With(x => x.IsActive, true)
            .Create();
        var exception = _fixture.Create<Exception>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Hizmet oluþturulamadý")
            .With(x => x.ErrorCode, "SERVICE_CREATE_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<ServiceDto>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (ServiceDto)null)
            .Create();

        _mockMapper
            .Setup(x => x.Map<Service>(
                It.IsAny<ServiceDto>(),
                It.IsAny<Action<IMappingOperationOptions<object, Service>>>()))
            .Returns(service);

        _mockServiceRepository.Setup(x => x.AddAsync(It.IsAny<Service>())).ThrowsAsync(exception);

        _mockResponseHelper
            .Setup(x => x.SetError<ServiceDto>(null, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.CreateAsync(serviceDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().NotBeNull();

        _mockServiceRepository.Verify(x => x.AddAsync(It.IsAny<Service>()), Times.Once);
        _mockServiceRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidDto_ShouldReturnSuccessWithUpdatedService() {
        // Arrange
        var serviceId = _fixture.Create<int>();
        var serviceDto = _fixture.Build<ServiceDto>()
            .With(x => x.Id, serviceId)
            .Create();
        var existingService = _fixture.Build<Service>()
            .With(x => x.Id, serviceId)
            .With(x => x.IsActive, true)
            .Create();
        var updatedServiceDto = _fixture.Build<ServiceDto>()
            .With(x => x.Id, serviceId)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<ServiceDto>>()
            .With(x => x.Data, updatedServiceDto)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Message, "Hizmet güncellendi")
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var services = new List<Service> { existingService };
        var mockQueryable = services.AsQueryable().BuildMock();

        _mockServiceRepository.Setup(x => x.Query()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map(serviceDto, existingService)).Returns(existingService);
        _mockMapper.Setup(x => x.Map<ServiceDto>(It.IsAny<Service>())).Returns(updatedServiceDto);
        _mockServiceRepository.Setup(x => x.UpdateAsync(It.IsAny<Service>())).ReturnsAsync(existingService);
        _mockServiceRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _mockResponseHelper.Setup(x => x.SetSuccess(updatedServiceDto, "Hizmet güncellendi")).Returns(expectedResponse);

        // Act
        var result = await _sut.UpdateAsync(serviceId, serviceDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Message.Should().Be("Hizmet güncellendi");
        _mockServiceRepository.Verify(x => x.UpdateAsync(It.IsAny<Service>()), Times.Once);
        _mockServiceRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenServiceNotFound_ShouldReturnError() {
        // Arrange
        var serviceId = _fixture.Create<int>();
        var serviceDto = _fixture.Build<ServiceDto>()
            .With(x => x.Id, serviceId)
            .Create();
        var services = new List<Service>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Hizmet bulunamadý")
            .With(x => x.ErrorCode, "SERVICE_NOTFOUND")
            .With(x => x.StatusCode, 404)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<ServiceDto>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (ServiceDto)null)
            .Create();

        var mockQueryable = services.AsQueryable().BuildMock();
        _mockServiceRepository.Setup(x => x.Query()).Returns(mockQueryable);
        _mockResponseHelper
            .Setup(x => x.SetError<ServiceDto>(null, "Hizmet bulunamadý", 404, "SERVICE_NOTFOUND"))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.UpdateAsync(serviceId, serviceDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.ErrorCode.Should().Be("SERVICE_NOTFOUND");
        _mockServiceRepository.Verify(x => x.UpdateAsync(It.IsAny<Service>()), Times.Never);
        _mockServiceRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var serviceId = _fixture.Create<int>();
        var serviceDto = _fixture.Build<ServiceDto>()
            .With(x => x.Id, serviceId)
            .Create();
        var exception = _fixture.Create<Exception>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Hizmet güncellenemedi")
            .With(x => x.ErrorCode, "SERVICE_UPDATE_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<ServiceDto>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (ServiceDto)null)
            .Create();

        _mockServiceRepository.Setup(x => x.Query()).Throws(exception);
        _mockResponseHelper
            .Setup(x => x.SetError<ServiceDto>(null, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.UpdateAsync(serviceId, serviceDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenServiceExists_ShouldReturnSuccessAndSoftDelete() {
        // Arrange
        var serviceId = _fixture.Create<int>();
        var service = _fixture.Build<Service>()
            .With(x => x.Id, serviceId)
            .With(x => x.IsActive, true)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, true)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Message, "Hizmet silindi")
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var services = new List<Service> { service };
        var mockQueryable = services.AsQueryable().BuildMock();

        _mockServiceRepository.Setup(x => x.Query()).Returns(mockQueryable);
        _mockServiceRepository.Setup(x => x.UpdateAsync(It.IsAny<Service>())).ReturnsAsync(service);
        _mockServiceRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _mockResponseHelper.Setup(x => x.SetSuccess(true, "Hizmet silindi")).Returns(expectedResponse);

        // Act
        var result = await _sut.DeleteAsync(serviceId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().BeTrue();
        result.Message.Should().Be("Hizmet silindi");
        service.IsActive.Should().BeFalse();
        service.UpdatedAt.Should().NotBeNull();
        _mockServiceRepository.Verify(x => x.UpdateAsync(It.Is<Service>(s => s.IsActive == false)), Times.Once);
        _mockServiceRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenServiceNotFound_ShouldReturnError() {
        // Arrange
        var serviceId = _fixture.Create<int>();
        var services = new List<Service>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Hizmet bulunamadý")
            .With(x => x.ErrorCode, "SERVICE_NOTFOUND")
            .With(x => x.StatusCode, 404)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, false)
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .Create();

        var mockQueryable = services.AsQueryable().BuildMock();
        _mockServiceRepository.Setup(x => x.Query()).Returns(mockQueryable);
        _mockResponseHelper
            .Setup(x => x.SetError<bool>(false, "Hizmet bulunamadý", 404, "SERVICE_NOTFOUND"))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.DeleteAsync(serviceId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Data.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.ErrorCode.Should().Be("SERVICE_NOTFOUND");
        _mockServiceRepository.Verify(x => x.UpdateAsync(It.IsAny<Service>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var serviceId = _fixture.Create<int>();
        var exception = _fixture.Create<Exception>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Hizmet silinemedi")
            .With(x => x.ErrorCode, "SERVICE_DELETE_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, false)
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .Create();

        _mockServiceRepository.Setup(x => x.Query()).Throws(exception);
        _mockResponseHelper
            .Setup(x => x.SetError<bool>(false, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.DeleteAsync(serviceId);

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
public class TestServiceServiceFactory : BaseFactory<ServiceService> {
    public TestServiceServiceFactory(
        UtilityFactory<ServiceService> utilityFactory,
        IRepositoryFactory repositoryFactory,
        IServiceProvider serviceProvider)
        : base(utilityFactory, repositoryFactory, serviceProvider) {
    }

    public override UtilityFactory<ServiceService> CreateUtilityFactory() => utilityFactory;
    public override IRepositoryFactory CreateRepositoryFactory() => repositoryFactory;
}
