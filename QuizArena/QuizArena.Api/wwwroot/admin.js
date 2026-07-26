/* ==========================================================================
   QuizArena — yönetim konsolu

   app.js ile aynı kuralla yazıldı: dış bağımlılık yok, derleme adımı yok.
   Grafikler bile elle üretilen SVG; bir grafik kütüphanesi eklemek projeyi
   "dotnet run ile hemen çalışır" olmaktan çıkarır ve sıkı
   Content-Security-Policy başlığını ihlal ederdi.

   app.js'teki `state`, `api`, `toast`, `escapeHtml`, `showScreen` burada da
   kullanılıyor: ikisi de klasik script olarak yükleniyor, dolayısıyla üst
   düzey bağlamalar ortak. Yükleme sırası index.html'de app.js → admin.js.
   ========================================================================== */

'use strict';

// ---------------------------------------------------------------------------
//  Sabitler ve durum
// ---------------------------------------------------------------------------

/** Sunucudaki yetki adları. Yazım hatası sessizce "yetkisiz" demek olurdu. */
const ROLES = {
  admin: 'Admin',
  categoryManage: 'Category.Manage',
  questionManage: 'Question.Manage',
  userManage: 'User.Manage',
  eventManage: 'Event.Manage'
};

const ROLE_LABELS = {
  [ROLES.admin]: 'Yönetici',
  [ROLES.categoryManage]: 'Kategori yönetimi',
  [ROLES.questionManage]: 'Soru yönetimi',
  [ROLES.userManage]: 'Kullanıcı yönetimi',
  [ROLES.eventManage]: 'Etkinlik yönetimi'
};

const ROOM_STATUS_LABELS = {
  Waiting: 'Planlandı',
  InProgress: 'Devam ediyor',
  Finished: 'Tamamlandı',
  Cancelled: 'İptal edildi'
};

/** Sunucudaki GameRules ile aynı değerler; burada yalnızca metin ve sınır. */
const DASHBOARD_MIN_TIMES_ASKED = 3;
const USER_PAGE_SIZE = 10;
const EVENT_PAGE_SIZE = 10;

const adminState = {
  tab: null,
  windowDays: 14,
  categories: [],
  questionFilter: '',
  userPage: 1,
  userSearch: '',
  eventPage: 1,
  claims: []
};

// ---------------------------------------------------------------------------
//  Yetki
// ---------------------------------------------------------------------------

const roles = () => state.user?.roles ?? [];
const hasRole = (...allowed) => allowed.some((role) => roles().includes(role));

const isAdmin = () => hasRole(ROLES.admin);
const canManageContent = () => hasRole(ROLES.admin, ROLES.categoryManage, ROLES.questionManage);
const canManageUsers = () => hasRole(ROLES.admin, ROLES.userManage);
const canManageEvents = () => hasRole(ROLES.admin, ROLES.eventManage);
const canAccessAdmin = () => isAdmin() || canManageContent() || canManageUsers() || canManageEvents();

/** Oturum değiştiğinde üst çubuktaki "Yönetim" düğmesini güncelle. */
function refreshAdminAccess() {
  $('adminNavButton').classList.toggle('hidden', !canAccessAdmin());
}

// ---------------------------------------------------------------------------
//  Gezinme
// ---------------------------------------------------------------------------

document.querySelectorAll('.navlink').forEach((link) => {
  link.addEventListener('click', () => {
    if (link.dataset.screen === 'adminScreen') {
      openAdmin();
      return;
    }
    showScreen('homeScreen');
  });
});

async function openAdmin() {
  if (!canAccessAdmin()) {
    toast('Bu bölüme erişim yetkiniz yok.', 'error');
    return;
  }

  showScreen('adminScreen');
  $('adminSubtitle').textContent = adminSubtitle();

  renderAdminTabs();
  await showAdminTab(adminState.tab ?? defaultTab());
}

function defaultTab() {
  if (isAdmin()) return 'dashboard';
  if (canManageContent()) return 'questions';
  return canManageEvents() ? 'events' : 'users';
}

function adminSubtitle() {
  if (isAdmin()) {
    return 'Sistem özeti, soru havuzu, kategoriler, etkinlikler ve kullanıcı hesapları.';
  }

  const parts = [];
  if (canManageContent()) parts.push('soru ve kategoriler');
  if (canManageEvents()) parts.push('etkinlikler');
  if (canManageUsers()) parts.push('kullanıcı hesapları');
  return `Yetkiniz dâhilinde: ${parts.join(', ')}.`;
}

function availableTabs() {
  const tabs = [];
  if (isAdmin()) tabs.push({ id: 'dashboard', label: '📊 Pano' });
  if (canManageContent()) {
    tabs.push({ id: 'questions', label: '❓ Sorular' });
    tabs.push({ id: 'categories', label: '🏷️ Kategoriler' });
  }
  if (canManageEvents()) tabs.push({ id: 'events', label: '📅 Etkinlikler' });
  if (canManageUsers()) tabs.push({ id: 'users', label: '👥 Kullanıcılar' });
  return tabs;
}

function renderAdminTabs() {
  $('adminTabs').innerHTML = availableTabs().map((tab) => `
    <button class="tab" type="button" role="tab" data-admin-tab="${tab.id}">${tab.label}</button>
  `).join('');

  $('adminTabs').querySelectorAll('[data-admin-tab]').forEach((button) => {
    button.addEventListener('click', () => showAdminTab(button.dataset.adminTab));
  });
}

async function showAdminTab(tab) {
  adminState.tab = tab;

  $('adminTabs').querySelectorAll('[data-admin-tab]').forEach((button) => {
    button.classList.toggle('active', button.dataset.adminTab === tab);
    button.setAttribute('aria-selected', String(button.dataset.adminTab === tab));
  });

  $('adminPanel').innerHTML = '<div class="empty">Yükleniyor…</div>';

  const loaders = {
    dashboard: loadDashboard,
    questions: loadAdminQuestions,
    categories: loadAdminCategories,
    events: loadAdminEvents,
    users: loadAdminUsers
  };

  try {
    await loaders[tab]();
  } catch (error) {
    $('adminPanel').innerHTML = `<div class="card"><div class="empty">${escapeHtml(error.message)}</div></div>`;
  }
}

