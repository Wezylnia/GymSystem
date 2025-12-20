using GymSystem.Domain.Enums;

namespace GymSystem.Application.Services.AI.Helpers;

/// <summary>
/// Gemini API için prompt þablonlarýný yöneten helper sýnýfý
/// </summary>
public static class GeminiPromptHelper {
    /// <summary>
    /// Workout planý için prompt oluþturur
    /// </summary>
    public static string BuildWorkoutPrompt(decimal height, decimal weight, Gender gender, string? bodyType, string goal) {
        var bmi = weight / ((height / 100) * (height / 100));
        var genderText = gender == Gender.Female ? "Kadýn" : "Erkek";
        var genderSpecificAdvice = gender == Gender.Female
            ? "Kadýnlar için özellikle alt vücut, kalça ve bacak egzersizlerine odaklan. Aðýrlýklarý kadýnlar için uygun seç."
            : "Erkekler için göðüs, omuz ve kol egzersizlerine aðýrlýk ver. Daha yüksek aðýrlýklarla çalýþýlabilir.";

        return $@"Sen bir fitness koçusun. Aþaðýdaki bilgilere göre KISA ve ÖZ bir haftalýk egzersiz planý oluþtur.

Bilgiler:
- Cinsiyet: {genderText}
- Boy: {height} cm, Kilo: {weight} kg, BMI: {bmi:F2}
- Vücut Tipi: {bodyType ?? "Belirtilmemiþ"}
- Hedef: {goal}

ÖNEMLÝ: Cinsiyete uygun egzersizler seç. {genderSpecificAdvice}

SADECE ÞU FORMATTA YAZ (gereksiz açýklama yapma):

?? DURUM ANALÝZÝ
BMI: {bmi:F2} - [deðerlendirme 1 cümle]

?? HAFTALIK EGZERSÝZ PLANI

PAZARTESÝ - [Kas Grubu]
• Egzersiz 1: 3x12
• Egzersiz 2: 3x12
• Egzersiz 3: 3x10

SALI - [Kas Grubu]
• Egzersiz 1: 3x12
• Egzersiz 2: 3x12

ÇARÞAMBA - Dinlenme

PERÞEMBE - [Kas Grubu]
• Egzersiz 1: 3x12
• Egzersiz 2: 3x12

CUMA - [Kas Grubu]
• Egzersiz 1: 3x12
• Egzersiz 2: 3x12

CUMARTESÝ - Cardio veya Dinlenme

PAZAR - Dinlenme

?? BESLENME ÖNERÝSÝ
Günlük kalori: [miktar] kcal
Protein: [miktar]g | Karbonhidrat: [miktar]g | Yað: [miktar]g

?? ÖNEMLÝ NOTLAR
• [Not 1]
• [Not 2]

Türkçe yaz. Kýsa ve net ol. Gereksiz açýklama yapma!";
    }

