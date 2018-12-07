$(document).ready(function () {
    $(".apply-link").on("click", function (event) {
        event.preventDefault();
        applyPayee($(this).parents('tr'));
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
        var form = modal.find('form');
        var tr = button.parents('tr');
        form.data('tr', tr);
        var id = tr.data('id')
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

    $('#editModal form').submit( function (event)
    {
        event.preventDefault();
        var tr = $(this).data('tr');

        $.ajax({
            url: "/api/tx/Edit/5",
            type: "POST",
            data: $(this).serialize(),
            success: function (jsonresult) {
                var result = JSON.parse(jsonresult);

                if (result.Ok) {
                    tr.find('.display-payee').text(result.Transaction.Payee);
                    tr.find('.display-memo').text(result.Transaction.Memo);
                    tr.find(".display-category").text(result.Transaction.Category);
                    tr.find(".display-subcategory").text(result.Transaction.SubCategory);
                }
                else
                    alert(result.Exception.Message);
            }
        });
        $('#editModal').modal('hide')
    });

    $('#editPayeeModal form').submit( function (event)
    {
        event.preventDefault();
        var tr = $(this).data('tr');

        $.ajax({
            url: "/api/tx/EditPayee/5",
            type: "POST",
            data: $(this).serialize(),
            success: function (jsonresult) {
                var result = JSON.parse(jsonresult);

                if (result.Ok) {
                    tr.find('.display-payee').text(result.Payee.Name);
                    tr.find(".display-category").text(result.Payee.Category);
                    tr.find(".display-subcategory").text(result.Payee.SubCategory);
                }
                else
                    alert(result.Exception.Message);
            }
        });
        $('#editPayeeModal').modal('hide')
    });

    $('#addPayeeModal form').submit(function (event)
    {
        event.preventDefault();
        var tr = $(this).data('tr');

        $.ajax({
            url: "/api/tx/AddPayee/",
            type: "POST",
            data: $(this).serialize(),
            success: function (jsonresult) {
                var result = JSON.parse(jsonresult);
                if (result.Ok)
                    applyPayee(tr);
                else
                    alert(result.Exception.Message);
            }
        });
        $('#addPayeeModal').modal('hide')

    });
});

function applyPayee(tr)
{
    var id = tr.data('id');

    $.ajax({
        url: "/api/tx/ApplyPayee/" + id,
        success: function (jsonresult) {
            var result = JSON.parse(jsonresult);

            if (result.Ok) {
                tr.find(".display-category").text(result.Payee.Category);
                tr.find(".display-subcategory").text(result.Payee.SubCategory);
            }
            else
                alert(result.Exception.Message);
        }
    });
}