// ---------------------------------------------------------------------------
//  Diyalog
//
//  Tek bir <dialog> hem formlara hem onaylara hizmet ediyor. `onConfirm`
//  false döndürürse (doğrulama başarısız) diyalog açık kalır.
// ---------------------------------------------------------------------------

let dialogConfirmHandler = null;

function openDialog({ title, body, confirmLabel = 'Kaydet', danger = false, onConfirm }) {
  $('adminDialogTitle').textContent = title;
  $('adminDialogBody').innerHTML = body;

  const confirmButton = $('adminDialogConfirm');
  confirmButton.textContent = confirmLabel;
  confirmButton.className = danger ? 'btn btn-danger' : 'btn btn-primary';

  dialogConfirmHandler = onConfirm;
  $('adminDialog').showModal();
}

function closeDialog() {
  dialogConfirmHandler = null;
  $('adminDialog').close();
}

$('adminDialogCancel').addEventListener('click', closeDialog);

$('adminDialogConfirm').addEventListener('click', async () => {
  if (!dialogConfirmHandler) {
    closeDialog();
    return;
  }

  await withButtonBusy($('adminDialogConfirm'), async () => {
    const keepOpen = (await dialogConfirmHandler()) === false;
    if (!keepOpen) {
      closeDialog();
    }
  });
});

/** Geri alınamayan işlemler için onay ister. */
function confirmAction({ title, message, confirmLabel, onConfirm }) {
  openDialog({
    title,
    body: `<p class="dialog-text">${escapeHtml(message)}</p>`,
    confirmLabel,
    danger: true,
    onConfirm
  });
}

/** Diyalog içindeki alanın değerini okur. */
const field = (name) => $('adminDialog').querySelector(`[name="${name}"]`);
const fieldValue = (name) => field(name)?.value?.trim() ?? '';

function dialogError(message) {
  const box = $('adminDialogBody').querySelector('.dialog-error');
  if (box) {
    box.textContent = message;
    box.classList.remove('hidden');
  } else {
    toast(message, 'error');
  }
}

// ---------------------------------------------------------------------------
//  Elle çizilen SVG grafikler
//
//  Grafik kütüphanesi yerine SVG üretmek burada birkaç yüz bayt ve tam
//  denetim demek. İki grafik tipi yetiyor: dikey çubuk (+ çizgi) ve yatay
//  çubuk.
// ---------------------------------------------------------------------------

/** Sayıyı SVG koordinatına çevirirken NaN üretmemek için. */
const safeRatio = (value, max) => (max > 0 ? value / max : 0);

/**
 * Günlük etkinlik: yarışma sayısı çubuk, benzersiz oyuncu çizgi.
 *
 * Sıfır olan günler de çiziliyor — veri olmayan günleri atlamak, iki uzak
 * tarihi yan yana getirip "her gün oynanıyor" izlenimi verirdi.
 */
function activityChart(points) {
  if (points.length === 0) {
    return '<div class="empty">Bu aralıkta tamamlanmış yarışma yok.</div>';
  }

  const width = 760;
  const height = 240;
  const padLeft = 34;
  const padRight = 10;
  const padTop = 16;
  const padBottom = 30;

  const innerWidth = width - padLeft - padRight;
  const innerHeight = height - padTop - padBottom;
  const max = Math.max(1, ...points.map((p) => Math.max(p.competitions, p.players)));

  const step = innerWidth / points.length;
  const barWidth = Math.min(26, step * 0.55);

  const x = (index) => padLeft + step * index + step / 2;
  const y = (value) => padTop + innerHeight - safeRatio(value, max) * innerHeight;

  const bars = points.map((point, index) => `
    <rect class="chart-bar" x="${(x(index) - barWidth / 2).toFixed(1)}" y="${y(point.competitions).toFixed(1)}"
          width="${barWidth.toFixed(1)}" height="${(padTop + innerHeight - y(point.competitions)).toFixed(1)}"
          rx="3"><title>${escapeHtml(shortDate(point.date))}: ${point.competitions} yarışma</title></rect>
  `).join('');

  const line = points.map((point, index) => `${x(index).toFixed(1)},${y(point.players).toFixed(1)}`).join(' ');

  // Yalnızca birkaç etiket: 14 günde her günü yazmak okunmaz hâle getirir.
  const labelEvery = Math.ceil(points.length / 7);
  const labels = points.map((point, index) => (index % labelEvery === 0 ? `
    <text class="chart-label" x="${x(index).toFixed(1)}" y="${height - 10}" text-anchor="middle">
      ${escapeHtml(shortDate(point.date))}
    </text>` : '')).join('');

  const ticks = [0, Math.round(max / 2), max].filter((value, index, all) => all.indexOf(value) === index);
  const gridlines = ticks.map((value) => `
    <line class="chart-grid" x1="${padLeft}" x2="${width - padRight}" y1="${y(value).toFixed(1)}" y2="${y(value).toFixed(1)}" />
    <text class="chart-label" x="${padLeft - 8}" y="${(y(value) + 4).toFixed(1)}" text-anchor="end">${value}</text>
  `).join('');

  return `
    <div class="chart-legend">
      <span><i class="swatch swatch-bar"></i>Yarışma</span>
      <span><i class="swatch swatch-line"></i>Benzersiz oyuncu</span>
    </div>
    <svg class="chart" viewBox="0 0 ${width} ${height}" role="img"
         aria-label="Günlük tamamlanan yarışma ve benzersiz oyuncu sayısı">
      ${gridlines}
      ${bars}
      <polyline class="chart-line" points="${line}" />
      ${labels}
    </svg>`;
}

/** Kategori doğruluk oranı: yatay çubuk, 0-100 sabit eksen. */
function accuracyChart(categories) {
  const rows = categories.filter((category) => category.timesAsked > 0);

  if (rows.length === 0) {
    return '<div class="empty">Henüz cevaplanmış soru yok.</div>';
  }

  const rowHeight = 30;
  const width = 380;
  const labelWidth = 128;
  const barMax = width - labelWidth - 42;

  const bars = rows.map((category, index) => {
    const top = index * rowHeight;
    const barLength = Math.max(2, (category.accuracyPercentage / 100) * barMax);
    const color = category.colorHex || '#6366f1';

    return `
      <text class="chart-label chart-label-strong" x="0" y="${top + 19}">${escapeHtml(trim(category.name, 16))}</text>
      <rect class="chart-track" x="${labelWidth}" y="${top + 8}" width="${barMax}" height="12" rx="6" />
      <rect x="${labelWidth}" y="${top + 8}" width="${barLength.toFixed(1)}" height="12" rx="6"
            fill="${escapeHtml(color)}"><title>${escapeHtml(category.name)}: %${category.accuracyPercentage}</title></rect>
      <text class="chart-label" x="${width - 4}" y="${top + 19}" text-anchor="end">%${category.accuracyPercentage}</text>`;
  }).join('');

  return `
    <svg class="chart" viewBox="0 0 ${width} ${rows.length * rowHeight}" role="img"
         aria-label="Kategori bazlı doğruluk oranı">${bars}</svg>`;
}

