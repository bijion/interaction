//
// common used lib
//

$(window).ready(function () {
    /*var voteContents = $(".votecontent");
    for (index in voteContents) {
        var voteContent = voteContents.eq(index);
     
    }*/
    ShareInit();

    AutomaticalIframeHeight();

    window.Market = $.trim($("#market").val());

    if (window.Market != "ja-jp")
    {
        PromoteWechatAccount();
    }
  
    window.LabelLang =
        {
            "VotedAlertText": window.Market == "ja-jp" ? "申し訳ありませんが、 投票は10分に1回しかできません。" : "您已投过票了，请十分钟之后再投，谢谢！"
        };
});

//
// Update vote answer
//
function UpdateVoteAnswerPKMode(optcount1, optcount2)
{
    optcount1 = (optcount1 == 0) ? 1 : optcount1;
    optcount2 = (optcount2 == 0) ? 1 : optcount2;
    var optcountsum = optcount1 + optcount2;
    var ratio1 = 1.0 * optcount1 / optcountsum;
    var ratio2 = 1.0 * optcount2 / optcountsum;
    var vsimapgesrc = (ratio1 >= ratio2 ? "/images/vote/vs_red.png" : "/images/vote/vs_blue.png");

    $("#vsimage").attr("src", vsimapgesrc);
    $("#optionprogressbar1").css({ "width" : ratio1 * 100 + "%" });
    $("#optionprogressbar2").css({ "width" : "auto" });
    $("#optionpercentage1").text(Math.round(ratio1 * 100) + "%");
    $("#optionpercentage2").text(Math.round(ratio2 * 100) + "%");

    if ($("#voteoption").val() >= 0)
    {
        $("#pk-votebutton1").attr("class", "btn-vote-pkmode-disable");
        $("#pk-votebutton2").attr("class", "btn-vote-pkmode-disable");
    }
}

//
// Share
//
function ShareInit()
{
    // Share data.
    var imageUrl = $("#shareImage").val();
    var shareUrl = $.trim($("#shareUrl").val());
    var topicTitle = $.trim($("#topicTitle").text().replace("\"", ""));
    var sitename = "必应互动";
    window.shareData =
        {
            "imageUrl": imageUrl,
            "shareUrl": shareUrl,
            "title": topicTitle,
            "summary": "",
            "sitename": sitename
        };

    // QR code for wechat
    var qrCodeUrl = "http://bingqrcode.cloudapp.net/?w=86&h=86&src=" + encodeURIComponent(shareUrl + "&form=wechat");
    $("#wechatQRCodeImg").attr("src", qrCodeUrl);
   
    // Set the share button
    if (!isMobile()) {
        $("#wechatIcon").mouseover(function (e) {
            ShowWechatQRCode(e.clientX, e.clientY);
        });
        $("#wechatIcon").mouseout(function (e) {
            HideWechatQRCode();
        });

        $("#wechatIconBar").mouseover(function (e) {
            ShowWechatQRCode(e.clientX, e.clientY);
        });
        $("#wechatIconBar").mouseout(function (e) {
            HideWechatQRCode();
        });
    } else {
        // Hide wechat and wechat timeline icon on non-Android mobile devices
        //if (!isAndroid()) {
        $("#wechatIcon").css("display", "none");
        $("#wechatIconBar").css("display", "none");
        //}
    }
}

function ShowWechatQRCode(eposX, eposY)
{
    var leftpos = eposX + "px";
    var toppos = (eposY - $("#wechatQRCodeDiv").outerHeight() - 20) + "px";
    $("#wechatQRCodeDiv").css({ left: leftpos, top: toppos, display: "block" });
}

function HideWechatQRCode()
{
    $("#wechatQRCodeDiv").css({ display: "none" });
}

function GenerateShareData()
{
    // Calculate same vote percentage.
    var voteOption = Number($("#voteoption").val());

    // Share message
    var shareMessage = "",
        shareVotePercentage = $.trim($("#optionpercentage" + voteOption).text()),
        shareOptionText = $.trim($("#optiontext" + voteOption).text());
    if (shareVotePercentage != "" && shareOptionText != "") {
        shareMessage = ("我和" + shareVotePercentage + "的网友都认为“" + shareOptionText + "”，").replace("\"", "");
    }
    shareMessage += "元芳，你怎么看？";

    window.shareData["summary"] = shareMessage;
}

