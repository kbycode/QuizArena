using QuizArena.BLL.Abstract;
using QuizArena.BLL.Constants;
using QuizArena.BLL.Mapping;
using QuizArena.BLL.Utilities;
using QuizArena.BLL.Validation;
using QuizArena.Core.Aspects.Authorization;
using QuizArena.Core.Aspects.Transaction;
using QuizArena.Core.Aspects.Validation;
using QuizArena.Core.DataAccess.Paging;
using QuizArena.Core.Exceptions;
using QuizArena.Core.Utilities.Clock;
using QuizArena.Core.Utilities.Results;
using QuizArena.Core.Utilities.Security;
using QuizArena.DAL.Abstract;
using QuizArena.Entities.Concrete;
using QuizArena.Entities.Dtos.Common;
using QuizArena.Entities.Dtos.Rooms;
using QuizArena.Entities.Enums;
using Microsoft.Extensions.Logging;

namespace QuizArena.BLL.Concrete;

/// <summary>
/// Zamanlanmış etkinlikler (turnuvalar).
/// </summary>
/// <remarks>
/// <para>
/// <b>Etkinlik = zamanlanmış oda.</b> Ayrı bir varlık üretmek yerine
/// <see cref="Room"/> iki alanla genişletildi (<c>IsOfficialEvent</c>,
/// <c>ScheduledStartUtc</c>). Kazanç doğrudan: soru seçimi, katılımcı
/// yönetimi, skor tablosu, SignalR yayını ve yarışma motoru <b>olduğu gibi</b>
/// yeniden kullanılıyor. Ayrı bir <c>Event</c> varlığı, bunların hepsinin
/// ikinci bir kopyasını ve zamanla ayrışacak iki ayrı kural setini
/// gerektirirdi.
/// </para>
/// <para>
/// <b>Kurucu oynamak zorunda değil.</b> Etkinliği oluşturan yönetici odanın
/// <c>HostUserId</c>'si olur ama katılımcı olarak eklenmez: turnuvayı
/// düzenlemek ile turnuvaya girmek farklı şeyler. Bu yüzden başlangıç anında
/// hiç kayıt yoksa etkinlik başlatılmaz, iptal edilir.
/// </para>
/// </remarks>
public sealed class EventManager : IEventService
{
    /// <summary>Oyuncuya gösterilecek yaklaşan etkinlik sayısı üst sınırı.</summary>
    private const int UpcomingTake = 20;

    private readonly IRoomRepository _roomRepository;
    private readonly IRoomParticipantRepository _participantRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IRoomService _roomService;
    private readonly ICurrentUserService _currentUser;
    private readonly IClock _clock;
    private readonly ILogger<EventManager> _logger;