// ---------------------------------------------------------------------------
//  Biçimlendirme yardımcıları
// ---------------------------------------------------------------------------

const number = (value) => Number(value ?? 0).toLocaleString('tr-TR');

function trim(text, max) {
  const value = String(text ?? '');
  return value.length > max ? `${value.slice(0, max - 1)}…` : value;
}

function shortDate(iso) {
  return new Date(iso).toLocaleDateString('tr-TR', { day: '2-digit', month: 'short' });
}

function dateTime(iso) {
  return new Date(iso).toLocaleString('tr-TR', {
    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit'
  });
}

/** "3 saat sonra" / "2 gün önce". */
function relativeTime(iso) {
  const diffMinutes = Math.round((new Date(iso).getTime() - Date.now()) / 60000);
  const formatter = new Intl.RelativeTimeFormat('tr-TR', { numeric: 'auto' });

  if (Math.abs(diffMinutes) < 60) return formatter.format(diffMinutes, 'minute');

  const diffHours = Math.round(diffMinutes / 60);
  if (Math.abs(diffHours) < 24) return formatter.format(diffHours, 'hour');

  return formatter.format(Math.round(diffHours / 24), 'day');
}

/** `datetime-local` alanı yerel saat bekler; ISO'nun Z'li hâli işe yaramaz. */
function toLocalInputValue(date) {
  const offset = date.getTimezoneOffset() * 60000;
  return new Date(date.getTime() - offset).toISOString().slice(0, 16);
}

// ---------------------------------------------------------------------------
//  Pano
// ---------------------------------------------------------------------------

async function loadDashboard() {
  const result = await api(`/api/dashboard?windowDays=${adminState.windowDays}`);
  const data = result.data;
  const summary = data.summary;

  const tiles = [
    ['👥 Kullanıcı', number(summary.totalUsers), `${summary.activeUsers} aktif` +
      (summary.lockedUsers > 0 ? ` · ${summary.lockedUsers} kilitli` : '')],
    ['➕ Yeni kayıt', number(summary.newUsersInWindow), `son ${data.windowDays} gün`],
    ['🚩 Yarışma', number(summary.finishedCompetitions), `son ${data.windowDays} günde ${summary.competitionsInWindow}`],
    ['⚡ Canlı oda', number(summary.liveRooms), 'bekleyen + oynanan'],
    ['❓ Soru', number(summary.activeQuestions), `${summary.totalQuestions} kayıt · ${summary.activeCategories} kategori`],
    ['✅ Doğruluk', `%${summary.overallAccuracy}`, `${number(summary.totalAnswers)} cevap üzerinden`]
  ];

  $('adminPanel').innerHTML = `
    <div class="admin-toolbar">
      <div class="segmented" role="group" aria-label="Zaman aralığı">
        ${[7, 14, 30].map((days) => `
          <button class="segment ${days === adminState.windowDays ? 'active' : ''}"
                  type="button" data-window="${days}">${days} gün</button>`).join('')}
      </div>
      <button id="dashboardRefresh" class="btn btn-ghost btn-sm" type="button">Yenile</button>
    </div>

    <div class="kpi-grid">
      ${tiles.map(([label, value, foot]) => `
        <div class="kpi">
          <span class="kpi-head">${label}</span>
          <span class="kpi-value">${value}</span>
          <span class="kpi-foot">${escapeHtml(foot)}</span>
        </div>`).join('')}
    </div>

    <div class="card">
      <div class="section-head-inline">
        <h3>Günlük etkinlik</h3>
        <small class="muted">tamamlanan yarışma / benzersiz oyuncu</small>
      </div>
      ${activityChart(data.dailyActivity)}
    </div>

    <div class="admin-columns">
      <div class="card">
        <h3>Kategori doğruluk oranı</h3>
        ${accuracyChart(data.categories)}
      </div>

      <div class="card">
        <h3>Kategori dağılımı</h3>
        <table class="table">
          <thead><tr><th>Kategori</th><th>Soru</th><th>Oyun</th></tr></thead>
          <tbody>
            ${data.categories.map((category) => `
              <tr>
                <td>${escapeHtml(category.icon || '')} ${escapeHtml(category.name)}
                  ${category.isActive ? '' : '<span class="chip chip-muted">Kapalı</span>'}</td>
                <td>${category.questionCount}</td>
                <td>${category.competitionCount}</td>
              </tr>`).join('')}
          </tbody>
        </table>
      </div>
    </div>

    <div class="admin-columns">
      ${questionStatCard('En çok yanılınan sorular', data.hardestQuestions,
        `en az ${DASHBOARD_MIN_TIMES_ASKED} kez soruldu`)}
      ${questionStatCard('En kolay sorular', data.easiestQuestions, 'havuzu dengelemek için')}
    </div>

    <p class="dashboard-foot">
      Veri ${escapeHtml(dateTime(data.generatedAtUtc))} itibarıyla (kısa süreli önbelleklenir).
    </p>`;

  $('adminPanel').querySelectorAll('[data-window]').forEach((button) => {
    button.addEventListener('click', () => {
      adminState.windowDays = Number(button.dataset.window);
      showAdminTab('dashboard');
    });
  });

  $('dashboardRefresh').addEventListener('click', () => showAdminTab('dashboard'));
}

