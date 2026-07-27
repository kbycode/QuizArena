/* ==========================================================================
   QuizArena — dil katmanı

   Tek dosya, dış bağımlılık yok. app.js ve admin.js'ten ÖNCE yüklenir; ikisi
   de `t()` fonksiyonunu kullanır.

   Varsayılan dil tarayıcıdan (`navigator.language`) okunur, kullanıcı
   değiştirirse `localStorage`'a yazılır. Seçim `Accept-Language` başlığıyla
   API'ye de gönderilir; böylece sunucudan gelen mesajlar arayüzle aynı dilde
   olur (bkz. app.js içindeki `api()`).

   HTML'deki sabit metinler `data-i18n` özniteliğiyle işaretlenir ve
   `applyStaticText()` tarafından doldurulur; JavaScript'te üretilen metinler
   doğrudan `t()` çağırır.
   ========================================================================== */

'use strict';

const LANGUAGE_STORAGE_KEY = 'quizarena.language';
const LANGUAGE_CHANGED_EVENT = 'quizarena:languagechange';
const SUPPORTED_LANGUAGES = ['tr', 'en'];

const TRANSLATIONS = {
  tr: {
    // --- Ortak -------------------------------------------------------------
    'common.save': 'Kaydet',
    'common.cancel': 'Vazgeç',
    'common.close': 'Kapat',
    'common.edit': 'Düzenle',
    'common.delete': 'Sil',
    'common.refresh': 'Yenile',
    'common.loading': 'Yükleniyor…',
    'common.waiting': 'Bekleyin…',
    'common.previous': 'Önceki',
    'common.next': 'Sonraki',
    'common.none': '—',
    'common.player': 'Oyuncu',
    'common.closed': 'Kapalı',
    'common.active': 'Aktif',
    'common.passive': 'Pasif',
    'common.locked': 'Kilitli',
    'common.points': 'puan',
    'common.questionsShort': 'soru',
    'common.playersShort': 'oyuncu',

    // --- Ağ / genel hata ---------------------------------------------------
    'api.networkError':
      'Sunucuya ulaşılamıyor. API çalışmıyor olabilir veya internet bağlantınız koptu. ' +
      'Lütfen birkaç saniye sonra tekrar deneyin.',
    'api.requestFailed': 'İstek başarısız ({status})',
    'auth.loginSuccess': 'Giriş başarılı.',
    'auth.registerSuccess': 'Kaydınız oluşturuldu.',
    'home.joinCodeLength': 'Katılım kodu {length} karakter olmalı.',
    'result.notFound': 'Yarışma sonucu bulunamadı.',
    'result.correct': 'Doğru',
    'result.wrong': 'Yanlış',
    'result.timedOut': 'Süre doldu',
    'result.duration': 'Süre',
    'result.secondsShort': 'sn',

    // --- Gezinme / üst çubuk -----------------------------------------------
    'nav.home': 'Ana sayfa',
    'nav.admin': 'Yönetim',
    'nav.apiDocs': 'API dokümantasyonu',
    'nav.logout': 'Çıkış',
    'nav.language': 'Dil',
    'brand.tagline': 'Çok oyunculu bilgi yarışması',

    // --- Kimlik ------------------------------------------------------------
    'auth.login': 'Giriş yap',
    'auth.register': 'Kayıt ol',
    'auth.registerAndStart': 'Kayıt ol ve başla',
    'auth.email': 'E-posta',
    'auth.password': 'Parola',
    'auth.firstName': 'Ad',
    'auth.lastName': 'Soyad',
    'auth.nickname': 'Takma ad',
    'auth.passwordHint': 'En az 8 karakter; büyük harf, küçük harf ve rakam içermeli.',
    'auth.loggedOut': 'Çıkış yapıldı.',

    // --- Ana sayfa ---------------------------------------------------------
    'home.pickCategory': 'Kategori seç',
    'home.pickCategoryHint': 'Bir kategori seçip tek başına ya da arkadaşlarınla yarış.',
    'home.gameSettings': 'Oyun ayarları',
    'home.questionCount': 'Soru sayısı',
    'home.secondsPerQuestion': 'Soru süresi',
    'home.questionOption': '{count} soru',
    'home.secondsOption': '{count} saniye',
    'home.roomMode': 'Oyun kipi',
    'home.modeSolo': 'Tek kişilik',
    'home.modePrivate': 'Kodla davet (özel)',
    'home.modePublic': 'Herkese açık oda',
    'home.start': 'Yarışmayı başlat',
    'home.joinCode': 'Katılım kodu',
    'home.joinRoom': 'Odaya katıl',
    'home.openRooms': 'Açık odalar',
    'home.myStats': 'İstatistiklerim',
    'home.leaderboard': '🏆 Sıralama',
    'home.myBadges': 'Rozetlerim',
    'home.upcomingEvents': '📅 Yaklaşan etkinlikler',
    'home.noCategories': 'Henüz kategori eklenmemiş.',
    'home.categoriesFailed': 'Kategoriler yüklenemedi: {error}',
    'home.noOpenRooms': 'Şu anda açık oda yok.',
    'home.roomsFailed': 'Odalar yüklenemedi.',
    'home.noLeaderboard': 'Henüz kimse oynamadı. İlk sen ol!',
    'home.leaderboardFailed': 'Sıralama yüklenemedi.',
    'home.statsFailed': 'İstatistikler yüklenemedi.',
    'home.noBadges': 'Henüz rozet kazanmadın.',
    'home.notEnoughQuestions': 'yetersiz',
    'home.pickCategoryFirst': 'Önce bir kategori seç.',

    // --- İstatistik etiketleri ---------------------------------------------
    'stats.competitions': 'Yarışma',
    'stats.score': 'Puan',
    'stats.accuracy': 'Doğruluk',
    'stats.bestStreak': 'En iyi seri',
    'stats.wins': 'Birincilik',
    'stats.averageTime': 'Ort. süre',

    // --- Lobi --------------------------------------------------------------
    'lobby.title': 'Oyuncular bekleniyor',
    'lobby.shareCode': 'Arkadaşların bu kodla odaya katılabilir:',
    'lobby.start': 'Yarışmayı başlat',
    'lobby.cancel': 'Odayı iptal et',
    'lobby.refreshHint': 'Oda durumu birkaç saniyede bir yenilenir.',
    'lobby.host': 'Kurucu',

    // --- Oyun --------------------------------------------------------------
    'game.questionProgress': 'Soru {order} / {total}',
    'game.liveScore': 'Canlı skor',
    'game.timeLeft': '{seconds} saniye',
    'game.correct': '✅ Doğru! +{points} puan',
    'game.wrong': '❌ Yanlış',
    'game.timedOut': '⏱ Süre doldu',
    'game.correctAnswerWas': 'Doğru cevap: {answer}',
    'game.base': 'Taban',
    'game.speed': 'Hız',
    'game.streak': 'Seri',
    'game.streakBadge': '🔥 {count} seri',

    // --- Zorluk ------------------------------------------------------------
    'difficulty.Easy': 'Kolay',
    'difficulty.Medium': 'Orta',
    'difficulty.Hard': 'Zor',

    // --- Sonuç -------------------------------------------------------------
    'result.title': 'Yarışma tamamlandı',
    'result.rank': '{rank}. oldun!',
    'result.longestStreak': 'En uzun seri',
    'result.newBadges': 'YENİ ROZETLER',
    'result.scoreboard': 'Skor tablosu',
    'result.backHome': 'Ana sayfaya dön',

    // --- Yönetim: genel ----------------------------------------------------
    'admin.title': 'Yönetim',
    'admin.timeRange': 'Zaman aralığı',
    'admin.noAccess': 'Bu bölüme erişim yetkiniz yok.',
    'admin.subtitleFull': 'Sistem özeti, soru havuzu, kategoriler, etkinlikler ve kullanıcı hesapları.',
    'admin.subtitlePartial': 'Yetkiniz dâhilinde: {areas}.',
    'admin.areaContent': 'soru ve kategoriler',
    'admin.areaEvents': 'etkinlikler',
    'admin.areaUsers': 'kullanıcı hesapları',
    'admin.tabDashboard': '📊 Pano',
    'admin.tabQuestions': '❓ Sorular',
    'admin.tabCategories': '🏷️ Kategoriler',
    'admin.tabEvents': '📅 Etkinlikler',
    'admin.tabUsers': '👥 Kullanıcılar',

    // --- Yönetim: pano -----------------------------------------------------
    'dash.days': '{count} gün',
    'dash.users': '👥 Kullanıcı',
    'dash.usersFoot': '{active} aktif',
    'dash.usersFootLocked': '{active} aktif · {locked} kilitli',
    'dash.newUsers': '➕ Yeni kayıt',
    'dash.newUsersFoot': 'son {days} gün',
    'dash.games': '🚩 Yarışma',
    'dash.gamesFoot': 'son {days} günde {count}',
    'dash.liveRooms': '⚡ Canlı oda',
    'dash.liveRoomsFoot': 'bekleyen + oynanan',
    'dash.questions': '❓ Soru',
    'dash.questionsFoot': '{total} kayıt · {categories} kategori',
    'dash.accuracy': '✅ Doğruluk',
    'dash.accuracyFoot': '{count} cevap üzerinden',
    'dash.activity': 'Günlük etkinlik',
    'dash.activityNote': 'tamamlanan yarışma / benzersiz oyuncu',
    'dash.activityEmpty': 'Bu aralıkta tamamlanmış yarışma yok.',
    'dash.barTooltip': '{date}: {count} yarışma',
    'dash.legendGames': 'Yarışma',
    'dash.legendPlayers': 'Benzersiz oyuncu',
    'dash.categoryAccuracy': 'Kategori doğruluk oranı',
    'dash.categoryAccuracyEmpty': 'Henüz cevaplanmış soru yok.',
    'dash.categoryBreakdown': 'Kategori dağılımı',
    'dash.colCategory': 'Kategori',
    'dash.colQuestions': 'Soru',
    'dash.colGames': 'Oyun',
    'dash.hardest': 'En çok yanılınan sorular',
    'dash.hardestNote': 'en az {count} kez soruldu',
    'dash.easiest': 'En kolay sorular',
    'dash.easiestNote': 'havuzu dengelemek için',
    'dash.notEnoughData': 'Yeterli veri toplanmadı.',
    'dash.generatedAt': 'Veri {time} itibarıyla (kısa süreli önbelleklenir).',
    'dash.a11yActivity': 'Günlük tamamlanan yarışma ve benzersiz oyuncu sayısı',
    'dash.a11yAccuracy': 'Kategori bazlı doğruluk oranı',

    // --- Yönetim: sorular --------------------------------------------------
    'q.allCategories': 'Tüm kategoriler',
    'q.timesAsked': '{count} kez',
    'q.count': '{count} soru',
    'q.new': 'Yeni soru',
    'q.empty': 'Bu filtreye uyan soru yok.',
    'q.colQuestion': 'Soru',
    'q.colCategory': 'Kategori',
    'q.colDifficulty': 'Zorluk',
    'q.colStats': 'İstatistik',
    'q.editTitle': 'Soruyu düzenle',
    'q.newTitle': 'Yeni soru',
    'q.category': 'Kategori',
    'q.text': 'Soru metni',
    'q.difficulty': 'Zorluk',
    'q.duration': 'Süre (sn)',
    'q.explanation': 'Açıklama (isteğe bağlı)',
    'q.options': 'Şıklar',
    'q.addOption': 'Şık ekle',
    'q.optionsHint': 'Tam olarak bir şık doğru işaretlenmelidir.',
    'q.optionPlaceholder': 'Şık {number}',
    'q.optionCorrectLabel': '{number}. şık doğru',
    'q.removeOption': 'Şıkkı kaldır',
    'q.textTooShort': 'Soru metni en az 10 karakter olmalıdır.',
    'q.optionTextRequired': 'Şık metinleri boş olamaz.',
    'q.oneCorrectRequired': 'Tam olarak bir şık doğru işaretlenmelidir.',
    'q.created': 'Soru eklendi.',
    'q.updated': 'Soru güncellendi.',
    'q.deleted': 'Soru kaldırıldı.',
    'q.deleteTitle': 'Soru silinsin mi?',
    'q.deleteMessage': '“{text}” kaldırılacak. Bu işlem geri alınamaz.',

    // --- Yönetim: kategoriler ----------------------------------------------
    'cat.count': '{count} kategori',
    'cat.new': 'Yeni kategori',
    'cat.colName': 'Ad',
    'cat.colQuestions': 'Soru',
    'cat.colStatus': 'Durum',
    'cat.editTitle': 'Kategoriyi düzenle',
    'cat.newTitle': 'Yeni kategori',
    'cat.name': 'Ad',
    'cat.description': 'Açıklama',
    'cat.icon': 'İkon (emoji)',
    'cat.color': 'Renk (#RRGGBB)',
    'cat.activeLabel': 'Aktif',
    'cat.nameTooShort': 'Kategori adı en az 2 karakter olmalıdır.',
    'cat.colorInvalid': 'Renk #RRGGBB biçiminde olmalıdır.',
    'cat.created': 'Kategori eklendi.',
    'cat.updated': 'Kategori güncellendi.',

    // --- Yönetim: etkinlikler ----------------------------------------------
    'ev.count': '{count} etkinlik',
    'ev.new': 'Yeni etkinlik',
    'ev.empty': 'Henüz etkinlik oluşturulmadı.',
    'ev.colEvent': 'Etkinlik',
    'ev.colCategory': 'Kategori',
    'ev.colStart': 'Başlangıç',
    'ev.colRegistered': 'Kayıt',
    'ev.colStatus': 'Durum',
    'ev.editTitle': 'Etkinliği düzenle',
    'ev.newTitle': 'Yeni etkinlik',
    'ev.name': 'Ad',
    'ev.description': 'Açıklama',
    'ev.category': 'Kategori',
    'ev.startLocal': 'Başlangıç (yerel saatiniz)',
    'ev.utcPreview': 'Sunucuya {value} (UTC) olarak gönderilecek.',
    'ev.utcHint': 'Sunucu tarafında UTC olarak saklanır.',
    'ev.questionCount': 'Soru sayısı',
    'ev.duration': 'Süre (sn)',
    'ev.capacity': 'Kontenjan',
    'ev.nameTooShort': 'Etkinlik adı en az 3 karakter olmalıdır.',
    'ev.startRequired': 'Başlangıç zamanı zorunludur.',
    'ev.startMustBeFuture': 'Başlangıç gelecekte bir zaman olmalıdır.',
    'ev.created': 'Etkinlik oluşturuldu.',
    'ev.updated': 'Etkinlik güncellendi.',
    'ev.cancelTitle': 'Etkinlik iptal edilsin mi?',
    'ev.cancelMessage': '“{name}” iptal edilecek.',
    'ev.cancelMessageWithPlayers': '“{name}” iptal edilecek. {count} kayıtlı oyuncu etkilenecek.',
    'ev.cancelAction': 'İptal et',
    'ev.cancelled': 'Etkinlik iptal edildi.',
    'ev.cancelShort': 'İptal',

    // --- Oda durumları -----------------------------------------------------
    'roomStatus.Waiting': 'Planlandı',
    'roomStatus.InProgress': 'Devam ediyor',
    'roomStatus.Finished': 'Tamamlandı',
    'roomStatus.Cancelled': 'İptal edildi',

    // --- Oyuncu tarafı: etkinlikler ----------------------------------------
    'pev.empty': 'Şu anda planlanmış etkinlik yok.',
    'pev.failed': 'Etkinlikler yüklenemedi.',
    'pev.register': 'Kaydol',
    'pev.withdraw': 'Vazgeç',
    'pev.registered': 'Kayıtlısınız',
    'pev.full': 'Kontenjan dolu',
    'pev.registeredToast': 'Etkinliğe kaydoldunuz.',
    'pev.withdrawnToast': 'Etkinlik kaydınız iptal edildi.',
    'pev.meta': '{date}, {time} · {registered}/{capacity} oyuncu · {questions} soru',

    // --- Yönetim: kullanıcılar ---------------------------------------------
    'usr.searchPlaceholder': 'Ad, takma ad veya e-posta',
    'usr.searchLabel': 'Kullanıcı ara',
    'usr.count': '{count} kullanıcı',
    'usr.empty': 'Aramaya uyan kullanıcı yok.',
    'usr.colUser': 'Kullanıcı',
    'usr.colEmail': 'E-posta',
    'usr.colRoles': 'Yetkiler',
    'usr.colStatus': 'Durum',
    'usr.colLastLogin': 'Son giriş',
    'usr.self': 'Bu sizsiniz',
    'usr.activate': 'Etkinleştir',
    'usr.deactivate': 'Pasifleştir',
    'usr.unlock': 'Kilidi aç',
    'usr.permissions': 'Yetkiler',
    'usr.lockTooltip': '{count} hatalı giriş',
    'usr.unlockedToast': '{name} yeniden giriş deneyebilir.',
    'usr.updatedToast': '{name} hesabı güncellendi.',
    'usr.activateTitle': 'Hesap etkinleştirilsin mi?',
    'usr.deactivateTitle': 'Hesap pasifleştirilsin mi?',
    'usr.activateMessage': '{name} yeniden giriş yapabilecek.',
    'usr.deactivateMessage':
      '{name} artık giriş yapamayacak. Mevcut oturumu, jetonu yenilenene kadar sürer.',
    'usr.claimsTitle': 'Yetkiler — {name}',
    'usr.claimsHint':
      'Değişiklik anında uygulanır ve kullanıcının bir sonraki jeton yenilemesinde etkili olur.',
    'usr.noClaims': 'Tanımlı yetki yok.',
    'usr.claimGranted': '{claim} verildi.',
    'usr.claimRevoked': '{claim} kaldırıldı.',

    // --- Yetki adları ------------------------------------------------------
    'role.Admin': 'Yönetici',
    'role.Category.Manage': 'Kategori yönetimi',
    'role.Question.Manage': 'Soru yönetimi',
    'role.User.Manage': 'Kullanıcı yönetimi',
    'role.Event.Manage': 'Etkinlik yönetimi'
  },

  en: {
    // --- Common ------------------------------------------------------------
    'common.save': 'Save',
    'common.cancel': 'Cancel',
    'common.close': 'Close',
    'common.edit': 'Edit',
    'common.delete': 'Delete',
    'common.refresh': 'Refresh',
    'common.loading': 'Loading…',
    'common.waiting': 'Please wait…',
    'common.previous': 'Previous',
    'common.next': 'Next',
    'common.none': '—',
    'common.player': 'Player',
    'common.closed': 'Disabled',
    'common.active': 'Active',
    'common.passive': 'Inactive',
    'common.locked': 'Locked',
    'common.points': 'points',
    'common.questionsShort': 'questions',
    'common.playersShort': 'players',

    // --- Network / generic errors ------------------------------------------
    'api.networkError':
      'Cannot reach the server. The API may be down, or your connection dropped. ' +
      'Please try again in a few seconds.',
    'api.requestFailed': 'Request failed ({status})',
    'auth.loginSuccess': 'Signed in.',
    'auth.registerSuccess': 'Your account is ready.',
    'home.joinCodeLength': 'The join code must be {length} characters.',
    'result.notFound': 'Could not find the result.',
    'result.correct': 'Correct',
    'result.wrong': 'Wrong',
    'result.timedOut': 'Timed out',
    'result.duration': 'Duration',
    'result.secondsShort': 's',

    // --- Navigation --------------------------------------------------------
    'nav.home': 'Home',
    'nav.admin': 'Admin',
    'nav.apiDocs': 'API docs',
    'nav.logout': 'Sign out',
    'nav.language': 'Language',
    'brand.tagline': 'Multiplayer trivia',

    // --- Auth --------------------------------------------------------------
    'auth.login': 'Sign in',
    'auth.register': 'Sign up',
    'auth.registerAndStart': 'Sign up and play',
    'auth.email': 'Email',
    'auth.password': 'Password',
    'auth.firstName': 'First name',
    'auth.lastName': 'Last name',
    'auth.nickname': 'Nickname',
    'auth.passwordHint': 'At least 8 characters, with an uppercase letter, a lowercase letter and a digit.',
    'auth.loggedOut': 'Signed out.',

    // --- Home --------------------------------------------------------------
    'home.pickCategory': 'Pick a category',
    'home.pickCategoryHint': 'Choose a category and play solo or against friends.',
    'home.gameSettings': 'Game settings',
    'home.questionCount': 'Questions',
    'home.secondsPerQuestion': 'Time per question',
    'home.questionOption': '{count} questions',
    'home.secondsOption': '{count} seconds',
    'home.roomMode': 'Mode',
    'home.modeSolo': 'Solo',
    'home.modePrivate': 'Invite by code (private)',
    'home.modePublic': 'Public room',
    'home.start': 'Start game',
    'home.joinCode': 'Join code',
    'home.joinRoom': 'Join room',
    'home.openRooms': 'Open rooms',
    'home.myStats': 'My stats',
    'home.leaderboard': '🏆 Leaderboard',
    'home.myBadges': 'My badges',
    'home.upcomingEvents': '📅 Upcoming events',
    'home.noCategories': 'No categories yet.',
    'home.categoriesFailed': 'Could not load categories: {error}',
    'home.noOpenRooms': 'No open rooms right now.',
    'home.roomsFailed': 'Could not load rooms.',
    'home.noLeaderboard': 'Nobody has played yet. Be the first!',
    'home.leaderboardFailed': 'Could not load the leaderboard.',
    'home.statsFailed': 'Could not load your stats.',
    'home.noBadges': 'No badges yet.',
    'home.notEnoughQuestions': 'not enough',
    'home.pickCategoryFirst': 'Pick a category first.',

    // --- Stat labels -------------------------------------------------------
    'stats.competitions': 'Games',
    'stats.score': 'Score',
    'stats.accuracy': 'Accuracy',
    'stats.bestStreak': 'Best streak',
    'stats.wins': 'Wins',
    'stats.averageTime': 'Avg. time',

    // --- Lobby -------------------------------------------------------------
    'lobby.title': 'Waiting for players',
    'lobby.shareCode': 'Your friends can join with this code:',
    'lobby.start': 'Start game',
    'lobby.cancel': 'Cancel room',
    'lobby.refreshHint': 'The room refreshes every few seconds.',
    'lobby.host': 'Host',

    // --- Game --------------------------------------------------------------
    'game.questionProgress': 'Question {order} of {total}',
    'game.liveScore': 'Live score',
    'game.timeLeft': '{seconds} seconds',
    'game.correct': '✅ Correct! +{points} points',
    'game.wrong': '❌ Wrong',
    'game.timedOut': '⏱ Time is up',
    'game.correctAnswerWas': 'Correct answer: {answer}',
    'game.base': 'Base',
    'game.speed': 'Speed',
    'game.streak': 'Streak',
    'game.streakBadge': '🔥 {count} streak',

    // --- Difficulty --------------------------------------------------------
    'difficulty.Easy': 'Easy',
    'difficulty.Medium': 'Medium',
    'difficulty.Hard': 'Hard',

    // --- Result ------------------------------------------------------------
    'result.title': 'Game complete',
    'result.rank': 'You finished #{rank}!',
    'result.longestStreak': 'Longest streak',
    'result.newBadges': 'NEW BADGES',
    'result.scoreboard': 'Scoreboard',
    'result.backHome': 'Back to home',

    // --- Admin: general ----------------------------------------------------
    'admin.title': 'Admin',
    'admin.timeRange': 'Time range',
    'admin.noAccess': 'You do not have access to this section.',
    'admin.subtitleFull': 'System overview, question pool, categories, events and user accounts.',
    'admin.subtitlePartial': 'Within your permissions: {areas}.',
    'admin.areaContent': 'questions and categories',
    'admin.areaEvents': 'events',
    'admin.areaUsers': 'user accounts',
    'admin.tabDashboard': '📊 Dashboard',
    'admin.tabQuestions': '❓ Questions',
    'admin.tabCategories': '🏷️ Categories',
    'admin.tabEvents': '📅 Events',
    'admin.tabUsers': '👥 Users',

    // --- Admin: dashboard --------------------------------------------------
    'dash.days': '{count} days',
    'dash.users': '👥 Users',
    'dash.usersFoot': '{active} active',
    'dash.usersFootLocked': '{active} active · {locked} locked',
    'dash.newUsers': '➕ New sign-ups',
    'dash.newUsersFoot': 'last {days} days',
    'dash.games': '🚩 Games',
    'dash.gamesFoot': '{count} in the last {days} days',
    'dash.liveRooms': '⚡ Live rooms',
    'dash.liveRoomsFoot': 'waiting + playing',
    'dash.questions': '❓ Questions',
    'dash.questionsFoot': '{total} total · {categories} categories',
    'dash.accuracy': '✅ Accuracy',
    'dash.accuracyFoot': 'across {count} answers',
    'dash.activity': 'Daily activity',
    'dash.activityNote': 'completed games / unique players',
    'dash.activityEmpty': 'No completed games in this range.',
    'dash.barTooltip': '{date}: {count} games',
    'dash.legendGames': 'Games',
    'dash.legendPlayers': 'Unique players',
    'dash.categoryAccuracy': 'Accuracy by category',
    'dash.categoryAccuracyEmpty': 'No answered questions yet.',
    'dash.categoryBreakdown': 'Category breakdown',
    'dash.colCategory': 'Category',
    'dash.colQuestions': 'Questions',
    'dash.colGames': 'Games',
    'dash.hardest': 'Most-missed questions',
    'dash.hardestNote': 'asked at least {count} times',
    'dash.easiest': 'Easiest questions',
    'dash.easiestNote': 'to balance the pool',
    'dash.notEnoughData': 'Not enough data yet.',
    'dash.generatedAt': 'Data as of {time} (briefly cached).',
    'dash.a11yActivity': 'Completed games and unique players per day',
    'dash.a11yAccuracy': 'Accuracy by category',

    // --- Admin: questions --------------------------------------------------
    'q.allCategories': 'All categories',
    'q.timesAsked': 'asked {count}×',
    'q.count': '{count} questions',
    'q.new': 'New question',
    'q.empty': 'No questions match this filter.',
    'q.colQuestion': 'Question',
    'q.colCategory': 'Category',
    'q.colDifficulty': 'Difficulty',
    'q.colStats': 'Stats',
    'q.editTitle': 'Edit question',
    'q.newTitle': 'New question',
    'q.category': 'Category',
    'q.text': 'Question text',
    'q.difficulty': 'Difficulty',
    'q.duration': 'Time (s)',
    'q.explanation': 'Explanation (optional)',
    'q.options': 'Options',
    'q.addOption': 'Add option',
    'q.optionsHint': 'Exactly one option must be marked correct.',
    'q.optionPlaceholder': 'Option {number}',
    'q.optionCorrectLabel': 'Option {number} is correct',
    'q.removeOption': 'Remove option',
    'q.textTooShort': 'Question text must be at least 10 characters.',
    'q.optionTextRequired': 'Option text cannot be empty.',
    'q.oneCorrectRequired': 'Exactly one option must be marked correct.',
    'q.created': 'Question added.',
    'q.updated': 'Question updated.',
    'q.deleted': 'Question removed.',
    'q.deleteTitle': 'Delete this question?',
    'q.deleteMessage': '“{text}” will be removed. This cannot be undone.',

    // --- Admin: categories -------------------------------------------------
    'cat.count': '{count} categories',
    'cat.new': 'New category',
    'cat.colName': 'Name',
    'cat.colQuestions': 'Questions',
    'cat.colStatus': 'Status',
    'cat.editTitle': 'Edit category',
    'cat.newTitle': 'New category',
    'cat.name': 'Name',
    'cat.description': 'Description',
    'cat.icon': 'Icon (emoji)',
    'cat.color': 'Colour (#RRGGBB)',
    'cat.activeLabel': 'Active',
    'cat.nameTooShort': 'Category name must be at least 2 characters.',
    'cat.colorInvalid': 'Colour must be in #RRGGBB format.',
    'cat.created': 'Category added.',
    'cat.updated': 'Category updated.',

    // --- Admin: events -----------------------------------------------------
    'ev.count': '{count} events',
    'ev.new': 'New event',
    'ev.empty': 'No events created yet.',
    'ev.colEvent': 'Event',
    'ev.colCategory': 'Category',
    'ev.colStart': 'Starts',
    'ev.colRegistered': 'Signed up',
    'ev.colStatus': 'Status',
    'ev.editTitle': 'Edit event',
    'ev.newTitle': 'New event',
    'ev.name': 'Name',
    'ev.description': 'Description',
    'ev.category': 'Category',
    'ev.startLocal': 'Start (your local time)',
    'ev.utcPreview': 'Will be sent to the server as {value} (UTC).',
    'ev.utcHint': 'Stored as UTC on the server.',
    'ev.questionCount': 'Questions',
    'ev.duration': 'Time (s)',
    'ev.capacity': 'Capacity',
    'ev.nameTooShort': 'Event name must be at least 3 characters.',
    'ev.startRequired': 'Start time is required.',
    'ev.startMustBeFuture': 'The start must be in the future.',
    'ev.created': 'Event created.',
    'ev.updated': 'Event updated.',
    'ev.cancelTitle': 'Cancel this event?',
    'ev.cancelMessage': '“{name}” will be cancelled.',
    'ev.cancelMessageWithPlayers': '“{name}” will be cancelled. {count} registered players are affected.',
    'ev.cancelAction': 'Cancel event',
    'ev.cancelled': 'Event cancelled.',
    'ev.cancelShort': 'Cancel',

    // --- Room status -------------------------------------------------------
    'roomStatus.Waiting': 'Scheduled',
    'roomStatus.InProgress': 'In progress',
    'roomStatus.Finished': 'Finished',
    'roomStatus.Cancelled': 'Cancelled',

    // --- Player-facing events ----------------------------------------------
    'pev.empty': 'No events scheduled right now.',
    'pev.failed': 'Could not load events.',
    'pev.register': 'Sign up',
    'pev.withdraw': 'Withdraw',
    'pev.registered': 'You are in',
    'pev.full': 'Full',
    'pev.registeredToast': 'You are registered for the event.',
    'pev.withdrawnToast': 'Your registration has been cancelled.',
    'pev.meta': '{date}, {time} · {registered}/{capacity} players · {questions} questions',

    // --- Admin: users ------------------------------------------------------
    'usr.searchPlaceholder': 'Name, nickname or email',
    'usr.searchLabel': 'Search users',
    'usr.count': '{count} users',
    'usr.empty': 'No users match your search.',
    'usr.colUser': 'User',
    'usr.colEmail': 'Email',
    'usr.colRoles': 'Permissions',
    'usr.colStatus': 'Status',
    'usr.colLastLogin': 'Last sign-in',
    'usr.self': 'This is you',
    'usr.activate': 'Activate',
    'usr.deactivate': 'Deactivate',
    'usr.unlock': 'Unlock',
    'usr.permissions': 'Permissions',
    'usr.lockTooltip': '{count} failed sign-ins',
    'usr.unlockedToast': '{name} can try signing in again.',
    'usr.updatedToast': '{name} account updated.',
    'usr.activateTitle': 'Activate this account?',
    'usr.deactivateTitle': 'Deactivate this account?',
    'usr.activateMessage': '{name} will be able to sign in again.',
    'usr.deactivateMessage':
      '{name} will no longer be able to sign in. Their current session lasts until its token is refreshed.',
    'usr.claimsTitle': 'Permissions — {name}',
    'usr.claimsHint':
      'Changes apply immediately and take effect at the user’s next token refresh.',
    'usr.noClaims': 'No permissions defined.',
    'usr.claimGranted': '{claim} granted.',
    'usr.claimRevoked': '{claim} revoked.',

    // --- Permission names --------------------------------------------------
    'role.Admin': 'Administrator',
    'role.Category.Manage': 'Category management',
    'role.Question.Manage': 'Question management',
    'role.User.Manage': 'User management',
    'role.Event.Manage': 'Event management'
  }
};

