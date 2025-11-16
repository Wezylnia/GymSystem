# 🏋️ GymSystem - Spor Salonu Yönetim Sistemi
## Proje Yol Haritası ve Modül Planlaması

---

## 📊 PROJE MİMARİSİ

### ✅ Tamamlanan Altyapı
- ✅ Clean Architecture (Domain, Application, Infrastructure, Persistence, API, MVC)
- ✅ Factory Pattern (BaseFactory, UtilityFactory, RepositoryFactory)
- ✅ Generic Repository Pattern
- ✅ Generic CRUD Service Pattern
- ✅ ServiceResponse Pattern (Error Handling)
- ✅ Otomatik Service Registration
- ✅ PostgreSQL Database
- ✅ Entity Framework Core
- ✅ API + MVC Dual Architecture

---

## 🎯 PROJE GEREKSİNİMLERİ ANALİZİ

### 1. **Varlıklar (Entities)**
```
✅ Member (Zaten var)
⬜ GymLocation (Spor Salonu)
⬜ Trainer (Antrenör/Eğitmen)
⬜ Service (Hizmet: Fitness, Yoga, Pilates vb.)
⬜ Appointment (Randevu)
⬜ TrainerAvailability (Antrenör Müsaitlik)
⬜ TrainerSpecialty (Antrenör Uzmanlık)
⬜ WorkingHours (Çalışma Saatleri)
⬜ AppUser (Kullanıcı - Identity)
⬜ Role (Rol - Admin, Member)
⬜ AIWorkoutPlan (Yapay Zeka Egzersiz Planı)
```

### 2. **Roller ve Yetkilendirme**
```
⬜ Admin (ogrencinumarasi@sakarya.edu.tr / sau)
⬜ Member (Kayıtlı kullanıcı)
⬜ Trainer (Antrenör - isteğe bağlı)
```

### 3. **Temel İşlevler**
```
⬜ CRUD Operations (Tüm entities için)
⬜ Authentication & Authorization (ASP.NET Core Identity)
⬜ Randevu Sistemi (Müsaitlik kontrolü)
⬜ Randevu Onay Mekanizması
⬜ REST API (LINQ ile filtreleme)
⬜ AI Entegrasyonu (OpenAI API - Egzersiz/Diyet önerileri)
⬜ Raporlama
⬜ Client & Server-side Validation
```

---

## 📋 MODÜL PLANLAMA (13 Modül)

### **MODÜL 1: Domain Layer - Entity'leri Oluştur** ⏱️ 2-3 saat
**Dosyalar:**
- `GymLocation.cs` - Spor salonu bilgileri
- `Trainer.cs` - Antrenör bilgileri
- `Service.cs` - Hizmet türleri (Fitness, Yoga, Pilates)
- `Appointment.cs` - Randevu
- `TrainerAvailability.cs` - Antrenör müsaitlik saatleri
- `TrainerSpecialty.cs` - Antrenör uzmanlık alanları
- `WorkingHours.cs` - Salon çalışma saatleri
- `AIWorkoutPlan.cs` - AI egzersiz planları
- `AppUser.cs` - Identity user extension
- `BaseEntity.cs` - Ortak base class (Id, CreatedAt, UpdatedAt)

**İlişkiler:**
```
GymLocation (1) → (N) Service
GymLocation (1) → (N) WorkingHours
GymLocation (1) → (N) Trainer

Trainer (1) → (N) TrainerSpecialty
Trainer (1) → (N) TrainerAvailability
Trainer (1) → (N) Appointment

Member (1) → (N) Appointment
Member (1) → (N) AIWorkoutPlan

Service (1) → (N) Appointment
```

---