function questionStatCard(title, questions, note) {
  const body = questions.length === 0
    ? '<div class="empty">Yeterli veri toplanmadı.</div>'
    : `<ul class="stat-list">
        ${questions.map((question) => `
          <li>
            <div class="stat-list-main">
              <span class="stat-list-title" title="${escapeHtml(question.text)}">${escapeHtml(trim(question.text, 58))}</span>
              <small class="muted">${escapeHtml(question.categoryName)} ·
                ${escapeHtml(DIFFICULTY_LABELS[question.difficulty] ?? question.difficulty)} ·
                ${question.timesAsked} kez</small>
            </div>
            <div class="rate">
              <div class="rate-track"><div class="rate-fill" style="width:${question.successRate}%"></div></div>
              <span class="rate-value">%${question.successRate}</span>
            </div>
          </li>`).join('')}
      </ul>`;

  return `
    <div class="card">
      <div class="section-head-inline">
        <h3>${escapeHtml(title)}</h3>
        <small class="muted">${escapeHtml(note)}</small>
      </div>
      ${body}
    </div>`;
}

// ---------------------------------------------------------------------------
//  Sorular
// ---------------------------------------------------------------------------

async function ensureCategories() {
  if (adminState.categories.length === 0) {
    const result = await api('/api/categories?includeInactive=true');
    adminState.categories = result.data ?? [];
  }
  return adminState.categories;
}

async function loadAdminQuestions() {
  const categories = await ensureCategories();

  const query = adminState.questionFilter ? `&categoryId=${adminState.questionFilter}` : '';
  const result = await api(`/api/questions?page=1&pageSize=50${query}`);
  const questions = result.data?.items ?? [];

  $('adminPanel').innerHTML = `
    <div class="admin-toolbar">
      <div class="toolbar-group">
        <select id="questionCategoryFilter">
          <option value="">Tüm kategoriler</option>
          ${categories.map((category) => `
            <option value="${category.id}" ${category.id === adminState.questionFilter ? 'selected' : ''}>
              ${escapeHtml(category.name)}
            </option>`).join('')}
        </select>
        <small class="muted">${questions.length} soru</small>
      </div>
      <button id="newQuestionButton" class="btn btn-primary btn-sm" type="button">Yeni soru</button>
    </div>

    <div class="card">
      ${questions.length === 0 ? '<div class="empty">Bu filtreye uyan soru yok.</div>' : `
        <table class="table">
          <thead>
            <tr><th>Soru</th><th>Kategori</th><th>Zorluk</th><th>İstatistik</th><th></th></tr>
          </thead>
          <tbody>
            ${questions.map((question) => `
              <tr>
                <td>
                  <span title="${escapeHtml(question.text)}">${escapeHtml(trim(question.text, 64))}</span>
                  ${question.isActive ? '' : '<span class="chip chip-muted">Kapalı</span>'}
                </td>
                <td>${escapeHtml(question.categoryName)}</td>
                <td><span class="chip chip-${question.difficulty.toLowerCase()}">
                  ${escapeHtml(DIFFICULTY_LABELS[question.difficulty] ?? question.difficulty)}</span></td>
                <td class="muted">${question.timesAnsweredCorrectly}/${question.timesAsked}</td>
                <td class="row-actions">
                  <button class="btn btn-ghost btn-sm" type="button" data-edit="${question.id}">Düzenle</button>
                  <button class="btn btn-ghost btn-sm danger" type="button" data-delete="${question.id}">Sil</button>
                </td>
              </tr>`).join('')}
          </tbody>
        </table>`}
    </div>`;

  $('questionCategoryFilter').addEventListener('change', (event) => {
    adminState.questionFilter = event.target.value;
    showAdminTab('questions');
  });

  $('newQuestionButton').addEventListener('click', () => openQuestionDialog(null, categories));

  $('adminPanel').querySelectorAll('[data-edit]').forEach((button) => {
    const question = questions.find((q) => q.id === button.dataset.edit);
    button.addEventListener('click', () => openQuestionDialog(question, categories));
  });

  $('adminPanel').querySelectorAll('[data-delete]').forEach((button) => {
    const question = questions.find((q) => q.id === button.dataset.delete);
    button.addEventListener('click', () => confirmAction({
      title: 'Soru silinsin mi?',
      message: `“${trim(question.text, 70)}” kaldırılacak. Bu işlem geri alınamaz.`,
      confirmLabel: 'Sil',
      onConfirm: async () => {
        await api(`/api/questions/${question.id}`, { method: 'DELETE' });
        toast('Soru kaldırıldı.', 'success');
        await showAdminTab('questions');
      }
    }));
  });
}

function openQuestionDialog(question, categories) {
  const answers = question
    ? question.answers.map((answer) => ({ text: answer.text, isCorrect: answer.isCorrect }))
    : [{ text: '', isCorrect: true }, { text: '', isCorrect: false },
       { text: '', isCorrect: false }, { text: '', isCorrect: false }];

  openDialog({
    title: question ? 'Soruyu düzenle' : 'Yeni soru',
    body: `
      <div class="form">
        <label>Kategori
          <select name="categoryId">
            ${categories.map((category) => `
              <option value="${category.id}" ${category.id === question?.categoryId ? 'selected' : ''}>
                ${escapeHtml(category.name)}
              </option>`).join('')}
          </select>
        </label>

        <label>Soru metni
          <textarea name="text" rows="3">${escapeHtml(question?.text ?? '')}</textarea>
        </label>

        <div class="grid-2">
          <label>Zorluk
            <select name="difficulty">
              ${Object.entries(DIFFICULTY_LABELS).map(([value, label]) => `
                <option value="${value}" ${value === (question?.difficulty ?? 'Medium') ? 'selected' : ''}>
                  ${label}
                </option>`).join('')}
            </select>
          </label>
          <label>Süre (sn)
            <input name="timeLimitSeconds" type="number" min="5" max="120"
                   value="${question?.timeLimitSeconds ?? 20}">
          </label>
        </div>

        <label>Açıklama (isteğe bağlı)
          <textarea name="explanation" rows="2">${escapeHtml(question?.explanation ?? '')}</textarea>
        </label>

        <div class="answers">
          <div class="section-head-inline">
            <strong>Şıklar</strong>
            <button id="addAnswerButton" class="btn btn-ghost btn-sm" type="button">Şık ekle</button>
          </div>
          <small class="hint">Tam olarak bir şık doğru işaretlenmelidir.</small>
          <div id="answerRows"></div>
        </div>

        <div class="dialog-error hidden" role="alert"></div>
      </div>`,
    onConfirm: () => saveQuestion(question),
  });

  renderAnswerRows(answers);

  $('addAnswerButton').addEventListener('click', () => {
    const current = readAnswerRows();
    if (current.length >= 6) return;
    renderAnswerRows([...current, { text: '', isCorrect: false }]);
  });
}

