(() => {
    // Theme persistence
    const savedTheme = localStorage.getItem("hub-theme") || "dark";
    if (savedTheme === "light") document.documentElement.setAttribute("data-theme", "light");

    window.toggleHubTheme = () => {
        const isLight = document.documentElement.getAttribute("data-theme") === "light";
        if (isLight) {
            document.documentElement.removeAttribute("data-theme");
            localStorage.setItem("hub-theme", "dark");
        } else {
            document.documentElement.setAttribute("data-theme", "light");
            localStorage.setItem("hub-theme", "light");
        }
    };

    // Ctrl+K search focus
    const searchInput = document.getElementById("globalSearchInput");
    const searchDropdown = document.getElementById("liveSearchDropdown");
    let debounceTimer = null;

    document.addEventListener("keydown", (e) => {
        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "k" && searchInput) {
            e.preventDefault();
            searchInput.focus();
            searchInput.select();
        }
        if (e.key === "Escape" && searchDropdown) {
            searchDropdown.style.display = "none";
        }
    });

    if (searchInput && searchDropdown) {
        searchInput.addEventListener("input", () => {
            clearTimeout(debounceTimer);
            const q = searchInput.value.trim();
            if (q.length < 2) {
                searchDropdown.style.display = "none";
                return;
            }
            debounceTimer = setTimeout(async () => {
                try {
                    const resp = await fetch(`/api/search?q=${encodeURIComponent(q)}`);
                    if (!resp.ok) return;
                    const data = await resp.json();
                    renderLiveDropdown(data, searchDropdown);
                } catch { }
            }, 220);
        });

        document.addEventListener("click", (e) => {
            if (!searchDropdown.contains(e.target) && e.target !== searchInput) {
                searchDropdown.style.display = "none";
            }
        });
    }

    function esc(s) {
        return String(s ?? "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    function escJs(s) {
        return String(s ?? "").replace(/\\/g, "\\\\").replace(/'/g, "\\'");
    }

    window.showHubToast = (message, variant = "info") => {
        const container = document.getElementById("hubToastContainer");
        if (!container) return;
        const borderCls = variant === "success" ? "border-success"
            : variant === "warning" ? "border-warning"
            : variant === "danger" ? "border-danger" : "border-info";
        const toastEl = document.createElement("div");
        toastEl.className = `toast align-items-center show border ${borderCls}`;
        toastEl.style.cssText = "background:var(--card);color:var(--text)";
        toastEl.innerHTML = `<div class="d-flex">
            <div class="toast-body small">${esc(message)}</div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>`;
        container.appendChild(toastEl);
        setTimeout(() => toastEl.remove(), 4200);
    };

    window.copyHubText = async (text, btnEl) => {
        try {
            if (navigator.clipboard) {
                await navigator.clipboard.writeText(text);
            } else {
                const ta = document.createElement("textarea");
                ta.value = text;
                document.body.appendChild(ta);
                ta.select();
                document.execCommand("copy");
                ta.remove();
            }
            if (btnEl) {
                const orig = btnEl.textContent;
                btnEl.textContent = "Copied ✓";
                setTimeout(() => btnEl.textContent = orig, 1500);
            }
            showHubToast("Copied path to clipboard", "success");
        } catch {
            showHubToast("Copy manually from the path box", "warning");
        }
    };

    function renderLiveDropdown(data, box) {
        const hasAny = (data.topics?.length || 0) + (data.conventionKeys?.length || 0) +
                       (data.videos?.length || 0) + (data.files?.length || 0);
        if (!hasAny) {
            box.innerHTML = `<div class="p-3 text-muted-hub small">No exact matches for "${esc(data.query)}". Try pressing Enter for full search.</div>`;
            box.style.display = "block";
            return;
        }

        let html = "";
        if (data.topics?.length) {
            html += `<div class="px-3 pt-2 pb-1 text-uppercase small fw-bold text-info">Topics (${data.topics.length})</div>`;
            for (const t of data.topics.slice(0, 6)) {
                html += `<a href="/Topics/Details/${esc(t.slug)}" class="d-flex justify-content-between align-items-center px-3 py-2 text-decoration-none border-bottom" style="border-color:var(--border)!important;color:var(--text)">
                    <div>
                        <span class="badge me-1" style="background:${esc(t.badgeColor)}22;color:${esc(t.badgeColor)};border:1px solid ${esc(t.badgeColor)}55">${esc(t.moduleCode)}</span>
                        <strong>${esc(t.titleEn)}</strong>
                        <div class="small text-muted-hub">${esc(t.titleAr)} · ${esc(t.sessionLabel)}</div>
                    </div>
                    <span class="badge badge-status-${esc(t.status)}">${esc(t.status)}</span>
                </a>`;
            }
        }
        if (data.conventionKeys?.length) {
            html += `<div class="px-3 pt-2 pb-1 text-uppercase small fw-bold text-warning">Convention Keys (${data.conventionKeys.length})</div>`;
            for (const k of data.conventionKeys.slice(0, 6)) {
                html += `<a href="/Topics/Details/${esc(k.topicSlug)}#conventions" class="d-block px-3 py-2 text-decoration-none border-bottom" style="border-color:var(--border)!important;color:var(--text)">
                    <span class="key-pill ${k.isCritical ? "critical" : ""} me-2">${esc(k.keyName)}</span>
                    <span class="small">${esc(k.shortRule)}</span>
                </a>`;
            }
        }
        if (data.videos?.length) {
            html += `<div class="px-3 pt-2 pb-1 text-uppercase small fw-bold text-success">Videos & Frame OCR (${data.videos.length})</div>`;
            for (const v of data.videos.slice(0, 6)) {
                const ts = v.matchedTimestampSeconds ?? 0;
                const tsChip = v.matchedTimestampLabel ? `<span class="ts-chip ms-2">@ ${esc(v.matchedTimestampLabel)}</span>` : "";
                html += `<button type="button" onclick="openVideoModal(${v.id}, ${ts})" class="btn btn-link text-start w-100 px-3 py-2 text-decoration-none border-bottom" style="border-color:var(--border)!important;color:var(--text)">
                    <div class="d-flex justify-content-between">
                        <span><strong>🎬 ${esc(v.fileName)}</strong>${tsChip}</span>
                        <small class="text-muted-hub">${esc(v.durationFormatted)}</small>
                    </div>
                    <div class="small text-muted-hub text-truncate">${esc(v.excerpt)}</div>
                </button>`;
            }
        }
        if (data.files?.length) {
            html += `<div class="px-3 pt-2 pb-1 text-uppercase small fw-bold" style="color:var(--accent-2)">Files / Slides / SQL / Code (${data.files.length})</div>`;
            for (const f of data.files.slice(0, 6)) {
                html += `<button type="button" onclick="openPreviewModal(${f.id})" class="btn btn-link text-start w-100 px-3 py-2 text-decoration-none border-bottom" style="border-color:var(--border)!important;color:var(--text)">
                    <div class="d-flex justify-content-between">
                        <span><strong>📄 ${esc(f.fileName)}</strong> <small class="text-muted-hub">[${esc(f.resourceType)}]</small></span>
                        <small class="text-muted-hub">${esc(f.instructor)}</small>
                    </div>
                    <div class="small text-muted-hub text-truncate">${esc(f.excerpt)}</div>
                </button>`;
            }
        }
        html += `<div class="p-2 text-center"><a href="/Home/Search?q=${encodeURIComponent(data.query)}" class="btn btn-sm btn-outline-info">Open Full Results Page →</a></div>`;
        box.innerHTML = html;
        box.style.display = "block";
    }

    // Video modal + frame OCR timeline (Stream Range + Google Drive Player toggle)
    let activeDrivePreviewUrl = "";

    window.switchVideoPlayerMode = (mode) => {
        const player = document.getElementById("hubVideoPlayer");
        const frame = document.getElementById("hubDriveVideoFrame");
        const btnStream = document.getElementById("btnModeStreamPlayer");
        const btnDrive = document.getElementById("btnModeDriveEmbed");
        const fallback = document.getElementById("videoCodecFallback");
        if (!player || !frame) return;

        if (mode === "drive" && activeDrivePreviewUrl) {
            player.pause();
            player.classList.add("d-none");
            if (fallback) fallback.classList.add("d-none");
            frame.src = activeDrivePreviewUrl;
            frame.classList.remove("d-none");
            btnDrive?.classList.replace("btn-outline-info", "btn-info");
            btnDrive?.classList.add("text-dark", "fw-semibold");
            btnStream?.classList.replace("btn-success", "btn-outline-success");
        } else {
            frame.src = "";
            frame.classList.add("d-none");
            player.classList.remove("d-none");
            btnStream?.classList.replace("btn-outline-success", "btn-success");
            btnDrive?.classList.replace("btn-info", "btn-outline-info");
            btnDrive?.classList.remove("text-dark", "fw-semibold");
        }
    };

    window.openVideoModal = async (resourceId, startSeconds = 0) => {
        const modalEl = document.getElementById("videoPlayerModal");
        if (!modalEl) return;
        const bsModal = bootstrap.Modal.getOrCreateInstance(modalEl);

        const titleEl = document.getElementById("videoModalTitle");
        const metaEl = document.getElementById("videoModalMeta");
        const player = document.getElementById("hubVideoPlayer");
        const fallbackAlert = document.getElementById("videoCodecFallback");
        const framesBox = document.getElementById("videoOcrTimeline");
        const openBtn = document.getElementById("videoOpenExtBtn");
        const dlBtn = document.getElementById("videoDownloadBtn");
        const revealBtn = document.getElementById("videoRevealBtn");

        activeDrivePreviewUrl = "";
        switchVideoPlayerMode("stream");
        if (fallbackAlert) fallbackAlert.classList.add("d-none");
        titleEl.textContent = "Loading video...";
        framesBox.innerHTML = `<div class="text-muted-hub small">Loading OCR keyframes...</div>`;
        bsModal.show();

        const resp = await fetch(`/api/media/preview/${resourceId}`);
        if (!resp.ok) {
            titleEl.textContent = "Failed to load video metadata";
            return;
        }
        const data = await resp.json();
        titleEl.textContent = data.fileName;
        metaEl.textContent = `${data.relativePath} · ${data.instructor} · ${data.durationFormatted}`;
        activeDrivePreviewUrl = data.drivePreviewUrl || "";

        player.onerror = () => {
            if (fallbackAlert) fallbackAlert.classList.remove("d-none");
        };
        player.src = `/api/media/stream/${resourceId}`;
        player.onloadedmetadata = () => {
            if (startSeconds > 0 && startSeconds < player.duration) {
                player.currentTime = startSeconds;
            }
            player.play().catch(() => { });
        };

        if (dlBtn) dlBtn.href = data.downloadUrl || `/api/media/download/${resourceId}`;
        openBtn.onclick = () => openExternalResource(resourceId);
        revealBtn.onclick = () => revealResource(resourceId);

        let frames = [];
        if (data.structuredPreviewJson) {
            try { frames = JSON.parse(data.structuredPreviewJson); } catch { }
        }
        if (frames.length) {
            framesBox.innerHTML = frames.map(f => {
                const activeCls = Math.abs((f.t || 0) - startSeconds) <= 20 ? "border-info" : "";
                return `<div class="p-2 mb-2 rounded border ${activeCls}" style="border-color:var(--border);background:var(--bg)">
                    <div class="d-flex justify-content-between align-items-center mb-1">
                        <button type="button" class="ts-chip" onclick="seekHubVideo(${f.t || 0})">▶ ${esc(f.label)}</button>
                    </div>
                    <div class="small text-muted-hub" style="white-space:pre-wrap;word-break:break-word">${esc(f.text)}</div>
                </div>`;
            }).join("");
        } else {
            framesBox.innerHTML = `<div class="small text-muted-hub">No OCR keyframes captured for this file. You can watch via Stream Player or Google Drive Player.</div>`;
        }
    };

    window.seekHubVideo = (seconds) => {
        switchVideoPlayerMode("stream");
        const player = document.getElementById("hubVideoPlayer");
        if (player) {
            player.currentTime = seconds;
            player.play().catch(() => { });
        }
    };

    const videoModalEl = document.getElementById("videoPlayerModal");
    if (videoModalEl) {
        videoModalEl.addEventListener("hidden.bs.modal", () => {
            const player = document.getElementById("hubVideoPlayer");
            const frame = document.getElementById("hubDriveVideoFrame");
            if (player) {
                player.onerror = null;
                player.pause();
                player.removeAttribute("src");
                player.load();
            }
            if (frame) frame.src = "";
        });
    }

    // File / Slide / Code / SQL preview modal
    window.openPreviewModal = async (resourceId) => {
        const modalEl = document.getElementById("filePreviewModal");
        if (!modalEl) return;
        const bsModal = bootstrap.Modal.getOrCreateInstance(modalEl);

        const titleEl = document.getElementById("previewModalTitle");
        const metaEl = document.getElementById("previewModalMeta");
        const bodyEl = document.getElementById("previewModalBody");
        const openBtn = document.getElementById("previewOpenExtBtn");
        const dlLink = document.getElementById("previewDownloadLink");
        const revealBtn = document.getElementById("previewRevealBtn");
        const embedLink = document.getElementById("previewRawLink");

        titleEl.textContent = "Loading preview...";
        bodyEl.innerHTML = `<div class="text-muted-hub">Loading file contents...</div>`;
        bsModal.show();

        const resp = await fetch(`/api/media/preview/${resourceId}`);
        if (!resp.ok) {
            titleEl.textContent = "Unable to load preview";
            return;
        }
        const data = await resp.json();
        titleEl.textContent = data.fileName;
        metaEl.textContent = `${data.relativePath} · ${data.resourceType} · ${data.instructor}`;
        openBtn.onclick = () => openExternalResource(resourceId);
        if (dlLink) dlLink.href = data.downloadUrl || `/api/media/download/${resourceId}`;
        revealBtn.onclick = () => revealResource(resourceId);

        if (data.canStreamInBrowser && data.streamUrl) {
            embedLink.href = data.streamUrl;
            embedLink.classList.remove("d-none");
        } else {
            embedLink.classList.add("d-none");
        }

        if (data.isImage && data.streamUrl) {
            bodyEl.innerHTML = `<div class="text-center p-2"><img src="${esc(data.streamUrl)}" class="img-fluid rounded border" style="max-height:66vh" alt="${esc(data.fileName)}" /></div>`;
            return;
        }

        let sections = [];
        if (data.structuredPreviewJson) {
            try { sections = JSON.parse(data.structuredPreviewJson); } catch { }
        }

        let pdfViewerHtml = "";
        if (data.extension === ".pdf" && data.streamUrl) {
            pdfViewerHtml = `
                <div class="mb-3">
                    <button type="button" class="btn btn-sm btn-outline-info mb-2" onclick="const f=this.nextElementSibling;f.classList.toggle('d-none')">
                        📄 Toggle Embedded PDF Viewer
                    </button>
                    <div class="d-none">
                        <iframe src="${esc(data.streamUrl)}" class="w-100 rounded border" style="height:58vh;border-color:var(--border)!important"></iframe>
                    </div>
                </div>`;
        }

        if (sections.length) {
            bodyEl.innerHTML = pdfViewerHtml + sections.map(s => `
                <div class="p-2 mb-2 rounded border" style="border-color:var(--border);background:var(--bg)">
                    <div class="badge bg-secondary mb-1">${esc(s.label)}</div>
                    <pre class="small mb-0 text-wrap" style="color:var(--text);font-family:inherit">${esc(s.text)}</pre>
                </div>`).join("");
        } else {
            bodyEl.innerHTML = pdfViewerHtml + `<pre class="code-box"><code>${esc(data.rawText || "(No extracted text for this binary file — click Download / Open Copy or Open File.)")}</code></pre>`;
        }
    };

    window.openExternalResource = async (id) => {
        try {
            const resp = await fetch(`/api/media/open/${id}`, { method: "POST" });
            if (!resp.ok) {
                showHubToast(`Could not open resource (#${id})`, "danger");
                return;
            }
            const data = await resp.json();

            const targetUrl = data.driveWebViewLink || (data.canRenderInTab ? data.streamUrl : data.downloadUrl);
            if (data.canRenderInTab && data.streamUrl) {
                window.open(data.streamUrl, "_blank", "noopener");
                showHubToast(`Opened "${data.file}" in a new browser tab`, "info");
                return;
            }

            if (data.driveWebViewLink) {
                window.open(data.driveWebViewLink, "_blank", "noopener");
                showHubToast(`Opened "${data.file}" in Google Drive`, "success");
                return;
            }

            const modalEl = document.getElementById("externalFileModal");
            if (!modalEl) return;
            document.getElementById("extModalTitle").textContent = `Open File: ${data.file}`;
            document.getElementById("extModalDesktopInfo").textContent = `Google Drive Online`;
            document.getElementById("extModalBody").innerHTML = `
                <div class="d-flex flex-wrap gap-2 mb-3">
                    ${targetUrl ? `<a href="${esc(targetUrl)}" target="_blank" rel="noopener" class="btn btn-info text-dark fw-semibold">☁️ Open in Google Drive</a>` : ""}
                    <a href="${esc(data.downloadUrl)}" class="btn btn-outline-warning">⬇ Download Copy</a>
                </div>`;
            bootstrap.Modal.getOrCreateInstance(modalEl).show();
        } catch {
            showHubToast("Network error while opening Google Drive resource", "danger");
        }
    };

    window.revealResource = async (id) => {
        try {
            const modalEl = document.getElementById("folderBrowserModal");
            const metaEl = document.getElementById("folderModalMeta");
            const bodyEl = document.getElementById("folderModalBody");
            if (!modalEl || !bodyEl) return;

            metaEl.textContent = "Loading Drive folder contents...";
            bodyEl.innerHTML = `<div class="text-muted-hub small">Reading Google Drive folder index...</div>`;
            bootstrap.Modal.getOrCreateInstance(modalEl).show();

            const fResp = await fetch(`/api/media/folder/${id}`);
            if (!fResp.ok) {
                bodyEl.innerHTML = `<div class="text-danger small">Could not read Drive folder metadata.</div>`;
                return;
            }
            const folder = await fResp.json();
            metaEl.textContent = `${folder.relativeFolder} · ${folder.files.length} indexed files`;

            const rowsHtml = folder.files.map(f => {
                const highlightCls = f.isSelected ? "text-info" : "";
                const idAttr = f.resourceId;
                const actionBtns = `
                    ${f.isMedia
                        ? `<button type="button" class="btn btn-sm btn-success" onclick="bootstrap.Modal.getInstance(document.getElementById('folderBrowserModal'))?.hide();openVideoModal(${idAttr}, 0)">▶ Play</button>`
                        : `<button type="button" class="btn btn-sm btn-info text-dark fw-semibold" onclick="bootstrap.Modal.getInstance(document.getElementById('folderBrowserModal'))?.hide();openPreviewModal(${idAttr})">👁 Preview</button>`}
                    ${f.driveWebViewLink ? `<a href="${esc(f.driveWebViewLink)}" target="_blank" rel="noopener" class="btn btn-sm btn-outline-info">☁️ Drive</a>` : ""}
                    ${f.downloadUrl ? `<a href="${esc(f.downloadUrl)}" class="btn btn-sm btn-outline-warning">⬇ Download</a>` : ""}`;

                return `<tr style="border-color:var(--border)" class="${f.isSelected ? "table-active" : ""}">
                    <td>
                        <div class="d-flex align-items-center gap-2">
                            <span class="badge ${f.isSelected ? "bg-info text-dark" : "bg-secondary"}">${esc(f.resourceType)}</span>
                            <strong class="${highlightCls}">${f.isSelected ? "⭐ " : ""}${esc(f.fileName)}</strong>
                        </div>
                    </td>
                    <td class="small text-muted-hub">${f.durationFormatted ? esc(f.durationFormatted) : `${f.sizeMb} MB`}</td>
                    <td><div class="d-flex flex-wrap gap-1">${actionBtns}</div></td>
                </tr>`;
            }).join("");

            bodyEl.innerHTML = `
                <div class="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3 p-2 px-3 rounded border border-info" style="background:var(--bg)">
                    <div class="small">
                        <strong>☁️ Google Drive Folder:</strong> <code>${esc(folder.relativeFolder)}</code>
                    </div>
                    <a href="${esc(folder.driveFolderUrl || folder.folderPath)}" target="_blank" rel="noopener" class="btn btn-sm btn-info text-dark fw-semibold">
                        📂 Open Folder in Google Drive ↗
                    </a>
                </div>
                <div class="table-responsive">
                    <table class="table table-sm align-middle mb-0" style="color:var(--text)">
                        <thead><tr class="text-muted-hub small" style="border-color:var(--border)"><th>File</th><th>Duration / Size</th><th>Actions</th></tr></thead>
                        <tbody>${rowsHtml}</tbody>
                    </table>
                </div>`;
        } catch {
            showHubToast("Network error while opening Drive folder view", "danger");
        }
    };

    // Topic progress saver
    window.saveTopicProgress = async (topicId, incrementReview = true) => {
        const status = document.getElementById("progStatusSelect")?.value || "InProgress";
        const confidence = parseInt(document.getElementById("progConfidenceRange")?.value || "3", 10);
        const userNotes = document.getElementById("progUserNotes")?.value ?? "";
        const indicator = document.getElementById("progSaveIndicator");

        const resp = await fetch("/api/progress", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ topicId, status, confidenceLevel: confidence, userNotes, incrementReview })
        });
        if (resp.ok) {
            const data = await resp.json();
            if (indicator) {
                indicator.textContent = `Saved (${data.status}, Confidence ${data.confidenceLevel}/5, Reviews: ${data.reviewCount})`;
                indicator.classList.remove("d-none");
            }
        }
    };

    // Flashcard / Quiz Answer handler
    window.submitQuizChoice = async (questionId, btnEl, chosenText) => {
        const card = btnEl.closest(".quiz-card");
        if (!card || card.dataset.answered === "1") return;
        card.dataset.answered = "1";

        const resp = await fetch("/api/quiz/answer", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ questionId, selectedAnswer: chosenText })
        });
        if (!resp.ok) return;
        const result = await resp.json();

        card.querySelectorAll(".flashcard-option").forEach(b => {
            b.disabled = true;
            if (b.dataset.option === result.correctAnswer) {
                b.classList.add("correct");
            } else if (b === btnEl && !result.isCorrect) {
                b.classList.add("wrong");
            }
        });

        const fb = card.querySelector(".quiz-feedback");
        if (fb) {
            fb.classList.remove("d-none");
            fb.innerHTML = `
                <div class="p-2 mt-2 rounded border ${result.isCorrect ? "border-success text-success" : "border-danger text-danger"}">
                    <strong>${result.isCorrect ? "✓ Correct!" : "✗ Correct Answer: " + esc(result.correctAnswer)}</strong>
                    ${result.explanation ? `<div class="small mt-1" style="color:var(--text)">${esc(result.explanation)}</div>` : ""}
                    ${result.sourceCitation ? `<div class="small text-muted-hub mt-1">📌 Source: ${esc(result.sourceCitation)}</div>` : ""}
                </div>`;
        }
    };

    window.copyCodeBlock = (btnEl) => {
        const pre = btnEl.closest(".code-wrapper")?.querySelector("pre");
        if (!pre) return;
        navigator.clipboard?.writeText(pre.innerText).then(() => {
            const orig = btnEl.textContent;
            btnEl.textContent = "Copied!";
            setTimeout(() => btnEl.textContent = orig, 1400);
        });
    };
})();
