const summary = document.getElementById("diagnostic-summary");
const providerList = document.getElementById("provider-diagnostics");

function addProvider(provider) {
    const card = document.createElement("article");
    card.className = "diagnostic-card";
    const top = document.createElement("div");
    top.className = "diagnostic-top";
    const name = document.createElement("span");
    name.className = "diagnostic-name";
    name.textContent = provider.name;
    const status = document.createElement("span");
    status.className = "status-pill status-" + provider.status;
    status.textContent = provider.status;
    top.append(name, status);
    const details = document.createElement("div");
    details.className = "diagnostic-details";
    const lastRun = provider.lastChecked ? new Date(provider.lastChecked).toLocaleString() : "Not run yet";
    const values = [
        "Successes: " + provider.successfulSearches,
        "Failures: " + provider.failedSearches,
        "Last results: " + provider.lastResultCount,
        "Last duration: " + Number(provider.lastDurationMilliseconds).toFixed(0) + " ms",
        "Last checked: " + lastRun
    ];
    if (provider.lastErrorType) values.push("Last error: " + provider.lastErrorType);
    for (const value of values) {
        const item = document.createElement("span");
        item.textContent = value;
        details.append(item);
    }
    card.append(top, details);
    providerList.append(card);
}

async function loadDiagnostics() {
    summary.textContent = "Loading diagnostics…";
    providerList.replaceChildren();
    try {
        const response = await fetch("/api/diagnostics");
        if (!response.ok) throw new Error("Could not load diagnostics.");
        const data = await response.json();
        summary.textContent = data.active_provider_count + " active providers · service " + data.status;
        for (const provider of data.providers) addProvider(provider);
    } catch (error) {
        summary.textContent = error.message;
    }
}

document.getElementById("refresh-diagnostics").addEventListener("click", loadDiagnostics);
loadDiagnostics();