/**
 * Şık satırlarını çizer.
 *
 * "Doğru" işaretleri radyo düğmesi gibi davranır: backend zaten "tam olarak
 * bir doğru şık" şartını doğruluyor, burada kullanıcıyı hataya hiç düşürmemek
 * için aynı kural arayüzde de uygulanıyor.
 */
function renderAnswerRows(answers) {
  $('answerRows').innerHTML = answers.map((answer, index) => `
    <div class="answer-row">
      <input type="radio" name="correctAnswer" value="${index}" ${answer.isCorrect ? 'checked' : ''}
             aria-label="${index + 1}. şık doğru">
      <input type="text" data-answer-text="${index}" value="${escapeHtml(answer.text)}"
             placeholder="Şık ${index + 1}">
      <button class="btn btn-ghost btn-sm danger" type="button" data-remove-answer="${index}"
              ${answers.length <= 2 ? 'disabled' : ''} aria-label="Şıkkı kaldır">×</button>
    </div>`).join('');

  $('answerRows').querySelectorAll('[data-remove-answer]').forEach((button) => {
    button.addEventListener('click', () => {
      const current = readAnswerRows();
      current.splice(Number(button.dataset.removeAnswer), 1);
      if (!current.some((answer) => answer.isCorrect)) {
        current[0].isCorrect = true;
      }
      renderAnswerRows(current);
    });
  });
}

function readAnswerRows() {
  const dialog = $('adminDialog');
  const checked = dialog.querySelector('[name="correctAnswer"]:checked')?.value;

  return Array.from(dialog.querySelectorAll('[data-answer-text]')).map((input, index) => ({
    text: input.value.trim(),
    isCorrect: String(index) === checked
  }));
}

async function saveQuestion(question) {
  const answers = readAnswerRows();
  const text = fieldValue('text');

  if (text.length < 10) {
    dialogError('Soru metni en az 10 karakter olmalıdır.');
    return false;
  }

  if (answers.some((answer) => !answer.text)) {
    dialogError('Şık metinleri boş olamaz.');
    return false;
  }

  if (answers.filter((answer) => answer.isCorrect).length !== 1) {
    dialogError('Tam olarak bir şık doğru işaretlenmelidir.');
    return false;
  }

  const payload = {
    categoryId: fieldValue('categoryId'),
    text,
    difficulty: fieldValue('difficulty'),
    timeLimitSeconds: Number(fieldValue('timeLimitSeconds')),
    explanation: fieldValue('explanation') || null,
    answers: answers.map((answer, index) => ({ ...answer, displayOrder: index + 1 }))
  };

  if (question) {
    await api(`/api/questions/${question.id}`, {
      method: 'PUT',
      body: { ...payload, isActive: question.isActive }
    });
  } else {
    await api('/api/questions', { method: 'POST', body: payload });
  }

  toast(question ? 'Soru güncellendi.' : 'Soru eklendi.', 'success');
  adminState.categories = [];
  await showAdminTab('questions');
  return true;
}

// ---------------------------------------------------------------------------
//  Kategoriler
// ---------------------------------------------------------------------------

async function loadAdminCategories() {
  adminState.categories = [];
  const categories = await ensureCategories();

  $('adminPanel').innerHTML = `
    <div class="admin-toolbar">
      <small class="muted">${categories.length} kategori</small>
      <button id="newCategoryButton" class="btn btn-primary btn-sm" type="button">Yeni kategori</button>
    </div>

    <div class="card">
      <table class="table">
        <thead><tr><th></th><th>Ad</th><th>Soru</th><th>Durum</th><th></th></tr></thead>
        <tbody>
          ${categories.map((category) => `
            <tr>
              <td class="icon-cell">${escapeHtml(category.icon || '')}</td>
              <td><strong>${escapeHtml(category.name)}</strong><br><small class="muted">${escapeHtml(category.slug)}</small></td>
              <td>${category.questionCount}</td>
              <td>${category.isActive
                ? '<span class="chip chip-ok">Aktif</span>'
                : '<span class="chip chip-muted">Kapalı</span>'}</td>
              <td class="row-actions">
                <button class="btn btn-ghost btn-sm" type="button" data-edit-category="${category.id}">Düzenle</button>
              </td>
            </tr>`).join('')}
        </tbody>
      </table>
    </div>`;

  $('newCategoryButton').addEventListener('click', () => openCategoryDialog(null));

  $('adminPanel').querySelectorAll('[data-edit-category]').forEach((button) => {
    const category = categories.find((c) => c.id === button.dataset.editCategory);
    button.addEventListener('click', () => openCategoryDialog(category));
  });
}

function openCategoryDialog(category) {
  openDialog({
    title: category ? 'Kategoriyi düzenle' : 'Yeni kategori',
    body: `
      <div class="form">
        <label>Ad
          <input name="name" type="text" maxlength="64" value="${escapeHtml(category?.name ?? '')}">
        </label>
        <label>Açıklama
          <textarea name="description" rows="2">${escapeHtml(category?.description ?? '')}</textarea>
        </label>
        <div class="grid-2">
          <label>İkon (emoji)
            <input name="icon" type="text" maxlength="8" value="${escapeHtml(category?.icon ?? '')}">
          </label>
          <label>Renk (#RRGGBB)
            <input name="colorHex" type="text" maxlength="9" value="${escapeHtml(category?.colorHex ?? '')}">
          </label>
        </div>
        ${category ? `
          <label class="check">
            <input name="isActive" type="checkbox" ${category.isActive ? 'checked' : ''}> Aktif
          </label>` : ''}
        <div class="dialog-error hidden" role="alert"></div>
      </div>`,
    onConfirm: async () => {
      const name = fieldValue('name');
      if (name.length < 2) {
        dialogError('Kategori adı en az 2 karakter olmalıdır.');
        return false;
      }

      const colorHex = fieldValue('colorHex');
      if (colorHex && !/^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$/.test(colorHex)) {
        dialogError('Renk #RRGGBB biçiminde olmalıdır.');
        return false;
      }

      const payload = {
        name,
        description: fieldValue('description') || null,
        icon: fieldValue('icon') || null,
        colorHex: colorHex || null,
        displayOrder: category?.displayOrder ?? adminState.categories.length + 1
      };

      if (category) {
        await api(`/api/categories/${category.id}`, {
          method: 'PUT',
          body: { ...payload, isActive: field('isActive').checked }
        });
      } else {
        await api('/api/categories', { method: 'POST', body: payload });
      }

      toast(category ? 'Kategori güncellendi.' : 'Kategori eklendi.', 'success');
      await showAdminTab('categories');
      return true;
    }
  });
}

