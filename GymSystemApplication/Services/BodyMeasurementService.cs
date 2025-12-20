using AutoMapper;
using GymSystem.Application.Abstractions.Services.IBodyMeasurement;
using GymSystem.Application.Abstractions.Services.IBodyMeasurement.Contract;
using GymSystem.Common.Factory.Managers;
using GymSystem.Common.Helpers;
using GymSystem.Common.Models;
using GymSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymSystem.Application.Services;

public class BodyMeasurementService : IBodyMeasurementService {
    private readonly BaseFactory<BodyMeasurementService> _baseFactory;
    private readonly IServiceResponseHelper _responseHelper;
    private readonly ILogger<BodyMeasurementService> _logger;
    private readonly IMapper _mapper;

    public BodyMeasurementService(BaseFactory<BodyMeasurementService> baseFactory) {
        _baseFactory = baseFactory;
        _responseHelper = baseFactory.CreateUtilityFactory().CreateServiceResponseHelper();
        _logger = baseFactory.CreateUtilityFactory().CreateLogger();
        _mapper = baseFactory.CreateUtilityFactory().CreateMapper();
    }

    public async Task<ServiceResponse<List<BodyMeasurementDto>>> GetMemberMeasurementsAsync(int memberId) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<BodyMeasurement>();
            var measurements = await repository.QueryNoTracking()
                .Include(m => m.Member)
                .Where(m => m.MemberId == memberId && m.IsActive)
                .OrderByDescending(m => m.MeasurementDate)
                .ToListAsync();

            var dtos = _mapper.Map<List<BodyMeasurementDto>>(measurements);

            // Deðiþimleri hesapla
            CalculateChanges(dtos);

