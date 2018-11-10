﻿$(document).ready(function () {
    $(".apply-link").on("click", function (event) {
        event.preventDefault();
        applyPayee(this.dataset.id, $(this).parent());
    });

    $(".checkbox-hidden").on("click", function (event) {
        var endpoint = $(this).is(":checked") ? "Hide" : "Show";
        $.ajax({
            url: "/api/tx/" + endpoint + "/" + this.dataset.id
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

        $.ajax({
            url: endpoint + id,
            success: function (htmlresult) {
                modal.find('.modal-body').html(htmlresult);
            },
            error: function (result) {
                alert(result.responseText);
                modal.find('.modal-body').text(result.responseText);
            }
            
        });
    })

    $("#editModal .btn-primary").on("click", function (event)
    {
        var modal = $('#editModal');
        var form = modal.find('form');
        var trigger = modal.data('trigger');
        var target = trigger.parent();

        $.ajax({
            type: "POST",
            url: "/api/tx/Edit/5",
            data: form.serialize(),
            success: function (jsonresult) {
                var result = JSON.parse(jsonresult);

                if (result.Ok) {
                    target.siblings('.display-payee').text(result.Transaction.Payee);
                    target.siblings('.display-memo').text(result.Transaction.Memo);
                    target.siblings(".display-category").text(result.Transaction.Category);
                    target.siblings(".display-subcategory").text(result.Transaction.SubCategory);
                }
                else
                    alert(result.Exception.Message);
            }
        });
    });

    $("#addPayeeModal .btn-primary").on("click", function (event) {

        var modal = $('#addPayeeModal');
        var form = modal.find('form');
        var data = form.serialize();
        var trigger = modal.data('trigger');

        var url = "/api/tx/AddPayee/";
        $.post(url, data, function (jsonresult) {
            var result = JSON.parse(jsonresult);
            if (result.Ok)
                applyPayee(trigger.data('id'), trigger.parent());
            else
                alert(result.Exception.Message);
        });
    });
});

function applyPayee(id, target)
{
    $.ajax({
        url: "/api/tx/ApplyPayee/" + id,
        success: function (jsonresult) {
            var result = JSON.parse(jsonresult);

            if (result.Ok) {
                target.siblings(".display-category").text(result.Payee.Category);
                target.siblings(".display-subcategory").text(result.Payee.SubCategory);
            }
            else
                alert(result.Exception.Message);
        }
    });
}
