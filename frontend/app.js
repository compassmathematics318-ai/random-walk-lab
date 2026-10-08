const els = {
  canvas: document.querySelector('#unity-canvas'),
  frame: document.querySelector('#unity-frame'),
  focusHint: document.querySelector('#sim-focus-hint'),
  loading: document.querySelector('#unity-loading'),
  loadingLabel: document.querySelector('#loading-label'),
  progress: document.querySelector('#progress-bar'),
  warning: document.querySelector('#unity-warning'),
  status: document.querySelector('#unity-status'),
  statusText: document.querySelector('#unity-status .status-text'),
  form: document.querySelector('#experiment-form'),
  run: document.querySelector('#run-button'),
  fullscreen: document.querySelector('#fullscreen-button'),
  defaults: document.querySelector('#reset-defaults'),
  agentCount: document.querySelector('#agent-count'),
  maxSteps: document.querySelector('#max-steps'),
  speed: document.querySelector('#speed-multiplier'),
  agentOutput: document.querySelector('#agent-count-output'),
  stepsOutput: document.querySelector('#max-steps-output'),
  speedOutput: document.querySelector('#speed-output'),
  message: document.querySelector('#form-message'),
  toast: document.querySelector('#toast'),
};

let unityInstance = null;
let toastTimer = null;
let serverConfig = {
  defaults: { agentCount: 100, maxMoves: 1000, speedMultiplier: 1 },
  limits: {
    agentCount: { min: 1, max: 2000 },
    maxMoves: { min: 1, max: 100000 },
    speedMultiplier: { min: 0.25, max: 5 },
  },
};

function setStatus(state, text) {
  els.status.dataset.state = state;
  els.statusText.textContent = text;
}

function showToast(message, type = 'info') {
  clearTimeout(toastTimer);
  els.toast.textContent = message;
  els.toast.className = `toast show${type === 'error' ? ' error' : ''}`;
  toastTimer = setTimeout(() => { els.toast.className = 'toast'; }, 3400);
}

function showUnityBanner(message, type) {
  const item = document.createElement('div');
  item.className = `warning-item${type === 'error' ? ' error' : ''}`;
  item.textContent = message;
  els.warning.appendChild(item);
  if (type !== 'error') setTimeout(() => item.remove(), 5000);
}

function formatInteger(value) {
  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) ? parsed.toLocaleString() : '—';
}

function syncOutputs() {
  els.agentOutput.textContent = formatInteger(els.agentCount.value);
  els.stepsOutput.textContent = formatInteger(els.maxSteps.value);
  els.speedOutput.textContent = `${Number(els.speed.value).toFixed(2)}×`;
}

function applyConfigToInputs(config) {
  els.agentCount.value = config.agentCount;
  els.maxSteps.value = config.maxMoves;
  els.speed.value = config.speedMultiplier;
  syncOutputs();
}

function clamp(value, min, max) {
  return Math.min(max, Math.max(min, value));
}

function readFormConfig() {
  const raw = {
    agentCount: Number.parseInt(els.agentCount.value, 10),
    maxMoves: Number.parseInt(els.maxSteps.value, 10),
    speedMultiplier: Number.parseFloat(els.speed.value),
  };

  if (!Number.isFinite(raw.agentCount) || !Number.isFinite(raw.maxMoves) || !Number.isFinite(raw.speedMultiplier)) {
    throw new Error('Please enter valid experiment values.');
  }

  const l = serverConfig.limits;
  const config = {
    agentCount: Math.round(clamp(raw.agentCount, l.agentCount.min, l.agentCount.max)),
    maxMoves: Math.round(clamp(raw.maxMoves, l.maxMoves.min, l.maxMoves.max)),
    speedMultiplier: clamp(raw.speedMultiplier, l.speedMultiplier.min, l.speedMultiplier.max),
  };

  applyConfigToInputs(config);
  return config;
}

async function loadServerConfig() {
  try {
    const response = await fetch('/api/config', { cache: 'no-store' });
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    serverConfig = await response.json();

    const l = serverConfig.limits;
    els.agentCount.min = l.agentCount.min;
    els.agentCount.max = l.agentCount.max;
    els.maxSteps.min = l.maxMoves.min;
    els.maxSteps.max = l.maxMoves.max;
    els.speed.min = l.speedMultiplier.min;
    els.speed.max = l.speedMultiplier.max;
    applyConfigToInputs(serverConfig.defaults);
  } catch (error) {
    console.warn('Using local defaults because /api/config was unavailable:', error);
  }
}

async function logExperiment(config) {
  try {
    await fetch('/api/experiments', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(config),
    });
  } catch (error) {
    console.debug('Experiment telemetry endpoint unavailable:', error);
  }
}

