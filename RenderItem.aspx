<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="RenderItem.aspx.cs" Inherits="ARI_POC.RenderItem" %>
<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head>
    <title>Item <%= ItemName %></title>
    <link rel="stylesheet" type="text/css" media="all" href=" <%= VirtualPathUtility.ToAbsolute("~/res/all.css") %>"/>
    <style type="text/css">
        <%= A11yColorCss %>
    </style>
    <script type="text/javascript">
        
        var ari_b = new Object();

        ari_b.a11y = <%= A11yJson %>;
        ari_b.response = <%= ItemResponseJson %>;

        toLocalPath = function(a) {return a;}

        ari_b.toLocalPath = function(packagePath)
        {
            // Combine paths 
            var result = "<%= PackageFoldername %>";
            var i = 0;
            var iEnd = packagePath.length;
            while (i < iEnd) {
                var scan = i;
                while (scan < iEnd && packagePath[scan] != '/')++scan;
                if (scan - i == 0) // leading slash
                {
                    result = "/";
                }
                else if (scan - i == 1 && packagePath[i] == '.') // Current folder
                {
                    // Do nothing
                }
                else if (scan - i == 2 && packagePath.substr(i, 2) == "..") {
                    var newEnd = result.lastIndexOf('/');
                    result = (newEnd > 0) ? result.substr(0, newEnd) : "/";
                }
                else {
                    if (result.length == 1) result = "";
                    result = result.concat("/", packagePath.substr(i, scan - i));
                }
                i = scan + 1;
            }
            var pathToPackage = "<%= PathToPackage %>";
            return pathToPackage.concat(result);
        };

        ari_b.doupdate = null; // Ready for a handler to be assigned

        function __ariOnNavigate(dstUrl)
        {
            if (ari_b.doupdate != null && typeof ari_b.doupdate == 'function')
            {
                ari_b.doupdate();
            }
            document.getElementById("__ariResponse").value = JSON.stringify(ari_b.response);
            document.getElementById("__ariNextUrl").value = dstUrl;
            document.getElementById("__ariNavForm").submit();
        }

    </script>
</head>
<body>

    <!-- Navigation Menu -->
    <div id="menu-main" class="menu-bar-holder">
        <ul class="menu-bar">
            <li>
                <a onclick="__ariOnNavigate('ScoreItem');">Score This Item</a>
            </li>
            <li>
                <a onclick="__ariOnNavigate('SelectItem');">Select Another Item</a>
            </li>
            <li>
                <a onclick="__ariOnNavigate('SelectA11y');">Change Accessibility</a>
            </li>
        </ul>
    </div>

    <h3>Item</h3>
    <div style="margin-left: 50px;"><%= ItemName %></div>
    <h3>Description</h3>
    <div style="margin-left: 50px;"><%= Server.HtmlEncode(ItemDescription) %></div>
    <% if (!string.IsNullOrEmpty(Message)) { %>
        <div style="border: 1px solid crimson; border-radius: 5px;">
            <%= Message %>
        </div>
    <% } %>
    <div class="ariA11y" style="border: 1px solid black; border-radius: 5px; padding: 8px; margin: 8px;"><% Render(); %></div>
    <form id="__ariNavForm" style="display:inline;" action="<%= VirtualPathUtility.ToAbsolute("~/RenderItem.aspx") %>" method="post" enctype="application/x-www-form-urlencoded">
        <input type="hidden" name="itemid", value="<%= ItemId %>" />
        <input type="hidden" name="response", id="__ariResponse" value="{empty}" />
        <input type="hidden" name="nexturl", id="__ariNextUrl" value="/" />
    </form>
</body>
</html>
