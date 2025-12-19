using AutoFixture;
using AutoFixture.AutoMoq;
using AutoMapper;
using FluentAssertions;
using GymSystem.Application.Abstractions.Services.IMembershipRequestService.Contract;
using GymSystem.Application.Services.Membership;
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

namespace GymSystem.Tests.Services.Membership;

public class MembershipRequestServiceTests {
    private readonly IFixture _fixture;
    private readonly Mock<IRepositoryFactory> _mockRepositoryFactory;
    private readonly Mock<IRepository<MembershipRequest>> _mockRequestRepository;
    private readonly Mock<UtilityFactory<MembershipRequestService>> _mockUtilityFactory;
    private readonly Mock<IServiceResponseHelper> _mockResponseHelper;
    private readonly Mock<ILogger<MembershipRequestService>> _mockLogger;
    private readonly Mock<IMapper> _mockMapper;
    private readonly TestMembershipRequestServiceFactory _testFactory;
    private readonly MembershipRequestService _sut;

    public MembershipRequestServiceTests() {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _mockRepositoryFactory = new Mock<IRepositoryFactory>();
        _mockRequestRepository = new Mock<IRepository<MembershipRequest>>();
        _mockUtilityFactory = new Mock<UtilityFactory<MembershipRequestService>>();
        _mockResponseHelper = new Mock<IServiceResponseHelper>();
        _mockLogger = new Mock<ILogger<MembershipRequestService>>();
        _mockMapper = new Mock<IMapper>();

        _mockRepositoryFactory.Setup(x => x.CreateRepository<MembershipRequest>()).Returns(_mockRequestRepository.Object);

        _mockUtilityFactory.Setup(x => x.CreateServiceResponseHelper()).Returns(_mockResponseHelper.Object);
        _mockUtilityFactory.Setup(x => x.CreateLogger()).Returns(_mockLogger.Object);
        _mockUtilityFactory.Setup(x => x.CreateMapper()).Returns(_mockMapper.Object);

        _testFactory = new TestMembershipRequestServiceFactory(
            _mockUtilityFactory.Object,
            _mockRepositoryFactory.Object,
            Mock.Of<IServiceProvider>());

        _sut = new MembershipRequestService(_testFactory, _mockLogger.Object);
    }

    #region GetAllRequestsAsync Tests

    [Fact]
    public async Task GetAllRequestsAsync_WhenRequestsExist_ShouldReturnSuccessWithRequests() {
        // Arrange
        var requests = _fixture.CreateMany<MembershipRequest>(3).ToList();
        var requestDtos = _fixture.CreateMany<MembershipRequestDto>(3).ToList();
        var expectedResponse = _fixture.Build<ServiceResponse<List<MembershipRequestDto>>>()
            .With(x => x.Data, requestDtos)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var mockQueryable = requests.AsQueryable().BuildMock();
        _mockRequestRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<List<MembershipRequestDto>>(It.IsAny<List<MembershipRequest>>())).Returns(requestDtos);
        _mockResponseHelper.Setup(x => x.SetSuccess(requestDtos)).Returns(expectedResponse);