### **MODÜL 2: Database Configuration & Migrations** ⏱️ 2 saat
**Dosyalar:**
- `GymDbContext.cs` güncelle (yeni DbSet'ler ekle)
- Entity configurations (Fluent API)
- Initial migration oluştur
- Seed data (Test verileri)

**Seed Data:**
- 1 Admin user
- 2 GymLocation
- 5 Service (Fitness, Yoga, Pilates, Cardio, Zumba)
- 3 Trainer
- TrainerSpecialty data
- WorkingHours data

---

### **MODÜL 3: Identity & Authentication** ⏱️ 3-4 saat
**Dosyalar:**
- `AppUser.cs` (IdentityUser'dan türetilmiş)
- `IdentityDbContext` yapılandırması
- Role seeding (Admin, Member, Trainer)
- Login/Register sayfaları (MVC)
- Authentication middleware
- Authorization policies

**Sayfalar:**
- `/Account/Register` - Üye kayıt
- `/Account/Login` - Giriş
- `/Account/Logout` - Çıkış
- `/Admin` - Admin paneli (sadece Admin erişebilir)

---

### **MODÜL 4: Admin Panel - Spor Salonu Yönetimi** ⏱️ 3 saat
**CRUD:**
- GymLocation (CRUD)
- Service (CRUD)
- WorkingHours (CRUD)

**API Endpoints:**
```
GET    /api/gymlocations
GET    /api/gymlocations/{id}
POST   /api/gymlocations
PUT    /api/gymlocations/{id}
DELETE /api/gymlocations/{id}

(Service ve WorkingHours için aynı pattern)
```

**MVC Pages:**
- `/Admin/GymLocations/Index` - Liste
- `/Admin/GymLocations/Create` - Yeni salon
- `/Admin/GymLocations/Edit/{id}` - Düzenle
- `/Admin/GymLocations/Delete/{id}` - Sil

---

### **MODÜL 5: Admin Panel - Antrenör Yönetimi** ⏱️ 3 saat
**CRUD:**
- Trainer (CRUD)
- TrainerSpecialty (CRUD)
- TrainerAvailability (CRUD)

**API Endpoints:**
```
GET    /api/trainers
GET    /api/trainers/{id}
GET    /api/trainers/{id}/specialties
GET    /api/trainers/{id}/availability
POST   /api/trainers
PUT    /api/trainers/{id}
DELETE /api/trainers/{id}
```

**MVC Pages:**
- `/Admin/Trainers/Index`
- `/Admin/Trainers/Create`
- `/Admin/Trainers/Edit/{id}`
- `/Admin/Trainers/Manage/{id}` - Uzmanlık ve müsaitlik yönetimi

---

### **MODÜL 6: Randevu Sistemi - Backend** ⏱️ 4 saat
**Servisler:**
- `IAppointmentService` - Generic CRUD + Custom logic
- `AppointmentService` implementation
  - `CheckAvailability()` - Müsaitlik kontrolü
  - `BookAppointment()` - Randevu al
  - `CancelAppointment()` - Randevu iptal
  - `ConfirmAppointment()` - Randevu onayla (Admin/Trainer)

**Business Rules:**
- Antrenör aynı saatte başka randevusu var mı?
- Seçilen saat, antrenörün müsait saatleri içinde mi?
- Üyenin aynı saatte başka randevusu var mı?
- Hizmet süresi geçerli mi?

**API Endpoints:**
```
GET    /api/appointments
GET    /api/appointments/{id}
GET    /api/appointments/my-appointments (üyenin randevuları)
GET    /api/appointments/check-availability
POST   /api/appointments
PUT    /api/appointments/{id}/confirm
DELETE /api/appointments/{id}
```

---

### **MODÜL 7: Randevu Sistemi - Frontend** ⏱️ 4 saat
**MVC Pages:**
- `/Appointments/Index` - Randevularım
- `/Appointments/Create` - Yeni randevu al
  - Salon seç
  - Hizmet seç
  - Antrenör seç (hizmete göre filtrelenmiş)
  - Tarih seç
  - Uygun saatleri göster (AJAX)
  - Randevu al
- `/Appointments/Details/{id}` - Randevu detayı
- `/Appointments/Cancel/{id}` - Randevu iptal

**Admin/Trainer:**
- `/Admin/Appointments/Index` - Tüm randevular
- `/Admin/Appointments/Confirm/{id}` - Randevu onayla

**JavaScript:**
- Availability checker (AJAX)
- Dynamic trainer/service filtering
- Calendar view (optional: FullCalendar.js)

---

### **MODÜL 8: AI Entegrasyonu - OpenAI API** ⏱️ 4-5 saat
**Servisler:**
- `IAIService` interface
- `OpenAIService` implementation
  - `GetWorkoutPlan()` - Egzersiz planı öner
  - `GetDietPlan()` - Diyet planı öner
  - `GenerateFitnessImage()` - DALL-E ile görsel oluştur

**Input:**
- Boy, kilo, vücut tipi
- Hedef (kilo verme, kas yapma, vb.)
- Fotoğraf (opsiyonel - DALL-E için)

**Output:**
- Egzersiz planı (AI tarafından oluşturulan metin)
- Diyet önerileri
- "Böyle görünebilirsin" görseli

**API Endpoints:**
```
POST /api/ai/workout-plan
POST /api/ai/diet-plan
POST /api/ai/fitness-image
```

**MVC Pages:**
- `/AI/Index` - AI asistanı ana sayfa
- `/AI/WorkoutPlan` - Egzersiz planı al
- `/AI/MyPlans` - Kayıtlı planlarım

**NuGet Package:**
```
dotnet add package OpenAI --version 1.11.0
```

---

### **MODÜL 9: Raporlama ve LINQ Sorguları** ⏱️ 2-3 saat
**API Endpoints (LINQ):**
```
GET /api/reports/trainers-by-specialty?specialty=yoga
GET /api/reports/available-trainers?date=2025-01-15&time=10:00
GET /api/reports/member-appointments?memberId=5
GET /api/reports/popular-services
GET /api/reports/trainer-workload?trainerId=3
GET /api/reports/monthly-revenue?month=1&year=2025
```

**MVC Pages:**
- `/Reports/Index` - Raporlar ana sayfa
- `/Reports/Trainers` - Antrenör raporları
- `/Reports/Appointments` - Randevu raporları
- `/Reports/Revenue` - Gelir raporları

**Charts:** Chart.js kullanarak grafikler

---

### **MODÜL 10: Validation (Client & Server)** ⏱️ 2 saat
**Server-side:**
- Data Annotations ([Required], [EmailAddress], [Range], vb.)
- FluentValidation (opsiyonel)
- Custom validation attributes

**Client-side:**
- jQuery Validation
- Unobtrusive validation
- Custom JavaScript validators

**Örnekler:**
- Email formatı
- Telefon formatı
- Tarih aralıkları (başlangıç < bitiş)
- Saat formatı
- Boy/kilo aralıkları

---

### **MODÜL 11: UI/UX ve Bootstrap Tema** ⏱️ 3-4 saat
**Layout:**
- `_Layout.cshtml` - Ana layout (Bootstrap 5)
- Navbar (role-based menü)
- Footer
- Alert messages (TempData)

**Sayfalar:**
- `/Home/Index` - Ana sayfa (hizmetler showcase)
- `/About` - Hakkımızda
- `/Contact` - İletişim
- `/Services` - Hizmetlerimiz

**CSS/JS:**
- Custom CSS (`site.css`)
- Bootstrap 5
- Font Awesome icons
- jQuery
- AJAX helpers

---

### **MODÜL 12: Testing & Bug Fixes** ⏱️ 3 saat
- Tüm CRUD işlemlerini test et
- Randevu sistemi end-to-end test
- Authorization test (rol bazlı erişim)
- AI entegrasyonu test
- Validation test
- Browser compatibility
- Responsive design test
- Bug fixes

---

### **MODÜL 13: Deployment & Documentation** ⏱️ 2-3 saat
**GitHub:**
- En az 10 meaningful commit
- README.md (proje açıklaması, setup, screenshots)
- .gitignore güncellemesi

**Rapor:**
- Kapak sayfası (isim, numara, grup, GitHub link)
- Proje tanıtımı
- Veritabanı modeli (ER diyagramı)
- Ekran görüntüleri
- Kullanılan teknolojiler
- Kurulum adımları

**Database:**
- Production connection string
- Seed data
- Migration scripts

---

## ⏰ TOPLAM SÜRE TAHMİNİ: 40-50 saat

---

## 🚀 GELİŞTİRME SIRASI

### **Faz 1: Temel (Hafta 1)**
1. Modül 1: Entity'ler
2. Modül 2: Database
3. Modül 3: Identity

### **Faz 2: Admin Panel (Hafta 2)**
4. Modül 4: Spor Salonu Yönetimi
5. Modül 5: Antrenör Yönetimi

### **Faz 3: Randevu (Hafta 3)**
6. Modül 6: Randevu Backend
7. Modül 7: Randevu Frontend

### **Faz 4: AI & Raporlama (Hafta 4)**
8. Modül 8: AI Entegrasyonu
9. Modül 9: Raporlama

### **Faz 5: Finalizasyon (Hafta 5)**
10. Modül 10: Validation
11. Modül 11: UI/UX
12. Modül 12: Testing
13. Modül 13: Deployment

---

## 📌 ÖNCELİK SIRASI

### **Kritik (Mutlaka olmalı):**
- ✅ Member CRUD (var)
- ⬜ GymLocation CRUD
- ⬜ Trainer CRUD
- ⬜ Service CRUD
- ⬜ Appointment CRUD + Logic
- ⬜ Identity & Authorization
- ⬜ AI Entegrasyonu
- ⬜ En az 1 LINQ API endpoint

### **Önemli (Puan artırıcı):**
- ⬜ Admin Panel
- ⬜ Randevu onay sistemi
- ⬜ Raporlama
- ⬜ Client/Server validation
- ⬜ UI/UX kalitesi

### **Opsiyonel (Bonus):**
- ⬜ Email notifications
- ⬜ Calendar view
- ⬜ Dashboard charts
- ⬜ Profile photo upload
- ⬜ Payment integration (gelecek özellik)

---

## 🎯 SONRAKİ ADIM

**Şimdi başlayalım! Hangi modülden başlamak istersin?**

**Önerim:** 
1. **MODÜL 1** - Entity'leri oluştur (Domain layer temeli)
2. Sonra **MODÜL 2** - Database migration
3. Sonra **MODÜL 3** - Identity ekle

Onay verirsen Modül 1'den başlayalım! 🚀
