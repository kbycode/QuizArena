/* ==========================================================================
   QuizArena — demo istemci

   Sade JavaScript, dış bağımlılık yok. Bu dosya API'nin nasıl kullanıldığını
   çalışan bir örnekle gösterir.

   NOT — gerçek zamanlı güncelleme hakkında:
   API, /hubs/quiz adresinde bir SignalR hub'ı sunuyor (canlı skor, oyuncu
   katılımı, oyun başlangıcı). Bu demo bilinçli olarak SignalR JavaScript
   istemcisini KULLANMIYOR; çünkü o istemci bir CDN'den ya da npm derlemesinden
   gelmek zorunda. İkisi de projeyi "dotnet run ile hemen çalışır" olmaktan
   çıkarır ve sıkı Content-Security-Policy başlığını ihlal eder. Demo, aynı
   sonucu kısa aralıklı yoklama (polling) ile üretiyor; üretim istemcisi
   hub'a bağlanmalıdır.
   ========================================================================== */

'use strict';

// ---------------------------------------------------------------------------
//  Durum
// ---------------------------------------------------------------------------
const state = {
  accessToken: null,
  refreshToken: null,
  user: null,
  selectedCategory: null,
  room: null,
  question: null,
  timerHandle: null,
  pollHandle: null,
  answering: false
};

const STORAGE_KEY = 'quizarena.session';
const OPTION_KEYS = ['A', 'B', 'C', 'D', 'E', 'F'];

const DIFFICULTY_LABELS = { Easy: 'Kolay', Medium: 'Orta', Hard: 'Zor' };

// ---------------------------------------------------------------------------
//  Kısayollar
// ---------------------------------------------------------------------------
const $ = (id) => document.getElementById(id);
const screens = ['authScreen', 'homeScreen', 'lobbyScreen', 'gameScreen', 'resultScreen'];

function showScreen(id) {
  screens.forEach((screen) => $(screen).classList.toggle('hidden', screen !== id));
}

let toastHandle = null;
function toast(message, kind = 'info') {
  const element = $('toast');
  element.textContent = message;
  element.className = `toast ${kind}`;
  clearTimeout(toastHandle);
  toastHandle = setTimeout(() => element.classList.add('hidden'), 4200);
}

/** Metni HTML'e yazmadan önce kaçırır. */
function escapeHtml(value) {
  const div = document.createElement('div');
  div.textContent = value ?? '';
  return div.innerHTML;
}

// ---------------------------------------------------------------------------
//  API katmanı
// ---------------------------------------------------------------------------

/**
 * API çağrısı yapar.
 *
 * 401 alındığında bir kez yenileme jetonuyla oturumu tazeleyip isteği
 * yineler. Böylece 15 dakikalık erişim jetonu dolduğunda kullanıcı
 * oyunun ortasında dışarı atılmaz.
 */
async function api(path, { method = 'GET', body, retryOnUnauthorized = true } = {}) {
  const headers = { 'Content-Type': 'application/json' };
  if (state.accessToken) {
    headers.Authorization = `Bearer ${state.accessToken}`;
  }

  let response;
  try {
    response = await fetch(path, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body)
    });
  } catch (error) {
    // fetch YALNIZCA ağ seviyesinde başarısız olduğunda reddeder (TypeError):
    // sunucu kapalı, bağlantı koptu, DNS çözülemedi. 4xx/5xx yanıtlar buraya
    // düşmez — onlar normal şekilde çözülür.
    //
    // Tarayıcının ham mesajı ("Failed to fetch" / "NetworkError") kullanıcıya
    // hiçbir şey anlatmaz; anlaşılır bir metne çeviriyoruz.
    throw new Error(
      'Sunucuya ulaşılamıyor. API çalışmıyor olabilir veya internet bağlantınız koptu. ' +
      'Lütfen birkaç saniye sonra tekrar deneyin.',
      { cause: error }
    );
  }

  if (response.status === 401 && retryOnUnauthorized && state.refreshToken) {
    const refreshed = await tryRefreshSession();
    if (refreshed) {
      return api(path, { method, body, retryOnUnauthorized: false });
    }
  }

  const text = await response.text();
  const payload = text ? JSON.parse(text) : null;

  if (!response.ok) {
    // Sunucu RFC 7807 ProblemDetails döner. Alan bazlı doğrulama hataları
    // 'errors' içinde gelir; kullanıcıya ilkini gösteriyoruz.
    const fieldError = payload?.errors && Object.values(payload.errors)[0]?.[0];
    throw new Error(fieldError || payload?.detail || payload?.title || `İstek başarısız (${response.status})`);
  }

  return payload;
}

