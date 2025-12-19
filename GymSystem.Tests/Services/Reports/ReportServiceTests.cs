using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using GymSystem.Application.Abstractions.Services.IAppointmentService;
using GymSystem.Application.Abstractions.Services.IAppointmentService.Contract;
using GymSystem.Application.Abstractions.Services.IGymLocationService;
using GymSystem.Application.Abstractions.Services.IGymLocationService.Contract;
using GymSystem.Application.Abstractions.Services.IMembershipRequestService;
using GymSystem.Application.Abstractions.Services.IMembershipRequestService.Contract;
using GymSystem.Application.Abstractions.Services.IServiceService;
using GymSystem.Application.Abstractions.Services.IServiceService.Contract;
using GymSystem.Application.Abstractions.Services.ITrainerService;
using GymSystem.Application.Abstractions.Services.ITrainerService.Contract;
using GymSystem.Application.Services.Reports;
using GymSystem.Common.Factory.Managers;
using GymSystem.Common.Factory.Repository;
using GymSystem.Common.Factory.Utility;
using GymSystem.Common.Helpers;
using GymSystem.Common.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace GymSystem.Tests.Services.Reports;

public class ReportServiceTests {
    private readonly IFixture _fixture;
    private readonly Mock<IRepositoryFactory> _mockRepositoryFactory;
    private readonly Mock<UtilityFactory<ReportService>> _mockUtilityFactory;
    private readonly Mock<IServiceResponseHelper> _mockResponseHelper;
    private readonly Mock<ILogger<ReportService>> _mockLogger;
    private readonly Mock<IAppointmentService> _mockAppointmentService;
    private readonly Mock<ITrainerService> _mockTrainerService;
    private readonly Mock<IServiceService> _mockServiceService;
    private readonly Mock<IMembershipRequestService> _mockMembershipRequestService;
    private readonly Mock<IGymLocationService> _mockGymLocationService;
    private readonly TestReportServiceFactory _testFactory;
    private readonly ReportService _sut;

    public ReportServiceTests() {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _mockRepositoryFactory = new Mock<IRepositoryFactory>();
        _mockUtilityFactory = new Mock<UtilityFactory<ReportService>>();
        _mockResponseHelper = new Mock<IServiceResponseHelper>();
        _mockLogger = new Mock<ILogger<ReportService>>();
        _mockAppointmentService = new Mock<IAppointmentService>();
        _mockTrainerService = new Mock<ITrainerService>();
        _mockServiceService = new Mock<IServiceService>();
        _mockMembershipRequestService = new Mock<IMembershipRequestService>();
        _mockGymLocationService = new Mock<IGymLocationService>();

        _mockUtilityFactory.Setup(x => x.CreateServiceResponseHelper()).Returns(_mockResponseHelper.Object);
        _mockUtilityFactory.Setup(x => x.CreateLogger()).Returns(_mockLogger.Object);

        _testFactory = new TestReportServiceFactory(
            _mockUtilityFactory.Object,
            _mockRepositoryFactory.Object,
            Mock.Of<IServiceProvider>());

        _sut = new ReportService(
            _testFactory,
            _mockAppointmentService.Object,
            _mockTrainerService.Object,
            _mockServiceService.Object,
            _mockMembershipRequestService.Object,
            _mockGymLocationService.Object,
            _mockLogger.Object);
    }

    #region GetPopularServicesAsync Tests

    [Fact]
    public async Task GetPopularServicesAsync_WhenDataExists_ShouldReturnPopularServices() {
        // Arrange
        var appointments = _fixture.Build<AppointmentDto>()
            .With(x => x.IsActive, true)
            .With(x => x.Status, "Confirmed")
            .CreateMany(5)
            .ToList();

        var services = _fixture.CreateMany<ServiceDto>(3).ToList();

        var appointmentsResponse = _fixture.Build<ServiceResponse<List<AppointmentDto>>>()
            .With(x => x.Data, appointments)
            .With(x => x.IsSuccessful, true)
            .Create();

        var servicesResponse = _fixture.Build<ServiceResponse<List<ServiceDto>>>()
            .With(x => x.Data, services)
            .With(x => x.IsSuccessful, true)
            .Create();

        var expectedResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, true)
            .Create();

        _mockAppointmentService.Setup(x => x.GetAllAsync()).ReturnsAsync(appointmentsResponse);
        _mockServiceService.Setup(x => x.GetAllAsync()).ReturnsAsync(servicesResponse);
        _mockResponseHelper.Setup(x => x.SetSuccess<object>(It.IsAny<object>())).Returns(expectedResponse);

