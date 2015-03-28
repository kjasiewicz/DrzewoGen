$('#deathDatePicker input,#birthDatePicker input').datepicker({
    weekStart: 1
});

$('#birthDatePicker input').change(function() {
    if ($(this).val() == '') {
        $("#fatherInput,#motherInput").val('');
        $("#fatherInput,#motherInput").attr('disabled', true);
    } else {
        $("#fatherInput,#motherInput").attr('disabled', false);
        $.getJSON("/Home/ReturnFathersMothersNames", { date: $(this).val(), ajax: 'true' }, function (j) {
            var optionsFathers = '';
            var optionsMothers = '';
            for (var i = 0; i < j.fatherNames.length; i++) {
                optionsFathers += '<option value="' + j.fatherNames[i].Value + '">' + j.fatherNames[i].Text + '</option>';
            }
            $("select#fatherInput").html(optionsFathers);
            for (var k = 0; k < j.motherNames.length; k++) {
                optionsMothers += '<option value="' + j.motherNames[k].Value + '">' + j.motherNames[k].Text + '</option>';
            }
            $("select#motherInput").html(optionsMothers);
        });
    }
});