/**
 * Aynı anda yalnızca BİR yenileme isteği yapılmasını sağlayan kilit.
 *
 * Neden gerekli: ana sayfa açılışta 4 isteği paralel atıyor. Erişim jetonu
 * dolmuşsa dördü birden 401 alır ve dördü birden yenileme çağırır.
 *
 * Sunucu tarafında yenileme jetonu ROTASYONLU: kullanılan jeton iptal edilip
 * yerine yenisi veriliyor. Dört istek aynı eski jetonu gönderdiğinde ilki
 * jetonu iptal eder; diğerleri iptal edilmiş bir jeton sunmuş olur ve sunucu
 * bunu haklı olarak "jeton çalınmış" sayarak kullanıcının TÜM oturumlarını
 * kapatır. Yani kullanıcı hiçbir şey yapmadan sistemden atılır.
 *
 * Çözüm: uçuştaki yenileme sözü (promise) paylaşılır — dört istek de aynı
 * sonucu bekler, sunucuya tek yenileme gider.
 */
let refreshInFlight = null;

function tryRefreshSession() {
  refreshInFlight ??= performRefresh().finally(() => { refreshInFlight = null; });
  return refreshInFlight;
}

async function performRefresh() {
  try {
    const response = await fetch('/api/auth/refresh', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken: state.refreshToken })
    });

    if (!response.ok) {
      return false;
    }

    const payload = await response.json();
    applySession(payload.data);
    return true;
  } catch {
    // Ağ hatası: oturum geçersiz sayılmaz, çağıran taraf asıl hatayı gösterir.
    return false;
  }
}

// ---------------------------------------------------------------------------
//  Oturum
// ---------------------------------------------------------------------------
function applySession(auth) {
  state.accessToken = auth.accessToken;
  state.refreshToken = auth.refreshToken;
  state.user = auth.user;

  // sessionStorage bilinçli tercih: localStorage'ın aksine sekme kapandığında
  // silinir. Ortak kullanılan bir bilgisayarda jeton geride kalmaz.
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify({
    accessToken: auth.accessToken,
    refreshToken: auth.refreshToken,
    user: auth.user
  }));

  renderUserBadge();
}

function restoreSession() {
  try {
    const saved = sessionStorage.getItem(STORAGE_KEY);
    if (!saved) {
      return false;
    }

    const parsed = JSON.parse(saved);
    state.accessToken = parsed.accessToken;
    state.refreshToken = parsed.refreshToken;
    state.user = parsed.user;
    renderUserBadge();
    return true;
  } catch {
    return false;
  }
}

function clearSession() {
  stopPolling();
  stopTimer();
  sessionStorage.removeItem(STORAGE_KEY);
  state.accessToken = null;
  state.refreshToken = null;
  state.user = null;
  state.room = null;
  state.question = null;
  $('userBadge').classList.add('hidden');
  showScreen('authScreen');
}

function renderUserBadge() {
  if (!state.user) {
    return;
  }
  $('userNickname').textContent = state.user.nickname;
  $('userBadge').classList.remove('hidden');
}

// ---------------------------------------------------------------------------
//  Giriş / kayıt
// ---------------------------------------------------------------------------
document.querySelectorAll('.tab').forEach((tab) => {
  tab.addEventListener('click', () => {
    document.querySelectorAll('.tab').forEach((t) => t.classList.remove('active'));
    tab.classList.add('active');
    $('loginForm').classList.toggle('hidden', tab.dataset.tab !== 'login');
    $('registerForm').classList.toggle('hidden', tab.dataset.tab !== 'register');
  });
});

$('loginForm').addEventListener('submit', async (event) => {
  event.preventDefault();
  const form = new FormData(event.target);

  await withButtonBusy(event.target.querySelector('button'), async () => {
    const result = await api('/api/auth/login', {
      method: 'POST',
      body: { email: form.get('email'), password: form.get('password') }
    });
    applySession(result.data);
    toast(result.message || 'Giriş başarılı.', 'success');
    await enterApp();
  });
});