function shareToSNS(sns)
{
    GenerateShareData();

    switch (sns)
    {
        case "wechat":
            if (isMobile()) {
                shareViaBingApp(false, showDownloadAppDiv);
            }

            // Log share action.
            $.get("/Vote/LogShare", { tid: $("#topicId").val(), source: "wechat", opt: $("#option").val() });

            break;

        case "wechatbar":
            if (isMobile()) {
                var top = ($(window).height() - $("#downloadAppDivBar").height()) / 2 + "px";
                var left = ($(window).width() - $("#downloadAppDivBar").width()) / 2 + "px";
                $("#downloadAppDivBar").css({ "top": top, "left": left });
                shareViaBingApp(false, showDownloadAppDivBar);
            }

            // Log share action.
            $.get("/Vote/LogShare", { tid: $("#topicId").val(), source: "wechat", opt: $("#option").val() });

            break;

        case "qqzone":
            var params = {
                "url": window.shareData.shareUrl + "&form=qqzone",
                "pics": window.shareData.imageUrl,
                "title": window.shareData.title,
                "summary": window.shareData.summary,
                "site": window.shareData.sitename
            };
            var paramStr = [];
            for (var i in params) {
                paramStr.push(i + '=' + encodeURIComponent(params[i] || ''));
            }
            window.open('http://sns.qzone.qq.com/cgi-bin/qzshare/cgi_qzshare_onekey?' + paramStr.join('&'), '_blank');

            // Log share action.
            $.get("/Vote/LogShare", { tid: $("#topicId").val(), source: "qqzone", opt: $("#option").val() });

            break;

        case "sinaweibo":
            var params = {
                "url": window.shareData.shareUrl + "&form=sinaweibo",
                "pic": window.shareData.imageUrl,
                "title": "#" + window.shareData.title + "# " + $("#sinaweibov").val() + " " + window.shareData.summary
            };
            var paramStr = [];
            for (var i in params) {
                paramStr.push(i + '=' + encodeURIComponent(params[i] || ''));
            }

            var url = 'http://service.weibo.com/share/share.php?language=zh_cn&searchPic=true&appkey=2505197526&ralateUid=5347281308&' + paramStr.join('&');
            window.open(url, '_blank');

            // Log share action.
            $.get("/Vote/LogShare", { tid: $("#topicId").val(), source: "sina weibo", opt: $("#option").val() });

            break;

        case "douban":
            var params = {
                "href": window.shareData.shareUrl + "&form=douban",
                "image": window.shareData.imageUrl,
                "name": window.shareData.title,
                "text": window.shareData.summary
            };
            var paramStr = [];
            for (var i in params) {
                paramStr.push(i + '=' + encodeURIComponent(params[i] || ''));
            }

            var url = 'http://www.douban.com/share/service?' + paramStr.join('&');
            window.open(url, '_blank');

            // Log share action.
            $.get("/Vote/LogShare", { tid: $("#topicId").val(), source: "douban", opt: $("#option").val() });

            break;

        case "renren":
            var params = {
                "resourceUrl": window.shareData.shareUrl + "&form=renren",
                "srcUrl": window.shareData.shareUrl,
                "pic": window.shareData.imageUrl,
                "title": window.shareData.title,
                "description": window.shareData.summary
            };
            var paramStr = [];
            for (var i in params) {
                paramStr.push(i + '=' + encodeURIComponent(params[i] || ''));
            }
            window.open('http://widget.renren.com/dialog/share?' + paramStr.join('&'), '_blank');

            // Log share action.
            $.get("/Vote/LogShare", { tid: $("#topicId").val(), source: "renren", opt: $("#option").val() });

            break;

        default :;
    }   
}

function hideDownloadAppDiv() {
    $("#downloadAppDiv").css("display", "none");
}

function hideDownloadAppDivBar() {
    $("#downloadAppDivBar").css("display", "none");
}

function showDownloadAppDiv() {
    $("#downloadAppDiv").css("display", "block");
}

function showDownloadAppDivBar() {
    $("#downloadAppDivBar").css("display", "block");
}

