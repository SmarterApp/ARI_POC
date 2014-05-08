<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ScoreItem.aspx.cs" Inherits="ARI_POC.ScoreItem" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head>
    <title>Item <%= ItemName %></title>
    <link rel="stylesheet" type="text/css" media="all" href=" <%= VirtualPathUtility.ToAbsolute("~/res/all.css") %>"/>
</head>
<body>
    <!-- Navigation Menu -->
    <div id="menu-main" class="menu-bar-holder">
        <ul class="menu-bar">
            <li>
                <a href="<%= PathToItem %>">Review This Item</a>
            </li>
            <li>
                <a href="<%= VirtualPathUtility.ToAbsolute("~/SelectItem") %>">Select Another Item</a>
            </li>
            <li>
                <a href="<%= VirtualPathUtility.ToAbsolute("~/SelectA11y") %>">Change Accessibility</a>
            </li>
        </ul>
    </div>

    <h3>Item</h3>
    <div style="margin-left: 50px;"><%= ItemName %></div>
    <h3>Score</h3>
    <div style="margin-left: 50px;"><%= ScoreValue %></div>
    <% if (!string.IsNullOrEmpty(Message)) { %>
        <div style="border: 1px solid crimson; border-radius: 5px;">
            <%= Message %>
        </div>
    <% } %>
    <div style="border: 1px solid black; border-radius: 5px; padding: 8px; margin: 8px;"><%= ScoreOutput %></div>
</body>
</html>
