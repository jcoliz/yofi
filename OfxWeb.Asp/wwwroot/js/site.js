// Write your JavaScript code.
$(document).ready(function () {
    $(".apply-link").on("click", function (event) {
        event.preventDefault();
        var id = this.dataset.id;
        var target_cat = $(this).parent().siblings(".display-category");
        var target_subcat = $(this).parent().siblings(".display-subcategory");
        var url = "/api/tx/ApplyPayee/" + id;
        $.ajax({
            url: url,
            success: function (result) {
                var payee = JSON.parse(result);

                if ("Category" in payee)
                    target_cat.html(payee.Category);
                if ("SubCategory" in payee)
                    target_subcat.html(payee.SubCategory);
            }
        });
    });
    $(".checkbox-hidden").on("click", function (event) {
        var id = this.dataset.id;

        var endpoint = "Show";
        if ($(this).is(":checked"))
            endpoint = "Hide";

        var url = "/api/tx/" + endpoint + "/" + id;
        $.ajax({
            url: url,
        });
    });

    $('#editModal').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget) // Button that triggered the modal
        var id = button.data('id') // Extract info from data-* attributes
        // If necessary, you could initiate an AJAX request here (and then do the updating in a callback).
        // Update the modal's content. We'll use jQuery here, but you could use a data binding library or other methods instead.
        var modal = $(this);

        var url = "/api/tx/" + id;
        $.ajax({
            url: url,
            success: function (result) {
                var tx = JSON.parse(result);

                modal.find('.modal-body').text(tx.Payee);
            }
        });
    })
});
