using AutoFixture;
using AutoFixture.AutoMoq;
using AutoMapper;
using FluentAssertions;
using GymSystem.Application.Abstractions.Contract.Trainer;
using GymSystem.Application.Services.Trainers;
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

namespace GymSystem.Tests.Services.Trainers;

public class TrainerServiceTests {
    private readonly IFixture _fixture;
    private readonly Mock<IRepositoryFactory> _mockRepositoryFactory;
    private readonly Mock<IRepository<Trainer>> _mockTrainerRepository;
    private readonly Mock<UtilityFactory<TrainerService>> _mockUtilityFactory;
    private readonly Mock<IServiceResponseHelper> _mockResponseHelper;
    private readonly Mock<ILogger<TrainerService>> _mockLogger;
    private readonly Mock<IMapper> _mockMapper;
    private readonly TestTrainerServiceFactory _testFactory;
    private readonly TrainerService _sut; // System Under Test

    public TrainerServiceTests() {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        // Configure AutoFixture to handle circular references
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // Create mocks
        _mockRepositoryFactory = new Mock<IRepositoryFactory>();
        _mockTrainerRepository = new Mock<IRepository<Trainer>>();
        _mockUtilityFactory = new Mock<UtilityFactory<TrainerService>>();
        _mockResponseHelper = new Mock<IServiceResponseHelper>();
        _mockLogger = new Mock<ILogger<TrainerService>>();
        _mockMapper = new Mock<IMapper>();

        // Setup factory behaviors
        _mockRepositoryFactory.Setup(x => x.CreateRepository<Trainer>()).Returns(_mockTrainerRepository.Object);

        _mockUtilityFactory.Setup(x => x.CreateServiceResponseHelper()).Returns(_mockResponseHelper.Object);
        _mockUtilityFactory.Setup(x => x.CreateLogger()).Returns(_mockLogger.Object);
        _mockUtilityFactory.Setup(x => x.CreateMapper()).Returns(_mockMapper.Object);

        // Create test factory
        _testFactory = new TestTrainerServiceFactory(
            _mockUtilityFactory.Object,
            _mockRepositoryFactory.Object,
            Mock.Of<IServiceProvider>());

        // Create System Under Test
        _sut = new TrainerService(_testFactory);
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WhenTrainersExist_ShouldReturnSuccessWithTrainers() {
        // Arrange
        var trainers = _fixture.CreateMany<Trainer>(3).ToList();
        var trainerDtos = _fixture.CreateMany<TrainerDto>(3).ToList();
        var expectedResponse = _fixture.Build<ServiceResponse<List<TrainerDto>>>()
            .With(x => x.Data, trainerDtos)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var mockQueryable = trainers.AsQueryable().BuildMock();
        _mockTrainerRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<List<TrainerDto>>(It.IsAny<List<Trainer>>())).Returns(trainerDtos);
        _mockResponseHelper.Setup(x => x.SetSuccess(trainerDtos)).Returns(expectedResponse);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().HaveCount(3);
        _mockTrainerRepository.Verify(x => x.QueryNoTracking(), Times.Once);
        _mockMapper.Verify(x => x.Map<List<TrainerDto>>(It.IsAny<List<Trainer>>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoActiveTrainers_ShouldReturnEmptyList() {
        // Arrange
        var trainers = new List<Trainer>();
        var trainerDtos = new List<TrainerDto>();
        var expectedResponse = _fixture.Build<ServiceResponse<List<TrainerDto>>>()
            .With(x => x.Data, trainerDtos)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var mockQueryable = trainers.AsQueryable().BuildMock();
        _mockTrainerRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<List<TrainerDto>>(It.IsAny<List<Trainer>>())).Returns(trainerDtos);
        _mockResponseHelper.Setup(x => x.SetSuccess(trainerDtos)).Returns(expectedResponse);

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
            .With(x => x.ErrorMessage, "Antrenörler getirilemedi")
            .With(x => x.ErrorCode, "TRAINER_GETALL_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<List<TrainerDto>>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (List<TrainerDto>)null)
            .Create();

        _mockTrainerRepository.Setup(x => x.QueryNoTracking()).Throws(exception);
        _mockResponseHelper
            .Setup(x => x.SetError<List<TrainerDto>>(null, It.IsAny<ErrorInfo>()))
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
    public async Task GetByIdAsync_WhenTrainerExists_ShouldReturnSuccessWithTrainer() {
        // Arrange
        var trainerId = _fixture.Create<int>();
        var trainer = _fixture.Build<Trainer>()
            .With(x => x.Id, trainerId)
            .With(x => x.IsActive, true)
            .Create();
        var trainerDto = _fixture.Build<TrainerDto>()
            .With(x => x.Id, trainerId)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<TrainerDto?>>()
            .With(x => x.Data, trainerDto)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var trainers = new List<Trainer> { trainer };
        var mockQueryable = trainers.AsQueryable().BuildMock();

        _mockTrainerRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<TrainerDto>(It.IsAny<Trainer>())).Returns(trainerDto);
        _mockResponseHelper.Setup(x => x.SetSuccess<TrainerDto?>(trainerDto)).Returns(expectedResponse);

        // Act
        var result = await _sut.GetByIdAsync(trainerId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(trainerId);
        _mockMapper.Verify(x => x.Map<TrainerDto>(It.IsAny<Trainer>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTrainerNotFound_ShouldReturnError() {
        // Arrange
        var trainerId = _fixture.Create<int>();
        var trainers = new List<Trainer>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Antrenör bulunamadý")
            .With(x => x.ErrorCode, "TRAINER_NOTFOUND")
            .With(x => x.StatusCode, 404)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<TrainerDto?>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (TrainerDto)null)
            .Create();

        var mockQueryable = trainers.AsQueryable().BuildMock();
        _mockTrainerRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockResponseHelper
            .Setup(x => x.SetError<TrainerDto?>(null, "Antrenör bulunamadý", 404, "TRAINER_NOTFOUND"))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.GetByIdAsync(trainerId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.ErrorCode.Should().Be("TRAINER_NOTFOUND");
        _mockMapper.Verify(x => x.Map<TrainerDto>(It.IsAny<Trainer>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var trainerId = _fixture.Create<int>();
        var exception = _fixture.Create<Exception>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Antrenör getirilemedi")
            .With(x => x.ErrorCode, "TRAINER_GET_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<TrainerDto?>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (TrainerDto)null)
            .Create();

        _mockTrainerRepository.Setup(x => x.QueryNoTracking()).Throws(exception);
        _mockResponseHelper
            .Setup(x => x.SetError<TrainerDto?>(null, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.GetByIdAsync(trainerId);

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
    public async Task CreateAsync_WithValidDto_ShouldReturnSuccessWithCreatedTrainer() {
        // Arrange
        var trainerDto = _fixture.Build<TrainerDto>()
            .Without(x => x.Id)
            .Without(x => x.SelectedServiceIds)
            .Create();
        var trainerId = _fixture.Create<int>();
        var trainer = _fixture.Build<Trainer>()
            .With(x => x.Id, trainerId)
            .With(x => x.IsActive, true)
            .Without(x => x.Specialties)
            .Without(x => x.GymLocation)
            .Create();
        var createdTrainerDto = _fixture.Build<TrainerDto>()
            .With(x => x.Id, trainerId)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<TrainerDto>>()
            .With(x => x.Data, createdTrainerDto)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Message, "Antrenör oluþturuldu")
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        // Setup QueryNoTracking for re-fetching after save
        var trainers = new List<Trainer> { trainer };
        var mockQueryable = trainers.AsQueryable().BuildMock();

        _mockMapper
            .Setup(x => x.Map<Trainer>(
                It.IsAny<TrainerDto>(),
                It.IsAny<Action<IMappingOperationOptions<object, Trainer>>>()))
            .Returns(trainer)
            .Verifiable();

        _mockMapper
            .Setup(x => x.Map<TrainerDto>(It.IsAny<Trainer>()))
            .Returns(createdTrainerDto)
            .Verifiable();

            _mockTrainerRepository
                .Setup(x => x.AddAsync(It.IsAny<Trainer>()))
                .ReturnsAsync(trainer)
                .Verifiable();

            _mockTrainerRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1)
                .Verifiable();

            _mockTrainerRepository
                .Setup(x => x.QueryNoTracking())
                .Returns(mockQueryable);

            _mockResponseHelper
                .Setup(x => x.SetSuccess(It.IsAny<TrainerDto>(), "Antrenör oluþturuldu"))
                .Returns(expectedResponse)
                .Verifiable();

            // Act
            var result = await _sut.CreateAsync(trainerDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(trainerId);
            result.Message.Should().Be("Antrenör oluþturuldu");

            // Verify all mocks were called
            _mockMapper.Verify();
            _mockTrainerRepository.Verify();
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
        var trainerDto = _fixture.Build<TrainerDto>()
            .Without(x => x.Id)
            .Create();
        var trainer = _fixture.Build<Trainer>()
            .With(x => x.IsActive, true)
            .Create();
        var exception = _fixture.Create<Exception>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Antrenör oluþturulamadý")
            .With(x => x.ErrorCode, "TRAINER_CREATE_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<TrainerDto>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (TrainerDto)null)
            .Create();

        _mockMapper
            .Setup(x => x.Map<Trainer>(
                It.IsAny<TrainerDto>(),
                It.IsAny<Action<IMappingOperationOptions<object, Trainer>>>()))
            .Returns(trainer);

        _mockTrainerRepository.Setup(x => x.AddAsync(It.IsAny<Trainer>())).ThrowsAsync(exception);

        _mockResponseHelper
            .Setup(x => x.SetError<TrainerDto>(null, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.CreateAsync(trainerDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().NotBeNull();

        _mockTrainerRepository.Verify(x => x.AddAsync(It.IsAny<Trainer>()), Times.Once);
        _mockTrainerRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidDto_ShouldReturnSuccessWithUpdatedTrainer() {
        // Arrange
        var trainerId = _fixture.Create<int>();
        var trainerDto = _fixture.Build<TrainerDto>()
            .With(x => x.Id, trainerId)
            .Without(x => x.SelectedServiceIds)
            .Create();
        var existingTrainer = _fixture.Build<Trainer>()
            .With(x => x.Id, trainerId)
            .With(x => x.IsActive, true)
            .With(x => x.Specialties, new List<TrainerSpecialty>())
            .Without(x => x.GymLocation)
            .Create();
        var updatedTrainerDto = _fixture.Build<TrainerDto>()
            .With(x => x.Id, trainerId)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<TrainerDto>>()
            .With(x => x.Data, updatedTrainerDto)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Message, "Antrenör güncellendi")
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var trainers = new List<Trainer> { existingTrainer };
        var mockQueryable = trainers.AsQueryable().BuildMock();

        // Mock for TrainerSpecialty repository
        var mockSpecialtyRepository = new Mock<IRepository<TrainerSpecialty>>();
        mockSpecialtyRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _mockRepositoryFactory.Setup(x => x.CreateRepository<TrainerSpecialty>()).Returns(mockSpecialtyRepository.Object);

        _mockTrainerRepository.Setup(x => x.Query()).Returns(mockQueryable);
        _mockTrainerRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map(trainerDto, existingTrainer)).Returns(existingTrainer);
        _mockMapper.Setup(x => x.Map<TrainerDto>(It.IsAny<Trainer>())).Returns(updatedTrainerDto);
        _mockTrainerRepository.Setup(x => x.UpdateAsync(It.IsAny<Trainer>())).ReturnsAsync(existingTrainer);
        _mockTrainerRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _mockResponseHelper.Setup(x => x.SetSuccess(It.IsAny<TrainerDto>(), "Antrenör güncellendi")).Returns(expectedResponse);

        // Act
        var result = await _sut.UpdateAsync(trainerId, trainerDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Message.Should().Be("Antrenör güncellendi");
        _mockTrainerRepository.Verify(x => x.UpdateAsync(It.IsAny<Trainer>()), Times.Once);
        _mockTrainerRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenTrainerNotFound_ShouldReturnError() {
        // Arrange
        var trainerId = _fixture.Create<int>();
        var trainerDto = _fixture.Build<TrainerDto>()
            .With(x => x.Id, trainerId)
            .Create();
        var trainers = new List<Trainer>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Antrenör bulunamadý")
            .With(x => x.ErrorCode, "TRAINER_NOTFOUND")
            .With(x => x.StatusCode, 404)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<TrainerDto>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (TrainerDto)null)
            .Create();

        var mockQueryable = trainers.AsQueryable().BuildMock();
        _mockTrainerRepository.Setup(x => x.Query()).Returns(mockQueryable);
        _mockResponseHelper
            .Setup(x => x.SetError<TrainerDto>(null, "Antrenör bulunamadý", 404, "TRAINER_NOTFOUND"))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.UpdateAsync(trainerId, trainerDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.ErrorCode.Should().Be("TRAINER_NOTFOUND");
        _mockTrainerRepository.Verify(x => x.UpdateAsync(It.IsAny<Trainer>()), Times.Never);
        _mockTrainerRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var trainerId = _fixture.Create<int>();
        var trainerDto = _fixture.Build<TrainerDto>()
            .With(x => x.Id, trainerId)
            .Create();
        var exception = _fixture.Create<Exception>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Antrenör güncellenemedi")
            .With(x => x.ErrorCode, "TRAINER_UPDATE_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<TrainerDto>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (TrainerDto)null)
            .Create();

        _mockTrainerRepository.Setup(x => x.Query()).Throws(exception);
        _mockResponseHelper
            .Setup(x => x.SetError<TrainerDto>(null, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.UpdateAsync(trainerId, trainerDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenTrainerExists_ShouldReturnSuccessAndSoftDelete() {
        // Arrange
        var trainerId = _fixture.Create<int>();
        var trainer = _fixture.Build<Trainer>()
            .With(x => x.Id, trainerId)
            .With(x => x.IsActive, true)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, true)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Message, "Antrenör silindi")
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var trainers = new List<Trainer> { trainer };
        var mockQueryable = trainers.AsQueryable().BuildMock();

        _mockTrainerRepository.Setup(x => x.Query()).Returns(mockQueryable);
        _mockTrainerRepository.Setup(x => x.UpdateAsync(It.IsAny<Trainer>())).ReturnsAsync(trainer);
        _mockTrainerRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _mockResponseHelper.Setup(x => x.SetSuccess(true, "Antrenör silindi")).Returns(expectedResponse);

        // Act
        var result = await _sut.DeleteAsync(trainerId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().BeTrue();
        result.Message.Should().Be("Antrenör silindi");
        trainer.IsActive.Should().BeFalse();
        trainer.UpdatedAt.Should().NotBeNull();
        _mockTrainerRepository.Verify(x => x.UpdateAsync(It.Is<Trainer>(t => t.IsActive == false)), Times.Once);
        _mockTrainerRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenTrainerNotFound_ShouldReturnError() {
        // Arrange
        var trainerId = _fixture.Create<int>();
        var trainers = new List<Trainer>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Antrenör bulunamadý")
            .With(x => x.ErrorCode, "TRAINER_NOTFOUND")
            .With(x => x.StatusCode, 404)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, false)
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .Create();

        var mockQueryable = trainers.AsQueryable().BuildMock();
        _mockTrainerRepository.Setup(x => x.Query()).Returns(mockQueryable);
        _mockResponseHelper
            .Setup(x => x.SetError<bool>(false, "Antrenör bulunamadý", 404, "TRAINER_NOTFOUND"))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.DeleteAsync(trainerId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Data.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.ErrorCode.Should().Be("TRAINER_NOTFOUND");
        _mockTrainerRepository.Verify(x => x.UpdateAsync(It.IsAny<Trainer>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var trainerId = _fixture.Create<int>();
        var exception = _fixture.Create<Exception>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Antrenör silinemedi")
            .With(x => x.ErrorCode, "TRAINER_DELETE_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, false)
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .Create();

        _mockTrainerRepository.Setup(x => x.Query()).Throws(exception);
        _mockResponseHelper
            .Setup(x => x.SetError<bool>(false, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.DeleteAsync(trainerId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Data.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    #endregion

    #region Edge Cases and Boundary Tests

    [Fact]
    public async Task GetAllAsync_WithLargeDataSet_ShouldHandlePerformance() {
        // Arrange
        var trainers = _fixture.CreateMany<Trainer>(100).ToList();
        var trainerDtos = _fixture.CreateMany<TrainerDto>(100).ToList();
        var expectedResponse = _fixture.Build<ServiceResponse<List<TrainerDto>>>()
            .With(x => x.Data, trainerDtos)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var mockQueryable = trainers.AsQueryable().BuildMock();
        _mockTrainerRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<List<TrainerDto>>(It.IsAny<List<Trainer>>())).Returns(trainerDtos);
        _mockResponseHelper.Setup(x => x.SetSuccess(trainerDtos)).Returns(expectedResponse);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().HaveCount(100);
    }

    [Fact]
    public async Task CreateAsync_WhenSaveChangesFails_ShouldReturnError() {
        // Arrange
        var trainerDto = _fixture.Build<TrainerDto>()
            .Without(x => x.Id)
            .Create();
        var trainer = _fixture.Build<Trainer>()
            .With(x => x.IsActive, true)
            .Create();
        var exception = new DbUpdateException();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Antrenör oluþturulamadý")
            .With(x => x.ErrorCode, "TRAINER_CREATE_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<TrainerDto>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (TrainerDto)null)
            .Create();

        _mockMapper
            .Setup(x => x.Map<Trainer>(
                It.IsAny<TrainerDto>(),
                It.IsAny<Action<IMappingOperationOptions<object, Trainer>>>()))
            .Returns(trainer);

        _mockTrainerRepository.Setup(x => x.AddAsync(It.IsAny<Trainer>())).ReturnsAsync(trainer);
        _mockTrainerRepository.Setup(x => x.SaveChangesAsync()).ThrowsAsync(exception);

        _mockResponseHelper
            .Setup(x => x.SetError<TrainerDto>(null, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.CreateAsync(trainerDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    #endregion
}

/// <summary>
/// Test implementation of BaseFactory for testing purposes
/// </summary>
public class TestTrainerServiceFactory : BaseFactory<TrainerService> {
    public TestTrainerServiceFactory(
        UtilityFactory<TrainerService> utilityFactory,
        IRepositoryFactory repositoryFactory,
        IServiceProvider serviceProvider)
        : base(utilityFactory, repositoryFactory, serviceProvider) {
    }

    public override UtilityFactory<TrainerService> CreateUtilityFactory() => utilityFactory;
    public override IRepositoryFactory CreateRepositoryFactory() => repositoryFactory;
}
