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

/// <summary>
/// AppointmentService Business Logic Tests - Complex scenarios
/// </summary>
public class AppointmentServiceBusinessLogicTests {
    private readonly IFixture _fixture;
    private readonly Mock<IRepositoryFactory> _mockRepositoryFactory;
    private readonly Mock<IRepository<Appointment>> _mockAppointmentRepository;
    private readonly Mock<IRepository<TrainerAvailability>> _mockTrainerAvailabilityRepository;
    private readonly Mock<IRepository<Service>> _mockServiceRepository;
    private readonly Mock<IRepository<WorkingHours>> _mockWorkingHoursRepository;
    private readonly Mock<IRepository<TrainerSpecialty>> _mockTrainerSpecialtyRepository;
    private readonly Mock<UtilityFactory<AppointmentService>> _mockUtilityFactory;
    private readonly Mock<IServiceResponseHelper> _mockResponseHelper;
    private readonly Mock<ILogger<AppointmentService>> _mockLogger;
    private readonly Mock<IMapper> _mockMapper;
    private readonly TestAppointmentServiceFactory _testFactory;
    private readonly AppointmentService _sut;

    public AppointmentServiceBusinessLogicTests() {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _mockRepositoryFactory = new Mock<IRepositoryFactory>();
        _mockAppointmentRepository = new Mock<IRepository<Appointment>>();
        _mockTrainerAvailabilityRepository = new Mock<IRepository<TrainerAvailability>>();
        _mockServiceRepository = new Mock<IRepository<Service>>();
        _mockWorkingHoursRepository = new Mock<IRepository<WorkingHours>>();
        _mockTrainerSpecialtyRepository = new Mock<IRepository<TrainerSpecialty>>();
        _mockUtilityFactory = new Mock<UtilityFactory<AppointmentService>>();
        _mockResponseHelper = new Mock<IServiceResponseHelper>();
        _mockLogger = new Mock<ILogger<AppointmentService>>();
        _mockMapper = new Mock<IMapper>();

        // Setup repository factory to return appropriate repositories
        _mockRepositoryFactory.Setup(x => x.CreateRepository<Appointment>()).Returns(_mockAppointmentRepository.Object);
        _mockRepositoryFactory.Setup(x => x.CreateRepository<TrainerAvailability>()).Returns(_mockTrainerAvailabilityRepository.Object);
        _mockRepositoryFactory.Setup(x => x.CreateRepository<Service>()).Returns(_mockServiceRepository.Object);
        _mockRepositoryFactory.Setup(x => x.CreateRepository<WorkingHours>()).Returns(_mockWorkingHoursRepository.Object);
        _mockRepositoryFactory.Setup(x => x.CreateRepository<TrainerSpecialty>()).Returns(_mockTrainerSpecialtyRepository.Object);

        _mockUtilityFactory.Setup(x => x.CreateServiceResponseHelper()).Returns(_mockResponseHelper.Object);
        _mockUtilityFactory.Setup(x => x.CreateLogger()).Returns(_mockLogger.Object);
        _mockUtilityFactory.Setup(x => x.CreateMapper()).Returns(_mockMapper.Object);

        _testFactory = new TestAppointmentServiceFactory(
            _mockUtilityFactory.Object,
            _mockRepositoryFactory.Object,
            Mock.Of<IServiceProvider>());

        _sut = new AppointmentService(_testFactory);
    }

    #region CheckTrainerAvailabilityAsync Tests

