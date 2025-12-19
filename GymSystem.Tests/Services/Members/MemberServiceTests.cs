using AutoFixture;
using AutoFixture.AutoMoq;
using AutoMapper;
using FluentAssertions;
using GymSystem.Application.Abstractions.Contract.Member;
using GymSystem.Application.Services.Members;
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

namespace GymSystem.Tests.Services.Members;

public class MemberServiceTests {
    private readonly IFixture _fixture;
    private readonly Mock<IRepositoryFactory> _mockRepositoryFactory;
    private readonly Mock<IRepository<Member>> _mockMemberRepository;
    private readonly Mock<UtilityFactory<MemberService>> _mockUtilityFactory;
    private readonly Mock<IServiceResponseHelper> _mockResponseHelper;
    private readonly Mock<ILogger<MemberService>> _mockLogger;
    private readonly Mock<IMapper> _mockMapper;
    private readonly TestMemberServiceFactory _testFactory;
    private readonly MemberService _sut; // System Under Test

    public MemberServiceTests() {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        // Configure AutoFixture to handle circular references
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // Create mocks
        _mockRepositoryFactory = new Mock<IRepositoryFactory>();
        _mockMemberRepository = new Mock<IRepository<Member>>();
        _mockUtilityFactory = new Mock<UtilityFactory<MemberService>>();
        _mockResponseHelper = new Mock<IServiceResponseHelper>();
        _mockLogger = new Mock<ILogger<MemberService>>();
        _mockMapper = new Mock<IMapper>();

        // Setup factory behaviors
        _mockRepositoryFactory.Setup(x => x.CreateRepository<Member>()).Returns(_mockMemberRepository.Object);

        _mockUtilityFactory.Setup(x => x.CreateServiceResponseHelper()).Returns(_mockResponseHelper.Object);
        _mockUtilityFactory.Setup(x => x.CreateLogger()).Returns(_mockLogger.Object);
        _mockUtilityFactory.Setup(x => x.CreateMapper()).Returns(_mockMapper.Object);

        // Create test factory
        _testFactory = new TestMemberServiceFactory(
            _mockUtilityFactory.Object,
            _mockRepositoryFactory.Object,
            Mock.Of<IServiceProvider>());

        // Create System Under Test
        _sut = new MemberService(_testFactory);
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WhenMembersExist_ShouldReturnSuccessWithMembers() {
        // Arrange
        var members = _fixture.CreateMany<Member>(3).ToList();
        var memberDtos = _fixture.CreateMany<MemberDto>(3).ToList();
        var expectedResponse = _fixture.Build<ServiceResponse<List<MemberDto>>>()
            .With(x => x.Data, memberDtos)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var mockQueryable = members.AsQueryable().BuildMock();
        _mockMemberRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<List<MemberDto>>(It.IsAny<List<Member>>())).Returns(memberDtos);
        _mockResponseHelper.Setup(x => x.SetSuccess(memberDtos)).Returns(expectedResponse);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().HaveCount(3);
        _mockMemberRepository.Verify(x => x.QueryNoTracking(), Times.Once);
        _mockMapper.Verify(x => x.Map<List<MemberDto>>(It.IsAny<List<Member>>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoActiveMembers_ShouldReturnEmptyList() {
        // Arrange
        var members = new List<Member>();
        var memberDtos = new List<MemberDto>();
        var expectedResponse = _fixture.Build<ServiceResponse<List<MemberDto>>>()
            .With(x => x.Data, memberDtos)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var mockQueryable = members.AsQueryable().BuildMock();
        _mockMemberRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<List<MemberDto>>(It.IsAny<List<Member>>())).Returns(memberDtos);
        _mockResponseHelper.Setup(x => x.SetSuccess(memberDtos)).Returns(expectedResponse);

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
            .With(x => x.ErrorMessage, "Member'lar getirilemedi")
            .With(x => x.ErrorCode, "MEMBER_GETALL_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<List<MemberDto>>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (List<MemberDto>)null)
            .Create();

        _mockMemberRepository.Setup(x => x.QueryNoTracking()).Throws(exception);
        _mockResponseHelper
            .Setup(x => x.SetError<List<MemberDto>>(null, It.IsAny<ErrorInfo>()))
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
    public async Task GetByIdAsync_WhenMemberExists_ShouldReturnSuccessWithMember() {
        // Arrange
        var memberId = _fixture.Create<int>();
        var member = _fixture.Build<Member>()
            .With(x => x.Id, memberId)
            .With(x => x.IsActive, true)
            .Create();
        var memberDto = _fixture.Build<MemberDto>()
            .With(x => x.Id, memberId)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<MemberDto?>>()
            .With(x => x.Data, memberDto)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var members = new List<Member> { member };
        var mockQueryable = members.AsQueryable().BuildMock();

        _mockMemberRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<MemberDto>(It.IsAny<Member>())).Returns(memberDto);
        _mockResponseHelper.Setup(x => x.SetSuccess<MemberDto?>(memberDto)).Returns(expectedResponse);

        // Act
        var result = await _sut.GetByIdAsync(memberId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(memberId);
        _mockMapper.Verify(x => x.Map<MemberDto>(It.IsAny<Member>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMemberNotFound_ShouldReturnError() {
        // Arrange
        var memberId = _fixture.Create<int>();
        var members = new List<Member>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Member bulunamadý")
            .With(x => x.ErrorCode, "MEMBER_NOTFOUND")
            .With(x => x.StatusCode, 404)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<MemberDto?>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (MemberDto)null)
            .Create();

        var mockQueryable = members.AsQueryable().BuildMock();
        _mockMemberRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockResponseHelper
            .Setup(x => x.SetError<MemberDto?>(null, "Member bulunamadý", 404, "MEMBER_NOTFOUND"))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.GetByIdAsync(memberId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.ErrorCode.Should().Be("MEMBER_NOTFOUND");
        _mockMapper.Verify(x => x.Map<MemberDto>(It.IsAny<Member>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var memberId = _fixture.Create<int>();
        var exception = _fixture.Create<Exception>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Member getirilemedi")
            .With(x => x.ErrorCode, "MEMBER_GET_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<MemberDto?>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (MemberDto)null)
            .Create();

        _mockMemberRepository.Setup(x => x.QueryNoTracking()).Throws(exception);
        _mockResponseHelper
            .Setup(x => x.SetError<MemberDto?>(null, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.GetByIdAsync(memberId);

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
    public async Task CreateAsync_WithValidDto_ShouldReturnSuccessWithCreatedMember() {
        // Arrange
        var memberDto = _fixture.Build<MemberDto>()
            .Without(x => x.Id)
            .Create();
        var memberId = _fixture.Create<int>();
        var member = _fixture.Build<Member>()
            .With(x => x.Id, memberId)
            .With(x => x.IsActive, true)
            .Create();
        var createdMemberDto = _fixture.Build<MemberDto>()
            .With(x => x.Id, memberId)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<MemberDto>>()
            .With(x => x.Data, createdMemberDto)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Message, "Member oluþturuldu")
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        _mockMapper
            .Setup(x => x.Map<Member>(
                It.IsAny<MemberDto>(),
                It.IsAny<Action<IMappingOperationOptions<object, Member>>>()))
            .Returns(member)
            .Verifiable();

        _mockMapper
            .Setup(x => x.Map<MemberDto>(It.IsAny<Member>()))
            .Returns(createdMemberDto)
            .Verifiable();

        _mockMemberRepository
            .Setup(x => x.AddAsync(It.IsAny<Member>()))
            .ReturnsAsync(member)
            .Verifiable();

        _mockMemberRepository
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1)
            .Verifiable();

        _mockResponseHelper
            .Setup(x => x.SetSuccess(It.IsAny<MemberDto>(), "Member oluþturuldu"))
            .Returns(expectedResponse)
            .Verifiable();

        // Act
        var result = await _sut.CreateAsync(memberDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(memberId);
        result.Message.Should().Be("Member oluþturuldu");

        // Verify all mocks were called
        _mockMapper.Verify();
        _mockMemberRepository.Verify();
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
        var memberDto = _fixture.Build<MemberDto>()
            .Without(x => x.Id)
            .Create();
        var member = _fixture.Build<Member>()
            .With(x => x.IsActive, true)
            .Create();
        var exception = _fixture.Create<Exception>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Member oluþturulamadý")
            .With(x => x.ErrorCode, "MEMBER_CREATE_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<MemberDto>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (MemberDto)null)
            .Create();

        _mockMapper
            .Setup(x => x.Map<Member>(
                It.IsAny<MemberDto>(),
                It.IsAny<Action<IMappingOperationOptions<object, Member>>>()))
            .Returns(member);

        _mockMemberRepository.Setup(x => x.AddAsync(It.IsAny<Member>())).ThrowsAsync(exception);

        _mockResponseHelper
            .Setup(x => x.SetError<MemberDto>(null, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.CreateAsync(memberDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().NotBeNull();

        _mockMemberRepository.Verify(x => x.AddAsync(It.IsAny<Member>()), Times.Once);
        _mockMemberRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidDto_ShouldReturnSuccessWithUpdatedMember() {
        // Arrange
        var memberId = _fixture.Create<int>();
        var memberDto = _fixture.Build<MemberDto>()
            .With(x => x.Id, memberId)
            .Create();
        var existingMember = _fixture.Build<Member>()
            .With(x => x.Id, memberId)
            .With(x => x.IsActive, true)
            .Create();
        var updatedMemberDto = _fixture.Build<MemberDto>()
            .With(x => x.Id, memberId)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<MemberDto>>()
            .With(x => x.Data, updatedMemberDto)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Message, "Member güncellendi")
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var members = new List<Member> { existingMember };
        var mockQueryable = members.AsQueryable().BuildMock();

        _mockMemberRepository.Setup(x => x.Query()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map(memberDto, existingMember)).Returns(existingMember);
        _mockMapper.Setup(x => x.Map<MemberDto>(It.IsAny<Member>())).Returns(updatedMemberDto);
        _mockMemberRepository.Setup(x => x.UpdateAsync(It.IsAny<Member>())).ReturnsAsync(existingMember);
        _mockMemberRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _mockResponseHelper.Setup(x => x.SetSuccess(updatedMemberDto, "Member güncellendi")).Returns(expectedResponse);

        // Act
        var result = await _sut.UpdateAsync(memberId, memberDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Message.Should().Be("Member güncellendi");
        _mockMemberRepository.Verify(x => x.UpdateAsync(It.IsAny<Member>()), Times.Once);
        _mockMemberRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenMemberNotFound_ShouldReturnError() {
        // Arrange
        var memberId = _fixture.Create<int>();
        var memberDto = _fixture.Build<MemberDto>()
            .With(x => x.Id, memberId)
            .Create();
        var members = new List<Member>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Member bulunamadý")
            .With(x => x.ErrorCode, "MEMBER_NOTFOUND")
            .With(x => x.StatusCode, 404)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<MemberDto>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (MemberDto)null)
            .Create();

        var mockQueryable = members.AsQueryable().BuildMock();
        _mockMemberRepository.Setup(x => x.Query()).Returns(mockQueryable);
        _mockResponseHelper
            .Setup(x => x.SetError<MemberDto>(null, "Member bulunamadý", 404, "MEMBER_NOTFOUND"))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.UpdateAsync(memberId, memberDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.ErrorCode.Should().Be("MEMBER_NOTFOUND");
        _mockMemberRepository.Verify(x => x.UpdateAsync(It.IsAny<Member>()), Times.Never);
        _mockMemberRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var memberId = _fixture.Create<int>();
        var memberDto = _fixture.Build<MemberDto>()
            .With(x => x.Id, memberId)
            .Create();
        var exception = _fixture.Create<Exception>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Member güncellenemedi")
            .With(x => x.ErrorCode, "MEMBER_UPDATE_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<MemberDto>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (MemberDto)null)
            .Create();

        _mockMemberRepository.Setup(x => x.Query()).Throws(exception);
        _mockResponseHelper
            .Setup(x => x.SetError<MemberDto>(null, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.UpdateAsync(memberId, memberDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenMemberExists_ShouldReturnSuccessAndSoftDelete() {
        // Arrange
        var memberId = _fixture.Create<int>();
        var member = _fixture.Build<Member>()
            .With(x => x.Id, memberId)
            .With(x => x.IsActive, true)
            .Create();

        var members = new List<Member> { member };
        var mockQueryable = members.AsQueryable().BuildMock();

        // Empty lists for related entities (cascade delete)
        var emptyAiPlans = new List<AIWorkoutPlan>().AsQueryable().BuildMock();
        var emptyRequests = new List<MembershipRequest>().AsQueryable().BuildMock();
        var emptyAppointments = new List<Appointment>().AsQueryable().BuildMock();

        // Mock repositories for cascade delete
        var mockAiPlanRepository = new Mock<IRepository<AIWorkoutPlan>>();
        var mockMembershipRequestRepository = new Mock<IRepository<MembershipRequest>>();
        var mockAppointmentRepository = new Mock<IRepository<Appointment>>();

        mockAiPlanRepository.Setup(x => x.Query()).Returns(emptyAiPlans);
        mockMembershipRequestRepository.Setup(x => x.Query()).Returns(emptyRequests);
        mockAppointmentRepository.Setup(x => x.Query()).Returns(emptyAppointments);

        _mockRepositoryFactory.Setup(x => x.CreateRepository<AIWorkoutPlan>()).Returns(mockAiPlanRepository.Object);
        _mockRepositoryFactory.Setup(x => x.CreateRepository<MembershipRequest>()).Returns(mockMembershipRequestRepository.Object);
        _mockRepositoryFactory.Setup(x => x.CreateRepository<Appointment>()).Returns(mockAppointmentRepository.Object);

        _mockMemberRepository.Setup(x => x.Query()).Returns(mockQueryable);
        _mockMemberRepository.Setup(x => x.UpdateAsync(It.IsAny<Member>())).ReturnsAsync(member);
        _mockMemberRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        var expectedResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, true)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Message, "Member ve 0 AI planý, 0 üyelik talebi, 0 randevu silindi")
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        _mockResponseHelper
            .Setup(x => x.SetSuccess(true, It.Is<string>(s => s.Contains("Member ve"))))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.DeleteAsync(memberId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().BeTrue();
        member.IsActive.Should().BeFalse();
        member.UpdatedAt.Should().NotBeNull();
        _mockMemberRepository.Verify(x => x.UpdateAsync(It.Is<Member>(m => m.IsActive == false)), Times.Once);
        _mockMemberRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenMemberNotFound_ShouldReturnError() {
        // Arrange
        var memberId = _fixture.Create<int>();
        var members = new List<Member>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Member bulunamadý")
            .With(x => x.ErrorCode, "MEMBER_NOTFOUND")
            .With(x => x.StatusCode, 404)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, false)
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .Create();

        var mockQueryable = members.AsQueryable().BuildMock();
        _mockMemberRepository.Setup(x => x.Query()).Returns(mockQueryable);
        _mockResponseHelper
            .Setup(x => x.SetError<bool>(false, "Member bulunamadý", 404, "MEMBER_NOTFOUND"))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.DeleteAsync(memberId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Data.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.ErrorCode.Should().Be("MEMBER_NOTFOUND");
        _mockMemberRepository.Verify(x => x.UpdateAsync(It.IsAny<Member>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var memberId = _fixture.Create<int>();
        var exception = _fixture.Create<Exception>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Member silinemedi")
            .With(x => x.ErrorCode, "MEMBER_DELETE_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, false)
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .Create();

        _mockMemberRepository.Setup(x => x.Query()).Throws(exception);
        _mockResponseHelper
            .Setup(x => x.SetError<bool>(false, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.DeleteAsync(memberId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Data.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    #endregion

    #region GetAllMembersWithGymLocationAsync Tests

    [Fact]
    public async Task GetAllMembersWithGymLocationAsync_WhenMembersExist_ShouldReturnSuccessWithMembers() {
        // Arrange
        var members = _fixture.CreateMany<Member>(3).ToList();
        var memberDtos = _fixture.CreateMany<MemberDto>(3).ToList();
        var expectedResponse = _fixture.Build<ServiceResponse<List<MemberDto>>>()
            .With(x => x.Data, memberDtos)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var mockQueryable = members.AsQueryable().BuildMock();
        _mockMemberRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<List<MemberDto>>(It.IsAny<List<Member>>())).Returns(memberDtos);
        _mockResponseHelper.Setup(x => x.SetSuccess(memberDtos)).Returns(expectedResponse);

        // Act
        var result = await _sut.GetAllMembersWithGymLocationAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().HaveCount(3);
        _mockMemberRepository.Verify(x => x.QueryNoTracking(), Times.Once);
    }

    [Fact]
    public async Task GetAllMembersWithGymLocationAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var exception = _fixture.Create<Exception>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Member'lar alýnýrken bir hata oluþtu")
            .With(x => x.ErrorCode, "MEMBER_ERROR_001")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<List<MemberDto>>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (List<MemberDto>)null)
            .Create();

        _mockMemberRepository.Setup(x => x.QueryNoTracking()).Throws(exception);
        _mockResponseHelper
            .Setup(x => x.SetError<List<MemberDto>>(null, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.GetAllMembersWithGymLocationAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.ErrorCode.Should().Be("MEMBER_ERROR_001");
    }

    #endregion

    #region Edge Cases and Boundary Tests

    [Fact]
    public async Task GetAllAsync_WithLargeDataSet_ShouldHandlePerformance() {
        // Arrange
        var members = _fixture.CreateMany<Member>(100).ToList();
        var memberDtos = _fixture.CreateMany<MemberDto>(100).ToList();
        var expectedResponse = _fixture.Build<ServiceResponse<List<MemberDto>>>()
            .With(x => x.Data, memberDtos)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var mockQueryable = members.AsQueryable().BuildMock();
        _mockMemberRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<List<MemberDto>>(It.IsAny<List<Member>>())).Returns(memberDtos);
        _mockResponseHelper.Setup(x => x.SetSuccess(memberDtos)).Returns(expectedResponse);

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
        var memberDto = _fixture.Build<MemberDto>()
            .Without(x => x.Id)
            .Create();
        var member = _fixture.Build<Member>()
            .With(x => x.IsActive, true)
            .Create();
        var exception = new DbUpdateException();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Üye oluþturulamadý")
            .With(x => x.ErrorCode, "MEMBER_CREATE_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<MemberDto>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (MemberDto)null)
            .Create();

        _mockMapper
            .Setup(x => x.Map<Member>(
                It.IsAny<MemberDto>(),
                It.IsAny<Action<IMappingOperationOptions<object, Member>>>()))
            .Returns(member);

        _mockMemberRepository.Setup(x => x.AddAsync(It.IsAny<Member>())).ReturnsAsync(member);
        _mockMemberRepository.Setup(x => x.SaveChangesAsync()).ThrowsAsync(exception);

        _mockResponseHelper
            .Setup(x => x.SetError<MemberDto>(null, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.CreateAsync(memberDto);

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
public class TestMemberServiceFactory : BaseFactory<MemberService> {
    public TestMemberServiceFactory(
        UtilityFactory<MemberService> utilityFactory,
        IRepositoryFactory repositoryFactory,
        IServiceProvider serviceProvider)
        : base(utilityFactory, repositoryFactory, serviceProvider) {
    }

    public override UtilityFactory<MemberService> CreateUtilityFactory() => utilityFactory;
    public override IRepositoryFactory CreateRepositoryFactory() => repositoryFactory;
}