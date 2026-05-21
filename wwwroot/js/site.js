// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Prevent the impatient double-click on "Buy": disable the submit button(s) of
// any [data-disable-on-submit] form once it has been submitted. Combined with
// the server-side Post/Redirect/Get, a single click yields a single order.
document.addEventListener("submit", function (event) {
    var form = event.target;
    if (!(form instanceof HTMLFormElement) || !form.hasAttribute("data-disable-on-submit")) {
        return;
    }
    if (form.checkValidity && !form.checkValidity()) {
        return; // let client-side validation surface its messages first
    }
    form.querySelectorAll("button[type=submit], input[type=submit]").forEach(function (btn) {
        btn.disabled = true;
        btn.dataset.originalText = btn.textContent;
        if (btn.tagName === "BUTTON") {
            btn.textContent = "Working…";
        }
    });
});
