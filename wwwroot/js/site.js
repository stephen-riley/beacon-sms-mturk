$(function () {
    showPanel('#stage1');
});


function verifyPhoneNumber() {
    var num = $('#phone-num').val(),
        filter = getAllUrlParams().token || '';

    $.ajax({
        url: `/sms/process?num=${encodeURIComponent(num)}&token=${filter}`,
        success: function (data) {
            if (data.status == "ok") {
                var a2 = data.numberResponse.country_code,
                    country = data.numberResponse.country_name;

                $('#num-country-info').text(`${country} (${a2})`);

                startCountdown('#timer1', didntReceive);
                showPanel('#stage2');
            } else {
                $('#num-country-info').text('Invalid phone number; please try again with a properly formatted number.');
            }
        },
        failure: function () {
            alert("failure");
        }
    });
}


function submitCode() {
    var code = $('#code').val(),
        num = $('#phone-num').val();

    $.ajax({
        url: `/sms/submitcode?code=${code}&num=${encodeURIComponent(num)}`,
        success: function (data) {
            showPanel('#stage3-ok');
        },
        failure: function () {
            showPanel('#stage3-err');
        }
    });
}


function sentResponse() {
    startCountdown('#timer2', function () { showPanel('#stage4-failed'); });
    var intervalToken = setInterval(function () {
        if (checkForResponse()) {
            clearInterval(intervalToken);
            responsePresentEvent();
        }
    }, 3000);
    showPanel('#stage4');
}


function didntReceive() {
    showPanel('#stage2-failed');
}


function checkForResponse() {
    var code = $('#code').val();

    $.getJSON(`/sms/checkresponse?code=${code}`, null, function (data) {
        if (data.status) {
            responsePresentEvent();
        }
    });
}


function responsePresentEvent() {
    $('#confirmation-code').text($('#code').val());
    showPanel('#stage4-ok');
}


function startCountdown(id, onTimeout) {
    $(id).knob({
        min: 0,
        max: 180,
        readOnly: true,
        width: 100,
        height: 100,

        format: formatToTime,

        parse: parseTime,
    });

    var token = setInterval(function () {
        var rawVal = $(id).val(),
            val = parseTime(rawVal);

        if (val == 0) {
            if (onTimeout != null) {
                onTimeout();
            }
            clearInterval(token);
        } else {
            var newVal = formatToTime(val - 1);
            $(id).val(newVal).trigger('change');
        }
    }, 1000);
}


function parseTime(s) {
    if (("" + s).indexOf(":") > -1) {
        var pieces = s.split(":");
        return parseInt(pieces[0]) * 60 + parseInt(pieces[1]);
    } else {
        return parseInt(s);
    }
}


function formatToTime(val) {
    var min = Math.floor(val / 60),
        sec = val % 60;

    if (("" + sec).length < 2) {
        sec = `0${sec}`;
    }

    return `${min}:${sec}`;
}


function showPanel(panel) {
    $('.frame').hide();
    $(panel).show();
}


function getAllUrlParams(url) {

    // get query string from url (optional) or window
    var queryString = url ? url.split('?')[1] : window.location.search.slice(1);

    // we'll store the parameters here
    var obj = {};

    // if query string exists
    if (queryString) {

        // stuff after # is not part of query string, so get rid of it
        queryString = queryString.split('#')[0];

        // split our query string into its component parts
        var arr = queryString.split('&');

        for (var i = 0; i < arr.length; i++) {
            // separate the keys and the values
            var a = arr[i].split('=');

            // in case params look like: list[]=thing1&list[]=thing2
            var paramNum = undefined;
            var paramName = a[0].replace(/\[\d*\]/, function (v) {
                paramNum = v.slice(1, -1);
                return '';
            });

            // set parameter value (use 'true' if empty)
            var paramValue = typeof (a[1]) === 'undefined' ? true : a[1];

            // (optional) keep case consistent
            // paramName = paramName.toLowerCase();
            // paramValue = paramValue.toLowerCase();

            // if parameter name already exists
            if (obj[paramName]) {
                // convert value to array (if still string)
                if (typeof obj[paramName] === 'string') {
                    obj[paramName] = [obj[paramName]];
                }
                // if no array index number specified...
                if (typeof paramNum === 'undefined') {
                    // put the value on the end of the array
                    obj[paramName].push(paramValue);
                }
                // if array index number specified...
                else {
                    // put the value at that index number
                    obj[paramName][paramNum] = paramValue;
                }
            }
            // if param name doesn't exist yet, set it
            else {
                obj[paramName] = paramValue;
            }
        }
    }

    return obj;
}