// ---------------------------------------------------------------------------
//  Etkinlikler
// ---------------------------------------------------------------------------

async function loadAdminEvents() {
  const categories = await ensureCategories();
  const result = await api(`/api/events?page=${adminState.eventPage}&pageSize=${EVENT_PAGE_SIZE}`);
  const page = result.data;
  const events = page?.items ?? [];

  $('adminPanel').innerHTML = `
    <div class="admin-toolbar">
      <small class="muted">${page?.totalCount ?? 0} etkinlik</small>
      <button id="newEventButton" class="btn btn-primary btn-sm" type="button">Yeni etkinlik</button>
    </div>

    <div class="card">
      ${events.length === 0 ? '<div class="empty">Henüz etkinlik oluşturulmadı.</div>' : `
        <table class="table">
          <thead>
            <tr><th>Etkinlik</th><th>Kategori</th><th>Başlangıç</th><th>Kayıt</th><th>Durum</th><th></th></tr>
          </thead>
          <tbody>
            ${events.map((item) => `
              <tr>
                <td>
                  <strong>${escapeHtml(item.name)}</strong>
                  ${item.description ? `<br><small class="muted">${escapeHtml(trim(item.description, 52))}</small>` : ''}
                </td>
                <td>${escapeHtml(item.categoryIcon || '')} ${escapeHtml(item.categoryName)}</td>
                <td>${escapeHtml(dateTime(item.scheduledStartUtc))}<br>
                    <small class="muted">${escapeHtml(relativeTime(item.scheduledStartUtc))}</small></td>
                <td>${item.registeredCount}/${item.maxPlayers}</td>
                <td><span class="chip chip-${item.status.toLowerCase()}">
                  ${escapeHtml(ROOM_STATUS_LABELS[item.status] ?? item.status)}</span></td>
                <td class="row-actions">
                  ${item.status === 'Waiting' ? `
                    <button class="btn btn-ghost btn-sm" type="button" data-edit-event="${item.id}">Düzenle</button>
                    <button class="btn btn-ghost btn-sm danger" type="button" data-cancel-event="${item.id}">İptal</button>
                  ` : '<span class="muted">—</span>'}
                </td>
              </tr>`).join('')}
          </tbody>
        </table>`}
      ${pager(page, 'event')}
    </div>`;

  $('newEventButton').addEventListener('click', () => openEventDialog(null, categories));
  bindPager('event', (target) => { adminState.eventPage = target; showAdminTab('events'); });

  $('adminPanel').querySelectorAll('[data-edit-event]').forEach((button) => {
    const item = events.find((e) => e.id === button.dataset.editEvent);
    button.addEventListener('click', () => openEventDialog(item, categories));
  });

  $('adminPanel').querySelectorAll('[data-cancel-event]').forEach((button) => {
    const item = events.find((e) => e.id === button.dataset.cancelEvent);
    button.addEventListener('click', () => confirmAction({
      title: 'Etkinlik iptal edilsin mi?',
      message: item.registeredCount > 0
        ? `“${item.name}” iptal edilecek. ${item.registeredCount} kayıtlı oyuncu etkilenecek.`
        : `“${item.name}” iptal edilecek.`,
      confirmLabel: 'İptal et',
      onConfirm: async () => {
        await api(`/api/events/${item.id}/cancel`, { method: 'POST' });
        toast('Etkinlik iptal edildi.', 'success');
        await showAdminTab('events');
      }
    }));
  });
}

function openEventDialog(item, categories) {
  const start = item ? new Date(item.scheduledStartUtc) : defaultEventStart();

  openDialog({
    title: item ? 'Etkinliği düzenle' : 'Yeni etkinlik',
    body: `
      <div class="form">
        <label>Ad
          <input name="name" type="text" maxlength="64" value="${escapeHtml(item?.name ?? '')}">
        </label>
        <label>Açıklama
          <textarea name="description" rows="2">${escapeHtml(item?.description ?? '')}</textarea>
        </label>
        <label>Kategori
          <select name="categoryId">
            ${categories.map((category) => `
              <option value="${category.id}" ${category.id === item?.categoryId ? 'selected' : ''}>
                ${escapeHtml(category.name)}
              </option>`).join('')}
          </select>
        </label>
        <label>Başlangıç (yerel saatiniz)
          <input name="scheduledStart" type="datetime-local" value="${toLocalInputValue(start)}">
          <small class="hint" id="utcPreview"></small>
        </label>
        <div class="grid-3">
          <label>Soru sayısı
            <input name="questionCount" type="number" min="5" max="30" value="${item?.questionCount ?? 10}">
          </label>
          <label>Süre (sn)
            <input name="secondsPerQuestion" type="number" min="5" max="60" value="${item?.secondsPerQuestion ?? 20}">
          </label>
          <label>Kontenjan
            <input name="maxPlayers" type="number" min="2" max="8" value="${item?.maxPlayers ?? 8}">
          </label>
        </div>
        <div class="dialog-error hidden" role="alert"></div>
      </div>`,
    onConfirm: async () => {
      const name = fieldValue('name');
      if (name.length < 3) {
        dialogError('Etkinlik adı en az 3 karakter olmalıdır.');
        return false;
      }

      const localStart = field('scheduledStart').value;
      if (!localStart) {
        dialogError('Başlangıç zamanı zorunludur.');
        return false;
      }

      const scheduled = new Date(localStart);
      if (scheduled.getTime() <= Date.now()) {
        dialogError('Başlangıç gelecekte bir zaman olmalıdır.');
        return false;
      }

      const payload = {
        categoryId: fieldValue('categoryId'),
        name,
        description: fieldValue('description') || null,
        // Yerel saat → UTC. Bu dönüşüm atlanırsa etkinlik, saat dilimi farkı
        // kadar yanlış anda başlar; UTC+0'daki bir makinede hiç fark edilmez.
        scheduledStartUtc: scheduled.toISOString(),
        questionCount: Number(fieldValue('questionCount')),
        secondsPerQuestion: Number(fieldValue('secondsPerQuestion')),
        maxPlayers: Number(fieldValue('maxPlayers'))
      };

      if (item) {
        await api(`/api/events/${item.id}`, { method: 'PUT', body: payload });
      } else {
        await api('/api/events', { method: 'POST', body: payload });
      }

      toast(item ? 'Etkinlik güncellendi.' : 'Etkinlik oluşturuldu.', 'success');
      await showAdminTab('events');
      return true;
    }
  });

  const input = field('scheduledStart');
  const preview = () => {
    const value = input.value ? new Date(input.value) : null;
    $('utcPreview').textContent = value && !Number.isNaN(value.getTime())
      ? `Sunucuya ${value.toISOString().slice(0, 16).replace('T', ' ')} (UTC) olarak gönderilecek.`
      : 'Sunucu tarafında UTC olarak saklanır.';
  };

  input.addEventListener('input', preview);
  preview();
}