$('registerForm').addEventListener('submit', async (event) => {
  event.preventDefault();
  const form = new FormData(event.target);

  await withButtonBusy(event.target.querySelector('button'), async () => {
    const result = await api('/api/auth/register', {
      method: 'POST',
      body: {
        email: form.get('email'),
        password: form.get('password'),
        firstName: form.get('firstName'),
        lastName: form.get('lastName'),
        nickname: form.get('nickname')
      }
    });
    applySession(result.data);
    toast(result.message || 'Kaydınız oluşturuldu.', 'success');
    await enterApp();
  });
});

$('logoutButton').addEventListener('click', async () => {
  try {
    await api('/api/auth/logout', { method: 'POST', body: { refreshToken: state.refreshToken } });
  } catch {
    // Çıkış her durumda yerel olarak tamamlanır.
  }
  clearSession();
  toast('Çıkış yapıldı.');
});

/** Butonu istek süresince kilitler ve hatayı bildirime çevirir. */
async function withButtonBusy(button, action) {
  const original = button?.textContent;
  if (button) {
    button.disabled = true;
    button.textContent = 'Bekleyin…';
  }

  try {
    await action();
  } catch (error) {
    toast(error.message, 'error');
  } finally {
    if (button) {
      button.disabled = false;
      button.textContent = original;
    }
  }
}

// ---------------------------------------------------------------------------
//  Ana sayfa
// ---------------------------------------------------------------------------
async function enterApp() {
  showScreen('homeScreen');

  await Promise.all([
    loadCategories(),
    loadLeaderboard(),
    loadMyStatistics(),
    loadOpenRooms()
  ]);

  // Sayfa yenilendiyse devam eden oyuna geri dön.
  await resumeActiveGame();
}

async function loadCategories() {
  const grid = $('categoryGrid');

  try {
    const result = await api('/api/categories');
    const categories = result.data ?? [];

    if (categories.length === 0) {
      grid.innerHTML = '<div class="empty">Henüz kategori eklenmemiş.</div>';
      return;
    }

    grid.innerHTML = categories.map((category) => {
      const enough = category.questionCount >= 5;
      return `
        <button class="category" type="button"
                data-id="${category.id}"
                data-max="${category.questionCount}"
                style="--accent:${escapeHtml(category.colorHex || '#6366f1')}"
                ${enough ? '' : 'disabled'}>
          <span class="category-icon">${escapeHtml(category.icon || '❓')}</span>
          <span class="category-name">${escapeHtml(category.name)}</span>
          <span class="category-meta">${category.questionCount} soru${enough ? '' : ' — yetersiz'}</span>
        </button>`;
    }).join('');

    grid.querySelectorAll('.category').forEach((button) => {
      button.addEventListener('click', () => selectCategory(button));
    });
  } catch (error) {
    grid.innerHTML = `<div class="empty">Kategoriler yüklenemedi: ${escapeHtml(error.message)}</div>`;
  }
}

function selectCategory(button) {
  document.querySelectorAll('.category').forEach((c) => c.classList.remove('selected'));
  button.classList.add('selected');

  state.selectedCategory = { id: button.dataset.id, maxQuestions: Number(button.dataset.max) };
  $('startGameButton').disabled = false;

  // Kategoride yeterli soru yoksa daha büyük soru sayısı seçeneklerini kapat:
  // hata mesajı yerine imkânsız seçimi baştan engellemek daha iyi bir deneyim.
  Array.from($('questionCount').options).forEach((option) => {
    option.disabled = Number(option.value) > state.selectedCategory.maxQuestions;
  });

  const select = $('questionCount');
  if (select.selectedOptions[0]?.disabled) {
    const firstEnabled = Array.from(select.options).find((o) => !o.disabled);
    if (firstEnabled) {
      select.value = firstEnabled.value;
    }
  }
}

async function loadLeaderboard() {
  const list = $('leaderboard');

  try {
    const result = await api('/api/leaderboard?top=10');
    const entries = result.data ?? [];

    list.innerHTML = entries.length === 0
      ? '<div class="empty">Henüz kimse oynamadı. İlk sen ol!</div>'
      : entries.map((entry) => renderLeaderboardRow(entry)).join('');
  } catch {
    list.innerHTML = '<div class="empty">Sıralama yüklenemedi.</div>';
  }
}

function renderLeaderboardRow(entry) {
  const medal = ['gold', 'silver', 'bronze'][entry.rank - 1] ?? '';
  const isMe = entry.userId === state.user?.id ? ' me' : '';

  return `
    <li class="${isMe}">
      <span class="rank ${medal}">${entry.rank}</span>
      <span class="name">${escapeHtml(entry.nickname)}</span>
      <span class="points">${entry.totalScore.toLocaleString('tr-TR')}</span>
    </li>`;
}