/**
 * Başlangıç dili.
 *
 * Sıra: kullanıcının önceki seçimi → cihaz dili → Türkçe. Cihaz dili
 * `navigator.language` üzerinden okunur ("en-GB" gibi bölgeli değerler de
 * "en" ile eşleşir).
 */
function detectLanguage() {
  const saved = localStorage.getItem(LANGUAGE_STORAGE_KEY);
  if (SUPPORTED_LANGUAGES.includes(saved)) {
    return saved;
  }

  const device = (navigator.language || '').slice(0, 2).toLowerCase();
  return SUPPORTED_LANGUAGES.includes(device) ? device : 'tr';
}

let currentLanguage = detectLanguage();

const getLanguage = () => currentLanguage;

/**
 * Çeviriyi verir; `{ad}` yer tutucularını doldurur.
 *
 * Anahtar bulunamazsa anahtarın kendisi döner — boş metin göstermektense
 * eksikliği görünür kılmak yeğdir.
 */
function t(key, params) {
  const table = TRANSLATIONS[currentLanguage] ?? TRANSLATIONS.tr;
  let text = table[key] ?? TRANSLATIONS.tr[key] ?? key;

  if (params) {
    for (const [name, value] of Object.entries(params)) {
      text = text.replaceAll(`{${name}}`, String(value));
    }
  }

  return text;
}

