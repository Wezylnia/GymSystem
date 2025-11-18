using GymSystem.Application.Abstractions.Services;
using GymSystem.Common.Factory.Managers;
using GymSystem.Common.Helpers;
using GymSystem.Common.Models;
using GymSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymSystem.Application.Services.Trainers;

public class TrainerService : ITrainerService {
    private readonly BaseFactory<TrainerService> _baseFactory;
    private readonly IServiceResponseHelper _responseHelper;
    private readonly ILogger<TrainerService> _logger;

    public TrainerService(BaseFactory<TrainerService> baseFactory) {
        _baseFactory = baseFactory;
        _responseHelper = baseFactory.CreateUtilityFactory().CreateServiceResponseHelper();
        _logger = baseFactory.CreateUtilityFactory().CreateLogger();
    }

    public async Task<ServiceResponse<List<Trainer>>> GetAllAsync() {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Trainer>();
            var trainers = await repository.QueryNoTracking().Include(t => t.GymLocation).Where(t => t.IsActive).OrderBy(t => t.FirstName).ToListAsync();
            return _responseHelper.SetSuccess(trainers);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Antrenörler getirilirken hata oluştu");
            return _responseHelper.SetError<List<Trainer>>(null, new ErrorInfo("Antrenörler getirilemedi", "TRAINER_GETALL_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<Trainer?>> GetByIdAsync(int id) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Trainer>();
            var trainer = await repository.QueryNoTracking().Include(t => t.GymLocation).Where(t => t.Id == id && t.IsActive).FirstOrDefaultAsync();

            if (trainer == null)
                return _responseHelper.SetError<Trainer?>(null, "Antrenör bulunamadı", 404, "TRAINER_NOTFOUND");

            return _responseHelper.SetSuccess<Trainer?>(trainer);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Antrenör getirilirken hata oluştu. ID: {Id}", id);
            return _responseHelper.SetError<Trainer?>(null, new ErrorInfo("Antrenör getirilemedi", "TRAINER_GET_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<Trainer>> CreateAsync(Trainer entity) {
        try {
            entity.CreatedAt = DateTimeHelper.Now;
            entity.IsActive = true;

            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Trainer>();
            await repository.AddAsync(entity);
            await repository.SaveChangesAsync();

            return _responseHelper.SetSuccess(entity, "Antrenör oluşturuldu");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Antrenör oluşturulurken hata oluştu");
            return _responseHelper.SetError<Trainer>(null, new ErrorInfo("Antrenör oluşturulamadı", "TRAINER_CREATE_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<Trainer>> UpdateAsync(int id, Trainer entity) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Trainer>();
            var existingTrainer = await repository.Query().Where(t => t.Id == id && t.IsActive).FirstOrDefaultAsync();

            if (existingTrainer == null)
                return _responseHelper.SetError<Trainer>(null, "Antrenör bulunamadı", 404, "TRAINER_NOTFOUND");

            entity.UpdatedAt = DateTimeHelper.Now;
            await repository.UpdateAsync(entity);
            await repository.SaveChangesAsync();

            return _responseHelper.SetSuccess(entity, "Antrenör güncellendi");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Antrenör güncellenirken hata oluştu. ID: {Id}", id);
            return _responseHelper.SetError<Trainer>(null, new ErrorInfo("Antrenör güncellenemedi", "TRAINER_UPDATE_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(int id) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Trainer>();
            var trainer = await repository.Query().Where(t => t.Id == id && t.IsActive).FirstOrDefaultAsync();

            if (trainer == null)
                return _responseHelper.SetError<bool>(false, "Antrenör bulunamadı", 404, "TRAINER_NOTFOUND");

            trainer.IsActive = false;
            trainer.UpdatedAt = DateTimeHelper.Now;

            await repository.UpdateAsync(trainer);
            await repository.SaveChangesAsync();

            return _responseHelper.SetSuccess(true, "Antrenör silindi");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Antrenör silinirken hata oluştu. ID: {Id}", id);
            return _responseHelper.SetError<bool>(false, new ErrorInfo("Antrenör silinemedi", "TRAINER_DELETE_ERROR", ex.StackTrace, 500));
        }
    }
}