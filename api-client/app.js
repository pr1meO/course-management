const API_BASE_URL = "http://localhost:5072/api/v2";

let authToken = localStorage.getItem("authToken");
let currentLogin = localStorage.getItem("login");

const idempotencyCache = {
  register: null,
  createJob: null
};

let projectsPage = 1;
let projectsPageSize = 10;

let jobsPage  = 1;
let jobsPageSize = 10;

let projectsLookup = {};

const $ = (sel) => document.querySelector(sel);

// =======================
// УВЕДОМЛЕНИЯ
// =======================

function showError(message) {
  const box = $("#error-box");
  const textEl = $("#error-text");
  if (!box || !textEl) return;

  textEl.textContent = message;
  box.classList.remove("hidden");
  box.onclick = () => box.classList.add("hidden");
}

function hideError() {
  $("#error-box")?.classList.add("hidden");
}

function showSuccess(message) {
  const box = $("#success-box");
  const textEl = $("#success-text");
  if (!box || !textEl) return;

  textEl.textContent = message;
  box.classList.remove("hidden");
  box.onclick = () => box.classList.add("hidden");
}

function hideSuccess() {
  $("#success-box")?.classList.add("hidden");
}

// =======================
// АВТОРИЗАЦИЯ / ТОКЕН
// =======================

function setAuthToken(token, login) {
  authToken = token;
  currentLogin = login || null;

  token ? localStorage.setItem("authToken", token) : localStorage.removeItem("authToken");
  login ? localStorage.setItem("login", login) : localStorage.removeItem("login");
}

function updateUserUI() {
  const pill = $("#user-pill");
  const loginEl = $("#user-login");

  if (!pill || !loginEl) return;

  if (authToken && currentLogin) {
    loginEl.textContent = currentLogin;
    pill.style.display = "flex";
  } else {
    pill.style.display = "none";
  }
}

function requireAuth() {
  if (!authToken) {
    showError("Необходима авторизация.");
    window.location.href = "login.html";
    throw new Error("Not authenticated");
  }
}

// =======================
// ОБЩИЙ ЗАПРОС К API
// =======================

async function apiRequest(path, options = {}) {
  const {
    method = "GET",
    body = null,
    idempotent = false,
    idempotencyKeyName = null
  } = options;

  const headers = {
    "Content-Type": "application/json"
  };

  if (authToken) headers["Authorization"] = `Bearer ${authToken}`;

  if (idempotent) {
    let key = idempotencyCache[idempotencyKeyName];
    if (!key) {
      key = crypto.randomUUID();
      idempotencyCache[idempotencyKeyName] = key;
    }
    headers["IdempotencyKey"] = key;
  }

  let resp;
  try {
    resp = await fetch(API_BASE_URL + path, {
      method,
      headers,
      body: body ? JSON.stringify(body) : null
    });
  } catch (err) {
    showError("Ошибка сети. Проверьте подключение к серверу.");
    throw err;
  }

  if (!resp.ok) {
    let payload = null;
    try { payload = await resp.json(); } catch {}

    let message;

    if (resp.status === 429) {
      const retryAfter = resp.headers.get("Retry-After");
      message = payload?.message || `Слишком много запросов. Попробуйте через ${retryAfter || 5} сек.`;
    } else if (resp.status === 401) {
      message = payload?.message || "Неавторизовано.";
    } else {
      message = payload?.message || `Ошибка (${resp.status})`;
    }

    showError(message);

    if (resp.status === 401) {
      setAuthToken(null, null);
      updateUserUI();

      const page = document.body.dataset.page;
      if (page !== "login" && page !== "register") {
        window.location.href = "login.html";
      }
    }

    throw new Error(message);
  }

  hideError();

  if (resp.status === 204) return null;

  try {
    return await resp.json();
  } catch {
    return null;
  }
}

// =======================
// LOGIN
// =======================