    /// <summary>
    /// Diyet planý için prompt oluþturur
    /// </summary>
    public static string BuildDietPrompt(decimal height, decimal weight, Gender gender, string? bodyType, string goal) {
        var bmi = weight / ((height / 100) * (height / 100));
        var genderText = gender == Gender.Female ? "Kadýn" : "Erkek";
        var genderSpecificAdvice = gender == Gender.Female
            ? "Kadýnlar için genelde 1500-2000 kcal aralýðýnda, demir ve kalsiyum içeren besinlere odaklan."
            : "Erkekler için genelde 2000-2500 kcal aralýðýnda, protein aðýrlýklý beslenme öner.";

        return $@"Sen bir beslenme uzmanýsýn. Aþaðýdaki bilgilere göre KISA ve ÖZ bir haftalýk diyet planý oluþtur.

Bilgiler:
- Cinsiyet: {genderText}
- Boy: {height} cm, Kilo: {weight} kg, BMI: {bmi:F2}
- Vücut Tipi: {bodyType ?? "Belirtilmemiþ"}
- Hedef: {goal}

ÖNEMLÝ: Cinsiyete uygun kalori ve makro besin hesapla. {genderSpecificAdvice}

SADECE ÞU FORMATTA YAZ (gereksiz açýklama yapma):

?? BESÝN ANALÝZÝ
Günlük kalori: [miktar] kcal
Protein: [miktar]g | Karbonhidrat: [miktar]g | Yað: [miktar]g

??? HAFTALIK DÝYET PLANI

PAZARTESÝ
Kahvaltý: [yiyecek] - [kalori]kcal
Öðle: [yiyecek] - [kalori]kcal
Akþam: [yiyecek] - [kalori]kcal

SALI
Kahvaltý: [yiyecek] - [kalori]kcal
Öðle: [yiyecek] - [kalori]kcal
Akþam: [yiyecek] - [kalori]kcal

ÇARÞAMBA
Kahvaltý: [yiyecek] - [kalori]kcal
Öðle: [yiyecek] - [kalori]kcal
Akþam: [yiyecek] - [kalori]kcal

PERÞEMBE
Kahvaltý: [yiyecek] - [kalori]kcal
Öðle: [yiyecek] - [kalori]kcal
Akþam: [yiyecek] - [kalori]kcal

CUMA
Kahvaltý: [yiyecek] - [kalori]kcal
Öðle: [yiyecek] - [kalori]kcal
Akþam: [yiyecek] - [kalori]kcal

CUMARTESÝ
Kahvaltý: [yiyecek] - [kalori]kcal
Öðle: [yiyecek] - [kalori]kcal
Akþam: [yiyecek] - [kalori]kcal

PAZAR
Kahvaltý: [yiyecek] - [kalori]kcal
Öðle: [yiyecek] - [kalori]kcal
Akþam: [yiyecek] - [kalori]kcal

?? SUPPLEMENT
• [takviye 1]
• [takviye 2]

?? SU: Günde en az 2.5 litre

?? ÖNEMLÝ NOTLAR
• [Not 1]
• [Not 2]

Türkçe yaz. Kýsa ve net ol. Gereksiz açýklama yapma!";
    }

    /// <summary>
    /// Vücut fotoðrafý analizi için prompt oluþturur
    /// </summary>
    public static string BuildBodyAnalysisPrompt(decimal height, decimal weight, Gender gender, string goal) {
        var genderText = gender == Gender.Female ? "Kadýn" : "Erkek";

        return $@"Bu fotoðraftaki kiþinin fiziksel durumunu analiz et.
Kiþi Bilgileri:
- Cinsiyet: {genderText}
- Boy: {height} cm
- Kilo: {weight} kg
- Hedef: {goal}

Lütfen þu bilgileri ver:
1. Vücut tipi analizi (ectomorph/mesomorph/endomorph)
2. Güncel fiziksel durum deðerlendirmesi
3. Hedefine ulaþmak için öneriler
4. Tahmini hedefe ulaþma süresi

Türkçe olarak detaylý bir analiz yap.";
    }

    /// <summary>
    /// Hedef vücut görseli için prompt oluþturur
    /// </summary>
    public static string BuildFutureBodyImagePrompt(Gender gender, string goal) {
        var goalLower = goal.ToLower();
        var genderText = gender == Gender.Female ? "30 year old female" : "30 year old male";

        if (goalLower.Contains("kas") || goalLower.Contains("muscle") || goalLower.Contains("bulk")) {
            return $"Generate only an image without any text response. A {genderText} lifting weights at the gym.";
        }

        if (goalLower.Contains("zayýfla") || goalLower.Contains("kilo ver") || goalLower.Contains("weight loss") || goalLower.Contains("diet")) {
            return $"Generate only an image without any text response. A {genderText} jogging in a park.";
        }

        return $"Generate only an image without any text response. A {genderText} stretching before workout.";
    }
}
