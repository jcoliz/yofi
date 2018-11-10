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
            success: function (jsonresult) {
                var result = JSON.parse(jsonresult);

                if (result.Ok)
                {
                    target_cat.html(result.Payee.Category);
                    target_subcat.html(result.Payee.SubCategory);
                }
                else
                    alert(result.Exception);
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

    $('.actiondialog').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget) // Button that triggered the modal
        var modal = $(this);
        modal.data('trigger',button);
        var id = button.data('id')
        var endpoint = modal.data('endpoint')

        // Fill om the "More..." button
        var needsid = modal.find('.asp-route-id'); 
        var href = needsid.attr('href');
        var newhref = href + '/' + id;
        needsid.attr('href', newhref);

        var url = endpoint + id;
        $.ajax({
            url: url,
            success: function (htmlresult) {
                modal.find('.modal-body').html(htmlresult);
            },
            error: function (result) {
                alert(result.responseText);
                modal.find('.modal-body').text(result.responseText);
            }
            
        });
    })

    $("#editModal .btn-primary").on("click", function (event) {

        var data = $('#EditPartialForm').serialize();
        var trigger = $('#editModal').data('trigger');

        var url = "/api/tx/Edit/5";        
        $.post(url, data, function (jsonresult)
        {
            var result = JSON.parse(jsonresult);

            if (result.Ok) {
                var td = trigger.parent();
                var payee = td.siblings('.display-payee');
                var memo = td.siblings('.display-memo');
                var category = td.siblings(".display-category");
                var subcategory = td.siblings(".display-subcategory");
                payee.text(result.Transaction.Payee);
                memo.text(result.Transaction.Memo);
                category.text(result.Transaction.Category);
                subcategory.text(result.Transaction.SubCategory);
            }
            else
                alert(result.Exception);            
        });

    });

    $("#addPayeeModal .btn-primary").on("click", function (event) {

        var data = $('#CreatePartialForm').serialize();
        var trigger = $('#addPayeeModal').data('trigger');
        var id = trigger.data('id')

        var url = "/api/tx/AddPayee/";
        $.post(url, data, function (jsonresult) {
            var result = JSON.parse(jsonresult);
            if (result.Ok) {

                // Apply it also!
                var url = "/api/tx/ApplyPayee/" + id;
                $.ajax({
                    url: url,
                    success: function (jsonresult) {
                        var result = JSON.parse(jsonresult);

                        var td = trigger.parent();
                        var category = td.siblings(".display-category");
                        var subcategory = td.siblings(".display-subcategory");
                        category.text(result.Payee.Category);
                        subcategory.text(result.Payee.SubCategory);
                    }
                });
            }
            else
                alert(result.Exception);
        });
    });
});