        // Act
        var result = await _sut.GetAllRequestsAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().HaveCount(3);
        _mockRequestRepository.Verify(x => x.QueryNoTracking(), Times.Once);
        _mockMapper.Verify(x => x.Map<List<MembershipRequestDto>>(It.IsAny<List<MembershipRequest>>()), Times.Once);
    }

    [Fact]
    public async Task GetAllRequestsAsync_WhenNoRequests_ShouldReturnEmptyList() {
        // Arrange
        var requests = new List<MembershipRequest>();
        var requestDtos = new List<MembershipRequestDto>();
        var expectedResponse = _fixture.Build<ServiceResponse<List<MembershipRequestDto>>>()
            .With(x => x.Data, requestDtos)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var mockQueryable = requests.AsQueryable().BuildMock();
        _mockRequestRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<List<MembershipRequestDto>>(It.IsAny<List<MembershipRequest>>())).Returns(requestDtos);
        _mockResponseHelper.Setup(x => x.SetSuccess(requestDtos)).Returns(expectedResponse);

        // Act
        var result = await _sut.GetAllRequestsAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllRequestsAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var exception = _fixture.Create<Exception>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Talepler getirilemedi")
            .With(x => x.ErrorCode, "MEMBERSHIPREQUEST_GETALL_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<List<MembershipRequestDto>>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (List<MembershipRequestDto>)null)
            .Create();

        _mockRequestRepository.Setup(x => x.QueryNoTracking()).Throws(exception);
        _mockResponseHelper
            .Setup(x => x.SetError<List<MembershipRequestDto>>(null, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.GetAllRequestsAsync();

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

    #region GetRequestByIdAsync Tests

    [Fact]
    public async Task GetRequestByIdAsync_WhenRequestExists_ShouldReturnSuccessWithRequest() {
        // Arrange
        var requestId = _fixture.Create<int>();
        var request = _fixture.Build<MembershipRequest>()
            .With(x => x.Id, requestId)
            .With(x => x.IsActive, true)
            .Create();
        var requestDto = _fixture.Build<MembershipRequestDto>()
            .With(x => x.Id, requestId)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<MembershipRequestDto?>>()
            .With(x => x.Data, requestDto)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var requests = new List<MembershipRequest> { request };
        var mockQueryable = requests.AsQueryable().BuildMock();

        _mockRequestRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<MembershipRequestDto>(It.IsAny<MembershipRequest>())).Returns(requestDto);
        _mockResponseHelper.Setup(x => x.SetSuccess<MembershipRequestDto?>(requestDto)).Returns(expectedResponse);

        // Act
        var result = await _sut.GetRequestByIdAsync(requestId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(requestId);
        _mockMapper.Verify(x => x.Map<MembershipRequestDto>(It.IsAny<MembershipRequest>()), Times.Once);
    }

    [Fact]
    public async Task GetRequestByIdAsync_WhenRequestNotFound_ShouldReturnError() {
        // Arrange
        var requestId = _fixture.Create<int>();
        var requests = new List<MembershipRequest>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Talep bulunamadý")
            .With(x => x.ErrorCode, "MEMBERSHIPREQUEST_NOTFOUND")
            .With(x => x.StatusCode, 404)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<MembershipRequestDto?>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (MembershipRequestDto)null)
            .Create();

        var mockQueryable = requests.AsQueryable().BuildMock();
        _mockRequestRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockResponseHelper
            .Setup(x => x.SetError<MembershipRequestDto?>(null, "Talep bulunamadý", 404, "MEMBERSHIPREQUEST_NOTFOUND"))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.GetRequestByIdAsync(requestId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.ErrorCode.Should().Be("MEMBERSHIPREQUEST_NOTFOUND");
        _mockMapper.Verify(x => x.Map<MembershipRequestDto>(It.IsAny<MembershipRequest>()), Times.Never);
    }

    [Fact]
    public async Task GetRequestByIdAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var requestId = _fixture.Create<int>();
        var exception = _fixture.Create<Exception>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Talep getirilemedi")
            .With(x => x.ErrorCode, "MEMBERSHIPREQUEST_GET_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<MembershipRequestDto?>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (MembershipRequestDto)null)
            .Create();

        _mockRequestRepository.Setup(x => x.QueryNoTracking()).Throws(exception);
        _mockResponseHelper
            .Setup(x => x.SetError<MembershipRequestDto?>(null, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.GetRequestByIdAsync(requestId);

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

    #region GetPendingRequestsAsync Tests

    [Fact]
    public async Task GetPendingRequestsAsync_WhenPendingRequestsExist_ShouldReturnSuccessWithRequests() {
        // Arrange
        var requests = _fixture.Build<MembershipRequest>()
            .With(x => x.Status, MembershipRequestStatus.Pending)
            .With(x => x.IsActive, true)
            .CreateMany(2)
            .ToList();
        var requestDtos = _fixture.CreateMany<MembershipRequestDto>(2).ToList();
        var expectedResponse = _fixture.Build<ServiceResponse<List<MembershipRequestDto>>>()
            .With(x => x.Data, requestDtos)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var mockQueryable = requests.AsQueryable().BuildMock();
        _mockRequestRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<List<MembershipRequestDto>>(It.IsAny<List<MembershipRequest>>())).Returns(requestDtos);
        _mockResponseHelper.Setup(x => x.SetSuccess(requestDtos)).Returns(expectedResponse);

        // Act
        var result = await _sut.GetPendingRequestsAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPendingRequestsAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var exception = _fixture.Create<Exception>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Bekleyen talepler getirilemedi")
            .With(x => x.ErrorCode, "MEMBERSHIPREQUEST_GETPENDING_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<List<MembershipRequestDto>>>()
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .With(x => x.Data, (List<MembershipRequestDto>)null)
            .Create();

        _mockRequestRepository.Setup(x => x.QueryNoTracking()).Throws(exception);
        _mockResponseHelper
            .Setup(x => x.SetError<List<MembershipRequestDto>>(null, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.GetPendingRequestsAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    #endregion

    #region DeleteRequestAsync Tests

    [Fact]
    public async Task DeleteRequestAsync_WhenRequestExists_ShouldReturnSuccessAndSoftDelete() {
        // Arrange
        var requestId = _fixture.Create<int>();
        var request = _fixture.Build<MembershipRequest>()
            .With(x => x.Id, requestId)
            .With(x => x.IsActive, true)
            .With(x => x.Status, MembershipRequestStatus.Pending)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, true)
            .With(x => x.IsSuccessful, true)
            .With(x => x.Message, "Üyelik talebi silindi")
            .With(x => x.Error, (ErrorInfo)null)
            .Create();

        var requests = new List<MembershipRequest> { request };
        var mockQueryable = requests.AsQueryable().BuildMock();

        _mockRequestRepository.Setup(x => x.Query()).Returns(mockQueryable);
        _mockRequestRepository.Setup(x => x.UpdateAsync(It.IsAny<MembershipRequest>())).ReturnsAsync(request);
        _mockRequestRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _mockResponseHelper.Setup(x => x.SetSuccess(true, "Üyelik talebi silindi")).Returns(expectedResponse);

        // Act
        var result = await _sut.DeleteRequestAsync(requestId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().BeTrue();
        result.Message.Should().Be("Üyelik talebi silindi");
        request.IsActive.Should().BeFalse();
        request.UpdatedAt.Should().NotBeNull();
        _mockRequestRepository.Verify(x => x.UpdateAsync(It.Is<MembershipRequest>(r => r.IsActive == false)), Times.Once);
        _mockRequestRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteRequestAsync_WhenRequestNotFound_ShouldReturnError() {
        // Arrange
        var requestId = _fixture.Create<int>();
        var requests = new List<MembershipRequest>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Talep bulunamadý")
            .With(x => x.ErrorCode, "MEMBERSHIPREQUEST_NOTFOUND")
            .With(x => x.StatusCode, 404)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, false)
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .Create();

        var mockQueryable = requests.AsQueryable().BuildMock();
        _mockRequestRepository.Setup(x => x.Query()).Returns(mockQueryable);
        _mockResponseHelper
            .Setup(x => x.SetError<bool>(false, "Talep bulunamadý", 404, "MEMBERSHIPREQUEST_NOTFOUND"))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.DeleteRequestAsync(requestId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Data.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.ErrorCode.Should().Be("MEMBERSHIPREQUEST_NOTFOUND");
        _mockRequestRepository.Verify(x => x.UpdateAsync(It.IsAny<MembershipRequest>()), Times.Never);
    }

    [Fact]
    public async Task DeleteRequestAsync_WhenExceptionThrown_ShouldReturnError() {
        // Arrange
        var requestId = _fixture.Create<int>();
        var exception = _fixture.Create<Exception>();
        var errorInfo = _fixture.Build<ErrorInfo>()
            .With(x => x.ErrorMessage, "Talep silinemedi")
            .With(x => x.ErrorCode, "MEMBERSHIPREQUEST_DELETE_ERROR")
            .With(x => x.StatusCode, 500)
            .Create();
        var expectedResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, false)
            .With(x => x.IsSuccessful, false)
            .With(x => x.Error, errorInfo)
            .Create();

        _mockRequestRepository.Setup(x => x.Query()).Throws(exception);
        _mockResponseHelper
            .Setup(x => x.SetError<bool>(false, It.IsAny<ErrorInfo>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.DeleteRequestAsync(requestId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Data.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    #endregion

    #region Edge Cases and Validation Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task GetRequestByIdAsync_WithInvalidIds_ShouldReturnNotFound(int invalidId) {
        // Arrange
        var requests = new List<MembershipRequest>();
        var errorResponse = _fixture.Build<ServiceResponse<MembershipRequestDto?>>()
            .With(x => x.IsSuccessful, false)
            .Create();

        var mockQueryable = requests.AsQueryable().BuildMock();
        _mockRequestRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockResponseHelper
            .Setup(x => x.SetError<MembershipRequestDto?>(null, "Talep bulunamadý", 404, "MEMBERSHIPREQUEST_NOTFOUND"))
            .Returns(errorResponse);

        // Act
        var result = await _sut.GetRequestByIdAsync(invalidId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public async Task GetPendingRequestsAsync_WhenAllApprovedOrRejected_ShouldReturnEmpty() {
        // Arrange
        var requests = new List<MembershipRequest> {
            _fixture.Build<MembershipRequest>()
                .With(x => x.Status, MembershipRequestStatus.Approved)
                .With(x => x.IsActive, true)
                .Create(),
            _fixture.Build<MembershipRequest>()
                .With(x => x.Status, MembershipRequestStatus.Rejected)
                .With(x => x.IsActive, true)
                .Create()
        };
        var requestDtos = new List<MembershipRequestDto>();
        var expectedResponse = _fixture.Build<ServiceResponse<List<MembershipRequestDto>>>()
            .With(x => x.Data, requestDtos)
            .With(x => x.IsSuccessful, true)
            .Create();

        var mockQueryable = requests.AsQueryable().BuildMock();
        _mockRequestRepository.Setup(x => x.QueryNoTracking()).Returns(mockQueryable);
        _mockMapper.Setup(x => x.Map<List<MembershipRequestDto>>(It.IsAny<List<MembershipRequest>>())).Returns(requestDtos);
        _mockResponseHelper.Setup(x => x.SetSuccess(requestDtos)).Returns(expectedResponse);

        // Act
        var result = await _sut.GetPendingRequestsAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteRequestAsync_WithSaveChangesFailing_ShouldReturnError() {
        // Arrange
        var requestId = _fixture.Create<int>();
        var request = _fixture.Build<MembershipRequest>()
            .With(x => x.Id, requestId)
            .With(x => x.IsActive, true)
            .With(x => x.Status, MembershipRequestStatus.Pending)
            .Create();
        var exception = new DbUpdateException();
        var errorResponse = _fixture.Build<ServiceResponse<bool>>()
            .With(x => x.Data, false)
            .With(x => x.IsSuccessful, false)
            .Create();

        var requests = new List<MembershipRequest> { request };
        var mockQueryable = requests.AsQueryable().BuildMock();

        _mockRequestRepository.Setup(x => x.Query()).Returns(mockQueryable);
        _mockRequestRepository.Setup(x => x.UpdateAsync(It.IsAny<MembershipRequest>())).ReturnsAsync(request);
        _mockRequestRepository.Setup(x => x.SaveChangesAsync()).ThrowsAsync(exception);
        _mockResponseHelper
            .Setup(x => x.SetError<bool>(false, It.IsAny<ErrorInfo>()))
            .Returns(errorResponse);

        // Act
        var result = await _sut.DeleteRequestAsync(requestId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
    }

    #endregion
}

/// <summary>
/// Test implementation of BaseFactory for testing purposes
/// </summary>
public class TestMembershipRequestServiceFactory : BaseFactory<MembershipRequestService> {
    public TestMembershipRequestServiceFactory(
        UtilityFactory<MembershipRequestService> utilityFactory,
        IRepositoryFactory repositoryFactory,
        IServiceProvider serviceProvider)
        : base(utilityFactory, repositoryFactory, serviceProvider) {
    }

    public override UtilityFactory<MembershipRequestService> CreateUtilityFactory() => utilityFactory;
    public override IRepositoryFactory CreateRepositoryFactory() => repositoryFactory;
}
