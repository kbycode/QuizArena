using QuizArena.Entities.Enums;

namespace QuizArena.DAL.Seed;

/// <summary>Seed sırasında kullanılan kategori tanımı.</summary>
internal sealed record SeedCategory(
    string Name,
    string Slug,
    string Description,
    string Icon,
    string ColorHex,
    int DisplayOrder,
    SeedQuestion[] Questions);

/// <summary>
/// Seed sırasında kullanılan soru tanımı.
/// <paramref name="CorrectIndex"/>, <paramref name="Options"/> içindeki doğru
/// şıkkın sırasıdır (0 tabanlı).
/// </summary>
internal sealed record SeedQuestion(
    string Text,
    QuestionDifficulty Difficulty,
    string? Explanation,
    string[] Options,
    int CorrectIndex);

/// <summary>
/// Başlangıç içeriği: kategoriler ve soru havuzu.
/// </summary>
/// <remarks>
/// Boş bir veritabanıyla açılan uygulama oynanamaz olurdu; bu içerik sayesinde
/// proje <c>dotnet run</c> sonrası ilk saniyeden itibaren denenebilir durumda.
/// Veriler koda gömülü çünkü uygulamanın <b>çalışması için gereken</b>
/// referans veridir (test verisi değil); ayrı bir dosya/servis bağımlılığı
/// getirmemesi tercih edildi.
/// </remarks>
internal static class SeedContent
{
    internal static SeedCategory[] Categories =>
    [
        new("Genel Kültür", "genel-kultur",
            "Her konudan biraz: coğrafyadan bilime, günlük hayattan tarihe.",
            "🧠", "#6366F1", 1,
            [
                new("Türkiye'nin nüfusu en fazla olan şehri hangisidir?",
                    QuestionDifficulty.Easy, null,
                    ["İstanbul", "Ankara", "İzmir", "Bursa"], 0),

                new("Bir futbol maçının normal süresi kaç dakikadır?",
                    QuestionDifficulty.Easy, "İki devre hâlinde 45+45 dakika oynanır.",
                    ["60", "80", "90", "120"], 2),

                new("Türk alfabesinde kaç harf bulunur?",
                    QuestionDifficulty.Easy,
                    "1928'de kabul edilen Latin esaslı Türk alfabesi 29 harften oluşur.",
                    ["26", "28", "29", "31"], 2),

                new("Güneş sistemindeki en büyük gezegen hangisidir?",
                    QuestionDifficulty.Easy, null,
                    ["Satürn", "Jüpiter", "Neptün", "Dünya"], 1),

                new("Suyun kimyasal formülü nedir?",
                    QuestionDifficulty.Easy, "İki hidrojen, bir oksijen atomu.",
                    ["CO₂", "H₂O", "O₂", "NaCl"], 1),

                new("Türkiye Cumhuriyeti hangi yıl kurulmuştur?",
                    QuestionDifficulty.Easy, "29 Ekim 1923.",
                    ["1919", "1920", "1922", "1923"], 3),

                new("Bir yılda 31 günü olan kaç ay vardır?",
                    QuestionDifficulty.Medium,
                    "Ocak, Mart, Mayıs, Temmuz, Ağustos, Ekim ve Aralık.",
                    ["5", "6", "7", "8"], 2),

                new("Dünyanın en büyük okyanusu hangisidir?",
                    QuestionDifficulty.Medium, null,
                    ["Atlas Okyanusu", "Hint Okyanusu", "Büyük Okyanus (Pasifik)", "Arktik Okyanusu"], 2)
            ]),

        new("Tarih", "tarih",
            "Osmanlı'dan Cumhuriyet'e, dünya tarihinden dönüm noktaları.",
            "🏛️", "#B45309", 2,
            [
                new("İstanbul hangi yıl fethedilmiştir?",
                    QuestionDifficulty.Easy, "29 Mayıs 1453, Fatih Sultan Mehmet döneminde.",
                    ["1071", "1299", "1453", "1517"], 2),

                new("Osmanlı Devleti'nin kurucusu kimdir?",
                    QuestionDifficulty.Easy, null,
                    ["Ertuğrul Gazi", "Osman Bey", "Orhan Gazi", "I. Murat"], 1),

                new("Malazgirt Savaşı hangi yıl yapılmıştır?",
                    QuestionDifficulty.Medium,
                    "1071'de Sultan Alparslan, Bizans ordusunu yenmiştir.",
                    ["1048", "1071", "1176", "1243"], 1),

                new("Türkiye Büyük Millet Meclisi hangi yıl açılmıştır?",
                    QuestionDifficulty.Medium, "23 Nisan 1920.",
                    ["1919", "1920", "1921", "1923"], 1),

                new("Birinci Dünya Savaşı hangi yıl başlamıştır?",
                    QuestionDifficulty.Easy, null,
                    ["1912", "1914", "1918", "1939"], 1),

                new("Lozan Antlaşması hangi yıl imzalanmıştır?",
                    QuestionDifficulty.Medium, "24 Temmuz 1923.",
                    ["1920", "1921", "1922", "1923"], 3),

                new("Fransız Devrimi hangi yıl başlamıştır?",
                    QuestionDifficulty.Medium, null,
                    ["1776", "1789", "1804", "1848"], 1),

                new("\"Hattı müdafaa yoktur, sathı müdafaa vardır\" sözü kime aittir?",
                    QuestionDifficulty.Hard,
                    "Mustafa Kemal'in Sakarya Savaşı sırasında verdiği emirde yer alır.",
                    ["İsmet İnönü", "Mustafa Kemal Atatürk", "Kâzım Karabekir", "Fevzi Çakmak"], 1)
            ]),

        new("Coğrafya", "cografya",
            "Türkiye ve dünya coğrafyası: dağlar, nehirler, başkentler.",
            "🌍", "#059669", 3,
            [
                new("Türkiye'nin en uzun nehri hangisidir?",
                    QuestionDifficulty.Medium, "Kızılırmak, yaklaşık 1.355 km ile en uzun nehirdir.",
                    ["Fırat", "Sakarya", "Kızılırmak", "Yeşilırmak"], 2),

                new("Türkiye'nin en yüksek dağı hangisidir?",
                    QuestionDifficulty.Easy, "Ağrı Dağı, 5.137 metre.",
                    ["Erciyes Dağı", "Ağrı Dağı", "Kaçkar Dağı", "Uludağ"], 1),

                new("Türkiye'nin en büyük gölü hangisidir?",
                    QuestionDifficulty.Easy, null,
                    ["Tuz Gölü", "Beyşehir Gölü", "Van Gölü", "İznik Gölü"], 2),

                new("Japonya'nın başkenti neresidir?",
                    QuestionDifficulty.Easy, null,
                    ["Osaka", "Kyoto", "Tokyo", "Nagoya"], 2),

                new("Afrika kıtasının en uzun nehri hangisidir?",
                    QuestionDifficulty.Medium, null,
                    ["Kongo", "Nil", "Nijer", "Zambezi"], 1),

                new("Türkiye kaç ilden oluşur?",
                    QuestionDifficulty.Easy, null,
                    ["79", "80", "81", "82"], 2),

                new("Aşağıdaki ülkelerden hangisinin Karadeniz'e kıyısı YOKTUR?",
                    QuestionDifficulty.Hard,
                    "Karadeniz'e kıyısı olan ülkeler: Türkiye, Bulgaristan, Romanya, Ukrayna, Rusya ve Gürcistan.",
                    ["Gürcistan", "Bulgaristan", "Romanya", "Sırbistan"], 3),

                new("Ekvator aşağıdaki kıtalardan hangisinden geçmez?",
                    QuestionDifficulty.Medium, null,
                    ["Afrika", "Güney Amerika", "Asya", "Avrupa"], 3)
            ]),

        new("Bilim ve Teknoloji", "bilim-teknoloji",
            "Fizik, kimya, biyoloji ve bilgisayar dünyasından sorular.",
            "🔬", "#0891B2", 4,
            [
                new("Deniz seviyesinde suyun kaynama noktası kaç santigrat derecedir?",
                    QuestionDifficulty.Easy, null,
                    ["90", "95", "100", "110"], 2),

                new("İnsan vücudundaki en büyük organ hangisidir?",
                    QuestionDifficulty.Medium,
                    "Deri, yaklaşık 2 m² yüzey alanıyla en büyük organdır.",
                    ["Karaciğer", "Deri", "Akciğer", "Beyin"], 1),

                new("Periyodik tabloda \"O\" simgesiyle gösterilen element hangisidir?",
                    QuestionDifficulty.Easy, null,
                    ["Altın", "Osmiyum", "Oksijen", "Azot"], 2),

                new("Bilgisayarda 1 byte kaç bitten oluşur?",
                    QuestionDifficulty.Easy, null,
                    ["4", "8", "16", "32"], 1),

                new("Işığın boşluktaki hızı yaklaşık kaç km/saniyedir?",
                    QuestionDifficulty.Medium, "Yaklaşık 299.792 km/s.",
                    ["3.000", "30.000", "300.000", "3.000.000"], 2),

                new("Kalıtım bilgisini taşıyan molekül hangisidir?",
                    QuestionDifficulty.Easy, null,
                    ["ATP", "DNA", "Glikoz", "Hemoglobin"], 1),

                new("Yer çekimi kanununu formüle eden bilim insanı kimdir?",
                    QuestionDifficulty.Medium, null,
                    ["Galileo Galilei", "Isaac Newton", "Albert Einstein", "Nikola Tesla"], 1),

                new("HTTP protokolünün varsayılan port numarası kaçtır?",
                    QuestionDifficulty.Hard, "HTTP 80, HTTPS ise 443 numaralı portu kullanır.",
                    ["21", "25", "80", "443"], 2)
            ]),

        new("Sanat ve Edebiyat", "sanat-edebiyat",
            "Roman, şiir, resim ve müzik dünyasından sorular.",
            "🎭", "#DB2777", 5,
            [
                new("\"Çalıkuşu\" romanının yazarı kimdir?",
                    QuestionDifficulty.Medium, null,
                    ["Reşat Nuri Güntekin", "Yakup Kadri Karaosmanoğlu", "Halit Ziya Uşaklıgil", "Peyami Safa"], 0),

                new("İstiklal Marşı'nın şairi kimdir?",
                    QuestionDifficulty.Easy, null,
                    ["Namık Kemal", "Mehmet Akif Ersoy", "Tevfik Fikret", "Yahya Kemal Beyatlı"], 1),

                new("\"Mona Lisa\" tablosunun ressamı kimdir?",
                    QuestionDifficulty.Easy, null,
                    ["Michelangelo", "Raffaello", "Leonardo da Vinci", "Vincent van Gogh"], 2),

                new("Nobel Edebiyat Ödülü'nü kazanan ilk Türk yazar kimdir?",
                    QuestionDifficulty.Medium, "Orhan Pamuk, 2006 yılında ödüle değer görüldü.",
                    ["Yaşar Kemal", "Nâzım Hikmet", "Orhan Pamuk", "Aziz Nesin"], 2),

                new("\"Kürk Mantolu Madonna\" hangi yazarın eseridir?",
                    QuestionDifficulty.Medium, null,
                    ["Sabahattin Ali", "Sait Faik Abasıyanık", "Ahmet Hamdi Tanpınar", "Oğuz Atay"], 0),

                new("\"Sinekli Bakkal\" romanının yazarı kimdir?",
                    QuestionDifficulty.Hard, null,
                    ["Halide Edib Adıvar", "Şükufe Nihal", "Suat Derviş", "Adalet Ağaoğlu"], 0),

                new("Ludwig van Beethoven hangi alanda ünlüdür?",
                    QuestionDifficulty.Easy, null,
                    ["Resim", "Müzik", "Heykel", "Mimarlık"], 1),

                new("\"Tutunamayanlar\" romanının yazarı kimdir?",
                    QuestionDifficulty.Hard, null,
                    ["Yusuf Atılgan", "Oğuz Atay", "Bilge Karasu", "Vüs'at O. Bener"], 1)
            ]),

        new("Spor", "spor",
            "Futbol, basketbol, olimpiyatlar ve daha fazlası.",
            "⚽", "#EA580C", 6,
            [
                new("Bir basketbol takımında sahada kaç oyuncu bulunur?",
                    QuestionDifficulty.Easy, null,
                    ["4", "5", "6", "7"], 1),

                new("Yaz Olimpiyat Oyunları kaç yılda bir düzenlenir?",
                    QuestionDifficulty.Easy, null,
                    ["2", "3", "4", "5"], 2),

                new("FIFA Dünya Kupası'nı en çok kazanan ülke hangisidir?",
                    QuestionDifficulty.Medium, "Brezilya 5 kez şampiyon oldu.",
                    ["Almanya", "İtalya", "Arjantin", "Brezilya"], 3),

                new("Voleybolda bir takımda sahada kaç oyuncu bulunur?",
                    QuestionDifficulty.Easy, null,
                    ["5", "6", "7", "8"], 1),

                new("Türkiye, 2002 FIFA Dünya Kupası'nı kaçıncı olarak tamamladı?",
                    QuestionDifficulty.Medium, null,
                    ["Birinci", "İkinci", "Üçüncü", "Dördüncü"], 2),

                new("Aşağıdakilerden hangisi bir Grand Slam tenis turnuvası DEĞİLDİR?",
                    QuestionDifficulty.Hard,
                    "Grand Slam turnuvaları: Avustralya Açık, Roland Garros, Wimbledon ve ABD Açık.",
                    ["Wimbledon", "Roland Garros", "ABD Açık", "Monte Carlo Masters"], 3),

                new("Satranç oyununun başında her oyuncunun kaç taşı vardır?",
                    QuestionDifficulty.Medium, "8 piyon, 2 kale, 2 at, 2 fil, 1 şah, 1 vezir.",
                    ["12", "14", "16", "18"], 2),

                new("Formula 1'de bir yarışı kazanan pilota kaç puan verilir?",
                    QuestionDifficulty.Hard, null,
                    ["10", "18", "25", "30"], 2)
            ])
    ];
}