async function loadMyStatistics() {
  try {
    const result = await api('/api/users/me/statistics');
    const stats = result.data;

    $('userScore').textContent = `${stats.totalScore.toLocaleString('tr-TR')} puan`;

    $('myStats').innerHTML = [
      ['Yarışma', stats.totalCompetitions],
      ['Puan', stats.totalScore.toLocaleString('tr-TR')],
      ['Doğruluk', `%${stats.accuracyPercentage}`],
      ['En iyi seri', stats.bestStreak],
      ['Birincilik', stats.winCount],
      ['Ort. süre', stats.averageAnswerMilliseconds ? `${(stats.averageAnswerMilliseconds / 1000).toFixed(1)} sn` : '—']
    ].map(([label, value]) => `
      <div class="stat"><span class="stat-value">${value}</span><span class="stat-label">${label}</span></div>
    `).join('');

    $('myAchievements').innerHTML = stats.achievements.length === 0
      ? '<div class="empty">Henüz rozet kazanmadın.</div>'
      : stats.achievements.map((achievement) => `
          <div class="badge" title="${escapeHtml(achievement.description)}">
            <span class="badge-icon">${escapeHtml(achievement.icon)}</span>
            ${escapeHtml(achievement.name)}
          </div>`).join('');
  } catch {
    $('myStats').innerHTML = '<div class="empty">İstatistikler yüklenemedi.</div>';
  }
}

async function loadOpenRooms() {
  const container = $('openRooms');

  try {
    const result = await api('/api/rooms?pageSize=5');
    const rooms = result.data?.items ?? [];

    container.innerHTML = rooms.length === 0
      ? '<div class="empty">Şu anda açık oda yok.</div>'
      : rooms.map((room) => `
          <div class="room">
            <div class="room-info">
              <strong>${escapeHtml(room.categoryIcon || '')} ${escapeHtml(room.name)}</strong>
              <small>${escapeHtml(room.hostNickname)} · ${room.currentPlayerCount}/${room.maxPlayers} oyuncu ·
                     ${room.questionCount} soru</small>
            </div>
            <span class="chip">${escapeHtml(room.categoryName)}</span>
          </div>`).join('');
  } catch {
    container.innerHTML = '<div class="empty">Odalar yüklenemedi.</div>';
  }
}

$('refreshRoomsButton').addEventListener('click', loadOpenRooms);

// ---------------------------------------------------------------------------
//  Oda kurma / katılma
// ---------------------------------------------------------------------------
$('startGameButton').addEventListener('click', async (event) => {
  if (!state.selectedCategory) {
    toast('Önce bir kategori seç.', 'error');
    return;
  }

  const mode = $('roomMode').value;

  await withButtonBusy(event.target, async () => {
    const result = await api('/api/rooms', {
      method: 'POST',
      body: {
        categoryId: state.selectedCategory.id,
        name: null,
        mode,
        questionCount: Number($('questionCount').value),
        secondsPerQuestion: Number($('secondsPerQuestion').value),
        maxPlayers: mode === 'Solo' ? 1 : 4
      }
    });

    state.room = result.data;

    // Tek kişilik odada yarışma sunucuda zaten başlatıldı.
    if (mode === 'Solo') {
      await loadNextQuestion();
    } else {
      showLobby();
    }
  });
});

$('joinRoomButton').addEventListener('click', async (event) => {
  const code = $('joinCodeInput').value.trim().toUpperCase();

  if (code.length !== 6) {
    toast('Katılım kodu 6 karakter olmalı.', 'error');
    return;
  }

  await withButtonBusy(event.target, async () => {
    const result = await api('/api/rooms/join', { method: 'POST', body: { joinCode: code } });
    state.room = result.data;
    $('joinCodeInput').value = '';
    showLobby();
  });
});

function showLobby() {
  showScreen('lobbyScreen');
  renderLobby();
  startPolling(pollLobby, 2500);
}