function isMobile()
{
    // Platform: win32, linux etc.
    var platform = navigator.platform.toLowerCase();
    // User Agent: ipad, iphone, android, windows mobile etc.
    var userAgent = navigator.userAgent.toLowerCase();
    //alert("platform: " + platform + ", user agent: " + userAgent);

    var isIpad = userAgent.match("ipad") == "ipad";
    var isIphone = userAgent.match("iphone os") == "iphone os";
    var isMidp = userAgent.match("midp") == "midp";
    var isUc7 = userAgent.match("rv:1.2.3.4") == "rv:1.2.3.4";
    var isUc = userAgent.match("ucweb") == "ucweb";
    var isAndroid = userAgent.match("android") == "android";
    var isCE = userAgent.match("windows ce") == "windows ce";
    var isWM = userAgent.match("windows mobile") == "windows mobile";
    var isWP = userAgent.match("windows phone") == "windows phone";

    if (isIphone || isMidp || isUc7 || isUc || isAndroid || isCE || isWM || isWP) {
        return true;
    } else {
        return false;
    }
}

function isAndroid()
{
    var userAgent = navigator.userAgent.toLowerCase();
    return userAgent.match("android") == "android";
}

// If Bing Android App already installed, share via it.
// If not, promote it.
function shareViaBingApp(isTimeline, showfunc)
{
    var params = {
        "link": window.shareData.shareUrl + "&form=bingapp",
        "image": window.shareData.imageUrl,
        "title": window.shareData.title,
        "desc": window.shareData.summary
    };
    var paramStr = "{";
    for (var i in params) {
        paramStr += "\"" + i + "\":\"" + params[i] + "\",";
    }
    if (isTimeline) {
        paramStr += "\"timeline\":true";
    } else {
        paramStr += "\"timeline\":false";
    }
    paramStr += "}";
    paramStr = encodeURIComponent(paramStr);
    var completeUrl = 'http://127.0.0.1:7106/wechat?callback=appInfoCallback&json=' + paramStr;
    var script = document.createElement('script');
    script.async = true;
    script.charset = "UTF-8";
    var done = false;
    script.onload = script.onreadystatechange = function () {
        if (!done && (!script.readyState || script.readyState === "loaded" || script.readyState === "complete")) {
            done = true;
            script.onload = script.onreadystatechange = null;
            if (script && script.parentNode) {
                script.parentNode.removeChild(script);
            }
        }
    };
    script.onerror = showfunc;
    var head = document.getElementsByTagName('head')[0];
    if (head) {
        head.appendChild(script);
    }
    script.src = completeUrl;
}

//
// Automatically adapt the iframe height
//
function AutomaticalIframeHeight() {

    var innerPageHeight = 0;
    var appIframeId = null;

    // Get iframe id
    var iframeIdMsg = location.href.split('&iframeId=');
    if (iframeIdMsg.length == 2) {
        appIframeId = iframeIdMsg[1].split('&')[0];
    }

    var dt = setInterval(function () {
        // Get iframe height
        var IE = !!window.attachEvent;
        var compatBody = document.compatMode == "BackCompat" ? document : document.documentElement;
        var elementheight = document.documentElement ? document.documentElement.offsetHeight : 0;
        var bodyheight = document.body ? document.body.offsetHeight : 0;
        var compatheight = compatBody ? compatBody.offsetHeight : 0;
        var currentPageHeight = IE ? bodyheight || compatheight : Math.max(elementheight, bodyheight, compatheight);

        // Send iframe height to Bing UX server
        if (currentPageHeight != innerPageHeight && currentPageHeight > 0) {
            innerPageHeight = currentPageHeight;

            if (innerPageHeight > 0 && null != appIframeId && null != parent) {
                var msg = [];
                msg.push(innerPageHeight);
                msg.push(appIframeId);
                var prefix = "AdjustIFrameHeight###";
                var content = prefix + msg.join("&");
                parent.postMessage(content, "*");
            }
        }
    }, 200);
};