            return _responseHelper.SetSuccess(dtos);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Ölçümler getirilirken hata. Member ID: {MemberId}", memberId);
            return _responseHelper.SetError<List<BodyMeasurementDto>>(null, new ErrorInfo("Ölçümler getirilirken hata oluþtu", "MEASUREMENT_001", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<BodyMeasurementDto?>> GetByIdAsync(int id) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<BodyMeasurement>();
            var measurement = await repository.QueryNoTracking()
                .Include(m => m.Member)
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

            if (measurement == null)
                return _responseHelper.SetError<BodyMeasurementDto?>(null, "Ölçüm bulunamadý", 404, "MEASUREMENT_NOT_FOUND");

            var dto = _mapper.Map<BodyMeasurementDto>(measurement);
            return _responseHelper.SetSuccess<BodyMeasurementDto?>(dto);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Ölçüm getirilirken hata. ID: {Id}", id);
            return _responseHelper.SetError<BodyMeasurementDto?>(null, new ErrorInfo("Ölçüm getirilirken hata oluþtu", "MEASUREMENT_002", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<BodyMeasurementDto>> CreateAsync(BodyMeasurementDto dto) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<BodyMeasurement>();

            var measurement = _mapper.Map<BodyMeasurement>(dto);
            measurement.CreatedAt = DateTimeHelper.Now;
            measurement.IsActive = true;

            await repository.AddAsync(measurement);
            await repository.SaveChangesAsync();

            _logger.LogInformation("Yeni ölçüm eklendi. ID: {Id}, Member ID: {MemberId}", measurement.Id, measurement.MemberId);

            // Bir önceki ölçümü bul ve deðiþimi hesapla
            var previousMeasurement = await repository.QueryNoTracking()
                .Where(m => m.MemberId == dto.MemberId && m.IsActive && m.Id != measurement.Id && m.MeasurementDate < measurement.MeasurementDate)
                .OrderByDescending(m => m.MeasurementDate)
                .FirstOrDefaultAsync();

            var resultDto = _mapper.Map<BodyMeasurementDto>(measurement);

            if (previousMeasurement != null) {
                resultDto.HeightChange = measurement.Height - previousMeasurement.Height;
                resultDto.WeightChange = measurement.Weight - previousMeasurement.Weight;
            }

            return _responseHelper.SetSuccess(resultDto, "Ölçüm baþarýyla eklendi");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Ölçüm eklenirken hata. Member ID: {MemberId}", dto.MemberId);
            return _responseHelper.SetError<BodyMeasurementDto>(null, new ErrorInfo("Ölçüm eklenirken hata oluþtu", "MEASUREMENT_003", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<BodyMeasurementDto>> UpdateAsync(BodyMeasurementDto dto) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<BodyMeasurement>();
            var measurement = await repository.Query()
                .FirstOrDefaultAsync(m => m.Id == dto.Id && m.IsActive);

            if (measurement == null)
                return _responseHelper.SetError<BodyMeasurementDto>(null, "Ölçüm bulunamadý", 404, "MEASUREMENT_NOT_FOUND");

            measurement.MeasurementDate = dto.MeasurementDate;
            measurement.Height = dto.Height;
            measurement.Weight = dto.Weight;
            measurement.Note = dto.Note;
            measurement.UpdatedAt = DateTimeHelper.Now;

            await repository.UpdateAsync(measurement);
            await repository.SaveChangesAsync();

            _logger.LogInformation("Ölçüm güncellendi. ID: {Id}", dto.Id);

            var resultDto = _mapper.Map<BodyMeasurementDto>(measurement);
            return _responseHelper.SetSuccess(resultDto, "Ölçüm baþarýyla güncellendi");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Ölçüm güncellenirken hata. ID: {Id}", dto.Id);
            return _responseHelper.SetError<BodyMeasurementDto>(null, new ErrorInfo("Ölçüm güncellenirken hata oluþtu", "MEASUREMENT_004", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(int id) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<BodyMeasurement>();
            var measurement = await repository.Query()
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

            if (measurement == null)
                return _responseHelper.SetError<bool>(false, "Ölçüm bulunamadý", 404, "MEASUREMENT_NOT_FOUND");

            // Soft delete
            measurement.IsActive = false;
            measurement.UpdatedAt = DateTimeHelper.Now;

            await repository.UpdateAsync(measurement);
            await repository.SaveChangesAsync();

            _logger.LogInformation("Ölçüm silindi. ID: {Id}", id);

            return _responseHelper.SetSuccess(true, "Ölçüm baþarýyla silindi");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Ölçüm silinirken hata. ID: {Id}", id);
            return _responseHelper.SetError<bool>(false, new ErrorInfo("Ölçüm silinirken hata oluþtu", "MEASUREMENT_005", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<List<BodyMeasurementDto>>> GetChartDataAsync(int memberId, DateTime? startDate = null, DateTime? endDate = null) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<BodyMeasurement>();
            var query = repository.QueryNoTracking()
                .Where(m => m.MemberId == memberId && m.IsActive);

            if (startDate.HasValue)
                query = query.Where(m => m.MeasurementDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(m => m.MeasurementDate <= endDate.Value);

            var measurements = await query
                .OrderBy(m => m.MeasurementDate)
                .ToListAsync();

            var dtos = _mapper.Map<List<BodyMeasurementDto>>(measurements);

            // Grafik için deðiþimleri hesapla
            CalculateChanges(dtos, ascending: true);

            return _responseHelper.SetSuccess(dtos);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Grafik verileri getirilirken hata. Member ID: {MemberId}", memberId);
            return _responseHelper.SetError<List<BodyMeasurementDto>>(null, new ErrorInfo("Grafik verileri getirilirken hata oluþtu", "MEASUREMENT_006", ex.StackTrace, 500));
        }
    }

    /// <summary>
    /// Ölçümler arasýndaki deðiþimleri hesaplar
    /// </summary>
    private void CalculateChanges(List<BodyMeasurementDto> measurements, bool ascending = false) {
        if (measurements.Count < 2) return;

        // Ascending = true ise tarihe göre artan sýralý (grafik için)
        // Ascending = false ise tarihe göre azalan sýralý (liste için)
        var ordered = ascending
            ? measurements.OrderBy(m => m.MeasurementDate).ToList()
            : measurements.OrderByDescending(m => m.MeasurementDate).ToList();

        for (int i = 1; i < ordered.Count; i++) {
            var current = ordered[i];
            var previous = ordered[i - 1];

            if (ascending) {
                // Grafik için: þimdiki - önceki
                current.HeightChange = current.Height - previous.Height;
                current.WeightChange = current.Weight - previous.Weight;
            }
            else {
                // Liste için: bir önceki kayda göre
                previous.HeightChange = previous.Height - current.Height;
                previous.WeightChange = previous.Weight - current.Weight;
            }
        }
    }
}
