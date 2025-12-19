using AutoMapper;
using GymSystem.Application.Abstractions.Services.ITrainerService;
using GymSystem.Application.Abstractions.Services.ITrainerService.Contract;
using GymSystem.Common.Factory.Managers;
using GymSystem.Common.Helpers;
using GymSystem.Common.Models;
using GymSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymSystem.Application.Services.Trainers;
public class TrainerService : ITrainerService
{
    private readonly BaseFactory<TrainerService> _baseFactory;
    private readonly IServiceResponseHelper _responseHelper;
    private readonly ILogger<TrainerService> _logger;
    private readonly IMapper _mapper;

    public TrainerService(BaseFactory<TrainerService> baseFactory)
    {
        _baseFactory = baseFactory;
        _responseHelper = baseFactory.CreateUtilityFactory().CreateServiceResponseHelper();
        _logger = baseFactory.CreateUtilityFactory().CreateLogger();
        _mapper = baseFactory.CreateUtilityFactory().CreateMapper();
    }

    public async Task<ServiceResponse<List<TrainerDto>>> GetAllAsync()
    {
        try
        {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Trainer>();
            var trainers = await repository.QueryNoTracking()
                .Include(t => t.GymLocation)
                .Include(t => t.Specialties.Where(s => s.IsActive))
                    .ThenInclude(s => s.Service)
                .Where(t => t.IsActive)
                .OrderBy(t => t.FirstName)
                .ToListAsync();

            var dtos = _mapper.Map<List<TrainerDto>>(trainers);
            return _responseHelper.SetSuccess(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Antrenörler getirilirken hata oluştu");
            return _responseHelper.SetError<List<TrainerDto>>(null, new ErrorInfo("Antrenörler getirilemedi", "TRAINER_GETALL_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<TrainerDto?>> GetByIdAsync(int id)
    {
        try
        {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Trainer>();
            var trainer = await repository.QueryNoTracking()
                .Include(t => t.GymLocation)
                .Include(t => t.Specialties.Where(s => s.IsActive))
                    .ThenInclude(s => s.Service)
                .Where(t => t.Id == id && t.IsActive)
                .FirstOrDefaultAsync();

            if (trainer == null)
                return _responseHelper.SetError<TrainerDto?>(null, "Antrenör bulunamadı", 404, "TRAINER_NOTFOUND");

            var dto = _mapper.Map<TrainerDto>(trainer);
            return _responseHelper.SetSuccess<TrainerDto?>(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Antrenör getirilirken hata oluştu. ID: {Id}", id);
            return _responseHelper.SetError<TrainerDto?>(null, new ErrorInfo("Antrenör getirilemedi", "TRAINER_GET_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<TrainerDto>> CreateAsync(TrainerDto dto)
    {
        try
        {
            var trainer = _mapper.Map<Trainer>(dto, opts => opts.AfterMap((src, dest) => {
                dest.CreatedAt = DateTimeHelper.Now;
                dest.IsActive = true;
            }));

            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Trainer>();
            await repository.AddAsync(trainer);
            await repository.SaveChangesAsync();

            // Hizmet uzmanlıklarını ekle
            if (dto.SelectedServiceIds != null && dto.SelectedServiceIds.Any())
            {
                var specialtyRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<TrainerSpecialty>();
                foreach (var serviceId in dto.SelectedServiceIds)
                {
                    var specialty = new TrainerSpecialty
                    {
                        TrainerId = trainer.Id,
                        ServiceId = serviceId,
                        ExperienceYears = 0,
                        CreatedAt = DateTimeHelper.Now,
                        IsActive = true
                    };
                    await specialtyRepository.AddAsync(specialty);
                }
                await specialtyRepository.SaveChangesAsync();
            }

            _logger.LogInformation("Antrenör oluşturuldu. ID: {Id}, İsim: {FirstName} {LastName}", trainer.Id, trainer.FirstName, trainer.LastName);

            // Güncel veriyi getir
            var createdTrainer = await repository.QueryNoTracking()
                .Include(t => t.GymLocation)
                .Include(t => t.Specialties.Where(s => s.IsActive))
                    .ThenInclude(s => s.Service)
                .FirstOrDefaultAsync(t => t.Id == trainer.Id);

            var responseDto = _mapper.Map<TrainerDto>(createdTrainer);
            return _responseHelper.SetSuccess(responseDto, "Antrenör oluşturuldu");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Antrenör oluşturulurken hata oluştu");
            return _responseHelper.SetError<TrainerDto>(null, new ErrorInfo("Antrenör oluşturulamadı", "TRAINER_CREATE_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<TrainerDto>> UpdateAsync(int id, TrainerDto dto)
    {
        try
        {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Trainer>();
            var trainer = await repository.Query()
                .Include(t => t.Specialties)
                .Where(t => t.Id == id && t.IsActive)
                .FirstOrDefaultAsync();

            if (trainer == null)
                return _responseHelper.SetError<TrainerDto>(null, "Antrenör bulunamadı", 404, "TRAINER_NOTFOUND");

            _mapper.Map(dto, trainer);
            trainer.UpdatedAt = DateTimeHelper.Now;

            await repository.UpdateAsync(trainer);
            await repository.SaveChangesAsync();

            // Hizmet uzmanlıklarını güncelle
            var specialtyRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<TrainerSpecialty>();
            
            // Mevcut uzmanlıkları soft delete yap
            foreach (var specialty in trainer.Specialties.Where(s => s.IsActive))
            {
                specialty.IsActive = false;
                specialty.UpdatedAt = DateTimeHelper.Now;
                await specialtyRepository.UpdateAsync(specialty);
            }

            // Yeni uzmanlıkları ekle
            if (dto.SelectedServiceIds != null && dto.SelectedServiceIds.Any())
            {
                foreach (var serviceId in dto.SelectedServiceIds)
                {
                    // Daha önce eklenmiş ve soft delete yapılmış bir kayıt var mı kontrol et
                    var existingSpecialty = trainer.Specialties.FirstOrDefault(s => s.ServiceId == serviceId);
                    if (existingSpecialty != null)
                    {
                        existingSpecialty.IsActive = true;
                        existingSpecialty.UpdatedAt = DateTimeHelper.Now;
                        await specialtyRepository.UpdateAsync(existingSpecialty);
                    }
                    else
                    {
                        var specialty = new TrainerSpecialty
                        {
                            TrainerId = trainer.Id,
                            ServiceId = serviceId,
                            ExperienceYears = 0,
                            CreatedAt = DateTimeHelper.Now,
                            IsActive = true
                        };
                        await specialtyRepository.AddAsync(specialty);
                    }
                }
            }
            await specialtyRepository.SaveChangesAsync();

            _logger.LogInformation("Antrenör güncellendi. ID: {Id}, İsim: {FirstName} {LastName}", trainer.Id, trainer.FirstName, trainer.LastName);

            // Güncel veriyi getir
            var updatedTrainer = await repository.QueryNoTracking()
                .Include(t => t.GymLocation)
                .Include(t => t.Specialties.Where(s => s.IsActive))
                    .ThenInclude(s => s.Service)
                .FirstOrDefaultAsync(t => t.Id == trainer.Id);

            var responseDto = _mapper.Map<TrainerDto>(updatedTrainer);
            return _responseHelper.SetSuccess(responseDto, "Antrenör güncellendi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Antrenör güncellenirken hata oluştu. ID: {Id}", id);
            return _responseHelper.SetError<TrainerDto>(null, new ErrorInfo("Antrenör güncellenemedi", "TRAINER_UPDATE_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(int id)
    {
        try
        {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Trainer>();
            var trainer = await repository.Query().Where(t => t.Id == id && t.IsActive).FirstOrDefaultAsync();

            if (trainer == null)
                return _responseHelper.SetError<bool>(false, "Antrenör bulunamadı", 404, "TRAINER_NOTFOUND");

            trainer.IsActive = false;
            trainer.UpdatedAt = DateTimeHelper.Now;

            await repository.UpdateAsync(trainer);
            await repository.SaveChangesAsync();

            _logger.LogInformation("Antrenör silindi. ID: {Id}, İsim: {FirstName} {LastName}", id, trainer.FirstName, trainer.LastName);
            return _responseHelper.SetSuccess(true, "Antrenör silindi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Antrenör silinirken hata oluştu. ID: {Id}", id);
            return _responseHelper.SetError<bool>(false, new ErrorInfo("Antrenör silinemedi", "TRAINER_DELETE_ERROR", ex.StackTrace, 500));
        }
    }
}