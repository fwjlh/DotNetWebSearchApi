const form = document.getElementById("search-form");
const queryInput = document.getElementById("query");
const apiKeyInput = document.getElementById("api-key");
const providerList = document.getElementById("provider-list");
const searchButton = document.getElementById("search-button");
const feedback = document.getElementById("search-feedback");
const results = document.getElementById("results");

async function loadProviders() {
    try {
        const response = await fetch("/api/providers");
        if (!response.ok) throw new Error("Could not load available providers.");
        const data = await response.json();
        providerList.replaceChildren();
        for (const provider of data.providers) {
            const label = document.createElement("label");
            label.className = "provider-option";
            const checkbox = document.createElement("input");
            checkbox.type = "checkbox";
            checkbox.value = provider;
            checkbox.checked = true;
            label.append(checkbox, document.createTextNode(provider));
            providerList.append(label);
        }
        searchButton.disabled = data.providers.length === 0;
        if (data.providers.length === 0) feedback.textContent = "No search providers are configured.";
    } catch (error) {
        feedback.textContent = error.message;
        feedback.className = "feedback warning";
    }
}

function addResult(result) {
    let url;
    try { url = new URL(result.url); } catch { return; }
    if (url.protocol !== "http:" && url.protocol !== "https:") return;
    const card = document.createElement("article");
    card.className = "result-card";
    const heading = document.createElement("h2");
    const link = document.createElement("a");
    link.href = url.href;
    link.target = "_blank";
    link.rel = "noopener noreferrer";
    link.textContent = result.title;
    heading.append(link);
    const snippet = document.createElement("p");
    snippet.textContent = result.content || "No description supplied.";
    const meta = document.createElement("div");
    meta.className = "result-meta";
    meta.textContent = result.engine + " · score " + Number(result.score).toFixed(3);
    card.append(heading, snippet, meta);
    results.append(card);
}

function renderResults(data) {
    results.replaceChildren();
    feedback.className = "feedback";
    feedback.textContent = data.number_of_results + " results for “" + data.query + "”";
    if (data.unresponsive_engines && data.unresponsive_engines.length > 0) {
        feedback.className = "feedback warning";
        feedback.textContent += " · No response from: " + data.unresponsive_engines.join(", ");
    }
    for (const result of data.results) addResult(result);
    if (data.results.length === 0) {
        const empty = document.createElement("p");
        empty.className = "muted";
        empty.textContent = "No results were returned. Try another query or provider selection.";
        results.append(empty);
    }
}

form.addEventListener("submit", async event => {
    event.preventDefault();
    const query = queryInput.value.trim();
    const selected = Array.from(providerList.querySelectorAll("input:checked")).map(input => input.value);
    if (!query || selected.length === 0) {
        feedback.textContent = selected.length === 0 ? "Select at least one provider." : "Enter a search query.";
        feedback.className = "feedback warning";
        return;
    }
    searchButton.disabled = true;
    searchButton.textContent = "Searching…";
    feedback.className = "feedback";
    feedback.textContent = "Searching selected providers…";
    results.replaceChildren();
    try {
        const parameters = new URLSearchParams({ q: query, format: "json", engines: selected.join(",") });
        const headers = {};
        if (apiKeyInput.value.trim()) headers.Authorization = "Bearer " + apiKeyInput.value.trim();
        const response = await fetch("/search?" + parameters.toString(), { headers });
        const data = await response.json();
        if (!response.ok) throw new Error(data.error || "Search failed (" + response.status + ").");
        renderResults(data);
    } catch (error) {
        feedback.textContent = error.message;
        feedback.className = "feedback warning";
    } finally {
        searchButton.disabled = false;
        searchButton.textContent = "Search";
    }
});

document.getElementById("select-all").addEventListener("click", () => {
    providerList.querySelectorAll("input[type=checkbox]").forEach(input => { input.checked = true; });
});
document.getElementById("select-none").addEventListener("click", () => {
    providerList.querySelectorAll("input[type=checkbox]").forEach(input => { input.checked = false; });
});
loadProviders();