async function initLoginPage() {
  if (authToken) {
    window.location.href = "projects.html";
    return;
  }

  const form = $("#login-form");

  if (!form)
    return;

  form.addEventListener("submit", async (e) => {
    e.preventDefault();

    const email = form.login.value.trim();
    const password = form.password.value.trim();

    if (!email || !password) {
      showError("Введите почту и пароль.");
      return;
    }

    try {
      const resp = await apiRequest("/auth/Login", {
        method: "POST",
        body: { email, password }
      });

      setAuthToken(resp.tokens.accessToken, email);
      updateUserUI();
      showSuccess("Успешный вход.");

      setTimeout(() => window.location.href = "projects.html", 800);
    } catch {}
  });
}

// =======================
// REGISTER
// =======================

async function initRegisterPage() {
  if (authToken) {
    window.location.href = "projects.html";
    return;
  }

  const form = $("#register-form");
  
  if (!form) 
    return;

  form.addEventListener("submit", async (e) => {
    e.preventDefault();

    const payload = {
      userName: form.login.value.trim(),
      email: form.email.value.trim(),
      password: form.password.value.trim(),
    };

    if (!payload.userName || !payload.email || !payload.password) {
      showError("Заполните обязательные поля.");
      return;
    }

    try {
      await apiRequest("/auth/Register", {
        method: "POST",
        body: payload,
        idempotent: true,
        idempotencyKeyName: "register"
      });

      idempotencyCache.register = null;
      showSuccess("Регистрация выполнена.");
      setTimeout(() => window.location.href = "login.html", 1000);
    } catch {}
  });
}

// =======================
// PROJECTS
// =======================

function openProjectModal() {
  $("#teacher-modal").style.display = "flex";
}

function closeProjectModal() {
  $("#teacher-modal").style.display = "none";
}

async function refreshProjects() {
  const pageLabel = $("#teachers-page");
  if (!pageLabel) return;

  pageLabel.textContent = String(projectsPage);

  try {
    const projects = await apiRequest(
      `/project/GetAll?pageNumber=${projectsPage}&pageSize=${projectsPageSize}`
    );

    renderProjectsTable(projects.projects || []);

    $("#teachers-next").disabled = (projects.projects || []).length < projectsPageSize;
    $("#teachers-prev").disabled = projectsPage <= 1;
  } catch {}
}

function renderProjectsTable(projects) {
  const tbody = $("#teachers-tbody");
  tbody.innerHTML = "";

  projects.forEach(t => {
    const tr = document.createElement("tr");
    tr.dataset.id = t.projectId;
    tr.innerHTML = `
      <td>${t.name}</td>
      <td>${t.createdAt}</td>
      <td>
        <button class="btn btn-outline btn-sm" data-action="edit">✎</button>
        <button class="btn btn-outline btn-sm" data-action="delete">✕</button>
      </td>
    `;
    tbody.appendChild(tr);
  });
}

function fillProjectFormFromRow(row) {
  const cells = row.querySelectorAll("td");
  const form = $("#teacher-form");
  form.id.value = row.dataset.id;
  form.name.value = cells[0].textContent.trim();
}

