using GymSystem.Application.Abstractions.Services.IAIWorkoutPlan;
using GymSystem.Application.Abstractions.Services.IGemini;
using GymSystem.Common.Factory.Managers;
using GymSystem.Common.Helpers;
using GymSystem.Common.Models;
using GymSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoMapper;
using GymSystem.Application.Abstractions.Services.IAIWorkoutPlan.Contract;

namespace GymSystem.Application.Services.AI;

public class AIWorkoutPlanService : IAIWorkoutPlanService {
    private readonly BaseFactory<AIWorkoutPlanService> _baseFactory;
    private readonly IServiceResponseHelper _responseHelper;
    private readonly ILogger<AIWorkoutPlanService> _logger;
    private readonly IMapper _mapper;

    public AIWorkoutPlanService(BaseFactory<AIWorkoutPlanService> baseFactory) {
        _baseFactory = baseFactory;
        _responseHelper = baseFactory.CreateUtilityFactory().CreateServiceResponseHelper();
        _logger = baseFactory.CreateUtilityFactory().CreateLogger();
        _mapper = baseFactory.CreateUtilityFactory().CreateMapper();
    }

    public async Task<ServiceResponse<AIWorkoutPlanDto>> GenerateWorkoutPlanAsync(AIWorkoutPlanDto request) {
        try {
            var memberRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<Member>();
            var member = await memberRepository.QueryNoTracking().FirstOrDefaultAsync(m => m.Id == request.MemberId);

            if (member == null)
                return _responseHelper.SetError<AIWorkoutPlanDto>(null, $"Member ID {request.MemberId} bulunamadı.", 404, "AI_WORKOUT_001");

            // Member'dan Gender bilgisini al
            request.Gender = member.Gender;

            var geminiService = _baseFactory.GetService<IGeminiApiService>();
            var aiPlanResponse = await geminiService.GenerateWorkoutPlanAsync(request);

            if (!aiPlanResponse.IsSuccessful || string.IsNullOrWhiteSpace(aiPlanResponse.Data)) {
                _logger.LogError("Gemini API'den plan alınırken hata. Member ID: {MemberId}, Error: {Error}", request.MemberId, aiPlanResponse.Error?.ErrorMessage);
                return _responseHelper.SetError<AIWorkoutPlanDto>(null, aiPlanResponse.Error?.ErrorMessage ?? "AI planı oluşturulamadı", aiPlanResponse.Error?.StatusCode ?? 500, aiPlanResponse.Error?.ErrorCode ?? "AI_WORKOUT_002");
            }

            // Fotoğraf varsa 6 ay sonraki hedef görselini oluştur
            string? futureBodyImage = null;
            if (!string.IsNullOrEmpty(request.PhotoBase64)) {
                _logger.LogInformation("Fotoğraf mevcut, 6 ay sonraki hedef görsel oluşturuluyor...");
                var futureImageResponse = await geminiService.GenerateFutureBodyImageAsync(request);
                if (futureImageResponse.IsSuccessful && !string.IsNullOrEmpty(futureImageResponse.Data)) {
                    futureBodyImage = futureImageResponse.Data;
                    _logger.LogInformation("6 ay sonraki hedef görsel başarıyla oluşturuldu");
                } else {
                    _logger.LogWarning("6 ay sonraki hedef görsel oluşturulamadı: {Error}", futureImageResponse.Error?.ErrorMessage);
                }
            }

            var workoutPlan = _mapper.Map<AIWorkoutPlan>(request, opts => opts.AfterMap((src, dest) => {
                dest.PlanType = "Workout";
                dest.AIGeneratedPlan = aiPlanResponse.Data!;
                dest.AIModel = !string.IsNullOrEmpty(request.PhotoBase64) ? "gemini-2.0-flash-exp-vision" : "gemini-2.0-flash-exp";
                dest.FutureBodyImageBase64 = futureBodyImage;
                dest.ImageUrl = request.PhotoBase64; // Kullanıcının yüklediği fotoğraf
            }));

            var workoutPlanRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<AIWorkoutPlan>();
            await workoutPlanRepository.AddAsync(workoutPlan);
            await workoutPlanRepository.SaveChangesAsync();

            _logger.LogInformation("Workout planı oluşturuldu. Member ID: {MemberId}, Plan ID: {PlanId}", request.MemberId, workoutPlan.Id);

            var responseDto = _mapper.Map<AIWorkoutPlanDto>(workoutPlan);
            responseDto.FutureBodyImageBase64 = futureBodyImage;
            return _responseHelper.SetSuccess(responseDto, "Workout planı başarıyla oluşturuldu");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Workout planı oluşturulurken hata. Member ID: {MemberId}", request.MemberId);
            return _responseHelper.SetError<AIWorkoutPlanDto>(null, new ErrorInfo("Workout planı oluşturulurken bir hata oluştu", "AI_WORKOUT_003", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<AIWorkoutPlanDto>> GenerateDietPlanAsync(AIWorkoutPlanDto request) {
        try {
            var memberRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<Member>();
            var member = await memberRepository.QueryNoTracking().FirstOrDefaultAsync(m => m.Id == request.MemberId);

            if (member == null)
                return _responseHelper.SetError<AIWorkoutPlanDto>(null, $"Member ID {request.MemberId} bulunamadı.", 404, "AI_DIET_001");

            // Member'dan Gender bilgisini al
            request.Gender = member.Gender;

            var geminiService = _baseFactory.GetService<IGeminiApiService>();
            var aiPlanResponse = await geminiService.GenerateDietPlanAsync(request);

            if (!aiPlanResponse.IsSuccessful || string.IsNullOrWhiteSpace(aiPlanResponse.Data)) {
                _logger.LogError("Gemini API'den plan alınırken hata. Member ID: {MemberId}, Error: {Error}", request.MemberId, aiPlanResponse.Error?.ErrorMessage);
                return _responseHelper.SetError<AIWorkoutPlanDto>(null, aiPlanResponse.Error?.ErrorMessage ?? "AI diet planı oluşturulamadı", aiPlanResponse.Error?.StatusCode ?? 500, aiPlanResponse.Error?.ErrorCode ?? "AI_DIET_002");
            }

            // Fotoğraf varsa 6 ay sonraki hedef görselini oluştur
            string? futureBodyImage = null;
            if (!string.IsNullOrEmpty(request.PhotoBase64)) {
                _logger.LogInformation("Fotoğraf mevcut, 6 ay sonraki hedef görsel oluşturuluyor...");
                var futureImageResponse = await geminiService.GenerateFutureBodyImageAsync(request);
                if (futureImageResponse.IsSuccessful && !string.IsNullOrEmpty(futureImageResponse.Data)) {
                    futureBodyImage = futureImageResponse.Data;
                    _logger.LogInformation("6 ay sonraki hedef görsel başarıyla oluşturuldu");
                } else {
                    _logger.LogWarning("6 ay sonraki hedef görsel oluşturulamadı: {Error}", futureImageResponse.Error?.ErrorMessage);
                }
            }

            var dietPlan = _mapper.Map<AIWorkoutPlan>(request, opts => opts.AfterMap((src, dest) => {
                dest.PlanType = "Diet";
                dest.AIGeneratedPlan = aiPlanResponse.Data!;
                dest.AIModel = !string.IsNullOrEmpty(request.PhotoBase64) ? "gemini-2.0-flash-exp-vision" : "gemini-2.0-flash-exp";
                dest.FutureBodyImageBase64 = futureBodyImage;
                dest.ImageUrl = request.PhotoBase64; // Kullanıcının yüklediği fotoğraf
            }));

            var dietPlanRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<AIWorkoutPlan>();
            await dietPlanRepository.AddAsync(dietPlan);
            await dietPlanRepository.SaveChangesAsync();

            _logger.LogInformation("Diet planı oluşturuldu. Member ID: {MemberId}, Plan ID: {PlanId}", request.MemberId, dietPlan.Id);

            var responseDto = _mapper.Map<AIWorkoutPlanDto>(dietPlan);
            responseDto.FutureBodyImageBase64 = futureBodyImage;
            return _responseHelper.SetSuccess(responseDto, "Diet planı başarıyla oluşturuldu");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Diet planı oluşturulurken hata. Member ID: {MemberId}", request.MemberId);
            return _responseHelper.SetError<AIWorkoutPlanDto>(null, new ErrorInfo("Diet planı oluşturulurken bir hata oluştu", "AI_DIET_003", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<List<AIWorkoutPlanDto>>> GetMemberPlansAsync(int memberId) {
        try {
            var planRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<AIWorkoutPlan>();
            var plans = await planRepository.QueryNoTracking().Include(p => p.Member).Where(p => p.MemberId == memberId && p.IsActive).OrderByDescending(p => p.CreatedAt).ToListAsync();

            var dtos = _mapper.Map<List<AIWorkoutPlanDto>>(plans);
            return _responseHelper.SetSuccess(dtos);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Member planları getirilirken hata. Member ID: {MemberId}", memberId);
            return _responseHelper.SetError<List<AIWorkoutPlanDto>>(null, new ErrorInfo("Planlar getirilirken bir hata oluştu", "AI_PLANS_001", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<AIWorkoutPlanDto?>> GetPlanByIdAsync(int id) {
        try {
            var planRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<AIWorkoutPlan>();
            var plan = await planRepository.QueryNoTracking().Include(p => p.Member).Where(p => p.Id == id && p.IsActive).FirstOrDefaultAsync();

            if (plan == null)
                return _responseHelper.SetError<AIWorkoutPlanDto?>(null, $"Plan ID {id} bulunamadı", 404, "AI_PLAN_001");

            var dto = _mapper.Map<AIWorkoutPlanDto>(plan);
            return _responseHelper.SetSuccess<AIWorkoutPlanDto?>(dto);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Plan getirilirken hata. Plan ID: {PlanId}", id);
            return _responseHelper.SetError<AIWorkoutPlanDto?>(null, new ErrorInfo("Plan getirilirken bir hata oluştu", "AI_PLAN_002", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<bool>> DeletePlanAsync(int id) {
        try {
            var planRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<AIWorkoutPlan>();
            var plan = await planRepository.Query().Where(p => p.Id == id && p.IsActive).FirstOrDefaultAsync();

            if (plan == null)
                return _responseHelper.SetError<bool>(false, $"Plan ID {id} bulunamadı", 404, "AI_DELETE_001");

            plan.IsActive = false;
            plan.UpdatedAt = DateTimeHelper.Now;

            await planRepository.UpdateAsync(plan);
            await planRepository.SaveChangesAsync();

            _logger.LogInformation("AI Plan silindi. Plan ID: {PlanId}", id);
            return _responseHelper.SetSuccess(true, "Plan başarıyla silindi");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Plan silinirken hata oluştu. Plan ID: {PlanId}", id);
            return _responseHelper.SetError<bool>(false, new ErrorInfo("Plan silinirken bir hata oluştu", "AI_DELETE_002", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<List<AIWorkoutPlanDto>>> GetAllPlansAsync() {
        try {
            var planRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<AIWorkoutPlan>();
            var plans = await planRepository.QueryNoTracking().Include(p => p.Member).Where(p => p.IsActive).OrderByDescending(p => p.CreatedAt).ToListAsync();

            var dtos = _mapper.Map<List<AIWorkoutPlanDto>>(plans);
            return _responseHelper.SetSuccess(dtos);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Tüm planlar getirilirken hata");
            return _responseHelper.SetError<List<AIWorkoutPlanDto>>(null, new ErrorInfo("Tüm planlar getirilirken bir hata oluştu", "AI_ALLPLANS_001", ex.StackTrace, 500));
        }
    }
}