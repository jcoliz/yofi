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
            type: "POST",
            error: function (request, status, error) {
                alert(status + ": " + error);
            }
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
                        url: '/ajax/tx/cat-ac'
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
        var id = tr.data('id');

        $.ajax({
            url: "/ajax/tx/edit/"+id,
            type: "POST",
            beforeSend: xsrf,
            data: $(this).serialize(),
            success: function (result) {
                tr.find('.display-payee').text(result.Payee);
                tr.find('.display-memo').text(result.Memo);
                tr.find(".display-category").text(result.Category);
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
        var id = tr.data('id');

        $.ajax({
            url: "/ajax/payee/edit/"+id,
            type: "POST",
            beforeSend: xsrf,
            data: $(this).serialize(),
            success: function (result) {
                tr.find('.display-payee').text(result.Name);
                tr.find(".display-category").text(result.Category);
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
            url: "/ajax/payee/add/",
            type: "POST",
            beforeSend: xsrf,
            data: $(this).serialize(),
            success: function (result) {
                applyPayee(tr);
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
                        url: "/ajax/tx/uprcpt/"+id,
                        type: "POST",
                        beforeSend: xsrf,
                        data: formData,
                        processData: false,
                        contentType: false,
                        error: function (jqxhr) {
                            alert(jqxhr.responseText);
                        },
                        success: function (result) {
                            tr.find('.display-receipt').children().show();
                            alert('Uploaded successfully.');
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

    $("a[data-help-topic]").click(function () {
        var topic = $(this).data("help-topic");
        var endpoint = "/Help?id=" + topic;
        var element = $("#helpModal");
        if (element.length) {
            var modal = new bootstrap.Modal(element);
            element.data("endpoint",endpoint);
            modal.show();
        }
    });

    $('.btnDismissModal').click(function () {
        $(this).parentsUntil('#modal').modal('hide');
    });

    $('.category-autocomplete').autoComplete({
        resolverSettings: {
            url: '/ajax/tx/cat-ac'
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
        url: "/ajax/tx/applypayee/" + id,
        type: "POST",
        success: function (result) {
            tr.find(".display-category").text(result);
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

$(window).on('load', function () {
    var element = $('.dialog-autoshow');
    if (element.length) {
        var endpoint = element.data('endpoint');
        var modal = new bootstrap.Modal(element);

        // I think I don't need to do this. I think just SHOWING it will cause the
        // load logic elsewhere to fire.
        $.ajax({
            url: endpoint,
            success: function (htmlresult) {
                element.find('.modal-body').html(htmlresult);
                modal.show();
            },
            error: function (result) {
                alert(result.responseText);
                element.find('.modal-body').text(result.responseText);
                modal.show();
            }
        });
    }
});
