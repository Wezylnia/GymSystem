using AutoFixture;
using AutoFixture.AutoMoq;
using AutoMapper;
using FluentAssertions;
using GymSystem.Application.Abstractions.Services.IAppointmentService.Contract;
using GymSystem.Application.Services.Appointments;
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

namespace GymSystem.Tests.Services.Appointments;

public class AppointmentServiceTests {
    private readonly IFixture _fixture;
    private readonly Mock<IRepositoryFactory> _mockRepositoryFactory;
    private readonly Mock<IRepository<Appointment>> _mockAppointmentRepository;
    private readonly Mock<UtilityFactory<AppointmentService>> _mockUtilityFactory;
    private readonly Mock<IServiceResponseHelper> _mockResponseHelper;
    private readonly Mock<ILogger<AppointmentService>> _mockLogger;
    private readonly Mock<IMapper> _mockMapper;
    private readonly TestAppointmentServiceFactory _testFactory;
    private readonly AppointmentService _sut; // System Under Test

    public AppointmentServiceTests() {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        // Configure AutoFixture to handle circular references
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // Create mocks
        _mockRepositoryFactory = new Mock<IRepositoryFactory>();
        _mockAppointmentRepository = new Mock<IRepository<Appointment>>();
        _mockUtilityFactory = new Mock<UtilityFactory<AppointmentService>>();
        _mockResponseHelper = new Mock<IServiceResponseHelper>();
        _mockLogger = new Mock<ILogger<AppointmentService>>();
        _mockMapper = new Mock<IMapper>();

        // Setup factory behaviors
        _mockRepositoryFactory.Setup(x => x.CreateRepository<Appointment>()).Returns(_mockAppointmentRepository.Object);

        _mockUtilityFactory.Setup(x => x.CreateServiceResponseHelper()).Returns(_mockResponseHelper.Object);
        _mockUtilityFactory.Setup(x => x.CreateLogger()).Returns(_mockLogger.Object);
        _mockUtilityFactory.Setup(x => x.CreateMapper()).Returns(_mockMapper.Object);

        // Create test factory
        _testFactory = new TestAppointmentServiceFactory(
            _mockUtilityFactory.Object,
            _mockRepositoryFactory.Object,
            Mock.Of<IServiceProvider>());

        // Create System Under Test
        _sut = new AppointmentService(_testFactory);
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WhenAppointmentsExist_ShouldReturnSuccessWithAppointments() {
        // Arrange
        var appointments = _fixture.CreateMany<Appointment>(3).ToList();
        var appointmentDtos = _fixture.CreateMany<AppointmentDto>(3).ToList();
        var expectedResponse = _fixture.Build<ServiceResponse<List<AppointmentDto>>>()
            .With(x => x.Data, appointmentDtos)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var mockQueryable = appointments.AsQueryable().BuildMock();
        _mockAppointmentRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<List<AppointmentDto>>(It.IsAny<List<Appointment>>())).Returns(appointmentDtos);
        _mockResponseHelper.Setup(x => x.SetSuccess(appointmentDtos)).Returns(expectedResponse);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().HaveCount(3);
        _mockAppointmentRepository.Verify(x => x.QueryNoTracking(), Times.Once);
        _mockMapper.Verify(x => x.Map<List<AppointmentDto>>(It.IsAny<List<Appointment>>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoActiveAppointments_ShouldReturnEmptyList() {
        // Arrange
        var appointments = new List<Appointment>();
        var appointmentDtos = new List<AppointmentDto>();
        var expectedResponse = _fixture.Build<ServiceResponse<List<AppointmentDto>>>()
            .With(x => x.Data, appointmentDtos)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var mockQueryable = appointments.AsQueryable().BuildMock();
        _mockAppointmentRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<List<AppointmentDto>>(It.IsAny<List<Appointment>>())).Returns(appointmentDtos);
        _mockResponseHelper.Setup(x => x.SetSuccess(appointmentDtos)).Returns(expectedResponse);

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
            .With(x => x.ErrorMessage, "Randevular getirilemedi")
            .With(x => x.ErrorCode, "APPOINTMENT_GETALL_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<List<AppointmentDto>>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (List<AppointmentDto>)null)
            .Create();

        _mockAppointmentRepository.Setup(x => x.QueryNoTracking()).Throws(exception);
        _mockResponseHelper
            .Setup(x => x.SetError<List<AppointmentDto>>(null, It.IsAny<ErrorInfo>()))
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
    public async Task GetByIdAsync_WhenAppointmentExists_ShouldReturnSuccessWithAppointment() {
        // Arrange
        var appointmentId = _fixture.Create<int>();
        var appointment = _fixture.Build<Appointment>()
            .With(x => x.Id, appointmentId)
            .With(x => x.IsActive, true)
            .Create();
        var appointmentDto = _fixture.Build<AppointmentDto>()
            .With(x => x.Id, appointmentId)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<AppointmentDto?>>()
            .With(x => x.Data, appointmentDto)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var appointments = new List<Appointment> { appointment };
        var mockQueryable = appointments.AsQueryable().BuildMock();

        _mockAppointmentRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<AppointmentDto>(It.IsAny<Appointment>())).Returns(appointmentDto);
        _mockResponseHelper.Setup(x => x.SetSuccess<AppointmentDto?>(appointmentDto)).Returns(expectedResponse);

        // Act
        var result = await _sut.GetByIdAsync(appointmentId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(appointmentId);
        _mockMapper.Verify(x => x.Map<AppointmentDto>(It.IsAny<Appointment>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenAppointmentNotFound_ShouldReturnError() {
        // Arrange
        var appointmentId = _fixture.Create<int>();
        var appointments = new List<Appointment>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Randevu bulunamadý")
            .With(x => x.ErrorCode, "APPOINTMENT_NOTFOUND")
            .With(x => x.StatusCode, 404)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<AppointmentDto?>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (AppointmentDto)null)
            .Create();

        var mockQueryable = appointments.AsQueryable().BuildMock();
        _mockAppointmentRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockResponseHelper
            .Setup(x => x.SetError<AppointmentDto?>(null, "Randevu bulunamadý", 404, "APPOINTMENT_NOTFOUND"))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.GetByIdAsync(appointmentId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.ErrorCode.Should().Be("APPOINTMENT_NOTFOUND");
        _mockMapper.Verify(x => x.Map<AppointmentDto>(It.IsAny<Appointment>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var appointmentId = _fixture.Create<int>();
        var exception = _fixture.Create<Exception>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Randevu getirilemedi")
            .With(x => x.ErrorCode, "APPOINTMENT_GET_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<AppointmentDto?>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (AppointmentDto)null)
            .Create();

        _mockAppointmentRepository.Setup(x => x.QueryNoTracking()).Throws(exception);
        _mockResponseHelper
            .Setup(x => x.SetError<AppointmentDto?>(null, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.GetByIdAsync(appointmentId);

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
    public async Task CreateAsync_WithValidDto_ShouldReturnSuccessWithCreatedAppointment() {
        // Arrange
        var appointmentDto = _fixture.Build<AppointmentDto>()
            .Without(x => x.Id)
            .Create();
        var appointmentId = _fixture.Create<int>();
        var appointment = _fixture.Build<Appointment>()
            .With(x => x.Id, appointmentId)
            .With(x => x.IsActive, true)
            .With(x => x.Status, AppointmentStatus.Pending)
            .Create();
        var createdAppointmentDto = _fixture.Build<AppointmentDto>()
            .With(x => x.Id, appointmentId)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<AppointmentDto>>()
            .With(x => x.Data, createdAppointmentDto)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Message, "Randevu oluþturuldu")
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        _mockMapper
            .Setup(x => x.Map<Appointment>(
                It.IsAny<AppointmentDto>(),
                It.IsAny<Action<IMappingOperationOptions<object, Appointment>>>()))
            .Returns(appointment)
            .Verifiable();

        _mockMapper
            .Setup(x => x.Map<AppointmentDto>(It.IsAny<Appointment>()))
            .Returns(createdAppointmentDto)
            .Verifiable();

        _mockAppointmentRepository
            .Setup(x => x.AddAsync(It.IsAny<Appointment>()))
            .ReturnsAsync(appointment)
            .Verifiable();

        _mockAppointmentRepository
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1)
            .Verifiable();

        _mockResponseHelper
            .Setup(x => x.SetSuccess(It.IsAny<AppointmentDto>(), "Randevu oluþturuldu"))
            .Returns(expectedResponse)
            .Verifiable();

        // Act
        var result = await _sut.CreateAsync(appointmentDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(appointmentId);
        result.Message.Should().Be("Randevu oluþturuldu");

        // Verify all mocks were called
        _mockMapper.Verify();
        _mockAppointmentRepository.Verify();
        _mockResponseHelper.Verify();
    }

    [Fact]
    public async Task CreateAsync_WhenRepositoryThrowsException_ShouldReturnError() {
        // Arrange
        var appointmentDto = _fixture.Build<AppointmentDto>()
            .Without(x => x.Id)
            .Create();
        var appointment = _fixture.Build<Appointment>()
            .With(x => x.IsActive, true)
            .With(x => x.Status, AppointmentStatus.Pending)
            .Create();
        var exception = _fixture.Create<Exception>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Randevu oluþturulamadý")
            .With(x => x.ErrorCode, "APPOINTMENT_CREATE_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<AppointmentDto>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (AppointmentDto)null)
            .Create();

        _mockMapper
            .Setup(x => x.Map<Appointment>(
                It.IsAny<AppointmentDto>(),
                It.IsAny<Action<IMappingOperationOptions<object, Appointment>>>()))
            .Returns(appointment);

        _mockAppointmentRepository.Setup(x => x.AddAsync(It.IsAny<Appointment>())).ThrowsAsync(exception);

        _mockResponseHelper
            .Setup(x => x.SetError<AppointmentDto>(null, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.CreateAsync(appointmentDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().NotBeNull();

        _mockAppointmentRepository.Verify(x => x.AddAsync(It.IsAny<Appointment>()), Times.Once);
        _mockAppointmentRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidDto_ShouldReturnSuccessWithUpdatedAppointment() {
        // Arrange
        var appointmentId = _fixture.Create<int>();
        var appointmentDto = _fixture.Build<AppointmentDto>()
            .With(x => x.Id, appointmentId)
            .Create();
        var existingAppointment = _fixture.Build<Appointment>()
            .With(x => x.Id, appointmentId)
            .With(x => x.IsActive, true)
            .Create();
        var updatedAppointmentDto = _fixture.Build<AppointmentDto>()
            .With(x => x.Id, appointmentId)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<AppointmentDto>>()
            .With(x => x.Data, updatedAppointmentDto)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Message, "Randevu güncellendi")
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var appointments = new List<Appointment> { existingAppointment };
        var mockQueryable = appointments.AsQueryable().BuildMock();

        _mockAppointmentRepository.Setup(x => x.Query()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map(appointmentDto, existingAppointment)).Returns(existingAppointment);
        _mockMapper.Setup(x => x.Map<AppointmentDto>(It.IsAny<Appointment>())).Returns(updatedAppointmentDto);
        _mockAppointmentRepository.Setup(x => x.UpdateAsync(It.IsAny<Appointment>())).ReturnsAsync(existingAppointment);
        _mockAppointmentRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _mockResponseHelper.Setup(x => x.SetSuccess(updatedAppointmentDto, "Randevu güncellendi")).Returns(expectedResponse);

        // Act
        var result = await _sut.UpdateAsync(appointmentId, appointmentDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Message.Should().Be("Randevu güncellendi");
        _mockAppointmentRepository.Verify(x => x.UpdateAsync(It.IsAny<Appointment>()), Times.Once);
        _mockAppointmentRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenAppointmentNotFound_ShouldReturnError() {
        // Arrange
        var appointmentId = _fixture.Create<int>();
        var appointmentDto = _fixture.Build<AppointmentDto>()
            .With(x => x.Id, appointmentId)
            .Create();
        var appointments = new List<Appointment>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Randevu bulunamadý")
            .With(x => x.ErrorCode, "APPOINTMENT_NOTFOUND")
            .With(x => x.StatusCode, 404)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<AppointmentDto>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (AppointmentDto)null)
            .Create();

        var mockQueryable = appointments.AsQueryable().BuildMock();
        _mockAppointmentRepository.Setup(x => x.Query()).Returns(mockQueryable);
        _mockResponseHelper
            .Setup(x => x.SetError<AppointmentDto>(null, "Randevu bulunamadý", 404, "APPOINTMENT_NOTFOUND"))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.UpdateAsync(appointmentId, appointmentDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.ErrorCode.Should().Be("APPOINTMENT_NOTFOUND");
        _mockAppointmentRepository.Verify(x => x.UpdateAsync(It.IsAny<Appointment>()), Times.Never);
        _mockAppointmentRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var appointmentId = _fixture.Create<int>();
        var appointmentDto = _fixture.Build<AppointmentDto>()
            .With(x => x.Id, appointmentId)
            .Create();
        var exception = _fixture.Create<Exception>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Randevu güncellenemedi")
            .With(x => x.ErrorCode, "APPOINTMENT_UPDATE_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<AppointmentDto>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (AppointmentDto)null)
            .Create();

        _mockAppointmentRepository.Setup(x => x.Query()).Throws(exception);
        _mockResponseHelper
            .Setup(x => x.SetError<AppointmentDto>(null, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.UpdateAsync(appointmentId, appointmentDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenAppointmentExists_ShouldReturnSuccessAndSoftDelete() {
        // Arrange
        var appointmentId = _fixture.Create<int>();
        var appointment = _fixture.Build<Appointment>()
            .With(x => x.Id, appointmentId)
            .With(x => x.IsActive, true)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, true)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Message, "Randevu silindi")
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var appointments = new List<Appointment> { appointment };
        var mockQueryable = appointments.AsQueryable().BuildMock();

        _mockAppointmentRepository.Setup(x => x.Query()).Returns(mockQueryable);
        _mockAppointmentRepository.Setup(x => x.UpdateAsync(It.IsAny<Appointment>())).ReturnsAsync(appointment);
        _mockAppointmentRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _mockResponseHelper.Setup(x => x.SetSuccess(true, "Randevu silindi")).Returns(expectedResponse);

        // Act
        var result = await _sut.DeleteAsync(appointmentId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().BeTrue();
        result.Message.Should().Be("Randevu silindi");
        appointment.IsActive.Should().BeFalse();
        appointment.UpdatedAt.Should().NotBeNull();
        _mockAppointmentRepository.Verify(x => x.UpdateAsync(It.Is<Appointment>(a => a.IsActive == false)), Times.Once);
        _mockAppointmentRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenAppointmentNotFound_ShouldReturnError() {
        // Arrange
        var appointmentId = _fixture.Create<int>();
        var appointments = new List<Appointment>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Randevu bulunamadý")
            .With(x => x.ErrorCode, "APPOINTMENT_NOTFOUND")
            .With(x => x.StatusCode, 404)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, false)
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .Create();

        var mockQueryable = appointments.AsQueryable().BuildMock();
        _mockAppointmentRepository.Setup(x => x.Query()).Returns(mockQueryable);
        _mockResponseHelper
            .Setup(x => x.SetError<bool>(false, "Randevu bulunamadý", 404, "APPOINTMENT_NOTFOUND"))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.DeleteAsync(appointmentId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Data.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.ErrorCode.Should().Be("APPOINTMENT_NOTFOUND");
        _mockAppointmentRepository.Verify(x => x.UpdateAsync(It.IsAny<Appointment>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var appointmentId = _fixture.Create<int>();
        var exception = _fixture.Create<Exception>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Randevu silinemedi")
            .With(x => x.ErrorCode, "APPOINTMENT_DELETE_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, false)
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .Create();

        _mockAppointmentRepository.Setup(x => x.Query()).Throws(exception);
        _mockResponseHelper
            .Setup(x => x.SetError<bool>(false, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.DeleteAsync(appointmentId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Data.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    #endregion

    #region Edge Cases and Business Logic Tests

    [Fact]
    public async Task CreateAsync_WithPastDate_ShouldHandleValidation() {
        // Arrange
        var appointmentDto = _fixture.Build<AppointmentDto>()
            .Without(x => x.Id)
            .With(x => x.AppointmentDate, DateTime.Now.AddDays(-1))
            .Create();
        var appointment = _fixture.Build<Appointment>()
            .With(x => x.IsActive, true)
            .Create();
        var createdAppointmentDto = _fixture.Build<AppointmentDto>().Create();
        var expectedResponse = _fixture.Build<ServiceResponse<AppointmentDto>>()
            .With(x => x.Data, createdAppointmentDto)
            .With(x => x.IsSuccessful, true)
            .Create();

        _mockMapper
            .Setup(x => x.Map<Appointment>(
                It.IsAny<AppointmentDto>(),
                It.IsAny<Action<IMappingOperationOptions<object, Appointment>>>()))
            .Returns(appointment);

        _mockMapper.Setup(x => x.Map<AppointmentDto>(It.IsAny<Appointment>())).Returns(createdAppointmentDto);
        _mockAppointmentRepository.Setup(x => x.AddAsync(It.IsAny<Appointment>())).ReturnsAsync(appointment);
        _mockAppointmentRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _mockResponseHelper.Setup(x => x.SetSuccess(It.IsAny<AppointmentDto>(), It.IsAny<string>())).Returns(expectedResponse);

        // Act
        var result = await _sut.CreateAsync(appointmentDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    [InlineData(-120)]
    public async Task CreateAsync_WithInvalidDuration_ShouldHandleGracefully(int duration) {
        // Arrange
        var appointmentDto = _fixture.Build<AppointmentDto>()
            .Without(x => x.Id)
            .With(x => x.DurationMinutes, duration)
            .Create();
        var appointment = _fixture.Build<Appointment>()
            .With(x => x.IsActive, true)
            .With(x => x.DurationMinutes, duration)
            .Create();
        var createdAppointmentDto = _fixture.Build<AppointmentDto>()
            .With(x => x.DurationMinutes, duration)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<AppointmentDto>>()
            .With(x => x.Data, createdAppointmentDto)
            .With(x => x.IsSuccessful, true)
            .Create();

        _mockMapper
            .Setup(x => x.Map<Appointment>(
                It.IsAny<AppointmentDto>(),
                It.IsAny<Action<IMappingOperationOptions<object, Appointment>>>()))
            .Returns(appointment);

        _mockMapper.Setup(x => x.Map<AppointmentDto>(It.IsAny<Appointment>())).Returns(createdAppointmentDto);
        _mockAppointmentRepository.Setup(x => x.AddAsync(It.IsAny<Appointment>())).ReturnsAsync(appointment);
        _mockAppointmentRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _mockResponseHelper.Setup(x => x.SetSuccess(It.IsAny<AppointmentDto>(), It.IsAny<string>())).Returns(expectedResponse);

        // Act
        var result = await _sut.CreateAsync(appointmentDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ChangingCompletedStatus_ShouldAllowUpdate() {
        // Arrange
        var appointmentId = _fixture.Create<int>();
        var appointmentDto = _fixture.Build<AppointmentDto>()
            .With(x => x.Id, appointmentId)
            .With(x => x.Status, "Cancelled")
            .Create();
        var existingAppointment = _fixture.Build<Appointment>()
            .With(x => x.Id, appointmentId)
            .With(x => x.Status, AppointmentStatus.Completed)
            .With(x => x.IsActive, true)
            .Create();
        var updatedAppointmentDto = _fixture.Build<AppointmentDto>()
            .With(x => x.Id, appointmentId)
            .With(x => x.Status, "Cancelled")
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<AppointmentDto>>()
            .With(x => x.Data, updatedAppointmentDto)
            .With(x => x.IsSuccessful, true)
            .Create();

        var appointments = new List<Appointment> { existingAppointment };
        var mockQueryable = appointments.AsQueryable().BuildMock();

        _mockAppointmentRepository.Setup(x => x.Query()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map(appointmentDto, existingAppointment)).Returns(existingAppointment);
        _mockMapper.Setup(x => x.Map<AppointmentDto>(It.IsAny<Appointment>())).Returns(updatedAppointmentDto);
        _mockAppointmentRepository.Setup(x => x.UpdateAsync(It.IsAny<Appointment>())).ReturnsAsync(existingAppointment);
        _mockAppointmentRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _mockResponseHelper.Setup(x => x.SetSuccess(updatedAppointmentDto, "Randevu güncellendi")).Returns(expectedResponse);

        // Act
        var result = await _sut.UpdateAsync(appointmentId, appointmentDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WhenAppointmentIsInProgress_ShouldStillDelete() {
        // Arrange
        var appointmentId = _fixture.Create<int>();
        var appointment = _fixture.Build<Appointment>()
            .With(x => x.Id, appointmentId)
            .With(x => x.IsActive, true)
            .With(x => x.Status, AppointmentStatus.Confirmed)
            .With(x => x.AppointmentDate, DateTime.Now.AddHours(-1))
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, true)
            .With(x => x.IsSuccessful, true)
            .Create();

        var appointments = new List<Appointment> { appointment };
        var mockQueryable = appointments.AsQueryable().BuildMock();

        _mockAppointmentRepository.Setup(x => x.Query()).Returns(mockQueryable);
        _mockAppointmentRepository.Setup(x => x.UpdateAsync(It.IsAny<Appointment>())).ReturnsAsync(appointment);
        _mockAppointmentRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _mockResponseHelper.Setup(x => x.SetSuccess(true, "Randevu silindi")).Returns(expectedResponse);

        // Act
        var result = await _sut.DeleteAsync(appointmentId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        appointment.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllAsync_WithMixedStatuses_ShouldReturnAll() {
        // Arrange
        var appointments = new List<Appointment> {
            _fixture.Build<Appointment>().With(x => x.Status, AppointmentStatus.Pending).Create(),
            _fixture.Build<Appointment>().With(x => x.Status, AppointmentStatus.Confirmed).Create(),
            _fixture.Build<Appointment>().With(x => x.Status, AppointmentStatus.Completed).Create(),
            _fixture.Build<Appointment>().With(x => x.Status, AppointmentStatus.Cancelled).Create()
        };
        var appointmentDtos = _fixture.CreateMany<AppointmentDto>(4).ToList();
        var expectedResponse = _fixture.Build<ServiceResponse<List<AppointmentDto>>>()
            .With(x => x.Data, appointmentDtos)
            .With(x => x.IsSuccessful, true)
            .Create();

        var mockQueryable = appointments.AsQueryable().BuildMock();
        _mockAppointmentRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<List<AppointmentDto>>(It.IsAny<List<Appointment>>())).Returns(appointmentDtos);
        _mockResponseHelper.Setup(x => x.SetSuccess(appointmentDtos)).Returns(expectedResponse);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().HaveCount(4);
    }

    #endregion
}

/// <summary>
/// Test implementation of BaseFactory for testing purposes
/// </summary>
public class TestAppointmentServiceFactory : BaseFactory<AppointmentService> {
    public TestAppointmentServiceFactory(
        UtilityFactory<AppointmentService> utilityFactory,
        IRepositoryFactory repositoryFactory,
        IServiceProvider serviceProvider)
        : base(utilityFactory, repositoryFactory, serviceProvider) {
    }

    public override UtilityFactory<AppointmentService> CreateUtilityFactory() => utilityFactory;
    public override IRepositoryFactory CreateRepositoryFactory() => repositoryFactory;
}
