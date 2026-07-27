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

// Etiketler dile bağlı olduğu için sabit tablo yerine çeviriden okunuyor;
// sunucudan bilinmeyen bir değer gelirse ham hâli gösterilir.
const roleLabel = (role) =>
  (Object.values(ROLES).includes(role) ? t(`role.${role}`) : role);

const ROOM_STATUSES = ['Waiting', 'InProgress', 'Finished', 'Cancelled'];
const roomStatusLabel = (status) =>
  (ROOM_STATUSES.includes(status) ? t(`roomStatus.${status}`) : status);

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
    toast(t('admin.noAccess'), 'error');
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
    return t('admin.subtitleFull');
  }

  const parts = [];
  if (canManageContent()) parts.push(t('admin.areaContent'));
  if (canManageEvents()) parts.push(t('admin.areaEvents'));
  if (canManageUsers()) parts.push(t('admin.areaUsers'));
  return t('admin.subtitlePartial', { areas: parts.join(', ') });
}

function availableTabs() {
  const tabs = [];
  if (isAdmin()) tabs.push({ id: 'dashboard', label: t('admin.tabDashboard') });
  if (canManageContent()) {
    tabs.push({ id: 'questions', label: t('admin.tabQuestions') });
    tabs.push({ id: 'categories', label: t('admin.tabCategories') });
  }
  if (canManageEvents()) tabs.push({ id: 'events', label: t('admin.tabEvents') });
  if (canManageUsers()) tabs.push({ id: 'users', label: t('admin.tabUsers') });
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

  $('adminPanel').innerHTML = `<div class="empty">${t('common.loading')}</div>`;

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

function openDialog({ title, body, confirmLabel, danger = false, onConfirm }) {
  $('adminDialogTitle').textContent = title;
  $('adminDialogBody').innerHTML = body;

  const confirmButton = $('adminDialogConfirm');
  confirmButton.textContent = confirmLabel ?? t('common.save');
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
    return `<div class="empty">${t('dash.activityEmpty')}</div>`;
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
          rx="3"><title>${escapeHtml(t('dash.barTooltip', {
            date: shortDate(point.date), count: point.competitions
          }))}</title></rect>
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
      <span><i class="swatch swatch-bar"></i>${t('dash.legendGames')}</span>
      <span><i class="swatch swatch-line"></i>${t('dash.legendPlayers')}</span>
    </div>
    <svg class="chart" viewBox="0 0 ${width} ${height}" role="img"
         aria-label="${escapeHtml(t('dash.a11yActivity'))}">
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
    return `<div class="empty">${t('dash.categoryAccuracyEmpty')}</div>`;
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
            fill="${escapeHtml(color)}"><title>${escapeHtml(category.name)}: ${percent(category.accuracyPercentage)}</title></rect>
      <text class="chart-label" x="${width - 4}" y="${top + 19}" text-anchor="end">${percent(category.accuracyPercentage)}</text>`;
  }).join('');

  return `
    <svg class="chart" viewBox="0 0 ${width} ${rows.length * rowHeight}" role="img"
         aria-label="${escapeHtml(t('dash.a11yAccuracy'))}">${bars}</svg>`;
}

// ---------------------------------------------------------------------------
//  Biçimlendirme yardımcıları
// ---------------------------------------------------------------------------

const number = (value) => Number(value ?? 0).toLocaleString(locale());

function trim(text, max) {
  const value = String(text ?? '');
  return value.length > max ? `${value.slice(0, max - 1)}…` : value;
}

function shortDate(iso) {
  return new Date(iso).toLocaleDateString(locale(), { day: '2-digit', month: 'short' });
}

function dateTime(iso) {
  return new Date(iso).toLocaleString(locale(), {
    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit'
  });
}

/** "3 saat sonra" / "2 gün önce". */
function relativeTime(iso) {
  const diffMinutes = Math.round((new Date(iso).getTime() - Date.now()) / 60000);
  const formatter = new Intl.RelativeTimeFormat(locale(), { numeric: 'auto' });

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
    [t('dash.users'), number(summary.totalUsers), summary.lockedUsers > 0
      ? t('dash.usersFootLocked', { active: summary.activeUsers, locked: summary.lockedUsers })
      : t('dash.usersFoot', { active: summary.activeUsers })],
    [t('dash.newUsers'), number(summary.newUsersInWindow),
      t('dash.newUsersFoot', { days: data.windowDays })],
    [t('dash.games'), number(summary.finishedCompetitions),
      t('dash.gamesFoot', { days: data.windowDays, count: summary.competitionsInWindow })],
    [t('dash.liveRooms'), number(summary.liveRooms), t('dash.liveRoomsFoot')],
    [t('dash.questions'), number(summary.activeQuestions),
      t('dash.questionsFoot', { total: summary.totalQuestions, categories: summary.activeCategories })],
    [t('dash.accuracy'), percent(summary.overallAccuracy),
      t('dash.accuracyFoot', { count: number(summary.totalAnswers) })]
  ];

  $('adminPanel').innerHTML = `
    <div class="admin-toolbar">
      <div class="segmented" role="group" aria-label="${escapeHtml(t('admin.timeRange'))}">
        ${[7, 14, 30].map((days) => `
          <button class="segment ${days === adminState.windowDays ? 'active' : ''}"
                  type="button" data-window="${days}">${t('dash.days', { count: days })}</button>`).join('')}
      </div>
      <button id="dashboardRefresh" class="btn btn-ghost btn-sm" type="button">${t('common.refresh')}</button>
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
        <h3>${t('dash.activity')}</h3>
        <small class="muted">${t('dash.activityNote')}</small>
      </div>
      ${activityChart(data.dailyActivity)}
    </div>

    <div class="admin-columns">
      <div class="card">
        <h3>${t('dash.categoryAccuracy')}</h3>
        ${accuracyChart(data.categories)}
      </div>

      <div class="card">
        <h3>${t('dash.categoryBreakdown')}</h3>
        <table class="table">
          <thead><tr><th>${t('dash.colCategory')}</th><th>${t('dash.colQuestions')}</th>
                     <th>${t('dash.colGames')}</th></tr></thead>
          <tbody>
            ${data.categories.map((category) => `
              <tr>
                <td>${escapeHtml(category.icon || '')} ${escapeHtml(category.name)}
                  ${category.isActive ? '' : `<span class="chip chip-muted">${t('common.closed')}</span>`}</td>
                <td>${category.questionCount}</td>
                <td>${category.competitionCount}</td>
              </tr>`).join('')}
          </tbody>
        </table>
      </div>
    </div>

    <div class="admin-columns">
      ${questionStatCard(t('dash.hardest'), data.hardestQuestions,
        t('dash.hardestNote', { count: DASHBOARD_MIN_TIMES_ASKED }))}
      ${questionStatCard(t('dash.easiest'), data.easiestQuestions, t('dash.easiestNote'))}
    </div>

    <p class="dashboard-foot">
      ${escapeHtml(t('dash.generatedAt', { time: dateTime(data.generatedAtUtc) }))}
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
    ? `<div class="empty">${t('dash.notEnoughData')}</div>`
    : `<ul class="stat-list">
        ${questions.map((question) => `
          <li>
            <div class="stat-list-main">
              <span class="stat-list-title" title="${escapeHtml(question.text)}">${escapeHtml(trim(question.text, 58))}</span>
              <small class="muted">${escapeHtml(question.categoryName)} ·
                ${escapeHtml(difficultyLabel(question.difficulty))} ·
                ${escapeHtml(t('q.timesAsked', { count: question.timesAsked }))}</small>
            </div>
            <div class="rate">
              <div class="rate-track"><div class="rate-fill" style="width:${question.successRate}%"></div></div>
              <span class="rate-value">${percent(question.successRate)}</span>
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
          <option value="">${t('q.allCategories')}</option>
          ${categories.map((category) => `
            <option value="${category.id}" ${category.id === adminState.questionFilter ? 'selected' : ''}>
              ${escapeHtml(category.name)}
            </option>`).join('')}
        </select>
        <small class="muted">${t('q.count', { count: questions.length })}</small>
      </div>
      <button id="newQuestionButton" class="btn btn-primary btn-sm" type="button">${t('q.new')}</button>
    </div>

    <div class="card">
      ${questions.length === 0 ? `<div class="empty">${t('q.empty')}</div>` : `
        <table class="table">
          <thead>
            <tr><th>${t('q.colQuestion')}</th><th>${t('q.colCategory')}</th>
                <th>${t('q.colDifficulty')}</th><th>${t('q.colStats')}</th><th></th></tr>
          </thead>
          <tbody>
            ${questions.map((question) => `
              <tr>
                <td>
                  <span title="${escapeHtml(question.text)}">${escapeHtml(trim(question.text, 64))}</span>
                  ${question.isActive ? '' : `<span class="chip chip-muted">${t('common.closed')}</span>`}
                </td>
                <td>${escapeHtml(question.categoryName)}</td>
                <td><span class="chip chip-${question.difficulty.toLowerCase()}">
                  ${escapeHtml(difficultyLabel(question.difficulty))}</span></td>
                <td class="muted">${question.timesAnsweredCorrectly}/${question.timesAsked}</td>
                <td class="row-actions">
                  <button class="btn btn-ghost btn-sm" type="button"
                          data-edit="${question.id}">${t('common.edit')}</button>
                  <button class="btn btn-ghost btn-sm danger" type="button"
                          data-delete="${question.id}">${t('common.delete')}</button>
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
      title: t('q.deleteTitle'),
      message: t('q.deleteMessage', { text: trim(question.text, 70) }),
      confirmLabel: t('common.delete'),
      onConfirm: async () => {
        await api(`/api/questions/${question.id}`, { method: 'DELETE' });
        toast(t('q.deleted'), 'success');
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
    title: question ? t('q.editTitle') : t('q.newTitle'),
    body: `
      <div class="form">
        <label><span>${t('q.category')}</span>
          <select name="categoryId">
            ${categories.map((category) => `
              <option value="${category.id}" ${category.id === question?.categoryId ? 'selected' : ''}>
                ${escapeHtml(category.name)}
              </option>`).join('')}
          </select>
        </label>

        <label><span>${t('q.text')}</span>
          <textarea name="text" rows="3">${escapeHtml(question?.text ?? '')}</textarea>
        </label>

        <div class="grid-2">
          <label><span>${t('q.difficulty')}</span>
            <select name="difficulty">
              ${DIFFICULTY_VALUES.map((value) => `
                <option value="${value}" ${value === (question?.difficulty ?? 'Medium') ? 'selected' : ''}>
                  ${difficultyLabel(value)}
                </option>`).join('')}
            </select>
          </label>
          <label><span>${t('q.duration')}</span>
            <input name="timeLimitSeconds" type="number" min="5" max="120"
                   value="${question?.timeLimitSeconds ?? 20}">
          </label>
        </div>

        <label><span>${t('q.explanation')}</span>
          <textarea name="explanation" rows="2">${escapeHtml(question?.explanation ?? '')}</textarea>
        </label>

        <div class="answers">
          <div class="section-head-inline">
            <strong>${t('q.options')}</strong>
            <button id="addAnswerButton" class="btn btn-ghost btn-sm"
                    type="button">${t('q.addOption')}</button>
          </div>
          <small class="hint">${t('q.optionsHint')}</small>
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
             aria-label="${escapeHtml(t('q.optionCorrectLabel', { number: index + 1 }))}">
      <input type="text" data-answer-text="${index}" value="${escapeHtml(answer.text)}"
             placeholder="${escapeHtml(t('q.optionPlaceholder', { number: index + 1 }))}">
      <button class="btn btn-ghost btn-sm danger" type="button" data-remove-answer="${index}"
              ${answers.length <= 2 ? 'disabled' : ''}
              aria-label="${escapeHtml(t('q.removeOption'))}">×</button>
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
    dialogError(t('q.textTooShort'));
    return false;
  }

  if (answers.some((answer) => !answer.text)) {
    dialogError(t('q.optionTextRequired'));
    return false;
  }

  if (answers.filter((answer) => answer.isCorrect).length !== 1) {
    dialogError(t('q.oneCorrectRequired'));
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

  toast(question ? t('q.updated') : t('q.created'), 'success');
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
      <small class="muted">${t('cat.count', { count: categories.length })}</small>
      <button id="newCategoryButton" class="btn btn-primary btn-sm" type="button">${t('cat.new')}</button>
    </div>

    <div class="card">
      <table class="table">
        <thead><tr><th></th><th>${t('cat.colName')}</th><th>${t('cat.colQuestions')}</th>
                   <th>${t('cat.colStatus')}</th><th></th></tr></thead>
        <tbody>
          ${categories.map((category) => `
            <tr>
              <td class="icon-cell">${escapeHtml(category.icon || '')}</td>
              <td><strong>${escapeHtml(category.name)}</strong><br><small class="muted">${escapeHtml(category.slug)}</small></td>
              <td>${category.questionCount}</td>
              <td>${category.isActive
                ? `<span class="chip chip-ok">${t('common.active')}</span>`
                : `<span class="chip chip-muted">${t('common.closed')}</span>`}</td>
              <td class="row-actions">
                <button class="btn btn-ghost btn-sm" type="button"
                        data-edit-category="${category.id}">${t('common.edit')}</button>
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
    title: category ? t('cat.editTitle') : t('cat.newTitle'),
    body: `
      <div class="form">
        <label><span>${t('cat.name')}</span>
          <input name="name" type="text" maxlength="64" value="${escapeHtml(category?.name ?? '')}">
        </label>
        <label><span>${t('cat.description')}</span>
          <textarea name="description" rows="2">${escapeHtml(category?.description ?? '')}</textarea>
        </label>
        <div class="grid-2">
          <label><span>${t('cat.icon')}</span>
            <input name="icon" type="text" maxlength="8" value="${escapeHtml(category?.icon ?? '')}">
          </label>
          <label><span>${t('cat.color')}</span>
            <input name="colorHex" type="text" maxlength="9" value="${escapeHtml(category?.colorHex ?? '')}">
          </label>
        </div>
        ${category ? `
          <label class="check">
            <input name="isActive" type="checkbox" ${category.isActive ? 'checked' : ''}>
            ${t('cat.activeLabel')}
          </label>` : ''}
        <div class="dialog-error hidden" role="alert"></div>
      </div>`,
    onConfirm: async () => {
      const name = fieldValue('name');
      if (name.length < 2) {
        dialogError(t('cat.nameTooShort'));
        return false;
      }

      const colorHex = fieldValue('colorHex');
      if (colorHex && !/^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$/.test(colorHex)) {
        dialogError(t('cat.colorInvalid'));
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

      toast(category ? t('cat.updated') : t('cat.created'), 'success');
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
      <small class="muted">${t('ev.count', { count: page?.totalCount ?? 0 })}</small>
      <button id="newEventButton" class="btn btn-primary btn-sm" type="button">${t('ev.new')}</button>
    </div>

    <div class="card">
      ${events.length === 0 ? `<div class="empty">${t('ev.empty')}</div>` : `
        <table class="table">
          <thead>
            <tr><th>${t('ev.colEvent')}</th><th>${t('ev.colCategory')}</th><th>${t('ev.colStart')}</th>
                <th>${t('ev.colRegistered')}</th><th>${t('ev.colStatus')}</th><th></th></tr>
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
                  ${escapeHtml(roomStatusLabel(item.status))}</span></td>
                <td class="row-actions">
                  ${item.status === 'Waiting' ? `
                    <button class="btn btn-ghost btn-sm" type="button"
                            data-edit-event="${item.id}">${t('common.edit')}</button>
                    <button class="btn btn-ghost btn-sm danger" type="button"
                            data-cancel-event="${item.id}">${t('ev.cancelShort')}</button>
                  ` : `<span class="muted">${t('common.none')}</span>`}
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
      title: t('ev.cancelTitle'),
      message: item.registeredCount > 0
        ? t('ev.cancelMessageWithPlayers', { name: item.name, count: item.registeredCount })
        : t('ev.cancelMessage', { name: item.name }),
      confirmLabel: t('ev.cancelAction'),
      onConfirm: async () => {
        await api(`/api/events/${item.id}/cancel`, { method: 'POST' });
        toast(t('ev.cancelled'), 'success');
        await showAdminTab('events');
      }
    }));
  });
}

function openEventDialog(item, categories) {
  const start = item ? new Date(item.scheduledStartUtc) : defaultEventStart();

  openDialog({
    title: item ? t('ev.editTitle') : t('ev.newTitle'),
    body: `
      <div class="form">
        <label><span>${t('ev.name')}</span>
          <input name="name" type="text" maxlength="64" value="${escapeHtml(item?.name ?? '')}">
        </label>
        <label><span>${t('ev.description')}</span>
          <textarea name="description" rows="2">${escapeHtml(item?.description ?? '')}</textarea>
        </label>
        <label><span>${t('ev.category')}</span>
          <select name="categoryId">
            ${categories.map((category) => `
              <option value="${category.id}" ${category.id === item?.categoryId ? 'selected' : ''}>
                ${escapeHtml(category.name)}
              </option>`).join('')}
          </select>
        </label>
        <label><span>${t('ev.startLocal')}</span>
          <input name="scheduledStart" type="datetime-local" value="${toLocalInputValue(start)}">
          <small class="hint" id="utcPreview"></small>
        </label>
        <div class="grid-3">
          <label><span>${t('ev.questionCount')}</span>
            <input name="questionCount" type="number" min="5" max="30" value="${item?.questionCount ?? 10}">
          </label>
          <label><span>${t('ev.duration')}</span>
            <input name="secondsPerQuestion" type="number" min="5" max="60" value="${item?.secondsPerQuestion ?? 20}">
          </label>
          <label><span>${t('ev.capacity')}</span>
            <input name="maxPlayers" type="number" min="2" max="8" value="${item?.maxPlayers ?? 8}">
          </label>
        </div>
        <div class="dialog-error hidden" role="alert"></div>
      </div>`,
    onConfirm: async () => {
      const name = fieldValue('name');
      if (name.length < 3) {
        dialogError(t('ev.nameTooShort'));
        return false;
      }

      const localStart = field('scheduledStart').value;
      if (!localStart) {
        dialogError(t('ev.startRequired'));
        return false;
      }

      const scheduled = new Date(localStart);
      if (scheduled.getTime() <= Date.now()) {
        dialogError(t('ev.startMustBeFuture'));
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

      toast(item ? t('ev.updated') : t('ev.created'), 'success');
      await showAdminTab('events');
      return true;
    }
  });

  const input = field('scheduledStart');
  const preview = () => {
    const value = input.value ? new Date(input.value) : null;
    $('utcPreview').textContent = value && !Number.isNaN(value.getTime())
      ? t('ev.utcPreview', { value: value.toISOString().slice(0, 16).replace('T', ' ') })
      : t('ev.utcHint');
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
      <input id="userSearchInput" type="search"
             placeholder="${escapeHtml(t('usr.searchPlaceholder'))}"
             value="${escapeHtml(adminState.userSearch)}"
             aria-label="${escapeHtml(t('usr.searchLabel'))}">
      <small class="muted">${t('usr.count', { count: page?.totalCount ?? 0 })}</small>
    </div>

    <div class="card">
      ${users.length === 0 ? `<div class="empty">${t('usr.empty')}</div>` : `
        <table class="table">
          <thead>
            <tr><th>${t('usr.colUser')}</th><th>${t('usr.colEmail')}</th><th>${t('usr.colRoles')}</th>
                <th>${t('usr.colStatus')}</th><th>${t('usr.colLastLogin')}</th><th></th></tr>
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
        toast(t('usr.unlockedToast', { name: user.nickname }), 'success');
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
    ? `<span class="chip chip-warn"
             title="${escapeHtml(t('usr.lockTooltip', { count: user.accessFailedCount }))}">🔒 ${t('common.locked')}</span>`
    : user.isActive
      ? `<span class="chip chip-ok">${t('common.active')}</span>`
      : `<span class="chip chip-muted">${t('common.passive')}</span>`;

  const roleChips = user.roles.length === 0
    ? `<span class="muted">${t('common.player')}</span>`
    : user.roles.map((role) => `
        <span class="chip ${role === ROLES.admin ? 'chip-danger' : 'chip-info'}">
          ${escapeHtml(roleLabel(role))}
        </span>`).join(' ');

  const actions = self
    ? `<span class="muted small">${t('usr.self')}</span>`
    : `
      <button class="btn btn-ghost btn-sm" type="button" data-toggle-active="${user.id}">
        ${user.isActive ? t('usr.deactivate') : t('usr.activate')}
      </button>
      ${isLocked(user) ? `<button class="btn btn-ghost btn-sm" type="button"
                                  data-unlock="${user.id}">${t('usr.unlock')}</button>` : ''}
      ${isAdmin() ? `<button class="btn btn-ghost btn-sm" type="button"
                             data-claims="${user.id}">${t('usr.permissions')}</button>` : ''}`;

  return `
    <tr>
      <td><strong>${escapeHtml(user.nickname)}</strong><br>
          <small class="muted">${escapeHtml(user.firstName)} ${escapeHtml(user.lastName)}</small></td>
      <td class="muted small">${escapeHtml(user.email)}</td>
      <td class="chip-cell">${roleChips}</td>
      <td>${statusChip}</td>
      <td class="muted small">${user.lastLoginAtUtc
        ? escapeHtml(dateTime(user.lastLoginAtUtc)) : t('common.none')}</td>
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
    title: next ? t('usr.activateTitle') : t('usr.deactivateTitle'),
    message: next
      ? t('usr.activateMessage', { name: user.nickname })
      : t('usr.deactivateMessage', { name: user.nickname }),
    confirmLabel: next ? t('usr.activate') : t('usr.deactivate'),
    onConfirm: async () => {
      await api(`/api/users/${user.id}/active?isActive=${next}`, { method: 'PATCH' });
      toast(t('usr.updatedToast', { name: user.nickname }), 'success');
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
        <strong>${escapeHtml(roleLabel(claim.name))}</strong>
        <small class="muted">${escapeHtml(claim.description ?? claim.name)}</small>
      </span>
    </label>`).join('');

  openDialog({
    title: t('usr.claimsTitle', { name: user.nickname }),
    body: `
      <p class="dialog-text">${escapeHtml(t('usr.claimsHint'))}</p>
      <div class="claim-list">${rows || `<div class="empty">${t('usr.noClaims')}</div>`}</div>`,
    confirmLabel: t('common.close'),
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
        const claim = roleLabel(box.dataset.claimName);
        toast(box.checked
          ? t('usr.claimGranted', { claim })
          : t('usr.claimRevoked', { claim }), 'success');
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
              ${page.hasPrevious ? '' : 'disabled'}>${t('common.previous')}</button>
      <span class="muted small">${page.page} / ${page.totalPages}</span>
      <button class="btn btn-ghost btn-sm" type="button" data-page-${prefix}="${page.page + 1}"
              ${page.hasNext ? '' : 'disabled'}>${t('common.next')}</button>
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
      ? `<div class="empty">${t('pev.empty')}</div>`
      : events.map((item) => `
          <div class="event">
            <div class="event-info">
              <strong>${escapeHtml(item.categoryIcon || '')} ${escapeHtml(item.name)}</strong>
              ${item.description ? `<small class="muted">${escapeHtml(trim(item.description, 60))}</small>` : ''}
              <small class="muted">${escapeHtml(t('pev.meta', {
                date: shortDate(item.scheduledStartUtc),
                time: new Date(item.scheduledStartUtc)
                  .toLocaleTimeString(locale(), { hour: '2-digit', minute: '2-digit' }),
                registered: item.registeredCount,
                capacity: item.maxPlayers,
                questions: item.questionCount
              }))}</small>
            </div>
            <div class="event-actions">
              ${item.isRegistered
                ? `<span class="chip chip-ok">${t('pev.registered')}</span>
                   <button class="btn btn-ghost btn-sm" type="button"
                           data-withdraw="${item.id}">${t('pev.withdraw')}</button>`
                : item.canRegister
                  ? `<button class="btn btn-primary btn-sm" type="button"
                             data-register="${item.id}">${t('pev.register')}</button>`
                  : `<span class="chip chip-muted">${t('pev.full')}</span>`}
            </div>
          </div>`).join('');

    container.querySelectorAll('[data-register]').forEach((button) => {
      button.addEventListener('click', () => withButtonBusy(button, async () => {
        await api(`/api/events/${button.dataset.register}/register`, { method: 'POST' });
        toast(t('pev.registeredToast'), 'success');
        await loadUpcomingEvents();
      }));
    });

    container.querySelectorAll('[data-withdraw]').forEach((button) => {
      button.addEventListener('click', () => withButtonBusy(button, async () => {
        await api(`/api/events/${button.dataset.withdraw}/register`, { method: 'DELETE' });
        toast(t('pev.withdrawnToast'), 'success');
        await loadUpcomingEvents();
      }));
    });
  } catch {
    // Etkinlikler ana sayfanın yardımcı bir bölümü; hata sayfayı durdurmamalı.
    container.innerHTML = `<div class="empty">${t('pev.failed')}</div>`;
  }
}

$('refreshEventsButton').addEventListener('click', loadUpcomingEvents);