    public EventManager(
        IRoomRepository roomRepository,
        IRoomParticipantRepository participantRepository,
        ICategoryRepository categoryRepository,
        IQuestionRepository questionRepository,
        IRoomService roomService,
        ICurrentUserService currentUser,
        IClock clock,
        ILogger<EventManager> logger)
    {
        _roomRepository = roomRepository;
        _participantRepository = participantRepository;
        _categoryRepository = categoryRepository;
        _questionRepository = questionRepository;
        _roomService = roomService;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    // =====================================================================
    //  Okuma
    // =====================================================================

    public async Task<IDataResult<IReadOnlyList<EventResponse>>> GetUpcomingAsync(
        CancellationToken cancellationToken = default)
    {
        DateTime now = _clock.UtcNow;
        Guid? userId = _currentUser.UserId;

        IReadOnlyList<Room> events =
            await _roomRepository.GetUpcomingEventsAsync(now, UpcomingTake, cancellationToken);

        IReadOnlyList<EventResponse> response = events
            .Select(room => room.ToEventResponse(userId, now))
            .ToArray();

        return new SuccessDataResult<IReadOnlyList<EventResponse>>(response);
    }

    [SecuredOperationAspect(Roles.Admin, Roles.EventManage)]
    public async Task<IDataResult<PagedResponse<EventResponse>>> GetForAdminAsync(
        PageRequest pageRequest,
        CancellationToken cancellationToken = default)
    {
        DateTime now = _clock.UtcNow;
        Guid? userId = _currentUser.UserId;

        PagedList<Room> page = await _roomRepository.GetEventsForAdminAsync(pageRequest, cancellationToken);
        PagedList<EventResponse> mapped = page.Map(room => room.ToEventResponse(userId, now));

        return new SuccessDataResult<PagedResponse<EventResponse>>(
            PagedResponse<EventResponse>.From(mapped));
    }

    // =====================================================================
    //  Yönetim
    // =====================================================================

    [SecuredOperationAspect(Roles.Admin, Roles.EventManage)]
    [ValidationAspect(typeof(SaveEventRequestValidator))]
    [TransactionAspect]
    public async Task<IDataResult<EventResponse>> CreateAsync(
        SaveEventRequest request,
        CancellationToken cancellationToken = default)
    {
        Guid userId = _currentUser.RequireUserId();
        DateTime now = _clock.UtcNow;

        EnsureFutureStart(request.ScheduledStartUtc, now);

        Category category = await LoadUsableCategoryAsync(
            request.CategoryId, request.QuestionCount, cancellationToken);

        var room = new Room
        {
            CategoryId = category.Id,
            HostUserId = userId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            // Etkinlik herkese açıktır ama katılım kodu yine de üretiliyor:
            // JoinCode sütunu zorunlu ve tekil. Kod hiçbir yanıtta dönmez.
            JoinCode = await GenerateEventJoinCodeAsync(cancellationToken),
            Mode = RoomMode.PublicMultiplayer,
            Status = RoomStatus.Waiting,
            QuestionCount = request.QuestionCount,
            SecondsPerQuestion = request.SecondsPerQuestion,
            MaxPlayers = request.MaxPlayers,
            IsOfficialEvent = true,
            ScheduledStartUtc = request.ScheduledStartUtc
        };

        await _roomRepository.AddAsync(room, cancellationToken);

        _logger.LogInformation(
            "Etkinlik oluşturuldu: {EventId} — {ScheduledStart:o}", room.Id, room.ScheduledStartUtc);

        return new SuccessDataResult<EventResponse>(
            await LoadResponseAsync(room.Id, userId, now, cancellationToken),
            Messages.EventCreated);
    }

    [SecuredOperationAspect(Roles.Admin, Roles.EventManage)]
    [ValidationAspect(typeof(SaveEventRequestValidator))]
    [TransactionAspect]
    public async Task<IDataResult<EventResponse>> UpdateAsync(
        Guid eventId,
        SaveEventRequest request,
        CancellationToken cancellationToken = default)
    {
        DateTime now = _clock.UtcNow;

        Room room = await LoadEventAsync(eventId, asNoTracking: false, cancellationToken);

        // Başlamış etkinliğin ayarları değiştirilemez: soru setleri dağıtıldı,
        // oyuncular oynuyor. "Soru sayısını 20'ye çıkar" demek, yarısı bitmiş
        // yarışmaları tutarsız hâle getirirdi.
        if (room.Status != RoomStatus.Waiting)
        {
            throw new BusinessException(Messages.EventAlreadyStarted);
        }

        EnsureFutureStart(request.ScheduledStartUtc, now);

        Category category = await LoadUsableCategoryAsync(
            request.CategoryId, request.QuestionCount, cancellationToken);

        int registeredCount = await _participantRepository.CountByRoomAsync(eventId, cancellationToken);

        // Kontenjanı kayıtlı oyuncu sayısının altına çekmek, kimin eleneceği
        // sorusunu doğurur. Bu kararı sessizce vermek yerine reddediyoruz.
        if (request.MaxPlayers < registeredCount)
        {
            throw new BusinessException(
                $"Kontenjan, kayıtlı oyuncu sayısının ({registeredCount}) altına düşürülemez.");
        }

        room.CategoryId = category.Id;
        room.Name = request.Name.Trim();
        room.Description = request.Description?.Trim();
        room.ScheduledStartUtc = request.ScheduledStartUtc;
        room.QuestionCount = request.QuestionCount;
        room.SecondsPerQuestion = request.SecondsPerQuestion;
        room.MaxPlayers = request.MaxPlayers;

        await _roomRepository.UpdateAsync(room, cancellationToken);

        return new SuccessDataResult<EventResponse>(
            await LoadResponseAsync(eventId, _currentUser.UserId, now, cancellationToken),
            Messages.EventUpdated);
    }

    [SecuredOperationAspect(Roles.Admin, Roles.EventManage)]
    public async Task<IResult> CancelAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        Room room = await LoadEventAsync(eventId, asNoTracking: true, cancellationToken);

        if (room.Status != RoomStatus.Waiting)
        {
            throw new BusinessException(Messages.EventAlreadyStarted);
        }

        // İptal mantığı odanınkiyle birebir aynı (durum + devam eden yarışmalar
        // + SignalR bildirimi). Kopyalamak yerine tek kaynağı çağırıyoruz.
        return await _roomService.CancelAsync(eventId, cancellationToken);
    }

