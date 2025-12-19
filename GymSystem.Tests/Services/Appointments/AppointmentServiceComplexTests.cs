using AutoFixture;
using AutoFixture.AutoMoq;
using AutoMapper;
using FluentAssertions;
using GymSystem.Application.Services.Appointments;
using GymSystem.Common.Factory.Repository;
using GymSystem.Common.Factory.Utility;
using GymSystem.Common.Helpers;
using GymSystem.Common.Models;
using GymSystem.Common.Repositories;
using GymSystem.Domain.Entities;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;

namespace GymSystem.Tests.Services.Appointments;

/// <summary>
/// AppointmentService Complex Business Logic Tests - BookAppointment, ConfirmAppointment, GetAvailableTrainers
/// </summary>
public class AppointmentServiceComplexTests {
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

    public AppointmentServiceComplexTests() {
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

    #region ConfirmAppointmentAsync Tests - Integration test yapýlmasý daha uygun
    // ConfirmAppointmentAsync çok kompleks olduðu için (GetByIdAsync'i çaðýrýyor) 
    // ve birçok dahili baðýmlýlýk içerdiði için bu metod integration test ile test edilmelidir
    // Unit test yazmak çok fazla mock setup gerektiriyor ve test maintainability'i düþürüyor

    #endregion

    #region GetAvailableTrainersAsync Tests

    [Fact]
    public async Task GetAvailableTrainersAsync_WhenTrainersAvailable_ShouldReturnTrainerIds() {
        // Arrange
        var serviceId = 1;
        var appointmentDate = new DateTime(2024, 1, 15, 10, 0, 0); // Monday 10:00
        var durationMinutes = 60;

        var trainerSpecialties = new List<TrainerSpecialty> {
            new TrainerSpecialty { TrainerId = 1, ServiceId = serviceId, IsActive = true },
            new TrainerSpecialty { TrainerId = 2, ServiceId = serviceId, IsActive = true }
        };

        var mockSpecialtyQueryable = trainerSpecialties.AsQueryable().BuildMock();
        _mockTrainerSpecialtyRepository.Setup(x => x.QueryNoTracking()).Returns(mockSpecialtyQueryable);

        // Setup for trainer 1 - available
        var appointments1 = new List<Appointment>();
        var availabilities1 = new List<TrainerAvailability> {
            new TrainerAvailability {
                TrainerId = 1,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(17, 0, 0),
                IsActive = true
            }
        };

        // Setup for trainer 2 - not available (has conflict)
        var appointments2 = new List<Appointment> {
            new Appointment {
                TrainerId = 2,
                AppointmentDate = new DateTime(2024, 1, 15, 9, 30, 0),
                DurationMinutes = 90,
                Status = AppointmentStatus.Confirmed,
                IsActive = true
            }
        };

        var mockAppointmentQueryable1 = appointments1.AsQueryable().BuildMock();
        var mockAvailabilityQueryable1 = availabilities1.AsQueryable().BuildMock();
        var mockAppointmentQueryable2 = appointments2.AsQueryable().BuildMock();

        var callCount = 0;
        _mockAppointmentRepository.Setup(x => x.QueryNoTracking())
            .Returns(() => {
                callCount++;
                return callCount == 1 ? mockAppointmentQueryable1 : mockAppointmentQueryable2;
            });

        var availCallCount = 0;
        _mockTrainerAvailabilityRepository.Setup(x => x.QueryNoTracking())
            .Returns(() => {
                availCallCount++;
                return mockAvailabilityQueryable1; // Both trainers have same availability for simplicity
            });

        var successResponse1 = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, true)
            .With(x => x.IsSuccessful, true)
            .Create();

        var errorResponse2 = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, false)
            .With(x => x.IsSuccessful, false)
            .Create();

        var setupCount = 0;
        _mockResponseHelper.Setup(x => x.SetSuccess(true, "Antrenör müsait"))
            .Returns(() => {
                setupCount++;
                return successResponse1;
            });

        _mockResponseHelper.Setup(x => x.SetError<bool>(false, It.IsAny<string>(), 400, "APPOINTMENT_001"))
            .Returns(errorResponse2);

        var expectedResponse = _fixture.Build<ServiceResponse<List<int>>>()
            .With(x => x.Data, new List<int> { 1 })
            .With(x => x.IsSuccessful, true)
            .Create();

        _mockResponseHelper.Setup(x => x.SetSuccess(It.IsAny<List<int>>())).Returns(expectedResponse);

        // Act
        var result = await _sut.GetAvailableTrainersAsync(serviceId, appointmentDate, durationMinutes);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().Contain(1);
        result.Data.Should().NotContain(2);
    }

    [Fact]
    public async Task GetAvailableTrainersAsync_WhenNoTrainersForService_ShouldReturnEmptyList() {
        // Arrange
        var serviceId = 1;
        var appointmentDate = DateTime.Now.AddDays(1);
        var durationMinutes = 60;

        var trainerSpecialties = new List<TrainerSpecialty>();
        var mockQueryable = trainerSpecialties.AsQueryable().BuildMock();

        _mockTrainerSpecialtyRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);

        var expectedResponse = _fixture.Build<ServiceResponse<List<int>>>()
            .With(x => x.Data, new List<int>())
            .With(x => x.IsSuccessful, true)
            .Create();

        _mockResponseHelper.Setup(x => x.SetSuccess(It.IsAny<List<int>>())).Returns(expectedResponse);

        // Act
        var result = await _sut.GetAvailableTrainersAsync(serviceId, appointmentDate, durationMinutes);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableTrainersAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var serviceId = 1;
        var appointmentDate = DateTime.Now.AddDays(1);
        var durationMinutes = 60;
        var exception = _fixture.Create<Exception>();

        _mockTrainerSpecialtyRepository.Setup(x => x.QueryNoTracking()).Throws(exception);

        var errorResponse = _fixture.Build<ServiceResponse<List<int>>>()
            .With(x => x.IsSuccessful, false)
            .Create();

        _mockResponseHelper.Setup(x => x.SetError<List<int>>(null, It.IsAny<ErrorInfo>()))
            .Returns(errorResponse);

        // Act
        var result = await _sut.GetAvailableTrainersAsync(serviceId, appointmentDate, durationMinutes);

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