async function initProjectsPage() {
  requireAuth();
  updateUserUI();

  const form = $("#teacher-form");

  $("#logout-btn")?.addEventListener("click", () => {
    setAuthToken(null, null);
    updateUserUI();
    window.location.href = "login.html";
  });

  $("#teacher-modal").addEventListener("click", (e) => {
    if (e.target.id === "teacher-modal") {
      form.reset();
      closeProjectModal();
    }
  });

  $("#teacher-modal-cancel")?.addEventListener("click", () => {
    form.reset();
    closeProjectModal();
  });

  $("#teacher-modal-close")?.addEventListener("click", () => {
    form.reset();
    closeProjectModal();
  });

  $("#project-create-btn").addEventListener("click", () => {
    form.reset();
    form.id.value = "";
    $("#project-modal-title").textContent = "Создание проекта";
    openProjectModal();
  });

  const sizeSelect = $("#teachers-page-size");
  if (sizeSelect) {
    sizeSelect.addEventListener("change", () => {
      projectsPageSize = Number(sizeSelect.value);
      projectsPage = 1;
      refreshProjects();
    });
  }

  form.addEventListener("submit", async (e) => {
    e.preventDefault();

    const id = form.id.value;

    const payload = {
      name: form.name.value.trim(),
      description: ""
    };

    if (!payload.name) {
      showError("Заполните обязательные поля.");
      return;
    }

    try {
      if (id) {
        await apiRequest(`/project/Update/${id}`, {
          method: "PUT",
          body: payload
        });
        showSuccess("Проект обновлён.");
      } else {
        await apiRequest(`/project/Create`, {
          method: "POST",
          body: payload,
          idempotent: true,
          idempotencyKeyName: "createProject"
        });
        idempotencyCache.createProject = null;
        showSuccess("Проект создан.");
      }

      form.reset();
      closeProjectModal();
      projectsPage = 1;
      refreshProjects();

    } catch {}
  });

  $("#teachers-tbody")?.addEventListener("click", async (e) => {
    const btn = e.target.closest("button");
    if (!btn) return;

    const row = btn.closest("tr");
    const id = row.dataset.id;
    const action = btn.dataset.action;

    if (action === "edit") {
      fillProjectFormFromRow(row);
      $("#project-modal-title").textContent = "Редактирование проекта";
      openProjectModal();
    }

    if (action === "delete") {
      if (!confirm("Удалить проект?")) return;

      try {
        await apiRequest(`/project/Delete/${id}`, { method: "DELETE" });
        showSuccess("Удален.");
        projectsPage = 1;
        refreshProjects();
      } catch {}
    }
  });

  $("#teachers-prev").addEventListener("click", () => {
    if (projectsPage > 1) {
      projectsPage--;
      refreshProjects();
    }
  });

  $("#teachers-next").addEventListener("click", () => {
    projectsPage++;
    refreshProjects();
  });

  refreshProjects();
}

// =======================
// JOBS
// =======================

function openJobModal() {
  $("#course-edit-modal").style.display = "flex";
}

function closeJobModal() {
  $("#course-edit-modal").style.display = "none";
}

function showProjectSelect(visible) {
  const row = $("#project-row");
  
  if (!row) 
    return;
  
  row.style.display = visible ? "" : "none";
}

async function loadProjectsForJobSelect() {
  const select = $("#course-edit-teacher");
  if (!select) return;

  try {
    const projects = await apiRequest("/project/GetAll?pageNumber=1&pageSize=200");

    projectsLookup = {};
    select.innerHTML = "";

    projects.projects.forEach(t => {
      projectsLookup[t.projectId] = t.name;

      const opt = document.createElement("option");
      opt.value = t.projectId;
      opt.textContent = t.name;
      select.appendChild(opt);
    });
  } catch {}
}

function renderJobsTable(jobs) {
  const tbody = $("#courses-tbody");
  tbody.innerHTML = "";

  jobs.forEach(c => {
    const tr = document.createElement("tr");
    tr.dataset.id = c.jobId;
    tr.dataset.projectId = c.projectId;

    tr.innerHTML = `
      <td>${c.title}</td>
      <td>${c.createdAt}</td>
      <td>
        <button class="btn btn-outline btn-sm" data-action="edit">✎</button>
        <button class="btn btn-outline btn-sm" data-action="delete">✕</button>
      </td>
    `;

    tbody.appendChild(tr);
  });
}

function fillJobEditFormFromRow(row) {
  const form = $("#course-edit-form");
  form.id.value = row.dataset.id;
  form.title.value = row.children[0].textContent.trim();
  form.projectId.value = row.dataset.projectId;
}