/** Varsayılan başlangıç: yarın 21:00 (yerel). */
function defaultEventStart() {
  const date = new Date();
  date.setDate(date.getDate() + 1);
  date.setHours(21, 0, 0, 0);
  return date;
}

// ---------------------------------------------------------------------------
//  Kullanıcılar
// ---------------------------------------------------------------------------

async function loadAdminUsers() {
  const search = adminState.userSearch ? `&search=${encodeURIComponent(adminState.userSearch)}` : '';
  const result = await api(`/api/users?page=${adminState.userPage}&pageSize=${USER_PAGE_SIZE}${search}`);
  const page = result.data;
  const users = page?.items ?? [];

  $('adminPanel').innerHTML = `
    <div class="admin-toolbar">
      <input id="userSearchInput" type="search" placeholder="Ad, takma ad veya e-posta"
             value="${escapeHtml(adminState.userSearch)}" aria-label="Kullanıcı ara">
      <small class="muted">${page?.totalCount ?? 0} kullanıcı</small>
    </div>

    <div class="card">
      ${users.length === 0 ? '<div class="empty">Aramaya uyan kullanıcı yok.</div>' : `
        <table class="table">
          <thead>
            <tr><th>Kullanıcı</th><th>E-posta</th><th>Yetkiler</th><th>Durum</th><th>Son giriş</th><th></th></tr>
          </thead>
          <tbody>
            ${users.map((user) => renderUserRow(user)).join('')}
          </tbody>
        </table>`}
      ${pager(page, 'user')}
    </div>`;

  bindUserSearch();
  bindPager('user', (target) => { adminState.userPage = target; showAdminTab('users'); });

  $('adminPanel').querySelectorAll('[data-toggle-active]').forEach((button) => {
    const user = users.find((u) => u.id === button.dataset.toggleActive);
    button.addEventListener('click', () => confirmSetActive(user));
  });

  $('adminPanel').querySelectorAll('[data-unlock]').forEach((button) => {
    const user = users.find((u) => u.id === button.dataset.unlock);
    button.addEventListener('click', async () => {
      await withButtonBusy(button, async () => {
        await api(`/api/users/${user.id}/unlock`, { method: 'POST' });
        toast(`${user.nickname} yeniden giriş deneyebilir.`, 'success');
        await showAdminTab('users');
      });
    });
  });

  $('adminPanel').querySelectorAll('[data-claims]').forEach((button) => {
    const user = users.find((u) => u.id === button.dataset.claims);
    button.addEventListener('click', () => openClaimsDialog(user));
  });
}

function isLocked(user) {
  return user.lockoutEndUtc !== null && new Date(user.lockoutEndUtc).getTime() > Date.now();
}

function renderUserRow(user) {
  const self = user.id === state.user?.id;

  const statusChip = isLocked(user)
    ? `<span class="chip chip-warn" title="${user.accessFailedCount} hatalı giriş">🔒 Kilitli</span>`
    : user.isActive
      ? '<span class="chip chip-ok">Aktif</span>'
      : '<span class="chip chip-muted">Pasif</span>';

  const roleChips = user.roles.length === 0
    ? '<span class="muted">Oyuncu</span>'
    : user.roles.map((role) => `
        <span class="chip ${role === ROLES.admin ? 'chip-danger' : 'chip-info'}">
          ${escapeHtml(ROLE_LABELS[role] ?? role)}
        </span>`).join(' ');

  const actions = self
    ? '<span class="muted small">Bu sizsiniz</span>'
    : `
      <button class="btn btn-ghost btn-sm" type="button" data-toggle-active="${user.id}">
        ${user.isActive ? 'Pasifleştir' : 'Etkinleştir'}
      </button>
      ${isLocked(user) ? `<button class="btn btn-ghost btn-sm" type="button" data-unlock="${user.id}">Kilidi aç</button>` : ''}
      ${isAdmin() ? `<button class="btn btn-ghost btn-sm" type="button" data-claims="${user.id}">Yetkiler</button>` : ''}`;

  return `
    <tr>
      <td><strong>${escapeHtml(user.nickname)}</strong><br>
          <small class="muted">${escapeHtml(user.firstName)} ${escapeHtml(user.lastName)}</small></td>
      <td class="muted small">${escapeHtml(user.email)}</td>
      <td class="chip-cell">${roleChips}</td>
      <td>${statusChip}</td>
      <td class="muted small">${user.lastLoginAtUtc ? escapeHtml(dateTime(user.lastLoginAtUtc)) : '—'}</td>
      <td class="row-actions">${actions}</td>
    </tr>`;
}

/**
 * Arama kutusu.
 *
 * Tuş başına istek atmamak için kısa bir gecikme var; ayrıca arama değişince
 * ilk sayfaya dönülüyor — 3. sayfada arama yapıp boş sonuç görmek kullanıcıyı
 * "kayıt yok" sanmaya iter.
 */
function bindUserSearch() {
  const input = $('userSearchInput');
  let handle = null;

  input.addEventListener('input', () => {
    clearTimeout(handle);
    handle = setTimeout(() => {
      adminState.userSearch = input.value.trim();
      adminState.userPage = 1;
      showAdminTab('users');
    }, 350);
  });
}

