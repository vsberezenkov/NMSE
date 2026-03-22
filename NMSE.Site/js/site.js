/* --------------------------------------------------------------
   NMSE.Site - JavaScript
   • Animated star-field background (NMS spectral-class colours)
   • Auto-populates the download link from the latest GitHub
     Release so users get a direct .zip download without needing
     to navigate the GitHub Actions page (with CI fallback).
   • Configurable: update SITE_CONFIG
   -------------------------------------------------------------- */

// -- Portable configuration ------------------------------------ */
const SITE_CONFIG = {
  owner: "vectorcmdr",                                                         // GitHub user / org
  repo: "NMSE",                                                                // Repository name
  releaseTag: "latest",                                                        // LEGACY – no longer used (kept for reference)
  workflowFile: "build-nmse.yml",                                              // Workflow that produces the artifact (fallback)
  discordInvite: "https://discord.gg/WbDQKKP3us",                              // Discord invite link
  userdoc: "https://github.com/vectorcmdr/NMSE/blob/main/docs/user/README.md", // Path to user guide documentation in the repo
  devdoc: "https://github.com/vectorcmdr/NMSE/blob/main/docs/dev/README.md",   // Path to developer documentation in the repo
};

// -- Helpers ---------------------------------------------------- */
function timeAgo(dateString) {
  const seconds = Math.floor((Date.now() - new Date(dateString).getTime()) / 1000);
  const intervals = [
    { label: "year",   secs: 31536000 },
    { label: "month",  secs: 2592000 },
    { label: "day",    secs: 86400 },
    { label: "hour",   secs: 3600 },
    { label: "minute", secs: 60 },
  ];
  for (const { label, secs } of intervals) {
    const count = Math.floor(seconds / secs);
    if (count >= 1) return `${count} ${label}${count > 1 ? "s" : ""} ago`;
  }
  return "just now";
}

// -- Download link + build info --------------------------------- */
(function initDownloadLink() {
  const link = document.getElementById("download-link");
  const note = document.getElementById("download-note");
  const buildInfo = document.getElementById("build-info");
  const buildNumber = document.getElementById("build-number");
  const buildUpdated = document.getElementById("build-updated");

  // Fallback: send users to the Actions page if the release API fails
  const actionsUrl =
    `https://github.com/${SITE_CONFIG.owner}/${SITE_CONFIG.repo}/actions/workflows/${SITE_CONFIG.workflowFile}`;
  const fallbackNote = "Opens the build workflow page \u2013 download the artifact from there.";

  link.href = actionsUrl;
  link.textContent = "Download";

  // Fetch the latest GitHub Release (created by CI with a versioned tag e.g. v1.2.3)
  const apiUrl =
    `https://api.github.com/repos/${SITE_CONFIG.owner}/${SITE_CONFIG.repo}/releases/latest`;

  fetch(apiUrl)
    .then(res => {
      if (!res.ok) throw new Error(res.status);
      return res.json();
    })
    .then(release => {
      if (release.assets && release.assets.length > 0) {
        const asset = release.assets[0];
        link.href = asset.browser_download_url;
        link.textContent = "Download NMSE";

        // Show the version from the release name or tag (e.g. "NMSE v1.2.3" or "v1.2.3")
        buildNumber.textContent = release.name || release.tag_name;
        buildUpdated.textContent = `updated ${timeAgo(release.published_at)}`;
        buildInfo.hidden = false;
        note.textContent = "Direct download \u2013 no GitHub login required.";
      } else {
        note.textContent = "No builds available yet.";
      }
    })
    .catch(() => {
      // Release not found - fall back to Actions page
      link.href = actionsUrl;
      note.textContent = fallbackNote;
    });
})();

// -- Discord link ----------------------------------------------- */
(function initDiscordLink() {
  const link = document.getElementById("discord-link");
  link.href = SITE_CONFIG.discordInvite;
})();

