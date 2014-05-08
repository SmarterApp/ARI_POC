<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SelectA11y.aspx.cs" Inherits="ARI_POC.SelectA11y" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head>
    <title>ARI - Select Accessibility</title>
    <link rel="stylesheet" type="text/css" media="all" href=" <%= VirtualPathUtility.ToAbsolute("~/res/all.css") %>"/>
    <style>
        .lbl {
            text-align: right;
        }
    </style>
<script type="text/javascript">

    var inboundA11y = '<%= AccessibilityJSON %>';

    function setA11y(a11y) {
        for (var key in a11y) {
            var ctrl = document.getElementById("a11y-" + key);
            if (ctrl) {
                if (ctrl.type == "checkbox") {
                    ctrl.checked = a11y[key];
                }
                else if (ctrl.multiple) {
                    var val = a11y[key];
                    var opt = ctrl.firstElementChild;
                    while (opt) {
                        opt.selected = val[opt.value] == true;
                        opt = opt.nextElementSibling;
                    }
                }
                else {
                    ctrl.value = a11y[key];
                }
            }
        }
    }

    function getA11y() {
        var a11y = new Object;
        var form = document.getElementById("a11yform");
        for (var key in form.elements) {
            var element = form.elements[key];
            if (key.indexOf("a11y-") == 0) {
                var val;
                if (element.multiple) {
                    val = new Object();
                    var opt = element.firstElementChild;
                    while (opt)
                    {
                        if (opt.selected)
                        {
                            if (opt.value != "false") {
                                val[opt.value] = true;
                            }
                        }
                        opt = opt.nextElementSibling;
                    }
                }
                else {
                    val = element.value;
                    if (val == "on") val = element.checked;
                    else if (val == "true") {
                        val = true;
                    }
                    else if (val == "false") {
                        val = false;
                    }
                }
                a11y[key.substr(5)] = val;
            }
        }
        return a11y;
    }

    function postA11y() {
        document.getElementById("a11y").value = JSON.stringify(getA11y());
        document.getElementById("submitform").submit();
    }

    function onPageLoad() {
        setA11y(JSON.parse(inboundA11y));
    }

    function onNavigate(dstUrl) {
        document.getElementById("post_a11y").value = JSON.stringify(getA11y());
        document.getElementById("post_nexturl").value = dstUrl;
        document.getElementById("post_form").submit();
    }

</script>
</head>
<body onload="onPageLoad()">
    <!-- Navigation Menu -->
    <div id="menu-main" class="menu-bar-holder">
        <ul class="menu-bar">
            <li>
                <a onclick="onNavigate('SelectItem');">Select Item</a>
            </li>
        </ul>
    </div>

    <% if (!string.IsNullOrEmpty(Message)) { %>
        <div style="border: 1px solid crimson; border-radius: 5px;">
            <%= Message %>
        </div>
    <% } %>
    <div style="border: 1px solid black; border-radius: 5px; padding: 8px; margin: 8px;">
        <div style="font-weight: bold; font-size: large;">Accessibility Settings</div>
        <form id="a11yform">
            <table>
                <tr>
                    <td class="lbl">Language:</td>
                    <td>
                        <select id="a11y-language">
                            <option value="en" selected="selected">English</option>
                            <option value="es">Spanish</option>
                            <option value="braille">Braille</option>
                        </select>
                    </td>
                </tr>
                <tr>
                    <td class="lbl">Masking:</td>
                    <td>
                        <select id="a11y-masking">
                            <option value="true" selected="selected">Masking Available</option>
                            <option value="false">Masking Not Available</option>
                        </select>
                    </td>
                </tr>
                <tr>
                    <td class="lbl">Strikethrough:</td>
                    <td>
                        <input type="checkbox" id="a11y-strikethrough" checked="checked" />
                    </td>
                </tr>
                <tr>
                    <td class="lbl">Color Choices:</td>
                    <td>
                        <select id="a11y-colorset">
                            <option value="blackOnWhite" selected="selected">Black on White</option>
                            <option value="blackOnRose">Black on Rose</option>
                            <option value="yellowOnBlue">Yellow on Blue</option>
                            <option value="mediumGrayOnLightGray">Medium Gray on Light Gray</option>
                            <option value="reverseContrast">Reverse Contrast</option>
                        </select>
                    </td>
                </tr>
                <tr>
                    <td class="lbl">Mark for Review:</td>
                    <td>
                        <input type="checkbox" id="a11y-markforreview" checked="checked" />
                    </td>
                </tr>
                <tr>
                    <td class="lbl">Highlight:</td>
                    <td>
                        <input type="checkbox" id="a11y-highlight" checked="checked" />
                    </td>
                </tr>
                <tr>
                    <td class="lbl">Text-to-Speech:</td>
                    <td>
                        <select id="a11y-texttospeech">
                            <option value="false" selected="selected">No Text-to-Speech</option>
                            <option value="true">Items</option>
                        </select>
                    </td>
                </tr>
                <tr>
                    <td class="lbl">American Sign Language:</td>
                    <td>
                        <select id="a11y-asl">
                            <option value="false" selected="selected">Off</option>
                            <option value="true">Show ASL Videos</option>
                        </select>
                    </td>
                </tr>
                <tr>
                    <td class="lbl">Word List:</td>
                    <td>
                        <select id="a11y-glossary" multiple="multiple">
                            <option value="false">No Glossary</option>
                            <option value="en" selected="selected">English Glossary</option>
                            <option value="es">Spanish Glossary</option>
                            <option value="fr">French Glossary</option>
                        </select>
                    </td>
                </tr>
                <tr>
                    <td class="lbl">Test Shell:</td>
                    <td>
                        <select id="a11y-streamline">
                            <option value="false" selected="selected">Standard Test Shell</option>
                            <option value="true">Streamlined Interface Available</option>
                        </select>
                    </td>
                </tr>
                <tr>
                    <td class="lbl">Expandable Passages:</td>
                    <td>
                        <select id="a11y-expandablepassages">
                            <option value="false">Expandable Passages Off</option>
                            <option value="true" selected="selected">Expandable Passages On</option>
                        </select>
                    </td>
                </tr>
            </table>
        </form>
    </div>
    <!-- Hidden form for submitting JSON -->
    <form id="post_form" style="display:inline;" action="SelectA11y.aspx" method="post" enctype="application/x-www-form-urlencoded">
        <input type="hidden" name="a11y" id="post_a11y" value="" />
        <input type="hidden" name="nexturl" id="post_nexturl" value="" />
    </form>
</body>
</html>