function sendExperimentConfig(config) {
  if (!unityInstance) throw new Error('The Unity simulation is still loading.');

  // IMPORTANT: This receiver is implemented by the patched AgentSpawner.cs
  // in /unity-source-patch/Scripts. Rebuild Unity Web after applying that patch.
  unityInstance.SendMessage('AgentSpawner', 'ApplyWebConfig', JSON.stringify(config));
}

async function runExperiment(event) {
  event.preventDefault();
  els.message.textContent = '';

  try {
    const config = readFormConfig();
    sendExperimentConfig(config);
    els.message.textContent = `Running ${config.agentCount.toLocaleString()} walkers with a ${config.maxMoves.toLocaleString()}-step limit at ${config.speedMultiplier.toFixed(2)}× speed.`;
    showToast('New experiment sent to Unity.');
    void logExperiment(config);
    els.canvas.focus();
  } catch (error) {
    els.message.textContent = error.message;
    showToast(error.message, 'error');
  }
}

function sizeCanvasForMobile() {
  // Let CSS size the canvas. Unity automatically matches the WebGL render target to the DOM canvas size.
  if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {
    document.documentElement.style.setProperty('--mobile-unity', '1');
  }
}

async function loadUnity() {
  const buildUrl = '/unity/Build';

  let manifest;
  try {
    const response = await fetch('/unity/build.json', { cache: 'no-store' });
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    manifest = await response.json();
  } catch (error) {
    console.error('Could not load Unity build manifest:', error);
    setStatus('error', 'Unity unavailable');
    els.loadingLabel.textContent = 'Could not read the Unity build manifest.';
    showToast('Could not read the Unity Web build manifest.', 'error');
    return;
  }

  const loaderUrl = `${buildUrl}/${manifest.loader}`;
  const config = {
    arguments: [],
    dataUrl: `${buildUrl}/${manifest.data}`,
    frameworkUrl: `${buildUrl}/${manifest.framework}`,
    codeUrl: `${buildUrl}/${manifest.code}`,
    streamingAssetsUrl: '/unity/StreamingAssets',
    companyName: manifest.companyName || 'DefaultCompany',
    productName: manifest.productName || '1D_prob_sim',
    productVersion: manifest.productVersion || '0.1.0',
    showBanner: showUnityBanner,
  };

  const script = document.createElement('script');
  script.src = loaderUrl;
  script.async = true;

  script.onload = () => {
    if (typeof createUnityInstance !== 'function') {
      setStatus('error', 'Unity unavailable');
      showToast('Unity loader did not initialize.', 'error');
      return;
    }

    createUnityInstance(els.canvas, config, (progress) => {
      const pct = Math.round(progress * 100);
      els.progress.style.width = `${pct}%`;
      els.loadingLabel.textContent = `Loading Unity runtime… ${pct}%`;
    }).then((instance) => {
      unityInstance = instance;
      els.loading.classList.add('hidden');
      els.run.disabled = false;
      els.fullscreen.disabled = false;
      setStatus('ready', 'Simulation ready');
      els.frame.classList.add('ready');
      els.canvas.addEventListener('click', () => {
        els.canvas.focus();
        els.frame.classList.add('engaged');
      });
    }).catch((error) => {
      console.error(error);
      setStatus('error', 'Unity failed');
      els.loadingLabel.textContent = 'The Unity build could not be loaded.';
      showToast(String(error), 'error');
    });
  };

  script.onerror = () => {
    setStatus('error', 'Unity failed');
    els.loadingLabel.textContent = 'Could not load the Unity loader script.';
    showToast('Could not load the Unity Web build.', 'error');
  };

  document.body.appendChild(script);
}

els.form.addEventListener('submit', runExperiment);
els.fullscreen.addEventListener('click', async () => {
  if (!unityInstance) return;

  try {
    // Fullscreen the entire website frame (not just the canvas) so the
    // on-screen control hints stay visible over the Unity simulation.
    if (!document.fullscreenElement) {
      await els.frame.requestFullscreen();
    } else {
      await document.exitFullscreen();
    }
  } catch (error) {
    console.warn('Frame fullscreen unavailable; falling back to Unity fullscreen.', error);
    unityInstance.SetFullscreen(1);
  }
});

document.addEventListener('fullscreenchange', () => {
  const frameIsFullscreen = document.fullscreenElement === els.frame;
  els.frame.classList.toggle('is-fullscreen', frameIsFullscreen);
  els.fullscreen.textContent = frameIsFullscreen ? 'Exit fullscreen' : 'Fullscreen';

  if (frameIsFullscreen) {
    els.canvas.focus();
    els.frame.classList.add('engaged');
  }
});
els.defaults.addEventListener('click', () => {
  applyConfigToInputs(serverConfig.defaults);
  els.message.textContent = 'Defaults restored. Click Run experiment to apply them.';
});
[els.agentCount, els.maxSteps, els.speed].forEach((el) => el.addEventListener('input', syncOutputs));

(async function init() {
  sizeCanvasForMobile();
  syncOutputs();
  await loadServerConfig();
  loadUnity();
})();