function renderLobby() {
  const room = state.room;
  const isHost = room.hostUserId === state.user?.id;

  $('lobbyTitle').textContent = room.name;
  $('lobbyJoinCode').textContent = room.joinCode ?? '------';

  $('lobbyParticipants').innerHTML = room.participants.map((participant) => `
    <div class="participant">
      <span>${escapeHtml(participant.nickname)}${participant.role === 'Host' ? ' 👑' : ''}</span>
      <span class="chip">${participant.role === 'Host' ? 'Kurucu' : 'Oyuncu'}</span>
    </div>`).join('');

  // "Başlat" ve "İptal" yalnızca kurucuya gösterilir. Sunucu da bunu
  // ayrıca kontrol ediyor — arayüzü gizlemek bir kolaylık, güvenlik önlemi değil.
  $('lobbyStartButton').classList.toggle('hidden', !isHost);
  $('lobbyCancelButton').classList.toggle('hidden', !isHost);
  $('lobbyStartButton').disabled = room.participants.length < 2;
}

async function pollLobby() {
  try {
    const result = await api(`/api/rooms/${state.room.id}`);
    state.room = result.data;

    if (state.room.status === 'InProgress') {
      stopPolling();
      await loadNextQuestion();
      return;
    }

    if (state.room.status === 'Cancelled') {
      stopPolling();
      toast('Oda iptal edildi.', 'error');
      await enterApp();
      return;
    }

    renderLobby();
  } catch (error) {
    stopPolling();
    toast(error.message, 'error');
  }
}

$('lobbyStartButton').addEventListener('click', async (event) => {
  await withButtonBusy(event.target, async () => {
    await api(`/api/rooms/${state.room.id}/start`, { method: 'POST' });
    stopPolling();
    await loadNextQuestion();
  });
});

$('lobbyCancelButton').addEventListener('click', async (event) => {
  await withButtonBusy(event.target, async () => {
    await api(`/api/rooms/${state.room.id}/cancel`, { method: 'POST' });
    stopPolling();
    toast('Oda iptal edildi.');
    await enterApp();
  });
});

// ---------------------------------------------------------------------------
//  Oyun
// ---------------------------------------------------------------------------
async function resumeActiveGame() {
  try {
    const result = await api('/api/rooms/mine');
    if (!result.data) {
      return;
    }

    state.room = result.data;

    if (state.room.status === 'InProgress') {
      await loadNextQuestion();
    } else if (state.room.status === 'Waiting') {
      showLobby();
    }
  } catch {
    // Devam eden oyun yok; ana sayfada kalınır.
  }
}

async function loadNextQuestion() {
  try {
    const result = await api('/api/play/current');

    // Veri boşsa yarışma bitti.
    if (!result.data) {
      await showResult();
      return;
    }

    state.question = result.data;
    state.answering = false;
    renderQuestion();
    showScreen('gameScreen');

    // Çok oyunculu odada canlı skoru yokla.
    const isMultiplayer = state.room && state.room.mode !== 'Solo';
    $('liveScoreboardCard').classList.toggle('hidden', !isMultiplayer);
    if (isMultiplayer) {
      startPolling(pollLiveScoreboard, 3000);
    }
  } catch (error) {
    toast(error.message, 'error');
    await enterApp();
  }
}

function renderQuestion() {
  const question = state.question;

  $('questionProgress').textContent = `Soru ${question.order} / ${question.totalQuestions}`;
  $('questionDifficulty').textContent = DIFFICULTY_LABELS[question.difficulty] ?? question.difficulty;
  $('gameScore').textContent = `${question.currentScore.toLocaleString('tr-TR')} puan`;
  $('questionText').textContent = question.text;

  const streak = $('streakBadge');
  streak.classList.toggle('hidden', question.currentStreak < 2);
  streak.textContent = `🔥 ${question.currentStreak} seri`;

  $('answerFeedback').classList.add('hidden');

  $('optionList').innerHTML = question.options.map((option, index) => `
    <button class="option" type="button" data-id="${option.id}">
      <span class="option-key">${OPTION_KEYS[index] ?? index + 1}</span>
      <span>${escapeHtml(option.text)}</span>
    </button>`).join('');

  $('optionList').querySelectorAll('.option').forEach((button) => {
    button.addEventListener('click', () => submitAnswer(button.dataset.id));
  });

  startTimer(new Date(question.closesAtUtc));
}

/**
 * Süre sayacı.
 *
 * Sayaç bittiğinde istemci cevabı boş gönderir; ama nihai karar SUNUCUYA
 * aittir. Sunucu da süreyi kendisi ölçtüğü için, tarayıcı saati ileri/geri
 * alınsa bile puan değişmez — sayaç yalnızca görsel bir yardımcıdır.
 */
