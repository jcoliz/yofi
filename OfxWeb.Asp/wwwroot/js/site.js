// Write your JavaScript code.
$(document).ready(function () {
    $(".apply-link").on("click", function (event) {
        var id = this.dataset.id;
        var url = "/api/tx/ApplyPayee/" + id;
            $.ajax({
                url: url,
                success: function (result) {
                    var payee = JSON.parse(result);
                    alert(payee.Category);
                }
            });
    });
});