// -- Developer links -------------------------------------------- */
(function initDevLinks() {
  const ghLink = document.getElementById("dev-github-link");
  const sponsorLink = document.getElementById("dev-sponsor-link");
  const repoLink = document.getElementById("dev-like-link");
  const issueLink = document.getElementById("dev-issue-link");
  const footerLink = document.getElementById("dev-github-link-footer");
  const docsUserLink = document.getElementById("docs-user-link");
  const docsDevLink = document.getElementById("docs-dev-link");

  ghLink.href = `https://github.com/${SITE_CONFIG.owner}`;
  ghLink.textContent = SITE_CONFIG.owner;

  sponsorLink.href = `https://github.com/sponsors/${SITE_CONFIG.owner}`;

  repoLink.href = `https://github.com/${SITE_CONFIG.owner}/${SITE_CONFIG.repo}`;

  issueLink.href = `https://github.com/${SITE_CONFIG.owner}/${SITE_CONFIG.repo}/issues`;

  footerLink.href = `https://github.com/${SITE_CONFIG.owner}`;
  footerLink.textContent = SITE_CONFIG.owner;

  docsUserLink.href = `${SITE_CONFIG.userdoc}`;
  docsDevLink.href = `${SITE_CONFIG.devdoc}`;
})();

// -- Star-field (NMS spectral-class colours) -------------------- */
(function initStarfield() {
  const canvas = document.getElementById("starfield");
  if (!canvas) return;
  const ctx = canvas.getContext("2d");

  // NMS stellar spectral-class colours
  const STAR_COLOURS = [
    [107, 136, 255],  // O - Blue
    [160, 196, 255],  // B - Blue-white
    [240, 240, 255],  // A - White
    [255, 248, 224],  // F - Yellow-white
    [255, 229, 102],  // G - Yellow
    [255, 170,  51],  // K - Orange
    [255, 102,  51],  // M - Red
    [ 51, 255, 153],  // E - Green (exotic)
  ];

  let width, height, stars;
  const STAR_COUNT = 260;
  const MAX_DEPTH = 600;
  const STAR_SPEED = 0.15;

  function resize() {
    width = canvas.width = window.innerWidth;
    height = canvas.height = window.innerHeight;
  }

  function createStars() {
    stars = Array.from({ length: STAR_COUNT }, () => ({
      x: Math.random() * width - width / 2,
      y: Math.random() * height - height / 2,
      z: Math.random() * MAX_DEPTH,
      colour: STAR_COLOURS[Math.floor(Math.random() * STAR_COLOURS.length)],
    }));
  }

  function draw() {
    ctx.clearRect(0, 0, width, height);

    for (const s of stars) {
      s.z -= STAR_SPEED;
      if (s.z <= 0) {
        s.x = Math.random() * width - width / 2;
        s.y = Math.random() * height - height / 2;
        s.z = MAX_DEPTH;
        s.colour = STAR_COLOURS[Math.floor(Math.random() * STAR_COLOURS.length)];
      }

      const scale = 128 / s.z;
      const sx = s.x * scale + width / 2;
      const sy = s.y * scale + height / 2;
      const r = Math.max(0, 1.2 - s.z / MAX_DEPTH) * 1.5;
      const alpha = Math.max(0, 1 - s.z / MAX_DEPTH);

      const [cr, cg, cb] = s.colour;
      ctx.beginPath();
      ctx.arc(sx, sy, r, 0, Math.PI * 2);
      ctx.shadowBlur = r * 8;
      ctx.shadowColor = `rgba(${cr},${cg},${cb},${(alpha * 2).toFixed(2)})`;
      ctx.fillStyle = `rgba(${cr},${cg},${cb},${(alpha * 1.6).toFixed(2)})`;
      ctx.fill();
    }

    ctx.shadowBlur = 0;
    ctx.shadowColor = "transparent";

    requestAnimationFrame(draw);
  }

  window.addEventListener("resize", () => {
    resize();
    createStars();
  });

  resize();
  createStars();
  draw();
})();
