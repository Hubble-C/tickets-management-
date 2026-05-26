document.addEventListener("submit", function (event) {
    var form = event.target;
    if (!(form instanceof HTMLFormElement) || !form.hasAttribute("data-disable-on-submit")) {
        return;
    }
    if (form.checkValidity && !form.checkValidity()) {
        return;
    }
    form.querySelectorAll("button[type=submit], input[type=submit]").forEach(function (btn) {
        btn.disabled = true;
        if (btn.tagName === "BUTTON") {
            btn.textContent = "Working…";
        }
    });
});