//
// Vote submit
// 
function voteshoot(topicId, voteOption, optionCount)
{
    var requrl = '/Vote/Shoot?tid=' + topicId + '&opt=' + voteOption;
    var formcode = $("#formcode").val();

    // Track vote event by baidu tongji
    try
    {
        if ("undefined" != typeof _hmt && null != _hmt)
        {
            _hmt.push(['_trackEvent', formcode != "" ? formcode : "other", "VoteAction", location.href, voteOption]);
        }
    }catch(e){}

    $.ajax({
        type : 'POST',
        url : requrl,
        datatype : 'json',
        async : false,
        crossDomain : true,
        success : function (data) {
            if (data["CanVote"] == false) {
                var height = ($(window).outerHeight() * 0.8 - 120) / 2 + "px";
                $("#alertmodal .modal-body div").text(window.LabelLang["VotedAlertText"]);
                $("#alertmodal .modal-dialog").css({ "margin-top": height });
                $("#alertmodal").modal("show");
            } else if (data["CanVote"] == true) {
                // Record the voted option
                $("#voteoption").val(voteOption);

                // Update the vote answer
                $("#SumVoteCount").text(data["OptionVoteCountSum"]);
                if (2 == optionCount)
                {
                    UpdateVoteAnswerPKMode(data["OptionVoteCount1"], data["OptionVoteCount2"]);
                } else
                {
                    for (var opt = 1; opt <= optionCount; opt++) {
                        $("#optionpercentage" + opt).text(data["OptionPercentage" + opt]);
                        $("#optionprogressbar" + opt).css({ width: data["ProgressBarWidth" + opt] });
                        (voteOption == opt)
                        ? $("#votebutton" + opt).attr("class", "btn-vote-click")
                        : $("#votebutton" + opt).attr("class", "btn-vote-nonclick");
                    }
                }
               
                // Update the share message
                $("#sharevotepercentage").text($.trim($("#optionpercentage" + voteOption).text()));
                $("#shareoptiontext").text($.trim($("#optiontext" + voteOption).text()));

                // Show the share modal or directly go to the share page
                switch (formcode)
                {
                    case "sinaweibo" :
                        shareToSNS("sinaweibo");
                        break;
                    
                    case "qqzone" : 
                        shareToSNS("qqzone");
                        break;
                    
                    default:
                        var height = ($(window).outerHeight() * 0.8 - 120) / 2 + "px";
                        
                        $("#shareModal .modal-dialog").css({ "margin-top": height });
                        $("#shareModal").modal("show");
                }
            }
        }
    });
}

/*function addcomment(topicid, type)
{
    var requrl = '/Vote/AddComment';

    // Track vote event by baidu tongji
    try {
        if ("undefined" != typeof _hmt && null != _hmt) {
            _hmt.push(['_trackEvent', formcode != "" ? formcode : "other", "AddComment", location.href, topCommentId]);
        }
    } catch (e) { }

    var commentText = $("#cmttext").val();
    commentText = (commentText == "我来说两句" || commentText == "コメントを書く") ? "" : commentText;

    if (commentText != "") {
        $.ajax({
            type: 'POST',
            url: requrl,
            datatype: 'json',
            async: false,
            crossDomain: true,
            data: {
                tid: topicid,
                cmttext: commentText
            },
            success: function (data) {
                if (type == "detail") {
                    $("#commentlist").prepend(
                        "<li id=\"" + data.CommentId + "\" style=\"padding-top:15px;font-size:12px;\">\
                        <div style=\"padding:0;margin:0 0 10px 0;\"><div style=\"color:#777777;margin:0;padding:0;\">" + data.Creator + "</div></div>\
                        <div style=\"color:#777777;padding:0;margin:0 0 10px 2px;\">" + data.CommentTimeStr + "</div>\
                        <div style=\"padding:0;margin:0 0 10px 0;word-break:break-all;\">" + data.CommentText + "</div>\
                        <div style=\"height:1px; background:#E5E5E5;\"></div>\
                    </li>");
                } else if (type == "answer") {
                    var height = ($("#voteanswer").position().top + 30) + "px";
                    $("#alertmodal .modal-body div").html("<span style='padding-right:10px;'>"
                                                            + (window.Market == "ja-jp" ? "投票しました" : "成功提交评论") + "：\""
                                                            + commentText.substr(0, 50) + "...\"</span>"
                                                            + $("#commentlistlink").html());
                    $("#alertmodal .modal-dialog").css({ "margin-top": height });
                    $("#alertmodal").modal("show");
                }

                $("#cmttext").val("");
            }
        });
    } else {
        $("#cmttext").attr("placeholder", "")
    }
}*/

