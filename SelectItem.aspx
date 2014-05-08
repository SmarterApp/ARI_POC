<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SelectItem.aspx.cs" Inherits="ARI_POC.SelectItem" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Select Assessment Item</title>
    <link rel="stylesheet" type="text/css" media="all" href=" <%= VirtualPathUtility.ToAbsolute("~/res/all.css") %>"/>
</head>
<body>
    <!-- Navigation Menu -->
    <div id="menu-main" class="menu-bar-holder">
        <ul class="menu-bar">
            <li>
                <a href="SelectA11y");">Change Accessibility</a>
            </li>
        </ul>
    </div>

    <div style="border: 1px solid black; border-radius: 5px; padding: 8px; margin: 8px;">
        <div style="font-weight: bold;">Select Assesment Item</div>
        <ul>
        <% foreach(var item in fItems) { %>
        <li>
            <a href="<%= item.Href %>"><%= item.Title %></a>
        </li>
        <% } %>
        </ul>
    </div>
</body>
</html>