        // Act
        var result = await _sut.GetPopularServicesAsync(5);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        _mockAppointmentService.Verify(x => x.GetAllAsync(), Times.Once);
        _mockServiceService.Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetPopularServicesAsync_WhenServicesNotAvailable_ShouldReturnError() {
        // Arrange
        var appointmentsResponse = _fixture.Build<ServiceResponse<List<AppointmentDto>>>()
            .With(x => x.IsSuccessful, false)
            .Create();

        var errorResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockAppointmentService.Setup(x => x.GetAllAsync()).ReturnsAsync(appointmentsResponse);
        _mockResponseHelper.Setup(x => x.SetError<object>(null, "Veriler alýnamadý", 500, "REPORT_008")).Returns(errorResponse);

        // Act
        var result = await _sut.GetPopularServicesAsync(5);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public async Task GetPopularServicesAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var exception = _fixture.Create<Exception>();
        var errorResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockAppointmentService.Setup(x => x.GetAllAsync()).ThrowsAsync(exception);
        _mockResponseHelper.Setup(x => x.SetError<object>(null, It.IsAny<ErrorInfo>())).Returns(errorResponse);

        // Act
        var result = await _sut.GetPopularServicesAsync(5);

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

    #region GetMembershipStatisticsAsync Tests

    [Fact]
    public async Task GetMembershipStatisticsAsync_WhenDataExists_ShouldReturnStatistics() {
        // Arrange
        var requests = _fixture.Build<MembershipRequestDto>()
            .With(x => x.Status, "Approved")
            .CreateMany(5)
            .ToList();

        var requestsResponse = _fixture.Build<ServiceResponse<List<MembershipRequestDto>>>()
            .With(x => x.Data, requests)
            .With(x => x.IsSuccessful, true)
            .Create();

        var expectedResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, true)
            .Create();

        _mockMembershipRequestService.Setup(x => x.GetAllRequestsAsync()).ReturnsAsync(requestsResponse);
        _mockResponseHelper.Setup(x => x.SetSuccess<object>(It.IsAny<object>())).Returns(expectedResponse);

        // Act
        var result = await _sut.GetMembershipStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        _mockMembershipRequestService.Verify(x => x.GetAllRequestsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetMembershipStatisticsAsync_WhenRequestsNotAvailable_ShouldReturnError() {
        // Arrange
        var requestsResponse = _fixture.Build<ServiceResponse<List<MembershipRequestDto>>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Data, (List<MembershipRequestDto>)null)
            .Create();

        var errorResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockMembershipRequestService.Setup(x => x.GetAllRequestsAsync()).ReturnsAsync(requestsResponse);
        _mockResponseHelper.Setup(x => x.SetError<object>(null, "Talepler alýnamadý", 500, "REPORT_015")).Returns(errorResponse);

        // Act
        var result = await _sut.GetMembershipStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public async Task GetMembershipStatisticsAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var exception = _fixture.Create<Exception>();
        var errorResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockMembershipRequestService.Setup(x => x.GetAllRequestsAsync()).ThrowsAsync(exception);
        _mockResponseHelper.Setup(x => x.SetError<object>(null, It.IsAny<ErrorInfo>())).Returns(errorResponse);

        // Act
        var result = await _sut.GetMembershipStatisticsAsync();

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

    #region GetTrainersBySpecialtyAsync Tests

    [Fact]
    public async Task GetTrainersBySpecialtyAsync_WhenSpecialtyMatchesAndTrainersExist_ShouldReturnTrainers() {
        // Arrange
        var specialty = "Yoga";
        var services = _fixture.Build<ServiceDto>()
            .With(x => x.Name, "Yoga Classes")
            .With(x => x.GymLocationId, 1)
            .CreateMany(1)
            .ToList();

        var trainers = _fixture.Build<TrainerDto>()
            .With(x => x.GymLocationId, 1)
            .With(x => x.IsActive, true)
            .CreateMany(3)
            .ToList();

        var servicesResponse = _fixture.Build<ServiceResponse<List<ServiceDto>>>()
            .With(x => x.Data, services)
            .With(x => x.IsSuccessful, true)
            .Create();

        var trainersResponse = _fixture.Build<ServiceResponse<List<TrainerDto>>>()
            .With(x => x.Data, trainers)
            .With(x => x.IsSuccessful, true)
            .Create();

        var expectedResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, true)
            .Create();

        _mockServiceService.Setup(x => x.GetAllAsync()).ReturnsAsync(servicesResponse);
        _mockTrainerService.Setup(x => x.GetAllAsync()).ReturnsAsync(trainersResponse);
        _mockResponseHelper.Setup(x => x.SetSuccess<object>(It.IsAny<object>())).Returns(expectedResponse);

        // Act
        var result = await _sut.GetTrainersBySpecialtyAsync(specialty);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        _mockServiceService.Verify(x => x.GetAllAsync(), Times.Once);
        _mockTrainerService.Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetTrainersBySpecialtyAsync_WhenSpecialtyNotFound_ShouldReturnEmptyWithMessage() {
        // Arrange
        var specialty = "NonExistent";
        var services = _fixture.CreateMany<ServiceDto>(3).ToList();

        var servicesResponse = _fixture.Build<ServiceResponse<List<ServiceDto>>>()
            .With(x => x.Data, services)
            .With(x => x.IsSuccessful, true)
            .Create();

        var expectedResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, true)
            .Create();

        _mockServiceService.Setup(x => x.GetAllAsync()).ReturnsAsync(servicesResponse);
        _mockResponseHelper.Setup(x => x.SetSuccess<object>(It.IsAny<object>())).Returns(expectedResponse);

        // Act
        var result = await _sut.GetTrainersBySpecialtyAsync(specialty);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        _mockServiceService.Verify(x => x.GetAllAsync(), Times.Once);
        _mockTrainerService.Verify(x => x.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task GetTrainersBySpecialtyAsync_WhenServicesNotAvailable_ShouldReturnError() {
        // Arrange
        var specialty = "Yoga";
        var servicesResponse = _fixture.Build<ServiceResponse<List<ServiceDto>>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Data, (List<ServiceDto>)null)
            .Create();

        var errorResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockServiceService.Setup(x => x.GetAllAsync()).ReturnsAsync(servicesResponse);
        _mockResponseHelper.Setup(x => x.SetError<object>(null, "Hizmetler alýnamadý", 500, "REPORT_001")).Returns(errorResponse);

        // Act
        var result = await _sut.GetTrainersBySpecialtyAsync(specialty);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public async Task GetTrainersBySpecialtyAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var specialty = "Yoga";
        var exception = _fixture.Create<Exception>();
        var errorResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockServiceService.Setup(x => x.GetAllAsync()).ThrowsAsync(exception);
        _mockResponseHelper.Setup(x => x.SetError<object>(null, It.IsAny<ErrorInfo>())).Returns(errorResponse);

        // Act
        var result = await _sut.GetTrainersBySpecialtyAsync(specialty);

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

    #region GetAvailableTrainersWithDetailsAsync Tests

    [Fact]
    public async Task GetAvailableTrainersWithDetailsAsync_WhenTrainersAvailable_ShouldReturnDetails() {
        // Arrange
        var serviceId = _fixture.Create<int>();
        var appointmentDateTime = DateTime.Now.AddDays(1);
        var durationMinutes = 60;

        var availableTrainerIds = new List<int> { 1, 2, 3 };
        var trainers = _fixture.Build<TrainerDto>()
            .With(x => x.Id, () => availableTrainerIds[new Random().Next(availableTrainerIds.Count)])
            .CreateMany(3)
            .ToList();

        var availableResponse = _fixture.Build<ServiceResponse<List<int>>>()
            .With(x => x.Data, availableTrainerIds)
            .With(x => x.IsSuccessful, true)
            .Create();

        var trainersResponse = _fixture.Build<ServiceResponse<List<TrainerDto>>>()
            .With(x => x.Data, trainers)
            .With(x => x.IsSuccessful, true)
            .Create();

        var expectedResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, true)
            .Create();

        _mockAppointmentService.Setup(x => x.GetAvailableTrainersAsync(serviceId, appointmentDateTime, durationMinutes))
            .ReturnsAsync(availableResponse);
        _mockTrainerService.Setup(x => x.GetAllAsync()).ReturnsAsync(trainersResponse);
        _mockResponseHelper.Setup(x => x.SetSuccess<object>(It.IsAny<object>())).Returns(expectedResponse);

        // Act
        var result = await _sut.GetAvailableTrainersWithDetailsAsync(serviceId, appointmentDateTime, durationMinutes);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        _mockAppointmentService.Verify(x => x.GetAvailableTrainersAsync(serviceId, appointmentDateTime, durationMinutes), Times.Once);
        _mockTrainerService.Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAvailableTrainersWithDetailsAsync_WhenNoTrainersAvailable_ShouldReturnEmpty() {
        // Arrange
        var serviceId = _fixture.Create<int>();
        var appointmentDateTime = DateTime.Now.AddDays(1);
        var durationMinutes = 60;

        var availableResponse = _fixture.Build<ServiceResponse<List<int>>>()
            .With(x => x.Data, new List<int>())
            .With(x => x.IsSuccessful, true)
            .Create();

        var trainersResponse = _fixture.Build<ServiceResponse<List<TrainerDto>>>()
            .With(x => x.Data, _fixture.CreateMany<TrainerDto>(3).ToList())
            .With(x => x.IsSuccessful, true)
            .Create();

        var expectedResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, true)
            .Create();

        _mockAppointmentService.Setup(x => x.GetAvailableTrainersAsync(serviceId, appointmentDateTime, durationMinutes))
            .ReturnsAsync(availableResponse);
        _mockTrainerService.Setup(x => x.GetAllAsync()).ReturnsAsync(trainersResponse);
        _mockResponseHelper.Setup(x => x.SetSuccess<object>(It.IsAny<object>())).Returns(expectedResponse);

        // Act
        var result = await _sut.GetAvailableTrainersWithDetailsAsync(serviceId, appointmentDateTime, durationMinutes);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task GetAvailableTrainersWithDetailsAsync_WhenAvailabilityCheckFails_ShouldReturnError() {
        // Arrange
        var serviceId = _fixture.Create<int>();
        var appointmentDateTime = DateTime.Now.AddDays(1);
        var durationMinutes = 60;

        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Uygun antrenörler alýnamadý")
            .With(x => x.ErrorCode, "REPORT_004")
            .With(x => x.StatusCode, 500)
            .Create();

        var availableResponse = _fixture.Build<ServiceResponse<List<int>>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .Create();

        var errorResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockAppointmentService.Setup(x => x.GetAvailableTrainersAsync(serviceId, appointmentDateTime, durationMinutes))
            .ReturnsAsync(availableResponse);
        _mockResponseHelper.Setup(x => x.SetError<object>(null, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .Returns(errorResponse);

        // Act
        var result = await _sut.GetAvailableTrainersWithDetailsAsync(serviceId, appointmentDateTime, durationMinutes);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public async Task GetAvailableTrainersWithDetailsAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var serviceId = _fixture.Create<int>();
        var appointmentDateTime = DateTime.Now.AddDays(1);
        var durationMinutes = 60;
        var exception = _fixture.Create<Exception>();
        var errorResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockAppointmentService.Setup(x => x.GetAvailableTrainersAsync(serviceId, appointmentDateTime, durationMinutes))
            .ThrowsAsync(exception);
        _mockResponseHelper.Setup(x => x.SetError<object>(null, It.IsAny<ErrorInfo>())).Returns(errorResponse);

        // Act
        var result = await _sut.GetAvailableTrainersWithDetailsAsync(serviceId, appointmentDateTime, durationMinutes);

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

    #region GetMemberAppointmentsWithDetailsAsync Tests

    [Fact]
    public async Task GetMemberAppointmentsWithDetailsAsync_WhenAppointmentsExist_ShouldReturnDetails() {
        // Arrange
        var memberId = _fixture.Create<int>();
        var appointments = _fixture.Build<AppointmentDto>()
            .With(x => x.MemberId, memberId)
            .CreateMany(5)
            .ToList();

        var appointmentsResponse = _fixture.Build<ServiceResponse<List<AppointmentDto>>>()
            .With(x => x.Data, appointments)
            .With(x => x.IsSuccessful, true)
            .Create();

        var expectedResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, true)
            .Create();

        _mockAppointmentService.Setup(x => x.GetMemberAppointmentsAsync(memberId)).ReturnsAsync(appointmentsResponse);
        _mockResponseHelper.Setup(x => x.SetSuccess<object>(It.IsAny<object>())).Returns(expectedResponse);

        // Act
        var result = await _sut.GetMemberAppointmentsWithDetailsAsync(memberId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        _mockAppointmentService.Verify(x => x.GetMemberAppointmentsAsync(memberId), Times.Once);
    }

    [Fact]
    public async Task GetMemberAppointmentsWithDetailsAsync_WhenNoAppointments_ShouldReturnEmptyList() {
        // Arrange
        var memberId = _fixture.Create<int>();
        var appointments = new List<AppointmentDto>();

        var appointmentsResponse = _fixture.Build<ServiceResponse<List<AppointmentDto>>>()
            .With(x => x.Data, appointments)
            .With(x => x.IsSuccessful, true)
            .Create();

        var expectedResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, true)
            .Create();

        _mockAppointmentService.Setup(x => x.GetMemberAppointmentsAsync(memberId)).ReturnsAsync(appointmentsResponse);
        _mockResponseHelper.Setup(x => x.SetSuccess<object>(It.IsAny<object>())).Returns(expectedResponse);

        // Act
        var result = await _sut.GetMemberAppointmentsWithDetailsAsync(memberId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task GetMemberAppointmentsWithDetailsAsync_WhenServiceFails_ShouldReturnError() {
        // Arrange
        var memberId = _fixture.Create<int>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Randevular alýnamadý")
            .With(x => x.ErrorCode, "REPORT_006")
            .With(x => x.StatusCode, 500)
            .Create();

        var appointmentsResponse = _fixture.Build<ServiceResponse<List<AppointmentDto>>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .Create();

        var errorResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockAppointmentService.Setup(x => x.GetMemberAppointmentsAsync(memberId)).ReturnsAsync(appointmentsResponse);
        _mockResponseHelper.Setup(x => x.SetError<object>(null, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .Returns(errorResponse);

        // Act
        var result = await _sut.GetMemberAppointmentsWithDetailsAsync(memberId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public async Task GetMemberAppointmentsWithDetailsAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var memberId = _fixture.Create<int>();
        var exception = _fixture.Create<Exception>();
        var errorResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockAppointmentService.Setup(x => x.GetMemberAppointmentsAsync(memberId)).ThrowsAsync(exception);
        _mockResponseHelper.Setup(x => x.SetError<object>(null, It.IsAny<ErrorInfo>())).Returns(errorResponse);

        // Act
        var result = await _sut.GetMemberAppointmentsWithDetailsAsync(memberId);

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

    #region GetTrainerWorkloadAsync Tests

    [Fact]
    public async Task GetTrainerWorkloadAsync_WhenDataExists_ShouldReturnWorkload() {
        // Arrange
        var appointments = _fixture.Build<AppointmentDto>()
            .With(x => x.IsActive, true)
            .With(x => x.Status, "Confirmed")
            .With(x => x.TrainerId, 1)
            .CreateMany(5)
            .ToList();

        var trainers = _fixture.Build<TrainerDto>()
            .With(x => x.Id, 1)
            .CreateMany(1)
            .ToList();

        var appointmentsResponse = _fixture.Build<ServiceResponse<List<AppointmentDto>>>()
            .With(x => x.Data, appointments)
            .With(x => x.IsSuccessful, true)
            .Create();

        var trainersResponse = _fixture.Build<ServiceResponse<List<TrainerDto>>>()
            .With(x => x.Data, trainers)
            .With(x => x.IsSuccessful, true)
            .Create();

        var expectedResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, true)
            .Create();

        _mockAppointmentService.Setup(x => x.GetAllAsync()).ReturnsAsync(appointmentsResponse);
        _mockTrainerService.Setup(x => x.GetAllAsync()).ReturnsAsync(trainersResponse);
        _mockResponseHelper.Setup(x => x.SetSuccess<object>(It.IsAny<object>())).Returns(expectedResponse);

        // Act
        var result = await _sut.GetTrainerWorkloadAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        _mockAppointmentService.Verify(x => x.GetAllAsync(), Times.Once);
        _mockTrainerService.Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetTrainerWorkloadAsync_WithSpecificTrainer_ShouldFilterByTrainerId() {
        // Arrange
        var trainerId = 1;
        var appointments = _fixture.Build<AppointmentDto>()
            .With(x => x.IsActive, true)
            .With(x => x.Status, "Confirmed")
            .With(x => x.TrainerId, trainerId)
            .CreateMany(3)
            .Concat(_fixture.Build<AppointmentDto>()
                .With(x => x.IsActive, true)
                .With(x => x.Status, "Confirmed")
                .With(x => x.TrainerId, 2)
                .CreateMany(2))
            .ToList();

        var trainers = _fixture.CreateMany<TrainerDto>(2).ToList();

        var appointmentsResponse = _fixture.Build<ServiceResponse<List<AppointmentDto>>>()
            .With(x => x.Data, appointments)
            .With(x => x.IsSuccessful, true)
            .Create();

        var trainersResponse = _fixture.Build<ServiceResponse<List<TrainerDto>>>()
            .With(x => x.Data, trainers)
            .With(x => x.IsSuccessful, true)
            .Create();

        var expectedResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, true)
            .Create();

        _mockAppointmentService.Setup(x => x.GetAllAsync()).ReturnsAsync(appointmentsResponse);
        _mockTrainerService.Setup(x => x.GetAllAsync()).ReturnsAsync(trainersResponse);
        _mockResponseHelper.Setup(x => x.SetSuccess<object>(It.IsAny<object>())).Returns(expectedResponse);

        // Act
        var result = await _sut.GetTrainerWorkloadAsync(trainerId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task GetTrainerWorkloadAsync_WhenDataNotAvailable_ShouldReturnError() {
        // Arrange
        var appointmentsResponse = _fixture.Build<ServiceResponse<List<AppointmentDto>>>()
            .With(x => x.IsSuccessful, false)
            .Create();

        var errorResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockAppointmentService.Setup(x => x.GetAllAsync()).ReturnsAsync(appointmentsResponse);
        _mockResponseHelper.Setup(x => x.SetError<object>(null, "Veriler alýnamadý", 500, "REPORT_011")).Returns(errorResponse);

        // Act
        var result = await _sut.GetTrainerWorkloadAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public async Task GetTrainerWorkloadAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var exception = _fixture.Create<Exception>();
        var errorResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockAppointmentService.Setup(x => x.GetAllAsync()).ThrowsAsync(exception);
        _mockResponseHelper.Setup(x => x.SetError<object>(null, It.IsAny<ErrorInfo>())).Returns(errorResponse);

        // Act
        var result = await _sut.GetTrainerWorkloadAsync();

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

    #region GetRevenueByGymLocationAsync Tests

    [Fact]
    public async Task GetRevenueByGymLocationAsync_WhenDataExists_ShouldReturnRevenueByLocation() {
        // Arrange
        var appointments = _fixture.Build<AppointmentDto>()
            .With(x => x.IsActive, true)
            .With(x => x.Status, "Confirmed")
            .With(x => x.TrainerId, 1)
            .CreateMany(5)
            .ToList();

        var trainers = _fixture.Build<TrainerDto>()
            .With(x => x.Id, 1)
            .With(x => x.GymLocationId, 1)
            .CreateMany(1)
            .ToList();

        var gymLocations = _fixture.Build<GymLocationDto>()
            .With(x => x.Id, 1)
            .CreateMany(1)
            .ToList();

        var appointmentsResponse = _fixture.Build<ServiceResponse<List<AppointmentDto>>>()
            .With(x => x.Data, appointments)
            .With(x => x.IsSuccessful, true)
            .Create();

        var trainersResponse = _fixture.Build<ServiceResponse<List<TrainerDto>>>()
            .With(x => x.Data, trainers)
            .With(x => x.IsSuccessful, true)
            .Create();

        var gymLocationsResponse = _fixture.Build<ServiceResponse<List<GymLocationDto>>>()
            .With(x => x.Data, gymLocations)
            .With(x => x.IsSuccessful, true)
            .Create();

        var expectedResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, true)
            .Create();

        _mockAppointmentService.Setup(x => x.GetAllAsync()).ReturnsAsync(appointmentsResponse);
        _mockTrainerService.Setup(x => x.GetAllAsync()).ReturnsAsync(trainersResponse);
        _mockGymLocationService.Setup(x => x.GetAllAsync()).ReturnsAsync(gymLocationsResponse);
        _mockResponseHelper.Setup(x => x.SetSuccess<object>(It.IsAny<object>())).Returns(expectedResponse);

        // Act
        var result = await _sut.GetRevenueByGymLocationAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        _mockAppointmentService.Verify(x => x.GetAllAsync(), Times.Once);
        _mockTrainerService.Verify(x => x.GetAllAsync(), Times.Once);
        _mockGymLocationService.Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetRevenueByGymLocationAsync_WhenDataNotAvailable_ShouldReturnError() {
        // Arrange
        var appointmentsResponse = _fixture.Build<ServiceResponse<List<AppointmentDto>>>()
            .With(x => x.IsSuccessful, false)
            .Create();

        var errorResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockAppointmentService.Setup(x => x.GetAllAsync()).ReturnsAsync(appointmentsResponse);
        _mockResponseHelper.Setup(x => x.SetError<object>(null, "Veriler alýnamadý", 500, "REPORT_016")).Returns(errorResponse);

        // Act
        var result = await _sut.GetRevenueByGymLocationAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public async Task GetRevenueByGymLocationAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var exception = _fixture.Create<Exception>();
        var errorResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockAppointmentService.Setup(x => x.GetAllAsync()).ThrowsAsync(exception);
        _mockResponseHelper.Setup(x => x.SetError<object>(null, It.IsAny<ErrorInfo>())).Returns(errorResponse);

        // Act
        var result = await _sut.GetRevenueByGymLocationAsync();

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

    #region GetRevenueTrendAsync Tests

    [Fact]
    public async Task GetRevenueTrendAsync_WhenDataExists_ShouldReturnTrend() {
        // Arrange
        var appointments = _fixture.Build<AppointmentDto>()
            .With(x => x.IsActive, true)
            .With(x => x.Status, "Confirmed")
            .With(x => x.AppointmentDate, DateTime.Now.AddMonths(-2))
            .CreateMany(5)
            .ToList();

        var trainers = _fixture.CreateMany<TrainerDto>(2).ToList();

        var appointmentsResponse = _fixture.Build<ServiceResponse<List<AppointmentDto>>>()
            .With(x => x.Data, appointments)
            .With(x => x.IsSuccessful, true)
            .Create();

        var trainersResponse = _fixture.Build<ServiceResponse<List<TrainerDto>>>()
            .With(x => x.Data, trainers)
            .With(x => x.IsSuccessful, true)
            .Create();

        var expectedResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, true)
            .Create();

        _mockAppointmentService.Setup(x => x.GetAllAsync()).ReturnsAsync(appointmentsResponse);
        _mockTrainerService.Setup(x => x.GetAllAsync()).ReturnsAsync(trainersResponse);
        _mockResponseHelper.Setup(x => x.SetSuccess<object>(It.IsAny<object>())).Returns(expectedResponse);

        // Act
        var result = await _sut.GetRevenueTrendAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        _mockAppointmentService.Verify(x => x.GetAllAsync(), Times.Once);
        _mockTrainerService.Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetRevenueTrendAsync_WithGymLocationId_ShouldFilterByLocation() {
        // Arrange
        var gymLocationId = 1;
        var appointments = _fixture.Build<AppointmentDto>()
            .With(x => x.IsActive, true)
            .With(x => x.Status, "Confirmed")
            .With(x => x.AppointmentDate, DateTime.Now.AddMonths(-2))
            .With(x => x.TrainerId, 1)
            .CreateMany(3)
            .ToList();

        var trainers = _fixture.Build<TrainerDto>()
            .With(x => x.Id, 1)
            .With(x => x.GymLocationId, gymLocationId)
            .CreateMany(1)
            .ToList();

        var appointmentsResponse = _fixture.Build<ServiceResponse<List<AppointmentDto>>>()
            .With(x => x.Data, appointments)
            .With(x => x.IsSuccessful, true)
            .Create();

        var trainersResponse = _fixture.Build<ServiceResponse<List<TrainerDto>>>()
            .With(x => x.Data, trainers)
            .With(x => x.IsSuccessful, true)
            .Create();

        var expectedResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, true)
            .Create();

        _mockAppointmentService.Setup(x => x.GetAllAsync()).ReturnsAsync(appointmentsResponse);
        _mockTrainerService.Setup(x => x.GetAllAsync()).ReturnsAsync(trainersResponse);
        _mockResponseHelper.Setup(x => x.SetSuccess<object>(It.IsAny<object>())).Returns(expectedResponse);

        // Act
        var result = await _sut.GetRevenueTrendAsync(gymLocationId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task GetRevenueTrendAsync_WhenDataNotAvailable_ShouldReturnError() {
        // Arrange
        var appointmentsResponse = _fixture.Build<ServiceResponse<List<AppointmentDto>>>()
            .With(x => x.IsSuccessful, false)
            .Create();

        var errorResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockAppointmentService.Setup(x => x.GetAllAsync()).ReturnsAsync(appointmentsResponse);
        _mockResponseHelper.Setup(x => x.SetError<object>(null, "Veriler alýnamadý", 500, "REPORT_017")).Returns(errorResponse);

        // Act
        var result = await _sut.GetRevenueTrendAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public async Task GetRevenueTrendAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var exception = _fixture.Create<Exception>();
        var errorResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockAppointmentService.Setup(x => x.GetAllAsync()).ThrowsAsync(exception);
        _mockResponseHelper.Setup(x => x.SetError<object>(null, It.IsAny<ErrorInfo>())).Returns(errorResponse);

        // Act
        var result = await _sut.GetRevenueTrendAsync();

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

    #region GetMemberGrowthTrendAsync Tests

    [Fact]
    public async Task GetMemberGrowthTrendAsync_WhenDataExists_ShouldReturnTrend() {
        // Arrange
        var requests = _fixture.Build<MembershipRequestDto>()
            .With(x => x.Status, "Approved")
            .With(x => x.ApprovedAt, DateTime.Now.AddMonths(-2))
            .CreateMany(5)
            .ToList();

        var requestsResponse = _fixture.Build<ServiceResponse<List<MembershipRequestDto>>>()
            .With(x => x.Data, requests)
            .With(x => x.IsSuccessful, true)
            .Create();

        var expectedResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, true)
            .Create();

        _mockMembershipRequestService.Setup(x => x.GetAllRequestsAsync()).ReturnsAsync(requestsResponse);
        _mockResponseHelper.Setup(x => x.SetSuccess<object>(It.IsAny<object>())).Returns(expectedResponse);

        // Act
        var result = await _sut.GetMemberGrowthTrendAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        _mockMembershipRequestService.Verify(x => x.GetAllRequestsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetMemberGrowthTrendAsync_WithGymLocationId_ShouldFilterByLocation() {
        // Arrange
        var gymLocationId = 1;
        var requests = _fixture.Build<MembershipRequestDto>()
            .With(x => x.Status, "Approved")
            .With(x => x.ApprovedAt, DateTime.Now.AddMonths(-2))
            .With(x => x.GymLocationId, gymLocationId)
            .CreateMany(3)
            .ToList();

        var requestsResponse = _fixture.Build<ServiceResponse<List<MembershipRequestDto>>>()
            .With(x => x.Data, requests)
            .With(x => x.IsSuccessful, true)
            .Create();

        var expectedResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, true)
            .Create();

        _mockMembershipRequestService.Setup(x => x.GetAllRequestsAsync()).ReturnsAsync(requestsResponse);
        _mockResponseHelper.Setup(x => x.SetSuccess<object>(It.IsAny<object>())).Returns(expectedResponse);

        // Act
        var result = await _sut.GetMemberGrowthTrendAsync(gymLocationId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task GetMemberGrowthTrendAsync_WhenRequestsNotAvailable_ShouldReturnError() {
        // Arrange
        var requestsResponse = _fixture.Build<ServiceResponse<List<MembershipRequestDto>>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Data, (List<MembershipRequestDto>)null)
            .Create();

        var errorResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockMembershipRequestService.Setup(x => x.GetAllRequestsAsync()).ReturnsAsync(requestsResponse);
        _mockResponseHelper.Setup(x => x.SetError<object>(null, "Talepler alýnamadý", 500, "REPORT_018")).Returns(errorResponse);

        // Act
        var result = await _sut.GetMemberGrowthTrendAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public async Task GetMemberGrowthTrendAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var exception = _fixture.Create<Exception>();
        var errorResponse = _fixture.Build<ServiceResponse<object>>()
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockMembershipRequestService.Setup(x => x.GetAllRequestsAsync()).ThrowsAsync(exception);
        _mockResponseHelper.Setup(x => x.SetError<object>(null, It.IsAny<ErrorInfo>())).Returns(errorResponse);

        // Act
        var result = await _sut.GetMemberGrowthTrendAsync();

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

public class TestReportServiceFactory : BaseFactory<ReportService> {
    public TestReportServiceFactory(
        UtilityFactory<ReportService> utilityFactory,
        IRepositoryFactory repositoryFactory,
        IServiceProvider serviceProvider)
        : base(utilityFactory, repositoryFactory, serviceProvider) {
    }

    public override UtilityFactory<ReportService> CreateUtilityFactory() => utilityFactory;
    public override IRepositoryFactory CreateRepositoryFactory() => repositoryFactory;
}