/**
 * HTML'deki sabit metinleri doldurur.
 *
 * `data-i18n` metni, `data-i18n-placeholder` ve `data-i18n-label` ise ilgili
 * öznitelikleri günceller.
 */
function applyStaticText(root = document) {
  root.querySelectorAll('[data-i18n]').forEach((element) => {
    element.textContent = t(element.dataset.i18n);
  });

  root.querySelectorAll('[data-i18n-placeholder]').forEach((element) => {
    element.placeholder = t(element.dataset.i18nPlaceholder);
  });

  root.querySelectorAll('[data-i18n-label]').forEach((element) => {
    element.setAttribute('aria-label', t(element.dataset.i18nLabel));
  });

  document.documentElement.lang = currentLanguage;
}

/** Dili değiştirir ve arayüzü yeniden çizer. */
function setLanguage(language) {
  if (!SUPPORTED_LANGUAGES.includes(language) || language === currentLanguage) {
    return;
  }

  currentLanguage = language;
  localStorage.setItem(LANGUAGE_STORAGE_KEY, language);

  applyStaticText();

  // Olay adı bilinçli olarak ön ekli: tarayıcının kendi `languagechange`
  // olayı window üzerinde tetikleniyor, ikisi karışmasın.
  document.dispatchEvent(new CustomEvent(LANGUAGE_CHANGED_EVENT));
}

/** Sayı ve tarih biçimleri de dile uysun diye. */
const locale = () => (currentLanguage === 'en' ? 'en-GB' : 'tr-TR');

/** Yüzde işareti Türkçede sayının önüne, İngilizcede arkasına gelir. */
const percent = (value) => (currentLanguage === 'en' ? `${value}%` : `%${value}`);
