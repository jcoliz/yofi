$(document).ready(function () {

    $(".apply-link").on("click", function (event) {
        event.preventDefault();
        applyPayee($(this).parents('tr'));
    });

    $(".checkbox-post").on("click", function (event) {
        $.ajax({
            url: this.dataset.endpoint,
            data: { value: $(this).is(":checked") },
            beforeSend: xsrf,
            type: "POST"
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

        // Fill in the "More..." button
        var needsid = modal.find('.asp-route-id');
        var originalhref = needsid.attr('originalhref');
        if (originalhref == null) {
            originalhref = needsid.attr('href');
            needsid.attr('originalhref',originalhref);
        }

        var newhref = originalhref + '/' + id;
        needsid.attr('href', newhref);

        $.ajax({
            url: endpoint + id,
            success: function (htmlresult) {
                modal.find('.modal-body').html(htmlresult);

                $('.category-autocomplete').autoComplete({
                    resolverSettings: {
                        url: '/api/cat-ac'
                    }
                });
            },
            error: function (result) {
                alert(result.responseText);
                modal.find('.modal-body').text(result.responseText);
            }            
        });
    })

    $('.partialdialog').on('show.bs.modal', function (event) {
        var modal = $(this);
        var endpoint = modal.data('endpoint')

        $.ajax({
            url: endpoint,
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
            url: "/api/Edit/5",
            type: "POST",
            beforeSend: xsrf,
            data: $(this).serialize(),
            success: function (result) {
                if (result.Ok) {
                    tr.find('.display-payee').text(result.Item.Payee);
                    tr.find('.display-memo').text(result.Item.Memo);
                    tr.find(".display-category").text(result.Item.Category);
                }
                else
                    alert(result.Error);
            },
            error: function (request, status, error) {
                alert(status + ": " + error);
            }
        });
        $(this).parents('.modal').modal('hide');
    });

    $('#editPayeeModal form').submit( function (event)
    {
        event.preventDefault();
        var tr = $(this).data('tr');

        $.ajax({
            url: "/api/EditPayee/5",
            type: "POST",
            beforeSend: xsrf,
            data: $(this).serialize(),
            success: function (result) {
                if (result.Ok) {
                    tr.find('.display-payee').text(result.Item.Name);
                    tr.find(".display-category").text(result.Item.Category);
                }
                else
                    alert(result.Error);
            },
            error: function (request, status, error) {
                alert(status + ": " + error);
            }
        });
        $(this).parents('.modal').modal('hide');
    });

    $('#addPayeeModal form').submit(function (event)
    {
        event.preventDefault();
        var tr = $(this).data('tr');

        $.ajax({
            url: "/api/AddPayee/",
            type: "POST",
            beforeSend: xsrf,
            data: $(this).serialize(),
            success: function (result) {
                if (result.Ok)
                    applyPayee(tr);
                else
                    alert(result.Error);
            },
            error: function (request, status, error) {
                alert(status + ": " + error);
            }
        });
        $(this).parents('.modal').modal('hide');
    });

    $('#exportHowModal form').submit(function (event) {
        $(this).parents('.modal').modal('hide');
    });

    $('.txdrop').on('drop', function (event) {

        event.preventDefault();
        if (event.originalEvent.dataTransfer.items) {
            // Use DataTransferItemList interface to access the file(s)
            for (var i = 0; i < event.originalEvent.dataTransfer.items.length; i++) {
                // If dropped items aren't files, reject them
                if (event.originalEvent.dataTransfer.items[i].kind === 'file') {
                    var file = event.originalEvent.dataTransfer.items[i].getAsFile();

                    var tr = $(this);
                    var id = tr.data('id');

                    let formData = new FormData()
                    formData.append('file', file)
                    formData.append('id', id)

                    $.ajax({
                        url: "/api/UpReceipt/5",
                        type: "POST",
                        beforeSend: xsrf,
                        data: formData,
                        processData: false,
                        contentType: false,
                        error: function (result) {
                            alert(result.responseText);
                        },
                        success: function (result) {
                            if (result.Ok) {
                                tr.find('.display-receipt').children().show();
                                alert('Ok');
                            }
                            else
                                alert(result.Error);
                        }
                    });

                }
            }
        } else {
            // Use DataTransfer interface to access the file(s)
            for (var i = 0; i < ev.dataTransfer.files.length; i++) {
                alert('... file[' + i + '].name = ' + ev.dataTransfer.files[i].name);
            }
        }
    });

    $('.btnDismissModal').click(function () {
        $(this).parentsUntil('#modal').modal('hide');
    });

    $('.category-autocomplete').autoComplete({
        resolverSettings: {
            url: '/api/cat-ac'
        }
    });

    $('input:file').change(function (event) {
        $(this).siblings(':submit').prop("disabled", false);
    });

    // Enable all tooltips on page
    $('[data-bs-toggle=tooltip]').tooltip();

});

function applyPayee(tr)
{
    var id = tr.data('id');

    $.ajax({
        url: "/api/ApplyPayee/" + id,
        type: "POST",
        success: function (result) {
            if (result.Ok)
                tr.find(".display-category").text(result.Item.Category);
            else
                alert(result.Error);
        },
        error: function (request, status, error) {
            alert(status + ": " + error);
        }
    });
}

function xsrf(xhr) {
    element = document.getElementById('xsrf');
    if (element) {
        token = element.value;
        xhr.setRequestHeader("RequestVerificationToken", token);
    }
}