function startTimer(closesAt) {
  stopTimer();

  const totalMs = Math.max(closesAt - new Date(), 1);
  const bar = $('timerBar');

  const tick = () => {
    const remainingMs = closesAt - new Date();
    const ratio = Math.max(0, Math.min(1, remainingMs / totalMs));

    bar.style.width = `${ratio * 100}%`;
    bar.classList.toggle('critical', ratio < 0.25);
    $('timerText').textContent = `${Math.max(0, Math.ceil(remainingMs / 1000))} saniye`;

    if (remainingMs <= 0) {
      stopTimer();
      if (!state.answering) {
        submitAnswer(null);
      }
    }
  };

  tick();
  state.timerHandle = setInterval(tick, 200);
}

function stopTimer() {
  clearInterval(state.timerHandle);
  state.timerHandle = null;
}

async function submitAnswer(selectedAnswerId) {
  if (state.answering) {
    return;
  }

  state.answering = true;
  stopTimer();

  const buttons = $('optionList').querySelectorAll('.option');
  buttons.forEach((button) => { button.disabled = true; });

  try {
    const result = await api('/api/play/answer', {
      method: 'POST',
      body: {
        competitionQuestionId: state.question.competitionQuestionId,
        selectedAnswerId: selectedAnswerId
      }
    });

    renderAnswerFeedback(result.data, selectedAnswerId, buttons);

    // Oyuncunun sonucu görmesi için kısa bir bekleme.
    setTimeout(async () => {
      if (result.data.hasNextQuestion) {
        await loadNextQuestion();
      } else {
        await showResult(result.data.competitionId);
      }
    }, result.data.isCorrect ? 1600 : 2600);
  } catch (error) {
    state.answering = false;
    buttons.forEach((button) => { button.disabled = false; });
    toast(error.message, 'error');
  }
}

function renderAnswerFeedback(result, selectedAnswerId, buttons) {
  // Doğru cevap yalnızca bu noktada, sunucudan gelir.
  buttons.forEach((button) => {
    if (button.dataset.id === result.correctAnswerId) {
      button.classList.add('correct');
    } else if (button.dataset.id === selectedAnswerId) {
      button.classList.add('wrong');
    }
  });

  $('gameScore').textContent = `${result.totalScore.toLocaleString('tr-TR')} puan`;

  const feedback = $('answerFeedback');
  feedback.className = `feedback ${result.isCorrect ? 'correct' : 'wrong'}`;

  const heading = result.isCorrect
    ? `✅ Doğru! +${result.pointsBreakdown.total} puan`
    : result.isTimedOut
      ? '⏱ Süre doldu'
      : '❌ Yanlış';

  const breakdown = result.isCorrect
    ? `<div class="points">
         <span>Taban: ${result.pointsBreakdown.basePoints}</span>
         <span>Hız: +${result.pointsBreakdown.speedBonus}</span>
         <span>Seri: +${result.pointsBreakdown.streakBonus}</span>
         <span>${(result.elapsedMilliseconds / 1000).toFixed(1)} sn</span>
       </div>`
    : `<div class="points"><span>Doğru cevap: ${escapeHtml(result.correctAnswerText)}</span></div>`;

  const explanation = result.explanation
    ? `<div class="explanation">💡 ${escapeHtml(result.explanation)}</div>`
    : '';

  feedback.innerHTML = `<strong>${heading}</strong>${breakdown}${explanation}`;
  feedback.classList.remove('hidden');
}

async function pollLiveScoreboard() {
  try {
    const result = await api(`/api/rooms/${state.room.id}`);
    const participants = [...(result.data?.participants ?? [])]
      .sort((a, b) => b.totalScore - a.totalScore);

    $('liveScoreboard').innerHTML = participants.map((participant, index) => `
      <li class="${participant.userId === state.user?.id ? 'me' : ''}">
        <span class="rank">${index + 1}</span>
        <span class="name">${escapeHtml(participant.nickname)}</span>
        <span class="points">${participant.totalScore.toLocaleString('tr-TR')}</span>
      </li>`).join('');
  } catch {
    // Canlı skor kritik değil; sessizce geçiyoruz.
  }
}

