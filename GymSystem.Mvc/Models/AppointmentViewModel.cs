using System.ComponentModel.DataAnnotations;

namespace GymSystem.Mvc.Models;

public class AppointmentViewModel
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Üye seçimi zorunludur")]
    public int MemberId { get; set; }
    
    [Required(ErrorMessage = "Antrenör seçimi zorunludur")]
    public int TrainerId { get; set; }
    
    [Required(ErrorMessage = "Hizmet seçimi zorunludur")]
    public int ServiceId { get; set; }
    
    [Required(ErrorMessage = "Randevu tarihi ve saati zorunludur")]
    public DateTime AppointmentDate { get; set; }
    
    [Required(ErrorMessage = "Süre zorunludur")]
    [Range(1, 480, ErrorMessage = "Süre 1-480 dakika arasında olmalıdır")]
    public int DurationMinutes { get; set; }
    
    [Required(ErrorMessage = "Fiyat zorunludur")]
    [Range(0.01, 999999.99, ErrorMessage = "Fiyat geçerli bir değer olmalıdır")]
    public decimal Price { get; set; }
    
    public string Status { get; set; } = "Pending";
    
    [StringLength(1000)]
    public string? Notes { get; set; }
    
    // Display properties
    public string? MemberName { get; set; }
    public string? TrainerName { get; set; }
    public string? ServiceName { get; set; }
    public string? GymLocationName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateAppointmentViewModel
{
    [Required(ErrorMessage = "Üye seçimi zorunludur")]
    public int MemberId { get; set; }
    
    [Required(ErrorMessage = "Spor salonu seçimi zorunludur")]
    public int GymLocationId { get; set; }
    
    [Required(ErrorMessage = "Hizmet seçimi zorunludur")]
    public int ServiceId { get; set; }
    
    [Required(ErrorMessage = "Antrenör seçimi zorunludur")]
    public int TrainerId { get; set; }
    
    [Required(ErrorMessage = "Randevu tarihi zorunludur")]
    [DataType(DataType.Date)]
    public DateTime AppointmentDate { get; set; }
    
    [Required(ErrorMessage = "Randevu saati zorunludur")]
    [DataType(DataType.Time)]
    public TimeSpan AppointmentTime { get; set; }
    
    [StringLength(1000)]
    public string? Notes { get; set; }
}