    [Fact]
    public async Task CheckTrainerAvailabilityAsync_WhenNoConflictAndAvailable_ShouldReturnSuccess() {
        // Arrange
        var trainerId = 1;
        var appointmentDate = new DateTime(2024, 1, 15, 10, 0, 0); // Monday 10:00
        var durationMinutes = 60;

        var appointments = new List<Appointment>();
        var availabilities = new List<TrainerAvailability> {
            new TrainerAvailability {
                TrainerId = trainerId,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(17, 0, 0),
                IsActive = true
            }
        };

        var mockAppointmentQueryable = appointments.AsQueryable().BuildMock();
        var mockAvailabilityQueryable = availabilities.AsQueryable().BuildMock();

        _mockAppointmentRepository.Setup(x => x.QueryNoTracking()).Returns(mockAppointmentQueryable);
        _mockTrainerAvailabilityRepository.Setup(x => x.QueryNoTracking()).Returns(mockAvailabilityQueryable);

        var expectedResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, true)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Message, "Antrenör müsait")
            .Create();

        _mockResponseHelper.Setup(x => x.SetSuccess(true, "Antrenör müsait")).Returns(expectedResponse);

        // Act
        var result = await _sut.CheckTrainerAvailabilityAsync(trainerId, appointmentDate, durationMinutes);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task CheckTrainerAvailabilityAsync_WhenHasConflict_ShouldReturnError() {
        // Arrange
        var trainerId = 1;
        var appointmentDate = new DateTime(2024, 1, 15, 10, 0, 0);
        var durationMinutes = 60;

        var existingAppointments = new List<Appointment> {
            new Appointment {
                TrainerId = trainerId,
                AppointmentDate = new DateTime(2024, 1, 15, 9, 30, 0),
                DurationMinutes = 60,
                Status = AppointmentStatus.Confirmed,
                IsActive = true
            }
        };

        var mockQueryable = existingAppointments.AsQueryable().BuildMock();
        _mockAppointmentRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);

        var errorResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, false)
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockResponseHelper.Setup(x => x.SetError<bool>(false, It.IsAny<string>(), 400, "APPOINTMENT_001"))
            .Returns(errorResponse);

        // Act
        var result = await _sut.CheckTrainerAvailabilityAsync(trainerId, appointmentDate, durationMinutes);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task CheckTrainerAvailabilityAsync_WhenNotInTrainerSchedule_ShouldReturnError() {
        // Arrange
        var trainerId = 1;
        var appointmentDate = new DateTime(2024, 1, 15, 10, 0, 0);
        var durationMinutes = 60;

        var appointments = new List<Appointment>();
        var availabilities = new List<TrainerAvailability> {
            new TrainerAvailability {
                TrainerId = trainerId,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeSpan(14, 0, 0), // 14:00-17:00 only
                EndTime = new TimeSpan(17, 0, 0),
                IsActive = true
            }
        };

        var mockAppointmentQueryable = appointments.AsQueryable().BuildMock();
        var mockAvailabilityQueryable = availabilities.AsQueryable().BuildMock();

        _mockAppointmentRepository.Setup(x => x.QueryNoTracking()).Returns(mockAppointmentQueryable);
        _mockTrainerAvailabilityRepository.Setup(x => x.QueryNoTracking()).Returns(mockAvailabilityQueryable);

        var errorResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, false)
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockResponseHelper.Setup(x => x.SetError<bool>(false, It.IsAny<string>(), 400, "APPOINTMENT_002"))
            .Returns(errorResponse);

        // Act
        var result = await _sut.CheckTrainerAvailabilityAsync(trainerId, appointmentDate, durationMinutes);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task CheckTrainerAvailabilityAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var trainerId = 1;
        var appointmentDate = DateTime.Now.AddDays(1);
        var durationMinutes = 60;
        var exception = _fixture.Create<Exception>();

        _mockAppointmentRepository.Setup(x => x.QueryNoTracking()).Throws(exception);

        var errorResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, false)
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockResponseHelper.Setup(x => x.SetError<bool>(false, It.IsAny<ErrorInfo>()))
            .Returns(errorResponse);

        // Act
        var result = await _sut.CheckTrainerAvailabilityAsync(trainerId, appointmentDate, durationMinutes);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
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

    #region CheckMemberAvailabilityAsync Tests

    [Fact]
    public async Task CheckMemberAvailabilityAsync_WhenNoConflict_ShouldReturnSuccess() {
        // Arrange
        var memberId = 1;
        var appointmentDate = DateTime.Now.AddDays(1);
        var durationMinutes = 60;

        var appointments = new List<Appointment>();
        var mockQueryable = appointments.AsQueryable().BuildMock();

        _mockAppointmentRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);

        var expectedResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, true)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Message, "Üye müsait")
            .Create();

        _mockResponseHelper.Setup(x => x.SetSuccess(true, "Üye müsait")).Returns(expectedResponse);

        // Act
        var result = await _sut.CheckMemberAvailabilityAsync(memberId, appointmentDate, durationMinutes);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task CheckMemberAvailabilityAsync_WhenHasConflict_ShouldReturnError() {
        // Arrange
        var memberId = 1;
        var appointmentDate = new DateTime(2024, 1, 15, 10, 0, 0);
        var durationMinutes = 60;

        var existingAppointments = new List<Appointment> {
            new Appointment {
                MemberId = memberId,
                AppointmentDate = new DateTime(2024, 1, 15, 10, 30, 0),
                DurationMinutes = 60,
                Status = AppointmentStatus.Confirmed,
                IsActive = true
            }
        };

        var mockQueryable = existingAppointments.AsQueryable().BuildMock();
        _mockAppointmentRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);

        var errorResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, false)
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockResponseHelper.Setup(x => x.SetError<bool>(false, It.IsAny<string>(), 400, "APPOINTMENT_004"))
            .Returns(errorResponse);

        // Act
        var result = await _sut.CheckMemberAvailabilityAsync(memberId, appointmentDate, durationMinutes);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task CheckMemberAvailabilityAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var memberId = 1;
        var appointmentDate = DateTime.Now.AddDays(1);
        var durationMinutes = 60;
        var exception = _fixture.Create<Exception>();

        _mockAppointmentRepository.Setup(x => x.QueryNoTracking()).Throws(exception);

        var errorResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, false)
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockResponseHelper.Setup(x => x.SetError<bool>(false, It.IsAny<ErrorInfo>()))
            .Returns(errorResponse);

        // Act
        var result = await _sut.CheckMemberAvailabilityAsync(memberId, appointmentDate, durationMinutes);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
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

    #region GetMemberAppointmentsAsync Tests

    [Fact]
    public async Task GetMemberAppointmentsAsync_WhenAppointmentsExist_ShouldReturnSuccess() {
        // Arrange
        var memberId = 1;
        var appointments = _fixture.Build<Appointment>()
            .With(x => x.MemberId, memberId)
            .With(x => x.IsActive, true)
            .CreateMany(3)
            .ToList();
        var appointmentDtos = _fixture.CreateMany<AppointmentDto>(3).ToList();
        var expectedResponse = _fixture.Build<ServiceResponse<List<AppointmentDto>>>()
            .With(x => x.Data, appointmentDtos)
            .With(x => x.IsSuccessful, true)
            .Create();

        var mockQueryable = appointments.AsQueryable().BuildMock();
        _mockAppointmentRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<List<AppointmentDto>>(It.IsAny<List<Appointment>>())).Returns(appointmentDtos);
        _mockResponseHelper.Setup(x => x.SetSuccess(appointmentDtos)).Returns(expectedResponse);

        // Act
        var result = await _sut.GetMemberAppointmentsAsync(memberId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetMemberAppointmentsAsync_WhenNoAppointments_ShouldReturnEmptyList() {
        // Arrange
        var memberId = 1;
        var appointments = new List<Appointment>();
        var appointmentDtos = new List<AppointmentDto>();
        var expectedResponse = _fixture.Build<ServiceResponse<List<AppointmentDto>>>()
            .With(x => x.Data, appointmentDtos)
            .With(x => x.IsSuccessful, true)
            .Create();

        var mockQueryable = appointments.AsQueryable().BuildMock();
        _mockAppointmentRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<List<AppointmentDto>>(It.IsAny<List<Appointment>>())).Returns(appointmentDtos);
        _mockResponseHelper.Setup(x => x.SetSuccess(appointmentDtos)).Returns(expectedResponse);

        // Act
        var result = await _sut.GetMemberAppointmentsAsync(memberId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMemberAppointmentsAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var memberId = 1;
        var exception = _fixture.Create<Exception>();
        var errorResponse = _fixture.Build<ServiceResponse<List<AppointmentDto>>>()
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockAppointmentRepository.Setup(x => x.QueryNoTracking()).Throws(exception);
        _mockResponseHelper.Setup(x => x.SetError<List<AppointmentDto>>(null, It.IsAny<ErrorInfo>()))
            .Returns(errorResponse);

        // Act
        var result = await _sut.GetMemberAppointmentsAsync(memberId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
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

    #region GetTrainerAppointmentsAsync Tests

    [Fact]
    public async Task GetTrainerAppointmentsAsync_WhenAppointmentsExist_ShouldReturnSuccess() {
        // Arrange
        var trainerId = 1;
        var appointments = _fixture.Build<Appointment>()
            .With(x => x.TrainerId, trainerId)
            .With(x => x.IsActive, true)
            .CreateMany(3)
            .ToList();
        var appointmentDtos = _fixture.CreateMany<AppointmentDto>(3).ToList();
        var expectedResponse = _fixture.Build<ServiceResponse<List<AppointmentDto>>>()
            .With(x => x.Data, appointmentDtos)
            .With(x => x.IsSuccessful, true)
            .Create();

        var mockQueryable = appointments.AsQueryable().BuildMock();
        _mockAppointmentRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<List<AppointmentDto>>(It.IsAny<List<Appointment>>())).Returns(appointmentDtos);
        _mockResponseHelper.Setup(x => x.SetSuccess(appointmentDtos)).Returns(expectedResponse);

        // Act
        var result = await _sut.GetTrainerAppointmentsAsync(trainerId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetTrainerAppointmentsAsync_WhenNoAppointments_ShouldReturnEmptyList() {
        // Arrange
        var trainerId = 1;
        var appointments = new List<Appointment>();
        var appointmentDtos = new List<AppointmentDto>();
        var expectedResponse = _fixture.Build<ServiceResponse<List<AppointmentDto>>>()
            .With(x => x.Data, appointmentDtos)
            .With(x => x.IsSuccessful, true)
            .Create();

        var mockQueryable = appointments.AsQueryable().BuildMock();
        _mockAppointmentRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<List<AppointmentDto>>(It.IsAny<List<Appointment>>())).Returns(appointmentDtos);
        _mockResponseHelper.Setup(x => x.SetSuccess(appointmentDtos)).Returns(expectedResponse);

        // Act
        var result = await _sut.GetTrainerAppointmentsAsync(trainerId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTrainerAppointmentsAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var trainerId = 1;
        var exception = _fixture.Create<Exception>();
        var errorResponse = _fixture.Build<ServiceResponse<List<AppointmentDto>>>()
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockAppointmentRepository.Setup(x => x.QueryNoTracking()).Throws(exception);
        _mockResponseHelper.Setup(x => x.SetError<List<AppointmentDto>>(null, It.IsAny<ErrorInfo>()))
            .Returns(errorResponse);

        // Act
        var result = await _sut.GetTrainerAppointmentsAsync(trainerId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
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

    #region CancelAppointmentAsync Tests

    [Fact]
    public async Task CancelAppointmentAsync_WhenAppointmentIsPending_ShouldReturnSuccess() {
        // Arrange
        var appointmentId = 1;
        var reason = "Personal reasons";
        var appointment = new Appointment {
            Id = appointmentId,
            Status = AppointmentStatus.Pending,
            IsActive = true
        };

        var appointments = new List<Appointment> { appointment };
        var mockQueryable = appointments.AsQueryable().BuildMock();

        _mockAppointmentRepository.Setup(x => x.Query()).Returns(mockQueryable);
        _mockAppointmentRepository.Setup(x => x.UpdateAsync(It.IsAny<Appointment>())).ReturnsAsync(appointment);
        _mockAppointmentRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        var expectedResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, true)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Message, "Randevu iptal edildi")
            .Create();

        _mockResponseHelper.Setup(x => x.SetSuccess(true, "Randevu iptal edildi")).Returns(expectedResponse);

        // Act
        var result = await _sut.CancelAppointmentAsync(appointmentId, reason);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().BeTrue();
        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
        appointment.Notes.Should().Contain(reason);
    }

    [Fact]
    public async Task CancelAppointmentAsync_WhenAlreadyCancelled_ShouldReturnError() {
        // Arrange
        var appointmentId = 1;
        var appointment = new Appointment {
            Id = appointmentId,
            Status = AppointmentStatus.Cancelled,
            IsActive = true
        };

        var appointments = new List<Appointment> { appointment };
        var mockQueryable = appointments.AsQueryable().BuildMock();

        _mockAppointmentRepository.Setup(x => x.Query()).Returns(mockQueryable);

        var errorResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, false)
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockResponseHelper.Setup(x => x.SetError<bool>(false, "Randevu zaten iptal edilmiþ", 400, "APPOINTMENT_015"))
            .Returns(errorResponse);

        // Act
        var result = await _sut.CancelAppointmentAsync(appointmentId, null);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        _mockAppointmentRepository.Verify(x => x.UpdateAsync(It.IsAny<Appointment>()), Times.Never);
    }

    [Fact]
    public async Task CancelAppointmentAsync_WhenCompleted_ShouldReturnError() {
        // Arrange
        var appointmentId = 1;
        var appointment = new Appointment {
            Id = appointmentId,
            Status = AppointmentStatus.Completed,
            IsActive = true
        };

        var appointments = new List<Appointment> { appointment };
        var mockQueryable = appointments.AsQueryable().BuildMock();

        _mockAppointmentRepository.Setup(x => x.Query()).Returns(mockQueryable);

        var errorResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, false)
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockResponseHelper.Setup(x => x.SetError<bool>(false, "Tamamlanmýþ randevular iptal edilemez", 400, "APPOINTMENT_016"))
            .Returns(errorResponse);

        // Act
        var result = await _sut.CancelAppointmentAsync(appointmentId, null);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        _mockAppointmentRepository.Verify(x => x.UpdateAsync(It.IsAny<Appointment>()), Times.Never);
    }

    [Fact]
    public async Task CancelAppointmentAsync_WhenNotFound_ShouldReturnError() {
        // Arrange
        var appointmentId = 1;
        var appointments = new List<Appointment>();
        var mockQueryable = appointments.AsQueryable().BuildMock();

        _mockAppointmentRepository.Setup(x => x.Query()).Returns(mockQueryable);

        var errorResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, false)
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockResponseHelper.Setup(x => x.SetError<bool>(false, "Randevu bulunamadý", 404, "APPOINTMENT_014"))
            .Returns(errorResponse);

        // Act
        var result = await _sut.CancelAppointmentAsync(appointmentId, null);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public async Task CancelAppointmentAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var appointmentId = 1;
        var exception = _fixture.Create<Exception>();

        _mockAppointmentRepository.Setup(x => x.Query()).Throws(exception);

        var errorResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, false)
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockResponseHelper.Setup(x => x.SetError<bool>(false, It.IsAny<ErrorInfo>()))
            .Returns(errorResponse);

        // Act
        var result = await _sut.CancelAppointmentAsync(appointmentId, null);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
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
}