// ---------------------------------------------------------------------------
//  Sonuç
// ---------------------------------------------------------------------------
async function showResult(competitionId) {
  stopPolling();
  stopTimer();

  try {
    // Yarışma kimliği bilinmiyorsa (ör. sayfa yenilenmişse) geçmişten en
    // son tamamlanan yarışmayı alıyoruz.
    let id = competitionId;
    if (!id) {
      const history = await api('/api/play/history?pageSize=1');
      id = history.data?.items?.[0]?.competitionId;
    }

    if (!id) {
      toast('Yarışma sonucu bulunamadı.', 'error');
      await enterApp();
      return;
    }

    const result = await api(`/api/play/summary/${id}`);
    renderResult(result.data);
    showScreen('resultScreen');
  } catch (error) {
    toast(error.message, 'error');
    await enterApp();
  }
}

function renderResult(summary) {
  const accuracy = summary.accuracyPercentage;

  $('resultEmoji').textContent = accuracy >= 90 ? '🏆' : accuracy >= 60 ? '🎉' : accuracy >= 30 ? '👍' : '📚';
  $('resultTitle').textContent = summary.rank
    ? `${summary.rank}. oldun!`
    : 'Yarışma tamamlandı';
  $('resultScore').textContent = `${summary.totalScore.toLocaleString('tr-TR')} puan`;

  $('resultStats').innerHTML = [
    ['Doğru', `${summary.correctCount}/${summary.questionCount}`],
    ['Doğruluk', `%${accuracy}`],
    ['En uzun seri', summary.longestStreak],
    ['Süre', `${summary.durationSeconds} sn`],
    ['Yanlış', summary.wrongCount],
    ['Süre doldu', summary.timedOutCount]
  ].map(([label, value]) => `
    <div class="stat"><span class="stat-value">${value}</span><span class="stat-label">${label}</span></div>
  `).join('');

  $('newAchievements').innerHTML = summary.newAchievements.length === 0
    ? ''
    : `<div style="width:100%;text-align:center;color:var(--text-muted);font-size:12px;margin-bottom:6px">
         YENİ ROZETLER
       </div>` +
      summary.newAchievements.map((achievement) => `
        <div class="badge new" title="${escapeHtml(achievement.description)}">
          <span class="badge-icon">${escapeHtml(achievement.icon)}</span>
          ${escapeHtml(achievement.name)} (+${achievement.rewardPoints})
        </div>`).join('');

  const hasScoreboard = summary.scoreboard.length > 1;
  $('resultScoreboardWrapper').classList.toggle('hidden', !hasScoreboard);
  if (hasScoreboard) {
    $('resultScoreboard').innerHTML = summary.scoreboard.map((entry) => `
      <li class="${entry.userId === state.user?.id ? 'me' : ''}">
        <span class="rank ${['gold', 'silver', 'bronze'][entry.rank - 1] ?? ''}">${entry.rank}</span>
        <span class="name">${escapeHtml(entry.nickname)}</span>
        <span class="points">${entry.totalScore.toLocaleString('tr-TR')}</span>
      </li>`).join('');
  }
}

$('playAgainButton').addEventListener('click', async () => {
  state.room = null;
  state.question = null;
  await enterApp();
});

// ---------------------------------------------------------------------------
//  Yoklama yardımcıları
// ---------------------------------------------------------------------------
function startPolling(action, intervalMs) {
  stopPolling();
  state.pollHandle = setInterval(action, intervalMs);
  action();
}

function stopPolling() {
  clearInterval(state.pollHandle);
  state.pollHandle = null;
}

// Sekme arka plana alındığında yoklamayı durdur: gereksiz istek üretmenin
// ve pil tüketmenin anlamı yok.
document.addEventListener('visibilitychange', () => {
  if (document.hidden) {
    stopPolling();
  }
});

// ---------------------------------------------------------------------------
//  Klavye kısayolları — A/B/C/D ile cevaplama
// ---------------------------------------------------------------------------
document.addEventListener('keydown', (event) => {
  if ($('gameScreen').classList.contains('hidden') || state.answering) {
    return;
  }

  const index = OPTION_KEYS.indexOf(event.key.toUpperCase());
  if (index === -1) {
    return;
  }

  const option = $('optionList').querySelectorAll('.option')[index];
  if (option && !option.disabled) {
    option.click();
  }
});

// ---------------------------------------------------------------------------
//  Başlangıç
// ---------------------------------------------------------------------------
(async function initialize() {
  if (restoreSession()) {
    try {
      await enterApp();
      return;
    } catch {
      clearSession();
    }
  }

  showScreen('authScreen');
})();