    // =====================================================================
    //  Oyuncu kaydı
    // =====================================================================

    [TransactionAspect]
    public async Task<IDataResult<EventResponse>> RegisterAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        Guid userId = _currentUser.RequireUserId();
        DateTime now = _clock.UtcNow;

        Room room = await LoadEventAsync(eventId, asNoTracking: true, cancellationToken);

        if (room.Status != RoomStatus.Waiting)
        {
            throw new BusinessException(Messages.EventAlreadyStarted);
        }

        if (room.ScheduledStartUtc <= now)
        {
            throw new BusinessException(Messages.EventAlreadyStarted);
        }

        RoomParticipant? existing = await _participantRepository.GetByRoomAndUserAsync(
            eventId, userId, cancellationToken: cancellationToken);

        if (existing is not null)
        {
            throw new ConflictException(Messages.EventAlreadyRegistered);
        }

        int registeredCount = await _participantRepository.CountByRoomAsync(eventId, cancellationToken);
        if (registeredCount >= room.MaxPlayers)
        {
            throw new ConflictException(Messages.EventFull);
        }

        await _participantRepository.AddAsync(
            new RoomParticipant
            {
                RoomId = eventId,
                UserId = userId,
                // Kurucu (yöneticinin) kendisi katılımcı değil; kaydolan herkes
                // sıradan oyuncudur.
                Role = ParticipantRole.Player,
                JoinOrder = registeredCount + 1,
                IsReady = true,
                LastSeenAtUtc = now
            },
            cancellationToken);