function confirmSetActive(user) {
  const next = !user.isActive;

  confirmAction({
    title: next ? 'Hesap etkinleştirilsin mi?' : 'Hesap pasifleştirilsin mi?',
    message: next
      ? `${user.nickname} yeniden giriş yapabilecek.`
      : `${user.nickname} artık giriş yapamayacak. Mevcut oturumu, jetonu yenilenene kadar sürer.`,
    confirmLabel: next ? 'Etkinleştir' : 'Pasifleştir',
    onConfirm: async () => {
      await api(`/api/users/${user.id}/active?isActive=${next}`, { method: 'PATCH' });
      toast(`${user.nickname} hesabı güncellendi.`, 'success');
      await showAdminTab('users');
    }
  });
}

/**
 * Yetki diyaloğu.
 *
 * Yalnızca `Admin` açabilir: yetki dağıtabilmek, kendine yetki verebilmek
 * demektir. Backend'de `OperationClaimsController` da tamamen Admin'e kapalı.
 */
async function openClaimsDialog(user) {
  if (adminState.claims.length === 0) {
    const result = await api('/api/operationclaims');
    adminState.claims = result.data ?? [];
  }

  const rows = adminState.claims.map((claim) => `
    <label class="claim-row">
      <input type="checkbox" data-claim="${claim.id}" data-claim-name="${escapeHtml(claim.name)}"
             ${user.roles.includes(claim.name) ? 'checked' : ''}>
      <span>
        <strong>${escapeHtml(ROLE_LABELS[claim.name] ?? claim.name)}</strong>
        <small class="muted">${escapeHtml(claim.description ?? claim.name)}</small>
      </span>
    </label>`).join('');

  openDialog({
    title: `Yetkiler — ${user.nickname}`,
    body: `
      <p class="dialog-text">
        Değişiklik anında uygulanır ve kullanıcının <strong>bir sonraki jeton
        yenilemesinde</strong> etkili olur.
      </p>
      <div class="claim-list">${rows || '<div class="empty">Tanımlı yetki yok.</div>'}</div>`,
    confirmLabel: 'Kapat',
    onConfirm: async () => {
      await showAdminTab('users');
      return true;
    }
  });

  // Her kutu anında uygulanır: "kaydet" beklemek, kısmen uygulanmış bir
  // yetki kümesiyle diyalogdan çıkma ihtimali doğururdu.
  $('adminDialog').querySelectorAll('[data-claim]').forEach((box) => {
    box.addEventListener('change', async () => {
      const body = { userId: user.id, operationClaimId: box.dataset.claim };
      const path = box.checked ? '/api/operationclaims/assign' : '/api/operationclaims/revoke';

      try {
        await api(path, { method: 'POST', body });
        toast(`${box.dataset.claimName} ${box.checked ? 'verildi' : 'kaldırıldı'}.`, 'success');
      } catch (error) {
        // Sunucu reddetti (ör. yönetici kendi Admin yetkisini kaldıramaz):
        // kutuyu eski hâline döndür.
        box.checked = !box.checked;
        toast(error.message, 'error');
      }
    });
  });
}

// ---------------------------------------------------------------------------
//  Sayfalama
// ---------------------------------------------------------------------------

function pager(page, prefix) {
  if (!page || page.totalPages <= 1) {
    return '';
  }

  return `
    <div class="pager">
      <button class="btn btn-ghost btn-sm" type="button" data-page-${prefix}="${page.page - 1}"
              ${page.hasPrevious ? '' : 'disabled'}>Önceki</button>
      <span class="muted small">${page.page} / ${page.totalPages}</span>
      <button class="btn btn-ghost btn-sm" type="button" data-page-${prefix}="${page.page + 1}"
              ${page.hasNext ? '' : 'disabled'}>Sonraki</button>
    </div>`;
}

function bindPager(prefix, onNavigate) {
  $('adminPanel').querySelectorAll(`[data-page-${prefix}]`).forEach((button) => {
    button.addEventListener('click', () => onNavigate(Number(button.dataset[`page${prefix[0].toUpperCase()}${prefix.slice(1)}`])));
  });
}

// ---------------------------------------------------------------------------
//  Oyuncu tarafı: yaklaşan etkinlikler
// ---------------------------------------------------------------------------

async function loadUpcomingEvents() {
  const container = $('upcomingEvents');

  try {
    const result = await api('/api/events/upcoming');
    const events = result.data ?? [];

    container.innerHTML = events.length === 0
      ? '<div class="empty">Şu anda planlanmış etkinlik yok.</div>'
      : events.map((item) => `
          <div class="event">
            <div class="event-info">
              <strong>${escapeHtml(item.categoryIcon || '')} ${escapeHtml(item.name)}</strong>
              ${item.description ? `<small class="muted">${escapeHtml(trim(item.description, 60))}</small>` : ''}
              <small class="muted">${escapeHtml(shortDate(item.scheduledStartUtc))},
                ${escapeHtml(new Date(item.scheduledStartUtc).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' }))}
                · ${item.registeredCount}/${item.maxPlayers} oyuncu · ${item.questionCount} soru</small>
            </div>
            <div class="event-actions">
              ${item.isRegistered
                ? `<span class="chip chip-ok">Kayıtlısınız</span>
                   <button class="btn btn-ghost btn-sm" type="button" data-withdraw="${item.id}">Vazgeç</button>`
                : item.canRegister
                  ? `<button class="btn btn-primary btn-sm" type="button" data-register="${item.id}">Kaydol</button>`
                  : '<span class="chip chip-muted">Kontenjan dolu</span>'}
            </div>
          </div>`).join('');

    container.querySelectorAll('[data-register]').forEach((button) => {
      button.addEventListener('click', () => withButtonBusy(button, async () => {
        await api(`/api/events/${button.dataset.register}/register`, { method: 'POST' });
        toast('Etkinliğe kaydoldunuz.', 'success');
        await loadUpcomingEvents();
      }));
    });

    container.querySelectorAll('[data-withdraw]').forEach((button) => {
      button.addEventListener('click', () => withButtonBusy(button, async () => {
        await api(`/api/events/${button.dataset.withdraw}/register`, { method: 'DELETE' });
        toast('Etkinlik kaydınız iptal edildi.', 'success');
        await loadUpcomingEvents();
      }));
    });
  } catch {
    // Etkinlikler ana sayfanın yardımcı bir bölümü; hata sayfayı durdurmamalı.
    container.innerHTML = '<div class="empty">Etkinlikler yüklenemedi.</div>';
  }
}

$('refreshEventsButton').addEventListener('click', loadUpcomingEvents);