async function refreshJobs() {
  $("#courses-page").textContent = String(jobsPage);

  try {
    const jobs = await apiRequest(
      `/job/GetAll?pageNumber=${jobsPage}&pageSize=${jobsPageSize}`
    );

    renderJobsTable(jobs.notes || []);

    $("#courses-next").disabled = (jobs.notes || []).length < jobsPageSize;
    $("#courses-prev").disabled = jobsPage <= 1;

  } catch {}
}

async function initJobsPage() {
  requireAuth();
  updateUserUI();

  await loadProjectsForJobSelect();

const jobsSizeSelect = $("#courses-page-size");
if (jobsSizeSelect) {
    jobsSizeSelect.addEventListener("change", () => {
        jobsPageSize = Number(jobsSizeSelect.value);
        jobsPage = 1;
        refreshJobs();
    });
}

  const form = $("#course-edit-form");

  $("#logout-btn").addEventListener("click", () => {
    setAuthToken(null, null);
    updateUserUI();
    window.location.href = "login.html";
  });

  $("#course-create-btn").addEventListener("click", async () => {
    form.reset();
    form.id.value = "";
    $("#course-modal-title").textContent = "Создание задачи";

    showProjectSelect(true);
    await loadProjectsForJobSelect();

    openJobModal();
  });

  $("#course-edit-modal").addEventListener("click", (e) => {
    if (e.target.id === "course-edit-modal") {
      form.reset();
      closeJobModal();
    }
  });

  $("#course-edit-close").addEventListener("click", () => {
    form.reset();
    closeJobModal();
  });

  $("#course-edit-cancel").addEventListener("click", () => {
    form.reset();
    closeJobModal();
  });

  // === PAGE SIZE SELECTOR FOR COURSES (если захочешь добавить позже)

  form.addEventListener("submit", async (e) => {
    e.preventDefault();

    const id = form.id.value;
    const payload = {
      title: form.title.value.trim(),
      projectId: form.projectId.value
    };

    if (!payload.title || !payload.projectId) {
      showError("Заполните поля.");
      return;
    }

    try {
      if (id) {
        await apiRequest(`/job/Update/${id}`, {
          method: "PUT",
          body: payload
        });
        showSuccess("Задача обновлёна.");
      } else {
        await apiRequest("/job/Create", {
          method: "POST",
          body: payload,
          idempotent: true,
          idempotencyKeyName: "createJob"
        });
        idempotencyCache.createJob = null;
        showSuccess("Задача создана.");
      }

      form.reset();
      closeJobModal();
      jobsPage = 1;
      await loadProjectsForJobSelect();
      refreshJobs();

    } catch {}
  });

  $("#courses-tbody").addEventListener("click", async (e) => {
    const btn = e.target.closest("button");
    if (!btn) return;

    const row = btn.closest("tr");
    const id = row.dataset.id;
    const action = btn.dataset.action;

    if (action === "edit") {
      fillJobEditFormFromRow(row);
      $("#course-modal-title").textContent = "Редактирование задачи";
      //showProjectSelect(false);
      openJobModal();
    }

    if (action === "delete") {
      if (!confirm("Удалить задачу?")) return;

      try {
        await apiRequest(`/job/Delete/${id}`, { method: "DELETE" });
        showSuccess("Удалена.");
        jobsPage = 1;
        refreshJobs();
      } catch {}
    }
  });

  $("#courses-prev").addEventListener("click", () => {
    if (jobsPage > 1) {
      jobsPage--;
      refreshJobs();
    }
  });

  $("#courses-next").addEventListener("click", () => {
    jobsPage++;
    refreshJobs();
  });

  refreshJobs();
}

// =======================
// ИНИЦИАЛИЗАЦИЯ
// =======================

window.addEventListener("DOMContentLoaded", () => {
  const page = document.body.dataset.page;

  if (page === "login") initLoginPage();
  if (page === "register") initRegisterPage();
  if (page === "projects") initProjectsPage();
  if (page === "jobs") initJobsPage();
});