        return new SuccessDataResult<EventResponse>(
            await LoadResponseAsync(eventId, userId, now, cancellationToken),
            Messages.EventRegistered);
    }

    [TransactionAspect]
    public async Task<IDataResult<EventResponse>> WithdrawAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        Guid userId = _currentUser.RequireUserId();
        DateTime now = _clock.UtcNow;

        Room room = await LoadEventAsync(eventId, asNoTracking: true, cancellationToken);

        if (room.Status != RoomStatus.Waiting)
        {
            throw new BusinessException(Messages.EventAlreadyStarted);
        }

        RoomParticipant participant = await _participantRepository.GetByRoomAndUserAsync(
                                          eventId, userId, asNoTracking: false, cancellationToken)
                                      ?? throw new BusinessException(Messages.EventNotRegistered);

        // Kalıcı silme: yumuşak silinen kayıt, tekil (RoomId, UserId) indeksini
        // meşgul ederdi ve oyuncu bir daha kaydolamazdı.
        await _participantRepository.DeleteAsync(participant, hardDelete: true, cancellationToken);

        return new SuccessDataResult<EventResponse>(
            await LoadResponseAsync(eventId, userId, now, cancellationToken),
            Messages.EventWithdrawn);
    }

    // =====================================================================
    //  Zamanlayıcı
    // =====================================================================

    /// <remarks>
    /// <b>Neden her etkinlik ayrı ayrı ele alınıyor?</b> Biri hata verirse
    /// (ör. kategoriden sorular silinmiş) diğerleri yine de başlamalı. Tek bir
    /// toplu işlem olsaydı ilk hata tüm turu düşürürdü ve bozuk tek bir kayıt,
    /// tüm etkinlik sistemini kalıcı olarak durdurabilirdi.
    /// </remarks>
    public async Task<int> ProcessDueEventsAsync(CancellationToken cancellationToken = default)
    {
        DateTime now = _clock.UtcNow;

        IReadOnlyList<Room> dueEvents = await _roomRepository.GetDueEventsAsync(now, cancellationToken);

        int startedCount = 0;

        foreach (Room room in dueEvents)
        {
            try
            {
                if (room.Participants.Count == 0)
                {
                    room.Status = RoomStatus.Cancelled;
                    room.FinishedAtUtc = now;
                    await _roomRepository.UpdateAsync(room, cancellationToken);

                    _logger.LogInformation(
                        "Etkinlik katılımcısız iptal edildi: {EventId}", room.Id);
                    continue;
                }

                await _roomService.StartScheduledEventAsync(room.Id, cancellationToken);
                startedCount++;
            }
            catch (Exception exception)
            {
                // Hata yutulmuyor, kaydediliyor: etkinlik `Waiting` durumunda
                // kaldığı için bir sonraki turda yeniden denenecek. Kalıcı bir
                // sorunsa (ör. yetersiz soru) günlükte tekrarlanarak görünür olur.
                _logger.LogError(
                    exception,
                    "Zamanlanmış etkinlik başlatılamadı: {EventId}. Sonraki turda yeniden denenecek.",
                    room.Id);
            }
        }

        return startedCount;
    }

    // =====================================================================
    //  Yardımcılar
    // =====================================================================

    private static void EnsureFutureStart(DateTime scheduledStartUtc, DateTime nowUtc)
    {
        if (scheduledStartUtc <= nowUtc)
        {
            throw new BusinessException(Messages.EventStartMustBeFuture);
        }
    }

    private async Task<Category> LoadUsableCategoryAsync(
        Guid categoryId,
        int questionCount,
        CancellationToken cancellationToken)
    {
        Category category = await _categoryRepository.GetAsync(
                                c => c.Id == categoryId, cancellationToken: cancellationToken)
                            ?? throw new NotFoundException(Messages.CategoryNotFound);

        if (!category.IsActive)
        {
            throw new BusinessException(Messages.CategoryInactive);
        }

        // Soru yetersizliğini etkinlik kurulurken yakalıyoruz. Başlangıç anında
        // yakalansaydı, kaydolan oyuncular etkinliğin hiç başlamadığını görürdü.
        int available = (await _questionRepository
            .GetSelectableQuestionIdsAsync(categoryId, cancellationToken)).Count;

        if (available < questionCount)
        {
            throw new BusinessException(
                $"{Messages.NotEnoughQuestions} (Bu kategoride {available} uygun soru var.)");
        }

        return category;
    }

    private async Task<Room> LoadEventAsync(
        Guid eventId,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        Room room = await _roomRepository.GetAsync(
                        r => r.Id == eventId, asNoTracking: asNoTracking,
                        cancellationToken: cancellationToken)
                    ?? throw new NotFoundException(Messages.EventNotFound);

        if (!room.IsOfficialEvent)
        {
            // Sıradan bir odanın kimliğiyle etkinlik uçlarına gelinirse,
            // "bulunamadı" demek doğru cevap: etkinlik uçları odaları yönetmez.
            throw new NotFoundException(Messages.EventNotFound);
        }

        return room;
    }

    private async Task<EventResponse> LoadResponseAsync(
        Guid eventId,
        Guid? userId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        Room detail = await _roomRepository.GetDetailAsync(eventId, cancellationToken: cancellationToken)
                      ?? throw new NotFoundException(Messages.EventNotFound);

        return detail.ToEventResponse(userId, nowUtc);
    }

    /// <summary>
    /// Etkinlik için katılım kodu üretir.
    /// </summary>
    /// <remarks>
    /// Kod hiçbir yanıtta dönmez; yalnızca <c>JoinCode</c> sütununun zorunlu ve
    /// tekil olması nedeniyle üretilir. Çakışma olasılığına karşı birkaç kez
    /// denenir.
    /// </remarks>
    private async Task<string> GenerateEventJoinCodeAsync(CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            string code = JoinCodeGenerator.Generate();

            if (!await _roomRepository.JoinCodeExistsAsync(code, cancellationToken))
            {
                return code;
            }
        }

        throw new BusinessException("Katılım kodu üretilemedi. Lütfen tekrar deneyin.");
    }
}
