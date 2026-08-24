const HOST_NAME = "com.devspacengrokfoot.launcher";
const PROJECT_PREFIX = "/g/g-p-6a709f3c5ef08191bc68fc40b7a05804-/project";
const recentByTab = new Map();

function shouldStart(rawUrl) {
  if (!rawUrl) return false;

  try {
    const url = new URL(rawUrl);
    if (url.protocol !== "https:" || url.hostname !== "chatgpt.com") return false;

    if (url.pathname.startsWith(PROJECT_PREFIX)) return true;
    return url.searchParams.get("temporary-chat") === "true";
  } catch (_) {
    return false;
  }
}

function ensureRunning(tabId, rawUrl) {
  if (!shouldStart(rawUrl)) return;

  const now = Date.now();
  const previous = recentByTab.get(tabId);
  if (previous && previous.url === rawUrl && now - previous.time < 5000) return;
  recentByTab.set(tabId, { url: rawUrl, time: now });

  chrome.runtime.sendNativeMessage(
    HOST_NAME,
    { action: "ensure_running", url: rawUrl },
    () => {
      if (chrome.runtime.lastError) {
        console.warn("DevSpace native host:", chrome.runtime.lastError.message);
      }
    }
  );
}

chrome.webNavigation.onCommitted.addListener((details) => {
  if (details.frameId === 0) ensureRunning(details.tabId, details.url);
});

chrome.webNavigation.onHistoryStateUpdated.addListener((details) => {
  if (details.frameId === 0) ensureRunning(details.tabId, details.url);
});

chrome.tabs.onUpdated.addListener((tabId, changeInfo, tab) => {
  if (changeInfo.url) ensureRunning(tabId, changeInfo.url);
  else if (changeInfo.status === "complete") ensureRunning(tabId, tab.url);
});

chrome.tabs.onRemoved.addListener((tabId) => {
  recentByTab.delete(tabId);
});

function scanExistingTabs() {
  chrome.tabs.query({ url: "https://chatgpt.com/*" }, (tabs) => {
    for (const tab of tabs) ensureRunning(tab.id, tab.url);
  });
}

chrome.runtime.onInstalled.addListener(scanExistingTabs);
chrome.runtime.onStartup.addListener(scanExistingTabs);