function getcomments(topicId)
{
    var requrl = '/Vote/GetComments?tid=' + topicId + '&tcid=' + $("#UpperCommentId").val();
    var formcode = $("#formcode").val();

    // Track vote event by baidu tongji
    try {
        if ("undefined" != typeof _hmt && null != _hmt) {
            _hmt.push(['_trackEvent', formcode != "" ? formcode : "other", "GetComments", location.href, topCommentId]);
        }
    } catch (e) { }

    $.ajax({
        type: 'POST',
        url: requrl,
        datatype: 'json',
        async: false,
        crossDomain: true,
        success: function (data) {
            var i = -1;
            for (i in data)
            {
                $("#commentlist").append(
                    "<li id=\"" + data[i].CommentId + "\" style=\"padding-top:15px;font-size:12px;\">\
                        <div style=\"padding:0;margin:0 0 10px 0;\"><div style=\"color:#777777;margin:0;padding:0;\">" + data[i].Creator + "</div></div>\
                        <div style=\"color:#777777;padding:0;margin:0 0 10px 2px;\">" + data[i].CommentTimeStr + "</div>\
                        <div style=\"padding:0;margin:0 0 10px 0;word-break:break-all;\">" + data[i].CommentText + "</div>\
                        <div style=\"height:1px; background:#E5E5E5;\"></div>\
                     </li>");
            }

            if (i >= 0) {
                $("#UpperCommentId").val(data[i].CommentId);
            } else{
                $("#SeeMoreComments").css({ "display": "none" });
            }
        }
    });  
}

function createtopic()
{
    // topic title
    var topicTitle = $.trim($("#topictitle").val());
    if (topicTitle.length < 5 || topicTitle > 20)
    {
        $("#topictitle").focus();
        alert("投票标题不符合规范：5-20个字符");
        return false;
    }

    // topic options
    for (var idx = 1; idx <= 5; idx++){
        var optionObj = $("#optiontext" + idx);
        var optionText = $.trim(optionObj.val());
        if ((idx == 1 || idx == 2 || (idx > 2 && optionText.length > 0)) &&
            (optionText.length < 2 || optionText > 15)) {
            optionObj.focus();
            alert("投票选项不符合规范：2-15个字符，并且至少填写两个选项");
            return false;
        }
    }

    // topic background
    var topicBkg = $.trim($("#topicbackground").val());
    if (topicBkg.length < 20 || topicBkg > 500) {
        $("#topicbackground").focus();
        alert("背景介绍不符合规范：20-500个字符");
        return false;
    }

    // topic news
    var urlReg = new RegExp("^http[s]?://.+", "i");
    for (var idx = 1; idx <= 4; idx++){
        var relatednewstitle = $.trim($("#relatednewstitle" + idx).val());
        var relatednewsurl = $.trim($("#relatednewsurl" + idx).val());
        if ((idx == 1 || idx == 2 || (idx > 2 && relatednewstitle.length > 0)) && 
            (relatednewstitle.length < 5 || relatednewstitle > 30 || !urlReg.test(relatednewsurl)))
        {
            $("#relatednewstitle" + idx).focus();
            alert("相关新闻的填写格式不符合规范");
            return false;
        }
    }

    // background picture
    if ($("#topicid").val().trim() == "" && $("#topicbackgroundpic").val().length == 0) {
        $("#topicbackgroundpic").focus();
        alert("请上传背景图片");
        return false;
    }

    // Email
    var emailReg = new RegExp("^[a-zA-Z0-9_.-]+@[a-zA-Z0-9_.-]+$", "i");
    if (!emailReg.test($.trim($("#author").val()))) {
        $("#author").focus();
        alert("邮箱格式不符合规范");
        return false;
    }

    $("#createtopic").submit();
}

//
// Promote the wechat public account
//
function PromoteWechatAccount()
{
    SetWechatPromotion();
    $(window).resize(SetWechatPromotion);
}

function SetWechatPromotion()
{
    if (isMobile())
        return;

    var leftpos = ($(window).width() - $("#wechatpublicaccount").outerWidth() - 5) + "px";
    var toppos = ($(window).height() - $("#wechatpublicaccount").outerHeight() - 5) + "px";
    $("#wechatpublicaccount").css({
        top: toppos, left: leftpos, display:"block"
    });
}
